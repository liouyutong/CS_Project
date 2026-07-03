using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance;

    [Header("玩家資訊")]
    public string playerId;
    public string playerName = "玩家";

    [Header("角色資訊")]
    public string characterName;
    public string characterGender;
    public int characterAge;
    public string characterPersonality;

    [Header("UI 顯示")]
    public TextMeshProUGUI favorTextUI;
    public TextMeshProUGUI emotionTextUI;

    [Header("好感度系統")]
    public int currentFavorability = 50;

    [Header("語音捏臉設定")]
    public float personalitySliderValue = 0.5f;
    public float maturitySliderValue = 0.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FindAndBindUI();
    }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndBindUI();
    }

    private void FindAndBindUI()
    {
        GameObject foundFavorUI = GameObject.Find("FavorText");
        if (foundFavorUI != null)
        {
            favorTextUI = foundFavorUI.GetComponent<TextMeshProUGUI>();
            UpdateFavorUI();
        }

        GameObject foundEmotionUI = GameObject.Find("EmotionText");
        if (foundEmotionUI != null)
        {
            emotionTextUI = foundEmotionUI.GetComponent<TextMeshProUGUI>();
            UpdateEmotionUI("中立");
        }
    }

    public void UpdateFavorability(int change)
    {
        currentFavorability = Mathf.Clamp(currentFavorability + change, 0, 100);
        UpdateFavorUI();
    }

    public void UpdateEmotionUI(string emotionDesc)
    {
        if (emotionTextUI == null) return;

        string displayEmotion = emotionDesc;

        switch (emotionDesc.ToLower())
        {
            case "elated": case "happy": displayEmotion = "開心"; break;
            case "pensive": displayEmotion = "沉思"; break;
            case "annoyed": case "angry": displayEmotion = "惱怒"; break;
            case "bashful": case "shy": displayEmotion = "羞澀"; break;
            case "puzzled": case "confused": displayEmotion = "困惑"; break;
            case "melancholic": case "sad": displayEmotion = "憂傷"; break;
            case "fatigued": displayEmotion = "疲憊"; break;
            case "neutral": displayEmotion = "中立"; break;
        }

        emotionTextUI.text = $"當前情緒: {displayEmotion}";
        emotionTextUI.ForceMeshUpdate();
    }

    public void UpdateFavorUI()
    {
        if (favorTextUI == null) return;

        string status = "";
        if (currentFavorability >= 80) status = "喜愛";
        else if (currentFavorability >= 60) status = "友好";
        else if (currentFavorability >= 40) status = "中立";
        else if (currentFavorability >= 20) status = "疏離";
        else status = "厭惡";

        favorTextUI.text = "好感度: " + currentFavorability + " (" + status + ")";
        favorTextUI.ForceMeshUpdate();
    }

    /// <summary>
    /// 重置所有資料（包含語音捏臉數值）
    /// </summary>
    public void ResetData()
    {
        // 1. 重置系統數值
        currentFavorability = 50;

        // 2. 關鍵修正：重置語音拉條數值
        personalitySliderValue = 0.5f;
        maturitySliderValue = 0.5f;

        // 3. 更新 UI
        UpdateFavorUI();
        UpdateEmotionUI("中立");

        Debug.Log("<color=green>【GameDataManager】所有資料已重置（包含語音設定）</color>");
    }
}