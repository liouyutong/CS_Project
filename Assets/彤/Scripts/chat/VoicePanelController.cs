using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VoicePanelController : MonoBehaviour
{
    [Header("UI 連結 - 拉條")]
    public Slider personalitySlider;
    public Slider maturitySlider;

    [Header("UI 連結 - 文字")]
    public Text personalityText;
    public Text maturityText;

    [Header("試聽設定")]
    [TextArea]
    public string testSpeechText = "哈囉，你覺得我現在的聲音聽起來怎麼樣？";
    public Button testVoiceButton;

    // 使用 OnEnable 而不是 Start，確保每次「打開面板」都會強制刷新一次
    void OnEnable()
    {
        RefreshSliderUI();
    }

    void Start()
    {
        // 綁定監聽 (這部分只需做一次)
        personalitySlider.onValueChanged.AddListener(delegate { UpdateValues(); });
        maturitySlider.onValueChanged.AddListener(delegate { UpdateValues(); });

        if (testVoiceButton != null)
        {
            testVoiceButton.onClick.AddListener(PlayTestVoice);
        }

        RefreshSliderUI();
    }

    // 新增：強制刷新 UI 的方法
    public void RefreshSliderUI()
    {
        if (GameDataManager.Instance != null)
        {
            float p = GameDataManager.Instance.personalitySliderValue;
            float m = GameDataManager.Instance.maturitySliderValue;

            // 加這行可以在 Console 看到數值
            Debug.Log($"<color=orange>【UI重置檢查】性格讀到: {p}, 成熟讀到: {m}</color>");

            personalitySlider.value = p;
            maturitySlider.value = m;
        }
        UpdateTextDisplay();
    }

    void UpdateValues()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.personalitySliderValue = personalitySlider.value;
            GameDataManager.Instance.maturitySliderValue = maturitySlider.value;
            UpdateTextDisplay();
        }
    }

    void UpdateTextDisplay()
    {
        if (personalityText != null)
        {
            float pVal = personalitySlider.value;
            // 對應你的 5 種性格標籤
            if (pVal < 0.2f) personalityText.text = "性格：非常高冷";
            else if (pVal < 0.4f) personalityText.text = "性格：較高冷";
            else if (pVal < 0.6f) personalityText.text = "性格：中間";
            else if (pVal < 0.8f) personalityText.text = "性格：較熱情";
            else personalityText.text = "性格：非常熱情";
        }

        if (maturityText != null)
        {
            float mVal = maturitySlider.value;
            // 對應你的 3 種年齡層標籤
            if (mVal < 0.33f) maturityText.text = "年齡：年輕";
            else if (mVal < 0.66f) maturityText.text = "年齡：中間";
            else maturityText.text = "年齡：成熟";
        }
    }

    public void PlayTestVoice()
    {
        // 確保這三個 Manager 都有在場景中
        if (ElevenLabsManager.Instance != null && GameDataManager.Instance != null)
        {
            // 1. 從 CharacterDataManager 讀取當前選擇的性別 (0:男, 1:女)
            int gender = 0;
            if (CharacterDataManager.Instance != null)
            {
                gender = CharacterDataManager.Instance.selectedGender;
            }

            // 2. 將性格 Slider (0~1) 映射到 5 個性格等級 (0, 1, 2, 3, 4)
            float pVal = GameDataManager.Instance.personalitySliderValue;
            int pIndex = Mathf.Clamp(Mathf.FloorToInt(pVal * 5f), 0, 4);
            if (pVal >= 1f) pIndex = 4; // 處理極限值

            // 3. 將成熟度 Slider (0~1) 映射到 3 個年齡層 (0:年輕, 1:中間, 2:成熟)
            float mVal = GameDataManager.Instance.maturitySliderValue;
            int aIndex = Mathf.Clamp(Mathf.FloorToInt(mVal * 3f), 0, 2);
            if (mVal >= 1f) aIndex = 2; // 處理極限值

            Debug.Log($"<color=cyan>[TTS測試]</color> 性別:{gender} | 性格索引:{pIndex} | 年齡索引:{aIndex}");

            // 4. 呼叫 ElevenLabsManager 並傳入完整的 4 個參數
            StartCoroutine(ElevenLabsManager.Instance.RequestAndPlaySpeech(
                testSpeechText,
                gender,
                pIndex,
                aIndex
            ));
        }
        else
        {
            Debug.LogError("請檢查場景中是否缺少 ElevenLabsManager 或 GameDataManager！");
        }
    }
}