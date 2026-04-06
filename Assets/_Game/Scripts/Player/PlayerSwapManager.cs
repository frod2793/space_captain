using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// [설명]: 활성 캐릭터의 드래그 조작 및 캐릭터 간 스왑을 관리하는 매니저 클래스입니다.
/// 화면 터치 시 정면 사격(Straight Fire), 정지 시 자동 타겟팅(Auto Targeting) 로직을 연계하여 처리합니다.
/// </summary>
public class PlayerSwapManager : MonoBehaviour
{
    [Header("캐릭터 리스트")]
    [Tooltip("게임에 참여하는 플레이어 캐릭터 슬롯입니다.")]
    [SerializeField] private List<PlayerCharacterController> m_characters;

    [Header("위치 설정")]
    [Tooltip("활성 캐릭터가 위치할 기준 지점(중앙)입니다.")]
    [SerializeField] private Transform m_activePosition;
    [Tooltip("대기 캐릭터들이 대기할 지점 리스트입니다.")]
    [SerializeField] private Transform[] m_standbyPositions;

    [Header("판정 설정")]
    [Tooltip("캐릭터 터치 인식을 위한 레이어 마스크입니다.")]
    [SerializeField] private LayerMask m_characterLayer; 
    [Tooltip("캐릭터 터치 인식을 위한 유효 반경입니다.")]
    [SerializeField] private float m_touchRadius = 1.5f;

    [Header("애니메이션")]
    [Tooltip("스왑 시 이동 속도에 영향을 주는 소요 시간입니다.")]
    [SerializeField] private float m_swapDuration = 0.3f;
    [Tooltip("이동 보간에 사용될 애니메이션 곡선입니다.")]
    [SerializeField] private AnimationCurve m_swapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("UI 연동")]
    [Tooltip("플레이어를 따라다니는 체력바 UI입니다.")]
    [SerializeField] private PlayerHpBar m_playerHUD;

    public event Action OnAllPlayersDead; 
    public List<PlayerCharacterController> Characters => m_characters;

    private PlayerCharacterController m_activeCharacter;
    private Camera m_mainCamera;
    
    private bool m_isDraggingActive; 
    private bool m_isAnimating; 
    private Vector2 m_lastScreenPos;

    private void Awake()
    {
        m_mainCamera = Camera.main;
        InitializeCharacters();
    }

    private void Start()
    {
        AlignCharactersToPositions();
    }

    private void Update()
    {
        if (m_isAnimating)
        {
            return;
        }

        HandleInput();

#if UNITY_EDITOR
        // [테스트]: T 키를 누르면 현재 활성 캐릭터에게 10 데미지를 입힙니다.
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            if (m_activeCharacter != null)
            {
                m_activeCharacter.TakeDamage(10);
            }
        }
#endif
    }

    /// <summary>
    /// [설명]: 등록된 캐릭터의 데이터를 초기화하고 기본 활성 캐릭터를 설정합니다.
    /// </summary>
    private void InitializeCharacters()
    {
        if (m_characters == null || m_characters.Count == 0)
        {
            return;
        }

        Barrier barrier = FindAnyObjectByType<Barrier>();

        for (int i = 0; i < m_characters.Count; i++)
        {
            bool isActiveZero = (i == 0);
            var stats = new PlayerStatsDTO 
            { 
                ID = $"Player_{i}", 
                IsActive = isActiveZero,
                MoveSpeed = 20f,
                CurrentX = m_characters[i].transform.position.x 
            };
            
            m_characters[i].Initialize(stats);
            m_characters[i].SetBarrier(barrier);
            m_characters[i].OnDead += HandlePlayerDead;

            if (isActiveZero)
            {
                m_activeCharacter = m_characters[i];
                
                
                if (m_playerHUD != null)
                {
                    m_playerHUD.SetTarget(m_activeCharacter.transform);
                    m_activeCharacter.OnHpChanged += m_playerHUD.UpdateHP;
                }
            }
        }
    }

    /// <summary>
    /// [설명]: 현재 활성 상태와 인덱스에 따라 캐릭터들을 지정된 앵커 위치로 즉시 이동시킵니다.
    /// </summary>
    private void AlignCharactersToPositions()
    {
        if (m_activeCharacter != null && m_activePosition != null)
        {
            m_activeCharacter.transform.position = m_activePosition.position;
            m_activeCharacter.MoveToX(m_activePosition.position.x, true);
        }
        
        int standbyIdx = 0;
        for (int i = 0; i < m_characters.Count; i++)
        {
            if (m_characters[i] == m_activeCharacter)
            {
                continue;
            }
            
            if (m_standbyPositions != null && standbyIdx < m_standbyPositions.Length)
            {
                if (m_standbyPositions[standbyIdx] != null)
                {
                    m_characters[i].transform.position = m_standbyPositions[standbyIdx].position;
                    m_characters[i].SetActive(false);
                }
                standbyIdx++;
            }
        }
    }

    /// <summary>
    /// [설명]: 신규 Input System을 바탕으로 사용자의 터치(또는 클릭) 상호작용을 처리합니다.
    /// </summary>
    private void HandleInput()
    {
        var pointer = Pointer.current;
        if (pointer == null)
        {
            return;
        }

        bool isPressed = pointer.press.isPressed;
        bool wasPressed = pointer.press.wasPressedThisFrame;
        bool wasReleased = pointer.press.wasReleasedThisFrame;
        Vector2 screenPos = pointer.position.ReadValue();

        // 1. 터치 시작 (Tap 또는 Drag Start)
        if (wasPressed)
        {
            m_lastScreenPos = screenPos;
            
            // 스왑 대상 캐릭터를 터치했는지 우선 판별
            bool swapTriggered = TrySwapCharacter(screenPos);
            
            // 규칙: 터치 중에는 정면 사격(IsDragging=true), 스왑 시에는 타겟팅 유지
            if (swapTriggered == false)
            {
                m_isDraggingActive = true;
                if (m_activeCharacter != null)
                {
                    m_activeCharacter.IsDragging = true;
                }
            }
            else
            {
                m_isDraggingActive = false;
                if (m_activeCharacter != null)
                {
                    m_activeCharacter.IsDragging = false;
                }
            }
        }

        // 2. 터치 유지 (Dragging)
        if (isPressed)
        {
            if (m_isDraggingActive)
            {
                if (m_activeCharacter != null)
                {
                    m_activeCharacter.IsDragging = true;
                }
                MoveActiveCharacter(screenPos);
            }
        }

        // 3. 터치 종료 (Release)
        if (wasReleased)
        {
            m_isDraggingActive = false;
            if (m_activeCharacter != null)
            {
                m_activeCharacter.IsDragging = false;
            }
        }

        m_lastScreenPos = screenPos;
    }

    /// <summary>
    /// [설명]: 터치된 위치가 다른 대기 캐릭터의 영역인지 확인하고, 그렇다면 스왑 애니메이션을 호출합니다.
    /// </summary>
    private bool TrySwapCharacter(Vector2 screenPos)
    {
        if (m_mainCamera == null)
        {
            return false;
        }

        float dist = Mathf.Abs(m_mainCamera.transform.position.z);
        Vector3 worldPos3D = m_mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
        Vector2 worldPos = new Vector2(worldPos3D.x, worldPos3D.y);

        foreach (var character in m_characters)
        {
            if (character == m_activeCharacter)
            {
                continue;
            }

            // [개선]: 고정 반경 대신 개별 캐릭터의 콜라이더 영역을 기준으로 클릭 판정
            if (character.Collider != null && character.Collider.OverlapPoint(worldPos))
            {
                SwitchToCharacter(character);
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// [설명]: 이전 프레임과의 화면 변위를 계산하여 활성 캐릭터를 조작감에 맞춰 수평 이동시킵니다.
    /// </summary>
    private void MoveActiveCharacter(Vector2 screenPos)
    {
        if (m_activeCharacter == null || m_mainCamera == null)
        {
            return;
        }

        // 월드 좌표 공간에서의 가로 변위 계산
        Vector3 lastPoint = m_mainCamera.ScreenToWorldPoint(new Vector3(m_lastScreenPos.x, m_lastScreenPos.y, Mathf.Abs(m_mainCamera.transform.position.z)));
        Vector3 currPoint = m_mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(m_mainCamera.transform.position.z)));
        float deltaX = currPoint.x - lastPoint.x;

        // 카메라 시야 범위를 기준으로 이동 가능한 임계 구역 계산
        float dist = Mathf.Abs(m_mainCamera.transform.position.z);
        float camX = m_mainCamera.transform.position.x;
        float camHalfWidth = m_mainCamera.orthographic 
            ? m_mainCamera.orthographicSize * m_mainCamera.aspect 
            : dist * Mathf.Tan(m_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * m_mainCamera.aspect;

        const float margin = 0.5f; 
        float minX = camX - camHalfWidth + margin;
        float maxX = camX + camHalfWidth - margin;

        float targetX = Mathf.Clamp(m_activeCharacter.transform.position.x + deltaX, minX, maxX);
        m_activeCharacter.MoveToX(targetX, true); // 1:1 정밀 조작 반영
    }
    
    private void SwitchToCharacter(PlayerCharacterController targetCharacter)
    {
        if (targetCharacter == null || targetCharacter == m_activeCharacter || m_isAnimating)
        {
            return;
        }
        
        SwapAnimationAsync(targetCharacter).Forget();
    }

    /// <summary>
    /// [설명]: 활성 캐릭터와 대상 캐릭터의 위치를 물리적으로 교환하며 상태 정보(타겟 등)를 인계합니다.
    /// </summary>
    private async UniTaskVoid SwapAnimationAsync(PlayerCharacterController targetCharacter)
    {
        try
        {
            m_isAnimating = true;
            m_isDraggingActive = false;

            PlayerCharacterController oldActive = m_activeCharacter;
            
            // 1. 타겟 정보 및 상태 초기화
            if (oldActive != null)
            {
                var oldAttack = oldActive.GetComponent<PlayerAttackComponent>();
                var newAttack = targetCharacter.GetComponent<PlayerAttackComponent>();
                
                if (oldAttack != null && newAttack != null)
                {
                    newAttack.CurrentTarget = oldAttack.CurrentTarget;
                }
                
                oldActive.IsDragging = false;
            }

            // 2. 관리 포인터 및 가시성 업데이트
            if (oldActive != null && m_playerHUD != null)
            {
                oldActive.OnHpChanged -= m_playerHUD.UpdateHP;
            }

            m_activeCharacter = targetCharacter;
            m_activeCharacter.SetActive(true);
            m_activeCharacter.IsDragging = false;

            if (m_playerHUD != null)
            {
                m_playerHUD.SetTarget(m_activeCharacter.transform);
                m_activeCharacter.OnHpChanged += m_playerHUD.UpdateHP;
                
                // 현재 체력 비율로 즉시 갱신
                float ratio = (float)m_activeCharacter.Stats.CurrentHp / m_activeCharacter.Stats.MaxHp;
                m_playerHUD.UpdateHP(ratio);
            }

            // 3. 교차 이동 보간 (Symmetry Move - DOTween 전환)
            Vector3 targetStart = targetCharacter.transform.position; // [복구]: 시작 위치 저장
            Vector3 targetEnd = m_activePosition != null ? m_activePosition.position : targetCharacter.transform.position;
            Vector3 oldEnd = targetStart; // 이전 활성 캐릭터는 대상의 시작 위치로

            var cts = this.GetCancellationTokenOnDestroy();
            Sequence swapSequence = DOTween.Sequence();

            if (oldActive != null)
            {
                swapSequence.Join(oldActive.transform.DOMove(oldEnd, m_swapDuration).SetEase(m_swapCurve));
            }
            
            swapSequence.Join(targetCharacter.transform.DOMove(targetEnd, m_swapDuration).SetEase(m_swapCurve));
            
            await swapSequence.Play().ToUniTask(cancellationToken: cts);

            // 4. 최종 배치 완료 및 대기 캐릭터 비활성화
            if (oldActive != null)
            {
                oldActive.SetActive(false);
                oldActive.IsDragging = false;
            }

            targetCharacter.SetActive(true);
            targetCharacter.MoveToX(targetEnd.x, true);
            
            // 5. 안전 장치: 애니메이션이 끝나면 조작 상태와 차단을 초기화합니다.
            m_isAnimating = false;
            m_isDraggingActive = false;
            
            if (m_activeCharacter != null)
            {
                m_activeCharacter.IsDragging = false;
            }
        }
        catch (OperationCanceledException)
        {
            // 취소 시에도 안전하게 플래그 초기화 (필요 시)
            m_isAnimating = false;
        }
    }
    /// <summary>
    /// [설명]: 캐릭터 사망 시 호출되어 자동 스왑 또는 게임 오버 처리를 수행합니다.
    /// </summary>
    private void HandlePlayerDead(PlayerCharacterController deadPlayer)
    {
        if (deadPlayer != m_activeCharacter)
        {
            Debug.LogWarning($"[대기 캐릭터 사망]: {deadPlayer.Stats.ID}");
            return;
        }

        PlayerCharacterController nextCharacter = m_characters.Find(c => c != deadPlayer && c.Stats.CurrentHp > 0);

        if (nextCharacter != null)
        {
            Debug.LogWarning($"[자동 스왑]: {deadPlayer.Stats.ID} 쥬금 {nextCharacter.Stats.ID}로 캐릭터 스왑");
            SwitchToCharacter(nextCharacter);
            return;
        }

        Debug.LogError("[Game Over]");
        OnAllPlayersDead?.Invoke();
    }
}
