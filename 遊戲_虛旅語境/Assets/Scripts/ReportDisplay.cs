using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ReportDisplay : MonoBehaviour
{
    [Header("UI Text")]
    public TextMeshProUGUI mbtiTypeText;
    public TextMeshProUGUI analysisContent;

    [Header("MBTI Bars (Fill Amount 0~1)")]
    public Image eiBarFill;
    public Image snBarFill;
    public Image tfBarFill;
    public Image jpBarFill;

    void Start()
    {
        DisplayReport();
    }

    public void DisplayReport()
    {
        Debug.Log("[Report] 開始從 PlayerPrefs 同步資料...");

        // 1. 讀取原始數據
        string savedTitle = PlayerPrefs.GetString("Saved_Title", "未知分析");
        string savedAnalysis = PlayerPrefs.GetString("Saved_Analysis", "分析內容讀取失敗");

        float ei = Mathf.Clamp01(PlayerPrefs.GetFloat("Saved_EI", 0.5f));
        float sn = Mathf.Clamp01(PlayerPrefs.GetFloat("Saved_SN", 0.5f));
        float tf = Mathf.Clamp01(PlayerPrefs.GetFloat("Saved_TF", 0.5f));
        float jp = Mathf.Clamp01(PlayerPrefs.GetFloat("Saved_JP", 0.5f));

        // 2. 程式判定核心邏輯：根據分數強制產生正確字母
        // 規則：< 0.5 為左邊字母，>= 0.5 為右邊字母
        string finalMBTI = "";
        finalMBTI += (ei < 0.5f) ? "E" : "I";
        finalMBTI += (sn < 0.5f) ? "S" : "N";
        finalMBTI += (tf < 0.5f) ? "T" : "F";
        finalMBTI += (jp < 0.5f) ? "J" : "P";

        Debug.Log($"[MBTI 程式校正] 原始數值 EI:{ei} SN:{sn} TF:{tf} JP:{jp} -> 判定結果: {finalMBTI}");

        // 3. UI 文字更新
        if (PlayerPrefs.GetString("Saved_MBTI", "N/A") == "N/A")
        {
            mbtiTypeText.text = "尚無分析資料";
            analysisContent.text = "請先回聊天室完成對話分析";
        }
        else
        {
            // 這裡改用我們程式跑出來的 finalMBTI，避免 AI 字串跟數據對不上
            mbtiTypeText.text = finalMBTI + " - " + savedTitle;
            analysisContent.text = savedAnalysis;
        }

        // 4. UI 拉條更新 (已移除 1f - 反轉)
        if (eiBarFill != null) eiBarFill.fillAmount = ei;
        if (snBarFill != null) snBarFill.fillAmount = sn;
        if (tfBarFill != null) tfBarFill.fillAmount = tf;
        if (jpBarFill != null) jpBarFill.fillAmount = jp;

        Debug.Log("<color=cyan>[UI]</color> MBTI 報告更新完成！");
    }

    public void BackToChat()
    {
        SceneManager.LoadScene("ChatScene");
    }
}