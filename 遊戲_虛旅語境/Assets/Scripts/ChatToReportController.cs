using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System;

[Serializable]
public class AnalysisRequest { public string history; }

public class ChatToReportController : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://127.0.0.1:5000/analyze_mbti";

    public void StartAnalysis()
    {
        string json = PlayerPrefs.GetString("chat_history", "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("沒有聊天紀錄可分析！");
            return;
        }

        ChatHistoryWrapper wrapper = JsonUtility.FromJson<ChatHistoryWrapper>(json);
        StringBuilder sb = new StringBuilder();
        foreach (var msg in wrapper.messages)
        {
            sb.AppendLine($"{msg.role}: {msg.text}");
        }

        StartCoroutine(PostToBackend(sb.ToString()));
    }

    IEnumerator PostToBackend(string historyContent)
    {
        AnalysisRequest req = new AnalysisRequest { history = historyContent };
        string jsonPayload = JsonUtility.ToJson(req);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=green>後端分析成功回傳！</color>");
                MBTIResponse response = JsonUtility.FromJson<MBTIResponse>(request.downloadHandler.text);

                // 1. 存入靜態變數
                MBTIDataHolder.FinalMBTI = response.mbti;
                MBTIDataHolder.Title = response.title;
                MBTIDataHolder.FullAnalysis = response.analysis_text;
                MBTIDataHolder.EIScore = response.ei_score;
                MBTIDataHolder.SNScore = response.sn_score;
                MBTIDataHolder.TFScore = response.tf_score;
                MBTIDataHolder.JPScore = response.jp_score;

                // 2. 同步存入 PlayerPrefs (雙重保險)
                PlayerPrefs.SetString("Saved_MBTI", response.mbti);
                PlayerPrefs.SetString("Saved_Title", response.title);
                PlayerPrefs.SetString("Saved_Analysis", response.analysis_text);
                PlayerPrefs.SetFloat("Saved_EI", response.ei_score);
                PlayerPrefs.SetFloat("Saved_SN", response.sn_score);
                PlayerPrefs.SetFloat("Saved_TF", response.tf_score);
                PlayerPrefs.SetFloat("Saved_JP", response.jp_score);
                PlayerPrefs.Save();

                Debug.Log($"[Storage] 資料已緩存，準備跳轉至報告頁面...");
                SceneManager.LoadScene("MBTIReportScene");
            }
            else
            {
                Debug.LogError($"分析失敗: {request.error} | 內容: {request.downloadHandler.text}");
            }
        }
    }
}