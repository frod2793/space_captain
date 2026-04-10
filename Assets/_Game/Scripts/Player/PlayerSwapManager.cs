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
    [Tooltip("예비 인원 대기 지점")]
    [SerializeField] private Transform[] m_reservePositions;

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

    [Header("쿨다운 설정")]
    [Tooltip("스왑 재사용 대기 시간 (전역)")]
    [SerializeField] private float m_swapCooldownDuration = 2.0f;
    [Tooltip("예비요원 전환 시 부여되는 개별 대기 시간")]
    [SerializeField] private float m_reserveSwapCooldownDuration = 10.0f;

    private float m_currentSwapCooldown;

    public event Action OnAllPlayersDead;
    public event Action<PlayerCharacterController, PlayerCharacterController, bool> OnCharactersSwapped;
    public List<PlayerCharacterController> Characters => m_characters;
    public float CooldownRatio => m_swapCooldownDuration > 0 ? Mathf.Clamp01(m_currentSwapCooldown / m_swapCooldownDuration) : 0f;
    public float CurrentSwapCooldown => m_currentSwapCooldown;
    public PlayerCharacterController ActiveCharacter => m_activeCharacter;

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
        m_currentSwapCooldown = m_swapCooldownDuration;
    }

    private void Update()
    {
        if (m_currentSwapCooldown > 0)
        {
            m_currentSwapCooldown -= Time.deltaTime;
        }

        for (int i = 0; i < m_characters.Count; i++)
        {
            if (m_characters[i] != null)
            {
                m_characters[i].SetSwapCooldown(Mathf.Max(0, m_characters[i].RemainingSwapCooldown - Time.deltaTime));
            }
        }

        if (m_isAnimating)
        {
            return;
        }

        HandleInput();

#if UNITY_EDITOR
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
    /// [설명]: 등록된 모든 캐릭터(최대 5명)를 초기화하고 초기 배치를 수행합니다.
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
            if (m_characters[i] == null) continue;

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
        for (int i = 0; i < m_characters.Count; i++)
        {
            var character = m_characters[i];
            if (character == null || character.Stats.CurrentHp <= 0) continue;

            if (character == m_activeCharacter)
            {
                if (m_activePosition != null)
                {
                    character.transform.position = m_activePosition.position;
                    character.MoveToX(m_activePosition.position.x, true);
                    character.SetActive(true);
                }
                continue;
            }


            int standbyIdx = -1;
            if (i == 1 || i == 2) standbyIdx = i - 1;
            else if (m_characters.IndexOf(m_activeCharacter) > 2 && (i == 1 || i == 2)) standbyIdx = i - 1; // 특수 케이스 대응


            if (i >= 1 && i <= 2 && m_standbyPositions != null && (i - 1) < m_standbyPositions.Length)
            {
                character.transform.position = m_standbyPositions[i - 1].position;
                character.SetActive(false);
            }
            else if (i >= 3 && m_reservePositions != null && (i - 3) < m_reservePositions.Length)
            {
                character.transform.position = m_reservePositions[i - 3].position;
                character.SetActive(false);
                character.gameObject.SetActive(false);
            }
        }
    }

    private void HandleInput()
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        bool isPressed = pointer.press.isPressed;
        bool wasPressed = pointer.press.wasPressedThisFrame;
        bool wasReleased = pointer.press.wasReleasedThisFrame;
        Vector2 screenPos = pointer.position.ReadValue();

        if (wasPressed)
        {
            m_lastScreenPos = screenPos;
            bool swapTriggered = TrySwapCharacter(screenPos);
            m_isDraggingActive = !swapTriggered;

            if (m_activeCharacter != null)
            {
                m_activeCharacter.IsDragging = m_isDraggingActive;
            }
        }

        if (isPressed && m_isDraggingActive)
        {
            MoveActiveCharacter(screenPos);
        }

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

    private bool TrySwapCharacter(Vector2 screenPos)
    {
        if (m_mainCamera == null) return false;

        float dist = Mathf.Abs(m_mainCamera.transform.position.z);
        Vector3 worldPos3D = m_mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
        Vector2 worldPos = new Vector2(worldPos3D.x, worldPos3D.y);

        foreach (var character in m_characters)
        {
            if (character == m_activeCharacter || !character.gameObject.activeSelf)
            {
                continue;
            }

            if (character.Collider != null && character.Collider.OverlapPoint(worldPos))
            {
                if (m_currentSwapCooldown > 0 || character.RemainingSwapCooldown > 0)
                {
                    character.PlayCooldownFeedback();
                    return true;
                }

                SwitchToCharacter(character);
                return true;
            }
        }

        return false;
    }

    private void MoveActiveCharacter(Vector2 screenPos)
    {
        if (m_activeCharacter == null || m_mainCamera == null) return;

        Vector3 lastPoint = m_mainCamera.ScreenToWorldPoint(new Vector3(m_lastScreenPos.x, m_lastScreenPos.y, Mathf.Abs(m_mainCamera.transform.position.z)));
        Vector3 currPoint = m_mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(m_mainCamera.transform.position.z)));
        float deltaX = currPoint.x - lastPoint.x;

        float dist = Mathf.Abs(m_mainCamera.transform.position.z);
        float camX = m_mainCamera.transform.position.x;
        float camHalfWidth = m_mainCamera.orthographic
            ? m_mainCamera.orthographicSize * m_mainCamera.aspect
            : dist * Mathf.Tan(m_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * m_mainCamera.aspect;

        const float margin = 0.5f;
        float targetX = Mathf.Clamp(m_activeCharacter.transform.position.x + deltaX, camX - camHalfWidth + margin, camX + camHalfWidth - margin);
        m_activeCharacter.MoveToX(targetX, true);
    }

    public async UniTask SwitchToCharacter(PlayerCharacterController targetCharacter)
    {
        if (targetCharacter == null || targetCharacter == m_activeCharacter || m_isAnimating)
        {
            return;
        }

        bool isReserve = !targetCharacter.gameObject.activeSelf;
        if (isReserve)
        {
            targetCharacter.gameObject.SetActive(true);
        }

        await SwapAnimationAsync(targetCharacter, isReserve);
    }

    public async UniTask ExecuteCharacterActionAsync(PlayerCharacterController target)
    {
        if (target == null)
        {
            return;
        }

        bool isAlreadyActive = (target == m_activeCharacter);
        bool isSkillReady = target.Skill != null && target.Skill.IsReady;
        bool isSwapCooldown = m_currentSwapCooldown > 0;

        if (!isAlreadyActive && isSwapCooldown && !isSkillReady)
        {
            target.PlayCooldownFeedback();
            return;
        }

        if (!isAlreadyActive)
        {
            if (isSwapCooldown || target.RemainingSwapCooldown > 0)
            {
                target.PlayCooldownFeedback();
                return;
            }

            await SwitchToCharacter(target);
        }

        // 2. 스킬 실행 (규칙 1: 아웃라인 이펙트(준비 완료) 시 발동)
        if (isSkillReady && target == m_activeCharacter)
        {
            await target.Skill.ExecuteAsync();
        }
        // 규칙 2: 스킬 쿨타임인 경우 여기서 스킬 실행 없이 종료 (스왑만 완료된 상태)
    }

    private async UniTask SwapAnimationAsync(PlayerCharacterController targetCharacter, bool isReserveSwap)
    {
        try
        {
            m_isAnimating = true;
            m_isDraggingActive = false;

            PlayerCharacterController oldActive = m_activeCharacter;

            if (oldActive != null)
            {
                var oldAttack = oldActive.GetComponent<PlayerAttackComponent>();
                var newAttack = targetCharacter.GetComponent<PlayerAttackComponent>();
                if (oldAttack != null && newAttack != null)
                {
                    newAttack.CurrentTarget = oldAttack.CurrentTarget;
                }
                oldActive.IsDragging = false;
                if (m_playerHUD != null) oldActive.OnHpChanged -= m_playerHUD.UpdateHP;

                if (isReserveSwap)
                {
                    oldActive.SetSwapCooldown(m_reserveSwapCooldownDuration);
                }
            }

            m_activeCharacter = targetCharacter;
            m_activeCharacter.SetActive(true);

            if (m_playerHUD != null)
            {
                m_playerHUD.SetTarget(m_activeCharacter.transform);
                m_activeCharacter.OnHpChanged += m_playerHUD.UpdateHP;
                m_playerHUD.UpdateHP((float)m_activeCharacter.Stats.CurrentHp / m_activeCharacter.Stats.MaxHp);
            }

            OnCharactersSwapped?.Invoke(oldActive, targetCharacter, isReserveSwap);

            Vector3 targetEnd = m_activePosition != null ? m_activePosition.position : targetCharacter.transform.position;
            Vector3 oldEnd = targetCharacter.transform.position;

            var cts = this.GetCancellationTokenOnDestroy();
            Sequence swapSequence = DOTween.Sequence();

            if (oldActive != null)
            {
                swapSequence.Join(oldActive.transform.DOMove(oldEnd, m_swapDuration).SetEase(m_swapCurve));
            }
            swapSequence.Join(targetCharacter.transform.DOMove(targetEnd, m_swapDuration).SetEase(m_swapCurve));

            await swapSequence.Play().ToUniTask(cancellationToken: cts);

            if (oldActive != null)
            {
                oldActive.SetActive(false);
                bool isReservePos = false;
                if (m_reservePositions != null)
                {
                    foreach (var p in m_reservePositions)
                    {
                        if (Vector3.Distance(oldActive.transform.position, p.position) < 0.1f)
                        {
                            isReservePos = true;
                            break;
                        }
                    }
                }
                if (isReservePos) oldActive.gameObject.SetActive(false);
            }

            targetCharacter.MoveToX(targetEnd.x, true);
            m_isAnimating = false;
            m_currentSwapCooldown = m_swapCooldownDuration;
        }
        catch (OperationCanceledException)
        {
            m_isAnimating = false;
        }
    }

    /// <summary>
    /// [설명]: 캐릭터 사망 시 호출됩니다. 필드 요원 사망 시 예비 인원을 즉시 충원합니다.
    /// </summary>
    private void HandlePlayerDead(PlayerCharacterController deadPlayer)
    {
        Debug.LogWarning($"[PLAYER DEAD]: {deadPlayer.Stats.ID}");

        PlayerCharacterController reserveCandidate = m_characters.Find(c =>
            c != null &&
            c.Stats.CurrentHp > 0 &&
            !c.gameObject.activeSelf);

        if (reserveCandidate != null)
        {
            Vector3 spawnPos = deadPlayer.transform.position;
            reserveCandidate.gameObject.SetActive(true);
            reserveCandidate.transform.position = spawnPos;

            Debug.LogWarning($"[RESERVE DEPLOYED]: {reserveCandidate.Stats.ID}가 {deadPlayer.Stats.ID}의 자리를 대체합니다.");

            if (deadPlayer == m_activeCharacter)
            {
                m_activeCharacter = reserveCandidate;
                if (m_playerHUD != null)
                {
                    m_playerHUD.SetTarget(m_activeCharacter.transform);
                    m_activeCharacter.OnHpChanged += m_playerHUD.UpdateHP;
                    m_playerHUD.UpdateHP(1f);
                }
            }

            m_currentSwapCooldown = 0f;
        }
        else
        {
            if (deadPlayer == m_activeCharacter)
            {
                PlayerCharacterController nextField = m_characters.Find(c => c != deadPlayer && c.Stats.CurrentHp > 0 && c.gameObject.activeSelf);
                if (nextField != null)
                {
                    SwitchToCharacter(nextField);
                    return;
                }
            }
        }

        // 모든 캐릭터 사망 체크
        if (!m_characters.Exists(c => c.Stats.CurrentHp > 0))
        {
            Debug.LogError("[Game Over]: ALL PLAYERS DEAD");
            OnAllPlayersDead?.Invoke();
        }
    }
}

