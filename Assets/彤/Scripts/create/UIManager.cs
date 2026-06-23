using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("常駐 UI (全程保持開啟)")]
    public GameObject btnFar;   // 拖入 Btn_far
    public GameObject btnClose; // 拖入 Btn_close

    [Header("UI 面板清單 (切換用)")]
    public GameObject skinPanel;        // 捏臉面板
    public GameObject colorPanel;       // 調色面板
    public GameObject equipmentPanel;   // 換裝面板
    public GameObject voicePanel;       // 語音面板
    public GameObject confirmationPopup; // 最後彈出的 Popup

    void Start()
    {
        // 1. 初始狀態：確保常駐按鈕是開啟的
        if (btnFar != null) btnFar.SetActive(true);
        if (btnClose != null) btnClose.SetActive(true);

        // 2. 隱藏 Popup 並顯示捏臉第一頁
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
        ShowSkinPanel();
    }

    // --- 導覽功能 ---

    public void ShowSkinPanel()
    {
        SetAllPanelsInactive();
        skinPanel.SetActive(true);
    }

    public void ShowColorPanel()
    {
        SetAllPanelsInactive();
        colorPanel.SetActive(true);
    }

    public void ShowEquipmentPanel()
    {
        SetAllPanelsInactive();
        equipmentPanel.SetActive(true);
    }

    public void ShowVoicePanel()
    {
        SetAllPanelsInactive();
        voicePanel.SetActive(true);
    }

    public void ShowConfirmationPopup()
    {
        // Popup 疊加在最上面，所以不關閉其他面板
        if (confirmationPopup != null) confirmationPopup.SetActive(true);
    }

    public void ClosePopup()
    {
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
    }

    /// <summary>
    /// 只負責關閉「可切換的面板」，不影響常駐按鈕
    /// </summary>
    private void SetAllPanelsInactive()
    {
        if (skinPanel != null) skinPanel.SetActive(false);
        if (colorPanel != null) colorPanel.SetActive(false);
        if (equipmentPanel != null) equipmentPanel.SetActive(false);
        if (voicePanel != null) voicePanel.SetActive(false);

        // 確保切換大面板時，Popup 先關閉（除非妳希望切換時 Popup 還在）
        if (confirmationPopup != null) confirmationPopup.SetActive(false);
    }

}