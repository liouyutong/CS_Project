using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;

public class FaceAISystem : MonoBehaviour
{
    [Header("API 設定")]
    public string apiKey = "";

    [Header("UI 連結")]
    public TMP_InputField myInputField;
    public FaceAutoGenerator faceGenerator;

    [System.Serializable] public class GeminiRequest { public List<Content> contents; }
    [System.Serializable] public class Content { public List<Part> parts; }
    [System.Serializable] public class Part { public string text; }

    [System.Serializable] public class FaceData { public List<FeatureValue> features; }
    [System.Serializable] public class FeatureValue { public string name; public float value; }

    public void SimpleAskAI()
    {
        if (string.IsNullOrEmpty(apiKey)) { Debug.LogError("請填入 API Key！"); return; }
        if (myInputField == null) { Debug.LogError("未連結 InputField！"); return; }
        StartCoroutine(CallGemini(myInputField.text));
    }

    IEnumerator CallGemini(string userInput)
    {
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + apiKey.Trim();

        // 強烈要求 AI 只回傳 JSON，不要加 Markdown 標籤
        // 1. 定義參數與座標/旋轉邏輯
        string allFeatures = @"
            [參數清單]: 
            eyebrow(PosX, PosZ, Rotate, SizeX), eye(PosX, PosZ, SizeX, SizeZ, Rotate), 
            eyeball(PosX, PosZ, Size), nose(PosY, PosZ, SizeX, SizeZ), 
            mouth(PosZ, SizeX, SizeZ), ear(PosY, PosZ, Size), 
            cheekUp(PosX, PosZ), cheekDown(PosX, PosZ), 
            jaw(PosX, PosZ), neck(Pos, Size), 
            shoulderWid, chestWid, waistWid";

        string technicalRules = @"
            [技術規範]:
            - X (左右): _max=向兩側延伸(加寬/長)；_min=向中心集中(縮短/窄)。
            - Y (前後): _max=往前；_min=往後。
            - Z (上下): _max=往上；_min=往下。
            - 旋轉(Rotate): _max=下垂(垂眼/八字眉)；_min=上挑(吊眼/劍眉)。
            - 強度判斷: 請根據玩家語氣的強烈程度給予數值。
              * 微調 (例如: 稍微、一點點): value = 10~30
              * 標準 (例如: 想要、變...): value = 40~65
              * 強烈 (例如: 超級、非常、極度): value = 80~100";

        // 2. 結合旋轉邏輯的美學指南
        string aestheticGuide = @"
            [美學風格指南]:
            - 無辜/可憐/慈祥/和藹: 眼睛與眉毛 Rotate 使用 _max。
            - 嫵媚/性感: 
                * 眼神: 眼睛與眉毛 Rotate 使用 _min (上挑)。
                * 眼型: eyeSizeX 使用 _max (拉長眼型) 或 _min (收窄眼型，依情況)，eyePosX 使用 _max (增加眼距營造疏離感)。
                * 唇部: 嘴唇厚度(mouthSizeZ)使用 _max。
                * 臉型: 下頷寬度(cheekDownPosX)使用 _min (縮短，打造 V 臉效果)。
            - 幼態/可愛/蘿莉/正太: 眼睛(eyeSizeX, eyeSizeZ)加大, 眼距(eyePosX)變寬(_max), 眼球(eyeballSize)加大, 臉頰與下巴(cheekUp, cheekDown, jawPosZ)加大(_max), 嘴巴(mouthSizeX)縮短(_min), 身體(shoulderWid, chestWid, waistWid)縮短(_min)。
            - 帥氣/英俊/高冷: 眉毛靠近眼睛(eyebrowPosZ_min), 臉型(jawPosX, cheekDownPosX)縮小(_min)。
            - 健壯: 肩寬(shoulderWid_max), 脖子(neckSize_max), 胸寬與腰寬(chestWid, waistWid)加長(_max)。";

        string semanticLogic = @"
            [語意判斷]: 請分析玩家描述中的形容詞，若出現相似語意（如：『誘人』對應『性感』、『強壯』對應『健壯』），請自動套用對應風格的參數邏輯。";

        // 3. 組合最終 System Prompt
        string systemPrompt = $@"
            你是一位具備審美直覺的 3D 建模專家。
            {allFeatures}
            {technicalRules}
            {aestheticGuide}
            {semanticLogic}

            請根據玩家指令回傳 JSON。範例：{{""features"":[{{""name"":""eyeSizeX_max"",""value"":100}}]}}。
            不要回傳 Markdown 標籤或文字解釋。";

        GeminiRequest req = new GeminiRequest
        {
            contents = new List<Content> {
                new Content { parts = new List<Part> { new Part { text = systemPrompt + "\n玩家指令：" + userInput } } }
            }
        };
        string jsonPayload = JsonUtility.ToJson(req);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=green>【連線成功】</color>");
                ProcessResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"失敗: {request.downloadHandler.text}");
            }
        }
    }

    void ProcessResponse(string responseText)
    {
        string flattened = responseText.Replace("\\\"", "\"").Replace("\\n", " ").Replace("\\r", "");
        Match match = Regex.Match(flattened, @"\{""features"".*?\]\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (match.Success)
        {
            string cleanJson = match.Value;
            try
            {
                FaceData data = JsonUtility.FromJson<FaceData>(cleanJson);
                // 在 FaceAISystem.cs 的 ProcessResponse 內
                foreach (var item in data.features)
                {
                    string sliderName = item.name.Replace("_max", "").Replace("_min", "");
                    float finalValue = item.name.Contains("_min") ? -item.value : item.value;

                    // 先直接更新模型（不依賴 UI 面板是否開啟）
                    faceGenerator.SetSliderValueByName(sliderName, finalValue);

                    // 強力搜尋 UI Slider (包含隱藏物件)
                    Slider targetSlider = null;
                    Slider[] allSceneSliders = Resources.FindObjectsOfTypeAll<Slider>();
                    foreach (var s in allSceneSliders)
                    {
                        if (s.gameObject.name == sliderName)
                        {
                            targetSlider = s;
                            break;
                        }
                    }

                    if (targetSlider != null)
                    {
                        targetSlider.value = finalValue;
                        // 注意：不一定要呼叫 Invoke，因為 SetSliderValueByName 已經改過模型了
                        // 呼叫 Invoke 是為了讓畫面上其他的監聽程式也跟著動
                        Debug.Log($"<color=lime>>> UI連動成功：{sliderName}</color>");
                    }
                }
                Debug.Log("<color=yellow>★ 捏臉連動完成！ ★</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogError("解析失敗: " + e.Message);
            }
        }
    }
}