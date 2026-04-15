using UnityEngine;

public class UpgradePanelView : MonoBehaviour
{
    [SerializeField] private UpgradeButton[] m_upgradeButtons;
    private IBattleHUDViewModel m_viewModel;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (m_viewModel != null)
        {
            m_viewModel.OnShowUpgradePanel -= ShowPanel;
            m_viewModel.OnShowUpgradePanelWithOptions -= ShowPanelWithOptions;
            m_viewModel.OnHideUpgradePanel -= HidePanel;
        }
    }

    public void Initialize(IBattleHUDViewModel viewModel)
    {
        m_viewModel = viewModel;

        if (m_viewModel != null)
        {
            m_viewModel.OnShowUpgradePanel += ShowPanel;
            m_viewModel.OnShowUpgradePanelWithOptions += ShowPanelWithOptions;
            m_viewModel.OnHideUpgradePanel += HidePanel;
        }

        if (m_upgradeButtons != null)
        {
            for (int i = 0; i < m_upgradeButtons.Length; i++)
            {
                var upgradeBtn = m_upgradeButtons[i];
                if (upgradeBtn != null)
                {
                    upgradeBtn.OnSelect = m_viewModel.SelectUpgrade;
                    upgradeBtn.Initialize();
                }
            }
        }
    }

    private void ShowPanel()
    {
        gameObject.SetActive(true);
    }

    private void ShowPanelWithOptions(UpgradeOptionDTO[] options)
    {
        if (options == null || m_upgradeButtons == null)
        {
            return;
        }

        for (int i = 0; i < m_upgradeButtons.Length; i++)
        {
            if (i < options.Length && m_upgradeButtons[i] != null)
            {
                m_upgradeButtons[i].gameObject.SetActive(true);
                m_upgradeButtons[i].SetData(options[i]);
            }
            else if (m_upgradeButtons[i] != null)
            {
                m_upgradeButtons[i].gameObject.SetActive(false);
            }
        }

        ShowPanel();
    }

    private void HidePanel()
    {
        gameObject.SetActive(false);
    }
}
