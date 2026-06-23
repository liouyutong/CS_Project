using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class ChatManager : MonoBehaviour, IPointerClickHandler
{
    // ... 原有的 Header 變數保持不變 ...
    [Header("Input Field")]
    public TMP_InputField chatInput;
    [Header("Text RectTransform")]
    public RectTransform textRect;
    [Header("Send Button")]
    public Button sendButton;
    [Header("RPG Response Bubble")]
    public RPGResponseBubble rpgBubble;
    [Header("Scroll Settings")]
    public ScrollRect scrollRect;
    [Header("Typewriter Settings")]
    public float typingSpeed = 0.03f;

    [Header("Player / AI Name")]
    public string playerName = "玩家";
    public string aiName = "AI";

    [Header("Confirmation Popup")]
    public GameObject confirmPopup; // 拖入剛才創立的 ResetConfirmPopup Panel

    private Vector2 offsetMinFixed;
    private Vector2 offsetMaxFixed;
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string fullMessage;
    private bool userScrolledUp = false;
    private List<ChatMessage> history = new List<ChatMessage>();

    void Start()
    {
        offsetMinFixed = textRect.offsetMin;
        offsetMaxFixed = textRect.offsetMax;

        StartCoroutine(ActivateInputNextFrame());

        chatInput.onValueChanged.AddListener(OnInputChanged);
        sendButton.onClick.AddListener(OnSendClicked);
        scrollRect.onValueChanged.AddListener(OnScrollChanged);

        if (OpenAIManager.Instance != null)
            OpenAIManager.Instance.InitializeCharacter();

        LoadHistory();
    }

    // --- 功能 1：僅清除對話歷史 (人物留在場景，記憶清空) ---
    public void ClearChatHistoryOnly()
    {
        Debug.Log("<color=cyan>[SYSTEM]</color> 執行：僅清除對話歷史紀錄...");

        // 1. 讓 AI 遺忘過去 (包含後端檔案與前端變數)
        if (OpenAIManager.Instance != null)
        {
            // 【新增】同步刪除後端 memory_db.json
            OpenAIManager.Instance.ResetServerMemory();
            // 原有的清空內部 List
            OpenAIManager.Instance.ResetChatHistory();

            // 【重要】重置後讓 AI 重新打招呼，生成新的故事開頭
            OpenAIManager.Instance.InitializeCharacter();
        }

        // 2. 清空當前場景的 UI 顯示
        history.Clear();
        if (rpgBubble != null)
        {
            rpgBubble.responseText.text = "";
            rpgBubble.gameObject.SetActive(false);
        }

        // 3. 刪除本地對話紀錄存檔 (PlayerPrefs)，確保 HistoryScene 進去也是空的
        PlayerPrefs.DeleteKey("chat_history");
        PlayerPrefs.Save();

        Debug.Log("<color=green>[SUCCESS]</color> 對話已清空，伺服器記憶已抹除，人物準備重新開始。");
    }

    // --- 功能 2：徹底重置並回登入場景 (創建新角色) ---
    public void ResetAndCreateNewCharacter()
    {
        Debug.Log("<color=orange>[SYSTEM]</color> 執行：刪除當前角色並重新啟動...");

        // 1. 呼叫上方的方法清空基本記憶與 UI
        ClearChatHistoryOnly();

        // 2. 處理模型歸位與隱藏
        if (OpenAIManager.Instance != null && OpenAIManager.Instance.characterAnimator != null)
        {
            GameObject currentModel = OpenAIManager.Instance.characterAnimator.gameObject;

            // 重置外觀組件 (FaceGen, Equipment)
            FaceAutoGenerator faceGen = currentModel.GetComponentInChildren<FaceAutoGenerator>();
            if (faceGen != null) faceGen.ResetToDefaultAppearance();

            CharacterEquipmentHandler equipment = currentModel.GetComponentInChildren<CharacterEquipmentHandler>();
            if (equipment != null) equipment.ClearAllEquipment();

            // 隱藏模型並清除引用
            currentModel.SetActive(false);
            OpenAIManager.Instance.characterAnimator = null;
        }

        // 3. 重置捏臉數據單例
        if (CharacterDataManager.Instance != null)
        {
            CharacterDataManager.Instance.selectedGender = 0;
            CharacterDataManager.Instance.faceShapeData.Clear();
            CharacterDataManager.Instance.skinColor = Color.white;
            CharacterDataManager.Instance.eyeColor = Color.white;
            CharacterDataManager.Instance.outfitIndex = -1;
            CharacterDataManager.Instance.hairIndex = -1;
        }

        // 4. 徹底清除所有本地存檔 (包含玩家與 AI 名字)
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.DeleteKey("CharacterName");
        PlayerPrefs.DeleteAll(); // 或者精確刪除特定 Key
        PlayerPrefs.Save();
        // 在切換場景前，先叫 AI 閉嘴並清空引用
        if (OpenAIManager.Instance != null)
        {
            OpenAIManager.Instance.ResetChatHistory();
        }

        // 5. 返回登入/捏臉場景
        Debug.Log("正在返回 LoginScene...");
        SceneController.instance.LoadScene("LoginScene");
    }

    // --- 以下為妳原本的邏輯，保持不變 ---

    IEnumerator ActivateInputNextFrame()
    {
        yield return null;
        chatInput.ActivateInputField();
        chatInput.Select();
    }

    void OnInputChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
            FixTextRect();
    }

    void Update()
    {
        // 【新增這行】如果輸入框不見了（代表不在聊天場景），就直接返回，不執行後面的邏輯
        if (chatInput == null) return;

        bool enterPressed = false;
        bool mouseClicked = false;
        Vector2 mousePos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            enterPressed = true;

        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            mouseClicked = true;
            mousePos = Pointer.current.position.ReadValue();
        }
#else
        if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            enterPressed = true;

        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            mouseClicked = true;
            mousePos = UnityEngine.Input.mousePosition;
        }
#endif

        if (chatInput.isFocused && !isTyping && enterPressed)
        {
            OnSendClicked();
        }

        if (mouseClicked && EventSystem.current != null)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = mousePos;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
            {
                Debug.Log("<color=yellow>[Raycast Result] 目前點擊到的物件是: </color>" + results[0].gameObject.name);
            }
        }
    }

    void FixTextRect()
    {
        textRect.offsetMin = offsetMinFixed;
        textRect.offsetMax = offsetMaxFixed;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            rpgBubble.responseText.text = fullMessage;
            FinishTyping();
            ScrollToBottom();
            return;
        }
        chatInput.ActivateInputField();
        chatInput.Select();
    }

    void OnSendClicked()
    {
        if (isTyping) return;
        string playerMessage = chatInput.text.Trim();
        if (string.IsNullOrEmpty(playerMessage)) return;

        chatInput.text = "";
        LockInput(true);
        AddMessage("player", playerName, playerMessage);

        StartCoroutine(OpenAIManager.Instance.SendMessageToAI(playerMessage, (aiReply) =>
        {
            ShowAIMessage(aiReply);
        }));
    }

    public void ShowAIMessage(string aiReply)
    {
        AddMessage("ai", aiName, aiReply);
        StartTyping(aiReply);
    }

    void StartTyping(string message)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        rpgBubble.gameObject.SetActive(true);
        fullMessage = message;
        userScrolledUp = false;
        typingCoroutine = StartCoroutine(TypeText(message));
    }

    IEnumerator TypeText(string message)
    {
        isTyping = true;
        rpgBubble.responseText.text = "";

        for (int i = 0; i < message.Length; i++)
        {
            rpgBubble.responseText.text += message[i];

            if (!userScrolledUp)
                ScrollToBottom();

            yield return new WaitForSeconds(typingSpeed);
        }

        FinishTyping();
    }

    void FinishTyping()
    {
        isTyping = false;
        LockInput(false);
        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    void OnScrollChanged(Vector2 pos)
    {
        userScrolledUp = scrollRect.verticalNormalizedPosition > 0.05f;
    }

    void LockInput(bool state)
    {
        chatInput.interactable = !state;
        sendButton.interactable = !state;
    }

    void AddMessage(string role, string name, string text)
    {
        history.Add(new ChatMessage
        {
            role = role,
            name = name,
            text = text,
            time = System.DateTime.Now.ToString()
        });

        SaveHistory();
    }

    void SaveHistory()
    {
        PlayerPrefs.SetString("chat_history", JsonUtility.ToJson(new ChatHistoryWrapper { messages = history }));
        PlayerPrefs.Save();
    }

    void LoadHistory()
    {
        if (!PlayerPrefs.HasKey("chat_history")) return;

        string json = PlayerPrefs.GetString("chat_history");
        ChatHistoryWrapper wrapper = JsonUtility.FromJson<ChatHistoryWrapper>(json);

        if (wrapper != null && wrapper.messages != null)
            history = wrapper.messages;
    }

    public void OpenHistoryScene()
    {
        Debug.Log("<color=cyan>[DEBUG]</color> OpenHistoryScene() 函數被成功呼叫！");
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        bool sceneFound = false;

        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (scenePath.Contains("ChatHistoryScene"))
            {
                sceneFound = true;
                break;
            }
        }

        if (sceneFound)
        {
            SceneManager.LoadScene("ChatHistoryScene");
        }
        else
        {
            Debug.LogError("<color=red>[ERROR]</color> 跳轉失敗：'ChatHistoryScene' 不在 Build Settings 中！");
        }
    }

    public void OnRequestCreateNewCharacter()
    {
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(true); // 開啟彈窗
        }
        else
        {
            // 如果忘了拉引用，就直接執行(保險用)
            ResetAndCreateNewCharacter();
        }
    }

    // --- 彈窗「返回」按鈕連結此方法 ---
    public void CloseConfirmPopup()
    {
        if (confirmPopup != null)
        {
            confirmPopup.SetActive(false); // 關閉彈窗，什麼都不做
        }
    }
}