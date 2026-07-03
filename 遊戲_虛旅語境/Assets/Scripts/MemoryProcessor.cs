using System;
using System.Text.RegularExpressions;
using UnityEngine;

public static class MemoryProcessor
{
    /// <summary>
    /// 判斷這句話是否值得記憶
    /// </summary>
    public static bool IsImportant(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // 太短的不記
        if (text.Length < 6) return false;

        // 關鍵字判斷
        string[] keywords =
        {
            "喜歡", "討厭", "生日", "名字", "叫做",
            "工作", "學校", "朋友", "家人",
            "難過", "開心", "生氣", "害怕",
            "夢想", "目標"
        };

        foreach (var k in keywords)
        {
            if (text.Contains(k))
                return true;
        }

        // 情緒判斷（簡單版）
        if (Regex.IsMatch(text, @"難過|傷心|崩潰|超開心|超爽|好氣"))
            return true;

        return false;
    }

    /// <summary>
    /// 判斷記憶類型
    /// </summary>
    public static string GetMemoryType(string text)
    {
        if (string.IsNullOrEmpty(text)) return "info";

        if (Regex.IsMatch(text, @"難過|傷心|開心|快樂|生氣"))
            return "emotion";

        if (Regex.IsMatch(text, @"今天|昨天|剛剛|發生"))
            return "event";

        return "info";
    }

    /// <summary>
    /// 計算重要性權重
    /// </summary>
    public static float CalculateWeight(string text)
    {
        if (string.IsNullOrEmpty(text)) return 1f;

        float weight = 1f;

        // 長度越長 → 越重要
        weight += text.Length * 0.02f;

        // 情緒加權
        if (Regex.IsMatch(text, @"非常|超級|很"))
            weight += 1f;

        // 驚嘆號加權
        if (text.Contains("!"))
            weight += 0.5f;

        return Mathf.Clamp(weight, 1f, 5f);
    }
}