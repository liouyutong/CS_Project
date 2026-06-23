using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class CharacterSetupUI : MonoBehaviour
{
    public TMP_InputField characterNameInputField;
    public TMP_Dropdown genderDropdown;
    public TMP_InputField personalityInputField;
    public Slider ageSlider;
    public TMP_Text ageText;

    public TMP_Text warningText;

    private void Start()
    {
        ageSlider.onValueChanged.AddListener(UpdateAgeText);
        UpdateAgeText(ageSlider.value);
        warningText.gameObject.SetActive(false);
    }

    void UpdateAgeText(float value)
    {
        ageText.text = "年齡: " + ((int)value).ToString();
    }

    public void OnClickConfirm()
    {
        // ====== 1. 防呆檢查 ======
        if (string.IsNullOrWhiteSpace(characterNameInputField.text) ||
            string.IsNullOrWhiteSpace(personalityInputField.text))
        {
            warningText.text = "設定未輸入完成";
            warningText.gameObject.SetActive(true);
            return;
        }

        if (AccountManager.Instance == null || AccountManager.Instance.CurrentAccount == null)
        {
            Debug.LogError("Account not found!");
            return;
        }

        // ====== 2. 先執行數據重置 (重要：順序提前) ======
        // 必須先清空舊的資料，再寫入這一次新的資料
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ResetData();
        }

        if (CharacterDataManager.Instance != null)
        {
            CharacterDataManager.Instance.ResetAllData();
        }

        // ====== 3. 儲存本次數據 (在 Reset 之後) ======
        warningText.gameObject.SetActive(false);
        string genderStr = genderDropdown.options[genderDropdown.value].text;

        // 存入 Account (用於永久存檔)
        AccountManager.Instance.CurrentAccount.characterName = characterNameInputField.text;
        AccountManager.Instance.CurrentAccount.gender = genderStr;
        AccountManager.Instance.CurrentAccount.age = (int)ageSlider.value;
        AccountManager.Instance.CurrentAccount.personality = personalityInputField.text;
        AccountManager.Instance.SaveAccount();

        // 存入 GameDataManager (用於場景間傳遞數據)
        GameDataManager.Instance.characterName = characterNameInputField.text;
        GameDataManager.Instance.characterGender = genderStr; // 這次就不會被 Reset 掉了
        GameDataManager.Instance.characterAge = (int)ageSlider.value;
        GameDataManager.Instance.characterPersonality = personalityInputField.text;

        // ====== 4. 處理跨場景物件 (選配) ======
        // 因為 GenderSwitcher 只在 CreationScene，這裡通常抓不到 Instance
        // 但如果它是 DontDestroyOnLoad 留下來的，這行就能運作
        if (GenderSwitcher.Instance != null)
        {
            GenderSwitcher.Instance.RefreshGender();
        }

        // ====== 4. 開啟捏臉 UI (Canvas_create) ======
        GameObject creationCanvas = null;
        foreach (Canvas c in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (c.name == "Canvas_create") { creationCanvas = c.gameObject; break; }
        }

        if (creationCanvas != null)
        {
            // 1. 先確保父層 Canvas 是開啟的
            creationCanvas.SetActive(true);

            // 2. 強制開啟視角切換按鈕 (因為它們在 DDOL 中可能被上一次操作關閉了)
            Transform farBtn = creationCanvas.transform.Find("Btn_far");
            Transform closeBtn = creationCanvas.transform.Find("Btn_close");
            if (farBtn != null) farBtn.gameObject.SetActive(true);
            if (closeBtn != null) closeBtn.gameObject.SetActive(true);

            // 3. 找到 UIManager 並強迫它切換到第一個面板 (SkinPanel)
            UIManager ui = creationCanvas.GetComponent<UIManager>();
            if (ui != null)
            {
                // 確保 SkinPanel 及其父物件是開啟的
                ui.ShowSkinPanel();

                // 如果 ShowSkinPanel 內部沒有寫 SetActive(true)，請在下面手動補上：
                Transform skinPanel = creationCanvas.transform.Find("SkinPanel");
                if (skinPanel != null) skinPanel.gameObject.SetActive(true);
            }
        }

        // ====== 5. 跳轉場景 ======
        SceneController.instance.LoadScene("CharacterCreationScene");
    }
}