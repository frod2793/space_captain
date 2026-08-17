using System;
using UnityEngine;
using UnityEngine.UI;

public class ShipSkillButton : MonoBehaviour
{
    [SerializeField] private int m_skillIndex;

    private Button m_button;


 
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
