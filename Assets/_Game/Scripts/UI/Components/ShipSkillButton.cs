using System;
using UnityEngine;
using UnityEngine.UI;

public class ShipSkillButton : MonoBehaviour
{
    #region 에디터 설정
    [Tooltip("함선 스킬의 고유 인덱스입니다.")]
    [SerializeField] private int m_skillIndex;

    private Button m_button;
    #endregion

 
    public int SkillIndex => m_skillIndex;

    public Button Button
    {
        get
        {
            if (m_button == null)
            {
                m_button = GetComponent<Button>();
            }
            return m_button;
        }
    }


}
