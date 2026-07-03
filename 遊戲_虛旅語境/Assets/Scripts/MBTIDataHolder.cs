using UnityEngine;

public static class MBTIDataHolder
{
    public static string FinalMBTI = "N/A";
    public static string Title = "分析中...";
    public static string FullAnalysis = "載入中...";

    public static float EIScore = 0.5f;
    public static float SNScore = 0.5f;
    public static float TFScore = 0.5f;
    public static float JPScore = 0.5f;
}

[System.Serializable]
public class MBTIResponse
{
    public string mbti;
    public string title;
    public string analysis_text;
    public float ei_score;
    public float sn_score;
    public float tf_score;
    public float jp_score;
}