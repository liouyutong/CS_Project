using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChatHistoryManager : MonoBehaviour
{
    [Header("ScrollView Content")]
    public Transform content;

    [Header("Bubble Prefabs (請從 Project 視窗拖入原始檔案)")]
    public GameObject playerBubblePrefab;
    public GameObject aiBubblePrefab;

    [Header("空狀態提示")]
    public GameObject emptyTextHint;

    [Header("確認彈窗 UI")]
    public GameObject clearConfirmPopup; // 拖入你的彈窗 Panel 物件

    void Start()
    {
        LoadHistory();
    }

    /// <summary>
    /// 載入聊天歷史
    /// </summary>
    void LoadHistory()
    {
        // 1. 安全清空舊物件
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in content) toDestroy.Add(child.gameObject);
        foreach (GameObject obj in toDestroy)
        {
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        if (!PlayerPrefs.HasKey("chat_history"))
        {
            SetEmptyState(true);
            return;
        }

        string json = PlayerPrefs.GetString("chat_history");
        ChatHistoryWrapper wrapper = JsonUtility.FromJson<ChatHistoryWrapper>(json);

        if (wrapper == null || wrapper.messages == null || wrapper.messages.Count == 0)
        {
            SetEmptyState(true);
            return;
        }

        SetEmptyState(false);

        foreach (ChatMessage msg in wrapper.messages)
        {
            GameObject prefab = null;
            if (msg.role == "player") prefab = playerBubblePrefab;
            else if (msg.role == "ai") prefab = aiBubblePrefab;

            if (prefab == null)
            {
                Debug.LogError("找不到對應的角色 Prefab，請檢查 Inspector 引用是否斷裂！");
                continue;
            }

            // 2. 生成泡泡父物件
            GameObject bubbleRoot = Instantiate(prefab, content);

            // ========================================================
            // 【終極修改】：強制點亮所有層級的物件與 Component
            // ========================================================

            // [1] 強制開啟父物件本身 (Active)
            bubbleRoot.SetActive(true);

            // [2] 強制開啟父物件上的 Component (如果有 Image 或 Layout)
            EnableAllComponents(bubbleRoot);

            // [3] 強制開啟所有子物件，以及「子物件身上的 Component」
            foreach (Transform child in bubbleRoot.transform)
            {
                // 開啟子物件本身 (Spacer, Bubble 物件)
                child.gameObject.SetActive(true);

                // 【關鍵點】：尋找並開啟子物件上的所有 Component (如 Image, Layout, Content Size Fitter)
                EnableAllComponents(child.gameObject);

                // [4] (選配) 如果 Bubble 裡面還有 TMP，也順便點亮
                // (因為 TMP 是氣泡出現的關鍵)
                TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.enabled = true;
            }

            // ========================================================
            // 【結束修改】
            // ========================================================

            // 獲取文字組件並設定內容 (GetComponentInChildren 可同時找到 enabled/disabled 的組件)
            TextMeshProUGUI textComponent = bubbleRoot.GetComponentInChildren<TextMeshProUGUI>(true);

            if (textComponent != null)
            {
                string displayName = msg.name;
                if (string.IsNullOrEmpty(displayName))
                {
                    if (msg.role == "player") displayName = GameDataManager.Instance.playerName;
                    else displayName = string.IsNullOrEmpty(GameDataManager.Instance.characterName)
                        ? "AI"
                        : GameDataManager.Instance.characterName;
                }
                textComponent.text = $"{displayName}: {msg.text}";
            }
        }

        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = content.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// 【新增方法】：遍歷開啟一個物件上所有關鍵的 UI Component
    /// </summary>
    private void EnableAllComponents(GameObject targetObj)
    {
        if (targetObj == null) return;

        // 開啟 Image背景
        Image img = targetObj.GetComponent<Image>();
        if (img != null) img.enabled = true;

        // 開啟所有 Layout 組件
        LayoutGroup layout = targetObj.GetComponent<LayoutGroup>();
        if (layout != null) layout.enabled = true;

        ContentSizeFitter fitter = targetObj.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = true;

        // 開啟任何其他自訂的 UI 相關 Component (如果有的話)
    }

    public void ClearHistory()
    {
        // 1. 刪除本地存檔 (PlayerPrefs)
        if (PlayerPrefs.HasKey("chat_history"))
        {
            PlayerPrefs.DeleteKey("chat_history");
            PlayerPrefs.Save();
        }

        // 2. 重置 AI 記憶 (包含後端 memory_db.json 與 前端 List)
        if (OpenAIManager.Instance != null)
        {
            // 【新增】通知伺服器刪除 JSON 檔案
            OpenAIManager.Instance.ResetServerMemory();
            // 原有的清空 C# List 邏輯
            OpenAIManager.Instance.ResetChatHistory();
            Debug.Log("<color=cyan>[AI]</color> 記憶與伺服器檔案已同步重置。");
        }

        // 3. 清除當前 ScrollView 裡的對話泡泡物件
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // 4. 顯示「目前沒有紀錄」的提示
        SetEmptyState(true);

        Debug.Log("<color=red>聊天紀錄與 AI 記憶已完全清空！</color>");

        // 保留原本的 UI 關閉邏輯
        if (clearConfirmPopup != null) clearConfirmPopup.SetActive(false);
    }

    private void SetEmptyState(bool isEmpty)
    {
        if (emptyTextHint != null)
        {
            emptyTextHint.SetActive(isEmpty);
        }
    }

    public void BackToChat()
    {
        SceneManager.LoadScene("ChatScene");
    }

    // --- 1. 當按下「一鍵清空」按鈕時呼叫此方法 ---
    public void OnRequestClearHistory()
    {
        if (clearConfirmPopup != null)
        {
            clearConfirmPopup.SetActive(true); // 顯示彈窗
        }
        else
        {
            // 如果忘了設定 UI，保險起見直接執行清空
            ClearHistory();
        }
    }

    // --- 2. 當按下彈窗內的「否」按鈕時呼叫此方法 ---
    public void CloseClearConfirmPopup()
    {
        if (clearConfirmPopup != null)
        {
            clearConfirmPopup.SetActive(false); // 關閉彈窗
        }
    }
}