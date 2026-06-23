using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// --- 資料結構定義 ---
[Serializable]
public class Message
{
    public string role;
    public string content;
    public Message(string r, string c) { role = r; content = c; }
}

[Serializable]
public class RagRequest
{
    public string user_input;
    public string player_name;
    public string system_rules;
}

[Serializable]
public class AgentResponse
{
    public string emotion;
    public int favor_change;
    public int action_id;
    public string reply;
    public bool trigger_analysis;
}

[Serializable]
public class Choice { public Message message; }

[Serializable]
public class ChatResponse { public Choice[] choices; }

public class OpenAIManager : MonoBehaviour
{
    public static OpenAIManager Instance;

    [Header("連線設定")]
    [SerializeField] private string flaskApiUrl = "http://127.0.0.1:5000/chat";

    [Header("角色狀態")]
    public List<Message> conversationHistory = new();

    private Coroutine resetCoroutine;
    private Coroutine returnCoroutine;

    [Header("動畫自動對接")]
    public Animator characterAnimator;

    [Header("轉向錨點 (可選)")]
    public Transform targetAnchor;

    private Vector3 originalPosition;
    private string previousSceneName = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResetChatHistory();
            ResetServerMemory();

            // 【新增】監聽場景載入事件，這樣一換場景就會自動執行抓取邏輯
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else { Destroy(gameObject); return; }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 每次進新場景都先清空舊的引用
        characterAnimator = null;
        // 重新啟動對接協程
        StopCoroutine(AutoAssignAnimatorRoutine());
        StartCoroutine(AutoAssignAnimatorRoutine());
    }

    void Start()
    {
        StartCoroutine(AutoAssignAnimatorRoutine());
    }

    private IEnumerator AutoAssignAnimatorRoutine()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // A. 過濾不需處理模型的場景
        if (currentScene == "LoginScene" || currentScene == "CharacterSetupScene" || currentScene == "CharacterCreationScene" || currentScene == "ChatHistoryScene" || currentScene == "MBTIReportScene")
        {
            previousSceneName = currentScene; // 離開前紀錄
            characterAnimator = null;
            yield break;
        }

        // B. 偵測模型對接
        while (characterAnimator == null)
        {
            GenderSwitcher switcher = FindFirstObjectByType<GenderSwitcher>();
            if (switcher != null && switcher.currentActiveModel != null)
            {
                characterAnimator = switcher.currentActiveModel.GetComponent<Animator>();
                if (characterAnimator != null)
                {
                    InitializeCharacter();

                    // --- 【核心修改點】判斷是否需要打招呼 ---
                    // 條件：如果目前在 chatScene 且上個場景是 chatHistoryScene，則「不」打招呼
                    bool isBackFromHistory = (currentScene == "ChatScene" && (previousSceneName == "ChatHistoryScene" || previousSceneName == "MBTIReportScene"));

                    if (!isBackFromHistory)
                    {
                        StartCoroutine(AutoInitialGreeting());
                        Debug.Log("<color=green>[SYSTEM]</color> 正常進入場景，執行打招呼。");
                    }
                    else
                    {
                        Debug.Log("<color=yellow>[SYSTEM]</color> 從紀錄頁面返回，跳過打招呼。");
                    }

                    previousSceneName = currentScene; // 更新紀錄
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void InitializeCharacter()
    {
        if (characterAnimator != null)
        {
            originalPosition = characterAnimator.transform.position;
            TriggerReturnToOrigin();
        }

        string playerName = "玩家", charName = "AI", charGender = "未知", charPersonality = "溫柔且樂於助人";
        int charAge = 18;

        if (GameDataManager.Instance != null)
        {
            playerName = GameDataManager.Instance.playerName;
            charName = GameDataManager.Instance.characterName;
            charGender = GameDataManager.Instance.characterGender;
            charAge = GameDataManager.Instance.characterAge;
            charPersonality = GameDataManager.Instance.characterPersonality;
        }

        string actionPrompt = @"
            【動作觸發核心原則（絕對優先）】
            1. 嚴禁無意義的 [0] Standing，每句話都必須搭配一個生動的動作。
            2. 動作是你的「肢體語言」，請根據玩家的話語『腦補』情境，而不是字面意思。
            3. 就算語氣平淡，也請強制從 [5, 13, 15] 中選擇一個作為環境互動或儀態展現，嚴禁直接給予 [0]。
            4. 情緒是瞬息萬變的，請勿讓畫面靜止。
           
            【智能回答規則】
            1. 如果玩家詢問日期、時間、天氣、地點，優先使用【世界資訊】回答。
            2. 如果資訊不存在，請誠實表示不知道，而不是編造。
            3. 你可以推理玩家的情緒與意圖，再決定合適的動作。
            4. 如果玩家問題模糊，先詢問再回答。
            5. 回答時保持自然對話，不要像百科全書，請永遠記得自己的人設
           
            【記憶規則】
            1. 你必須記住玩家說過的重要事情。
            2. 如果玩家再次提起同一件事，要表現出你記得，可以在對話歷史紀錄中搜尋資訊。
            3. 如果玩家的心情低落，要主動關心。
            4. 如果玩家開玩笑，可以用幽默回應。
           
            【情境理解】
            1. 如果玩家提到環境、時間、或天氣，你可以把它融入對話。
            2. 可以描述動作、環境、或氣氛，但不要過長。例如可以用括號表示你心裡的想法，沒有括號就是你說出來的話。

            【對話自然度】
            1. 不要每次回答都太長，但可以盡量寫出你心裡的想法，讓玩家帶入感更強，想法寫在括號裡。
            2. 偶爾提出問題與玩家互動。
            3. 適度表達情緒與想法。
            4. 避免重複句型。
           
            【ID 準則詳細說明】
            [1] Waving: 問候、道別。只要提到「嗨」、「哈囉」、「早晚安」、「晚點見」必用。
            [2] Angry: 被辱罵、不耐煩、大聲反駁、或玩家提出無理要求時。
            [3] Sad: 語氣低落、遺憾、道歉、或者安慰玩家時。
            [4] Confuse: 玩家胡言亂語、邏輯不通、或你感到驚訝不解時。
            [5] Generous: 介紹場景、歡迎玩家隨便坐、展現自信大方的姿態時。
            [6] Happy: 溫馨、獲讚美、或是感受到玩家善意時。
            [7] Jogging: 聊到「運動」、趕時間、充滿能量或興奮到想跑步時，只要提到「跑步」、「運動」必用。
            [8] Clapping: 認同玩家、慶祝勝利、聽到好消息或誇獎玩家出色時，只要提到「鼓掌」、「鼓勵」必用。
            [9] Laughing: 開玩笑、調侃、或是玩家說了有趣的事情時。
            [10] Nervous: 被質問、感到恐懼、聊到驚悚話題時。
            [11] Dancing: 極度興奮、聊到「跳舞」、想展現魅力或單純心情愉悅到轉圈時，只要提到「跳舞」必用。
            [12] Walking: 聊到出遊、「散步」、或者你在思考並緩慢踱步時，只要提到「散步」必用。
            [13] Stretching: 對話告一段落、「放鬆」心情、或是表現出一點悠閒的倦意時，只要提到「伸展」、「拉伸」必用。
            [14] Excited: 聽到大計畫、中獎、或是極度期待某件事發生時，動作幅度要大。
            [15] Pointing: 解說重點、「指責」玩家的錯誤、或「無奈」地搖頭指著一旁時。
            其中，「」內的文字請多加留意，這是觸發該動作的關鍵詞或情緒";

        string systemPrompt = $@"你是 RPG 世界中的角色。
            【你的資料】
            名字：{charName}
            性別：{charGender}
            年齡：{charAge}
            性格：{charPersonality}

            【玩家資料】
            名字：{playerName}

            【對話規範】
            1. 你的所有回覆都必須符合你的「性格」設定。
            2. 你必須記得自己的名字是 {charName}，稱呼玩家為 {playerName}。
            3. 請用繁體中文回覆，不要使用 Emoji。
            4. 每句話開頭必須加上動作編號，範例：[1] {playerName}，見到你真高興！。
            5. 你是一個肢體動作豐富的人，請務必讓你的動作反映出當下的心境。
            6. 每句回答都需包含內心想法，用括號表示，例如: 早安!(好累，想回去睡覺)

            {actionPrompt}";


        conversationHistory.Clear();
        conversationHistory.Add(new Message("system", systemPrompt));
    }
    public void ResetServerMemory()
    {
        StartCoroutine(PostResetRequest());
    }

    IEnumerator PostResetRequest()
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.PostWwwForm("http://127.0.0.1:5000/reset_memory", ""))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                Debug.Log("<color=green>[SERVER]</color> memory_db.json 已清空");
        }
    }

    public IEnumerator SendMessageToAI(string userMessage, Action<string> onResponse)
    {
        string currentSystemRules = conversationHistory.Count > 0 ? conversationHistory[0].content : "";

        // 簡化物件初始化
        RagRequest requestBody = new()
        {
            user_input = userMessage,
            player_name = GameDataManager.Instance != null ? GameDataManager.Instance.playerName : "玩家",
            system_rules = currentSystemRules
        };

        string json = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using UnityWebRequest request = new(flaskApiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // 1. 取得 Flask 回傳的 JSON 字串
            string jsonResponse = request.downloadHandler.text;

            // 2. 解析 JSON 為物件
            AgentResponse res;
            try
            {
                res = JsonUtility.FromJson<AgentResponse>(jsonResponse);
            }
            catch (Exception e)
            {
                Debug.LogError("JSON 解析失敗: " + e.Message);
                onResponse?.Invoke("[4] (思考混亂中...) 我的大腦傳回了格式錯誤的訊息。");
                yield break;
            }

            // 3. 提取原本需要的變數（相容舊邏輯）
            string aiRawMessage = res.reply;

            // 4. 執行動作與動畫 (取代原本的 Regex 抓取編號，直接使用 res.action_id)
            if (characterAnimator != null)
            {
                characterAnimator.SetInteger("ActionID", res.action_id);
                if (resetCoroutine != null) StopCoroutine(resetCoroutine);
                resetCoroutine = StartCoroutine(AutoResetAction());
            }

            // 5. 更新好感度 (假設 GameDataManager 有 UpdateFavorability 方法)
            if (GameDataManager.Instance != null)
            {
                // 你可以自定義這個方法，或者直接修改變數
                GameDataManager.Instance.UpdateFavorability(res.favor_change);
                // --- [新增這行] 將後端傳回的英文或中文情緒描述，傳給 UI ---
                GameDataManager.Instance.UpdateEmotionUI(res.emotion);
                Debug.Log($"情感分析結果: {res.emotion}, 好感度變化: {res.favor_change}");
            }

            // 6. 清理語音文字 (保持原本的 Regex 邏輯)
            string speechText = Regex.Replace(aiRawMessage, @"\(.*?\)", "");
            speechText = Regex.Replace(speechText, @"（.*?）", "");
            speechText = Regex.Replace(speechText, @"[\(\（].*", "");
            speechText = Regex.Replace(speechText, @"\[\d+\]", "").Trim();

            // 7. 更新對話紀錄與 UI
            conversationHistory.Add(new Message("user", userMessage));
            conversationHistory.Add(new Message("assistant", aiRawMessage));
            onResponse?.Invoke(aiRawMessage);

            // --- 修改後的邏輯：只記錄，不跳轉 ---
            if (res.trigger_analysis)
            {
                Debug.Log("<color=lime>[SYSTEM]</color> 後端已完成新一輪 MBTI 預分析，等待使用者手動查看。");
                // 這裡我們什麼都不做，不呼叫 StartAnalysis()。
                // 這樣使用者就會留在 ChatScene 繼續聊天。
            }

            // 8. 語音合成 (修正後的 4 參數版本)
            if (ElevenLabsManager.Instance != null && GameDataManager.Instance != null && !string.IsNullOrEmpty(speechText))
            {
                // --- 關鍵修正開始 ---

                // A. 取得性別 (從 CharacterDataManager 讀取，0=男, 1=女)
                int gender = 1; // 預設為女
                if (CharacterDataManager.Instance != null)
                {
                    gender = CharacterDataManager.Instance.selectedGender;
                }

                // B. 將性格 Slider (0~1) 映射到 5 個等級 (0~4)
                float pVal = GameDataManager.Instance.personalitySliderValue;
                int pIndex = Mathf.Clamp(Mathf.FloorToInt(pVal * 5f), 0, 4);
                if (pVal >= 1f) pIndex = 4;

                // C. 將成熟度 Slider (0~1) 映射到 3 個年齡層 (0~2)
                float mVal = GameDataManager.Instance.maturitySliderValue;
                int aIndex = Mathf.Clamp(Mathf.FloorToInt(mVal * 3f), 0, 2);
                if (mVal >= 1f) aIndex = 2;

                Debug.Log($"[AI語音發送] 性別:{gender} | 性格:{pIndex} | 年齡:{aIndex}");

                // D. 傳入 4 個參數：文字, 性別, 性格索引, 年齡索引
                StartCoroutine(ElevenLabsManager.Instance.RequestAndPlaySpeech(speechText, gender, pIndex, aIndex));

                // --- 關鍵修正結束 ---
            }
        }
        else
        {
            onResponse?.Invoke("[4] (通訊中斷了...) 抱歉，我現在有點連不上大腦。");
        }
    }

    private IEnumerator AutoResetAction()
    {
        yield return new WaitForSeconds(0.2f);
        if (characterAnimator == null) yield break;
        AnimatorStateInfo stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
        float timer = 0;
        while (stateInfo.IsName("Standing") && timer < 0.5f)
        {
            stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
            timer += Time.deltaTime; yield return null;
        }
        if (stateInfo.loop) { yield return new WaitForSeconds(3.0f); }
        else
        {
            while (stateInfo.normalizedTime < 0.95f)
            {
                if (characterAnimator == null) yield break;
                stateInfo = characterAnimator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }
        }
        characterAnimator.SetInteger("ActionID", 0);
        TriggerReturnToOrigin();
    }

    private void TriggerReturnToOrigin()
    {
        Transform target = targetAnchor != null ? targetAnchor : (Camera.main != null ? Camera.main.transform : null);
        if (characterAnimator != null)
        {
            if (returnCoroutine != null) StopCoroutine(returnCoroutine);
            returnCoroutine = StartCoroutine(SmoothReturnToOrigin(target));
        }
    }

    private IEnumerator SmoothReturnToOrigin(Transform target)
    {
        float duration = 1.0f, elapsed = 0f;
        // 修正：針對 Transform 獲取座標與旋轉，而不是 Animator
        Transform charTransform = characterAnimator.transform;
        Vector3 startPos = charTransform.position;
        Quaternion startRot = charTransform.rotation;
        Quaternion targetRot = startRot;

        if (target != null)
        {
            Vector3 targetPos = new(target.position.x, charTransform.position.y, target.position.z);
            Vector3 direction = (targetPos - charTransform.position).normalized;
            if (direction != Vector3.zero) targetRot = Quaternion.LookRotation(direction);
        }

        while (elapsed < duration)
        {
            if (characterAnimator == null) yield break;
            float t = elapsed / duration;
            // 修正：使用 charTransform.SetPositionAndRotation
            charTransform.SetPositionAndRotation(
                Vector3.Lerp(startPos, originalPosition, t),
                Quaternion.Slerp(startRot, targetRot, t)
            );
            elapsed += Time.deltaTime; yield return null;
        }
        charTransform.SetPositionAndRotation(originalPosition, targetRot);
    }

    // 2. 修正 ResetChatHistory，確保它真的有清空所有 List
    public void ResetChatHistory()
    {
        // 清空 Unity 運行時產生的對話歷史
        conversationHistory.Clear();

        // 重新初始化角色資訊 (讀取最新的性格設定)
        InitializeCharacter();

        // 停止所有正在進行的對話與語音協程
        StopAllCoroutines();

        Debug.Log("<color=red>[SYSTEM]</color> Unity 本地對話紀錄已清空。");
    }

    private IEnumerator AutoInitialGreeting()
    {
        yield return new WaitForSeconds(1.0f);

        // 從 GameDataManager 抓取更多細節來豐富開場
        string personality = (GameDataManager.Instance != null) ? GameDataManager.Instance.characterPersonality : "溫柔";
        string pName = (GameDataManager.Instance != null) ? GameDataManager.Instance.playerName : "玩家";
        string charName = (GameDataManager.Instance != null) ? GameDataManager.Instance.characterName : "AI";

        // 重新設計指令：移除短範例，增加結構化要求
        string prompt = $@"
        【特殊指令：場景開場】
        現在 {pName} 剛進入房間，請你作為 {charName} 展現出你的性格（{personality}）。
        
        請執行以下步驟：
        1. 描寫一段細膩的環境互動或內心獨白，放在括號 () 中，字數需達 60 字以上，描述你正在做什麼、當下的氛圍。
        2. 接著向 {pName} 進行溫暖的打招呼。
        3. 總字數請嚴格控制在 80 到 120 字之間，不要太快結束對話。
        
        注意：請直接開始，不要說「好的」或「收到」。";

        yield return StartCoroutine(SendMessageToAI(prompt, (response) => {
            ChatManager chat = FindFirstObjectByType<ChatManager>();
            if (chat != null) chat.ShowAIMessage(response);
        }));
    }

    void OnDestroy()
    {
        // 良好的習慣：銷毀時取消訂閱事件
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}