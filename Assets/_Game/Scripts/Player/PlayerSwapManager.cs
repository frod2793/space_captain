using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using SpaceCaptain.Player;
using SpaceCaptain.Player.Swap;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerSwapManager : MonoBehaviour
{
    [Header("위치 설정")]
    [SerializeField] private Transform m_activePosition;
    [SerializeField] private Transform[] m_standbyPositions;
    [SerializeField] private Transform[] m_reservePositions;

    [Header("판정 설정")]
    [SerializeField] private LayerMask m_characterLayer;

    [Header("애니메이션")]
    [SerializeField] private float m_swapDuration = 0.3f;
    [SerializeField] private AnimationCurve m_swapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool m_useCircularSwap;
    [SerializeField] private float m_circularSwapOffset = 1.0f;

    [Header("쿨다운 설정")]
    [SerializeField] private float m_swapCooldownDuration = 2.0f;
    [SerializeField] private float m_reserveSwapCooldownDuration = 10.0f;

    private List<PlayerCharacterController> m_characters = new List<PlayerCharacterController>();
    private PlayerCharacterController m_activeCharacter;
    private Camera m_mainCamera;
    private bool m_isInputLocked;
    private bool m_isDraggingActive;
    private bool m_isAnimating;
    private Vector2 m_lastScreenPos;

    private Dictionary<PlayerCharacterController, PlayerAttackComponent> m_attackComponentCache = new Dictionary<PlayerCharacterController, PlayerAttackComponent>();
    private Dictionary<PlayerCharacterController, float> m_individualCooldownEnds = new Dictionary<PlayerCharacterController, float>();
    private float m_swapCooldownEndTime;
    private int m_aliveCount;
    private HashSet<PlayerCharacterController> m_deadProcessedCharacters = new HashSet<PlayerCharacterController>();

    private readonly ISwapStrategy m_fieldSwap = new FieldSwapStrategy();
    private readonly ISwapStrategy m_circularSwap = new CircularSwapStrategy();
    private readonly ISwapStrategy m_reserveSwap = new ReserveSwapStrategy();
    private readonly ISwapStrategy m_deathSwap = new DeathSwapStrategy();

    private float m_regenTimer;
    private const float REGEN_INTERVAL = 10f;
    private const float REGEN_PERCENT = 0.05f;

    public event Action OnAllPlayersDead;
    public event Action OnCharactersInitialized;
    public event Action<PlayerCharacterController, PlayerCharacterController> OnSwapStarted;
    public event Action<PlayerCharacterController> OnSwapCompleted;
    public event Action<float> OnSwapCooldownChanged;

    public List<PlayerCharacterController> Characters => m_characters;
    public float SwapDuration => m_swapDuration;
    public float CooldownRatio => m_swapCooldownDuration > 0 ? Mathf.Clamp01((m_swapCooldownEndTime - Time.time) / m_swapCooldownDuration) : 0f;
    public float CurrentSwapCooldown => Mathf.Max(0, m_swapCooldownEndTime - Time.time);
    public PlayerCharacterController ActiveCharacter => m_activeCharacter;
    public bool IsAnimating => m_isAnimating;

    public Barrier Barrier { get; set; }
    public PlayerHpBar PlayerHUD { get; set; }

    private void Awake()
    {
        m_mainCamera = Camera.main;
    }

    private void Start()
    {
        InitializeCharacters();
        AlignCharactersToPositions();
        m_swapCooldownEndTime = Time.time + m_swapCooldownDuration;
    }

    private float m_lastCooldownRatio = -1f;

    private void Update()
    {
        HandleRegenTick();
        UpdateSkillCooldowns();
        UpdateCooldownRatio();

        if (!m_isInputLocked)
        {
            HandleInput();
        }
    }

    private void UpdateCooldownRatio()
    {
        float ratio = CooldownRatio;
        if (Mathf.Abs(m_lastCooldownRatio - ratio) > 0.002f)
        {
            m_lastCooldownRatio = ratio;
            OnSwapCooldownChanged?.Invoke(ratio);
        }
    }


    private void UpdateSkillCooldowns()
    {
        for (int i = 0; i < m_characters.Count; i++)
        {
            var character = m_characters[i];
            if (character.SwapState != CharacterSwapState.Dead && character.Skill != null)
            {
                character.Skill.UpdateCooldown(Time.deltaTime);
            }
        }
    }

    private void InitializeCharacters()
    {
        m_characters.Clear();
        var foundCharacters = FindObjectsByType<PlayerCharacterController>(FindObjectsSortMode.None);

        if (foundCharacters == null || foundCharacters.Length == 0)
        {
            return;
        }

        for (int i = 0; i < foundCharacters.Length; i++)
        {
            var character = foundCharacters[i];
            if (character != null)
            {
                m_characters.Add(character);
            }
        }

        if (m_characters.Count == 0)
        {
            return;
        }

        m_characters.Sort((a, b) => a.SwapState.CompareTo(b.SwapState));

        m_deadProcessedCharacters.Clear();
        m_aliveCount = 0;

        bool hasBarrier = (Barrier != null);
        for (int i = 0; i < m_characters.Count; i++)
        {
            var character = m_characters[i];
            m_aliveCount++;

            bool isActiveZero = (i == 0);
            var stats = new PlayerStatsDTO
            {
                ID = $"Player_{i}",
                IsActive = isActiveZero,
                MoveSpeed = 20f,
                CurrentX = character.transform.position.x
            };

            character.Initialize(stats);

            if (hasBarrier)
            {
                character.SetBarrier(Barrier);
            }

            if (character.TryGetComponent<PlayerAttackComponent>(out var attack))
            {
                m_attackComponentCache[character] = attack;
            }

            character.OnDead += HandlePlayerDead;

            if (isActiveZero)
            {
                m_activeCharacter = character;
                SubscribeHUD(character);
            }
        }

        OnCharactersInitialized?.Invoke();
    }

    private void AlignCharactersToPositions()
    {
        for (int i = 0; i < m_characters.Count; i++)
        {
            var character = m_characters[i];
            if (character == null)
            {
                continue;
            }

            if (character == m_activeCharacter)
            {
                character.SwapState = CharacterSwapState.Active;
                if (m_activePosition != null)
                {
                    character.transform.position = m_activePosition.position;
                    character.MoveToX(m_activePosition.position.x, true);
                    character.SetActive(true);
                }
                continue;
            }

            if (i >= 1 && i <= 2 && m_standbyPositions != null && (i - 1) < m_standbyPositions.Length)
            {
                character.SwapState = CharacterSwapState.Standby;
                character.transform.position = m_standbyPositions[i - 1].position;
                character.SetActive(false);
            }
            else if (i >= 3 && m_reservePositions != null && (i - 3) < m_reservePositions.Length)
            {
                character.SwapState = CharacterSwapState.Reserve;
                character.transform.position = m_reservePositions[i - 3].position;
                character.SetActive(false);
                character.gameObject.SetActive(false);
            }
        }
    }

    private void HandleInput()
    {
        var pointer = Pointer.current;
        if (pointer == null)
        {
            return;
        }

        Vector2 screenPos = pointer.position.ReadValue();

        if (pointer.press.wasPressedThisFrame)
        {
            if (!m_isAnimating)
            {
                m_lastScreenPos = screenPos;
                bool swapTriggered = TrySwapCharacter(screenPos);
                m_isDraggingActive = !swapTriggered;
            }
        }
        else if (pointer.press.isPressed && !m_isDraggingActive)
        {
            m_isDraggingActive = true;
            m_lastScreenPos = screenPos;
        }

        if (m_activeCharacter != null)
        {
            m_activeCharacter.IsDragging = m_isDraggingActive;
        }

        if (m_isDraggingActive && pointer.press.isPressed)
        {
            MoveActiveCharacter(screenPos);
        }

        if (pointer.press.wasReleasedThisFrame)
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
        if (m_mainCamera == null)
        {
            return false;
        }

        float dist = Mathf.Abs(m_mainCamera.transform.position.z);
        Vector3 worldPos3D = m_mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
        Vector2 worldPos = new Vector2(worldPos3D.x, worldPos3D.y);

        float currentCooldown = CurrentSwapCooldown;
        const float touchRadius = 0.5f;
        const float sqrTouchRadius = touchRadius * touchRadius;

        for (int i = 0; i < m_characters.Count; i++)
        {
            var character = m_characters[i];
            if (character == null || character == m_activeCharacter || !character.gameObject.activeSelf)
            {
                continue;
            }

            if (character.Collider != null)
            {
                Vector2 closestPoint = character.Collider.ClosestPoint(worldPos);
                if ((closestPoint - worldPos).sqrMagnitude <= sqrTouchRadius)
                {
                    if (currentCooldown > 0 || character.RemainingSwapCooldown > 0)
                    {
                        character.PlayCooldownFeedback();
                        return true;
                    }

                    SwitchToCharacter(character).Forget();
                    return true;
                }
            }
        }

        return false;
    }

    private void MoveActiveCharacter(Vector2 screenPos)
    {
        if (m_activeCharacter == null || m_mainCamera == null)
        {
            return;
        }

        Vector3 lastPoint = m_mainCamera.ScreenToWorldPoint(new Vector3(m_lastScreenPos.x, m_lastScreenPos.y, Mathf.Abs(m_mainCamera.transform.position.z)));
        Vector3 currPoint = m_mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(m_mainCamera.transform.position.z)));
        float deltaX = currPoint.x - lastPoint.x;

        float camDist = Mathf.Abs(m_mainCamera.transform.position.z);
        float camX = m_mainCamera.transform.position.x;
        float camHalfWidth = m_mainCamera.orthographic
            ? m_mainCamera.orthographicSize * m_mainCamera.aspect
            : camDist * Mathf.Tan(m_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * m_mainCamera.aspect;

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

        if (targetCharacter.IsDying || (m_activeCharacter != null && m_activeCharacter.IsDying))
        {
            return;
        }

        if (CurrentSwapCooldown > 0 || targetCharacter.RemainingSwapCooldown > 0)
        {
            targetCharacter.PlayCooldownFeedback();
            return;
        }

        bool isReserve = !targetCharacter.gameObject.activeSelf;
        ISwapStrategy strategy;

        if (isReserve)
        {
            strategy = m_reserveSwap;
        }
        else
        {
            strategy = m_useCircularSwap ? m_circularSwap : m_fieldSwap;
        }

        await ExecuteSwapAsync(strategy, targetCharacter, m_activeCharacter);
    }

    public async UniTask ExecuteCharacterActionAsync(PlayerCharacterController target)
    {
        if (target == null || m_isAnimating || Time.timeScale <= 0f)
        {
            return;
        }

        bool isAlreadyActive = (target == m_activeCharacter);
        var skill = target.Skill;
        bool isSkillReady = (skill != null) && skill.IsReady;
        bool isSwapCooldown = (CurrentSwapCooldown > 0);

        if (!isAlreadyActive)
        {
            if (isSwapCooldown || target.RemainingSwapCooldown > 0)
            {
                target.PlayCooldownFeedback();
                return;
            }

            await SwitchToCharacter(target);
        }

        if (isSkillReady && target == m_activeCharacter)
        {
            try
            {
                m_isInputLocked = true;
                m_isDraggingActive = false;
                if (m_activeCharacter != null)
                {
                    m_activeCharacter.IsDragging = false;
                }

                await skill.ExecuteAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                m_isInputLocked = false;
                if (m_activeCharacter != null)
                {
                    m_activeCharacter.IsDragging = false;
                }
            }
        }
    }

    private async UniTask ExecuteSwapAsync(ISwapStrategy strategy, PlayerCharacterController entering, PlayerCharacterController leaving, bool isDeathSwap = false)
    {
        m_isAnimating = true;

        try
        {
            var context = new SwapContextDTO
            {
                EnteringCharacter = entering,
                LeavingCharacter = leaving,
                ActivePosition = m_activePosition,
                SwapDuration = m_swapDuration,
                MainCamera = m_mainCamera,
                IsDraggingActive = (leaving != null) && leaving.IsDragging,
                CancellationToken = this.GetCancellationTokenOnDestroy(),
                LeavingOriginPos = (leaving != null) ? leaving.transform.position : Vector3.zero,
                EnteringOriginPos = (entering != null) ? entering.transform.position : Vector3.zero,
                SwapOffset = m_circularSwapOffset
            };

            if (leaving != null)
            {
                OnSwapStarted?.Invoke(entering, leaving);
                TransferTarget(leaving, entering);
                UnsubscribeHUD(leaving);
            }

            if (isDeathSwap)
            {
                if (entering == null || entering.Stats.CurrentHp <= 0)
                {
                    return;
                }

                m_activeCharacter = entering;
                entering.SwapState = CharacterSwapState.Active;
                entering.SetSwapCooldown(0f);
                SubscribeHUD(m_activeCharacter);
            }

            await strategy.PrepareAsync(context);

            if (!isDeathSwap)
            {
                entering.SwapState = CharacterSwapState.Active;
                if (leaving != null && leaving.Stats.CurrentHp > 0)
                {
                    leaving.SwapState = (strategy == m_reserveSwap) ? CharacterSwapState.Reserve : CharacterSwapState.Standby;
                }

                m_activeCharacter = entering;
                SubscribeHUD(m_activeCharacter);

                m_swapCooldownEndTime = Time.time + m_swapCooldownDuration;
                if (strategy == m_reserveSwap && leaving != null)
                {
                    leaving.SetSwapCooldown(m_reserveSwapCooldownDuration);
                }
            }
            else
            {
                m_swapCooldownEndTime = Time.time + m_swapCooldownDuration;
                if (leaving != null)
                {
                    leaving.SwapState = CharacterSwapState.Dead;
                }
            }

            await strategy.AnimateAsync(context);
            await strategy.FinalizeAsync(context);

            int enteringIdx = m_characters.IndexOf(entering);
            int leavingIdx = (leaving != null) ? m_characters.IndexOf(leaving) : -1;

            if (enteringIdx != -1 && leavingIdx != -1)
            {
                m_characters[enteringIdx] = leaving;
                m_characters[leavingIdx] = entering;
            }

            OnSwapCompleted?.Invoke(m_activeCharacter);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            m_isAnimating = false;
        }
    }

    private void TransferTarget(PlayerCharacterController from, PlayerCharacterController to)
    {
        if (from == null || to == null)
        {
            return;
        }

        if (m_attackComponentCache.TryGetValue(from, out var fromAttack) &&
            m_attackComponentCache.TryGetValue(to, out var toAttack))
        {
            toAttack.CurrentTarget = fromAttack.CurrentTarget;
        }
    }

    private void HandlePlayerDead(PlayerCharacterController deadPlayer)
    {
        HandlePlayerDeadAsync(deadPlayer).Forget();
    }

    private async UniTaskVoid HandlePlayerDeadAsync(PlayerCharacterController deadPlayer)
    {
        if (deadPlayer == null || m_deadProcessedCharacters.Contains(deadPlayer))
        {
            return;
        }

        await UniTask.Yield();

        if (m_isAnimating)
        {
            await UniTask.WaitUntil(() => !m_isAnimating);
        }

        m_deadProcessedCharacters.Add(deadPlayer);
        m_aliveCount = Mathf.Max(0, m_aliveCount - 1);
        deadPlayer.SwapState = CharacterSwapState.Dead;

        if (m_aliveCount == 0)
        {
            OnAllPlayersDead?.Invoke();
            return;
        }

        if (deadPlayer != m_activeCharacter)
        {
            return;
        }

        PlayerCharacterController candidate = null;
        int count = m_characters.Count;

        for (int i = count - 1; i >= 0; i--)
        {
            var character = m_characters[i];
            if (character != null && character.Stats.CurrentHp > 0 && character.SwapState == CharacterSwapState.Reserve)
            {
                candidate = character;
                break;
            }
        }

        if (candidate == null)
        {
            for (int i = count - 1; i >= 0; i--)
            {
                var character = m_characters[i];
                if (character != null && character.Stats.CurrentHp > 0 && character.SwapState == CharacterSwapState.Standby)
                {
                    candidate = character;
                    break;
                }
            }
        }

        if (candidate != null)
        {
            ISwapStrategy strategy;
            if (candidate.SwapState == CharacterSwapState.Reserve)
            {
                strategy = m_reserveSwap;
            }
            else
            {
                strategy = m_useCircularSwap ? m_circularSwap : m_fieldSwap;
            }

            ExecuteSwapAsync(strategy, candidate, deadPlayer, true).Forget();
        }
        else if (m_aliveCount <= 0)
        {
            OnAllPlayersDead?.Invoke();
        }
    }

    private void HandleRegenTick()
    {
        m_regenTimer += Time.deltaTime;
        if (m_regenTimer >= REGEN_INTERVAL)
        {
            m_regenTimer = 0f;
            RegenInactiveCharacters();
        }
    }

    private void RegenInactiveCharacters()
    {
        int count = m_characters.Count;
        for (int i = 3; i < count; i++)
        {
            var character = m_characters[i];
            int regenAmount = Mathf.CeilToInt(character.Stats.MaxHp * REGEN_PERCENT);
            character.Heal(regenAmount);
        }
    }

    private void SubscribeHUD(PlayerCharacterController character)
    {
        if (PlayerHUD != null && character != null)
        {
            character.OnHpChanged -= PlayerHUD.UpdateHP;
            character.OnHpChanged += PlayerHUD.UpdateHP;
            PlayerHUD.SetTarget(character.transform);

            float ratio = (character.Stats.MaxHp > 0) ? (float)character.Stats.CurrentHp / character.Stats.MaxHp : 0f;
            PlayerHUD.UpdateHP(ratio);
        }
    }

    private void UnsubscribeHUD(PlayerCharacterController character)
    {
        if (PlayerHUD != null && character != null)
        {
            character.OnHpChanged -= PlayerHUD.UpdateHP;
        }
    }
}

