using UnityEngine;
using System;
using DG.Tweening;

#region 데이터 모델 (DTO)
/// <summary>
/// [설명]: 모선의 현재 상태(체력, 생존 여부 등)를 담는 데이터 전송 객체입니다.
/// </summary>
[Serializable]
public class MasterShipDTO
{
    public int MaxHp = 1000;
    public int CurrentHp = 1000;
    public bool IsDestroyed = false;
}
#endregion

#region 모선 로직 (POCO)
/// <summary>
/// [설명]: 모선의 핵심 비즈니스 로직(피격, 파괴 판정 등)을 처리하는 순수 C# 클래스입니다.
/// </summary>
public class MasterShipLogic
{
    private MasterShipDTO m_data;

    public MasterShipLogic(MasterShipDTO data)
    {
        m_data = data;
    }

    /// <summary>
    /// [설명]: 외부로부터 데미지를 입었을 때 체력을 깎고 파괴 여부를 갱신합니다.
    /// </summary>
    /// <param name="damage">입힐 데미지 수치</param>
    /// <returns>변경된 현재 체력 비율 (0.0 ~ 1.0)</returns>
    public float OnDamaged(int damage)
    {
        if (m_data == null || m_data.IsDestroyed) return 0f;

        m_data.CurrentHp = Mathf.Max(0, m_data.CurrentHp - damage);

        if (m_data.CurrentHp <= 0)
        {
            m_data.IsDestroyed = true;
        }

        return (float)m_data.CurrentHp / m_data.MaxHp;
    }

    public bool CheckIsDestroyed() => m_data != null && m_data.IsDestroyed;
}
#endregion

#region 뷰 (View)
/// <summary>
/// [설명]: 게임 월드에서 보호 대상인 모선을 표현하고 이벤트를 수신하는 뷰 클래스입니다.
/// </summary>
public class MasterShip : MonoBehaviour
{
    #region 에디터 설정
    [Header("모선 설정")]
    [SerializeField] private MasterShipDTO m_shipData;

    [Header("연출 설정")]
    [Tooltip("피격 시 깜빡이는 효과를 줄 스프라이트 렌더러입니다.")]
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    #endregion

    #region 내부 필드
    private MasterShipLogic m_logic;
    #endregion

    #region 이벤트
    /// <summary>
    /// [설명]: 모선이 파괴되었을 때 발생하는 이벤트입니다. (예: 게임 오버 처리용)
    /// </summary>
    public event Action OnMasterShipDestroyed;

    /// <summary>
    /// [설명]: 모선의 체력이 변경되었을 때 발생하는 이벤트입니다. (0.0 ~ 1.0 비율 전달)
    /// </summary>
    public event Action<float> OnHpChanged;
    #endregion

    #region 유니티 생명주기
    private void Awake()
    {
        Initialize();
    }
    #endregion

    #region 초기화 로직
    /// <summary>
    /// [설명]: 데이터와 로직을 연결하고 초기 상태를 설정합니다.
    /// </summary>
    private void Initialize()
    {
        if (m_shipData == null)
        {
            m_shipData = new MasterShipDTO();
        }

        m_logic = new MasterShipLogic(m_shipData);
    }
    #endregion

    #region 공개 메서드
    /// <summary>
    /// [설명]: 적의 총알이나 충돌 등으로 인해 데미지를 입을 때 호출되는 메서드입니다.
    /// </summary>
    /// <param name="damageAmount">데미지 수치</param>
    public void TakeDamage(int damageAmount)
    {
        if (m_logic == null || m_logic.CheckIsDestroyed()) return;

        // 1. 로직 처리 (체력 감소 및 파괴 판정)
        float hpRatio = m_logic.OnDamaged(damageAmount);

        // 2. 이벤트 발생 (UI 갱신용)
        OnHpChanged?.Invoke(hpRatio);

        // 3. 피격 연출
        PlayDamageEffect();

        // 4. 파괴 시 처리
        if (m_logic.CheckIsDestroyed())
        {
            HandleDestruction();
        }
    }
    #endregion

    #region 내부 로직
    private void PlayDamageEffect()
    {
        if (m_spriteRenderer != null)
        {
            // DOTween을 사용한 붉은색 점멸 효과 (0.1초 동안 빨간색으로 변했다가 복구)
            m_spriteRenderer.DOKill(); // 기존 트윈 중단
            m_spriteRenderer.color = Color.white; // 초기화
            m_spriteRenderer.DOColor(Color.red, 0.05f).SetLoops(2, LoopType.Yoyo);
        }
        
#if UNITY_EDITOR
        Debug.Log($"[MasterShip] 피격됨! 현재 체력: {m_shipData.CurrentHp}");
#endif
    }

    private void HandleDestruction()
    {
        Debug.LogError("[MasterShip] 모선이 파괴되었습니다! 게임 오버!");
        OnMasterShipDestroyed?.Invoke();
        
        // 파괴 연출 후 비활성화 등
        gameObject.SetActive(false);
    }
    #endregion
}
#endregion
