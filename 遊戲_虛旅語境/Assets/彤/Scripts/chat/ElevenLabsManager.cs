using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class TTSData
{
    public string text;
    public string model_id = "eleven_multilingual_v2";
}

public class ElevenLabsManager : MonoBehaviour
{
    public static ElevenLabsManager Instance;

    [Header("API 設定")]
    public string apiKey = "你的_API_KEY_放這裡";

    [Serializable]
    public class VoiceSet
    {
        public string label; // 方便你在 Inspector 識別
        public string veryCold;      // 非常高冷 (0)
        public string cold;          // 較高冷 (1)
        public string neutral;       // 中間 (2)
        public string warm;          // 較熱情 (3)
        public string veryWarm;      // 非常熱情 (4)

        public string GetIDByPersonality(int pIndex)
        {
            return pIndex switch
            {
                0 => veryCold,
                1 => cold,
                2 => neutral,
                3 => warm,
                4 => veryWarm,
                _ => neutral
            };
        }
    }

    [Header("--- 女性語音矩陣 (f) ---")]
    public VoiceSet f_young;      // 對應年輕
    public VoiceSet f_middle;     // 對應中間
    public VoiceSet f_mature;     // 對應成熟

    [Header("--- 男性語音矩陣 (m) ---")]
    public VoiceSet m_young;
    public VoiceSet m_middle;
    public VoiceSet m_mature;

    [Header("音頻播放器")]
    public AudioSource voiceSource;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    /// <summary>
    /// gender: 0 男, 1 女
    /// personalityIndex: 0~4
    /// ageGroupIndex: 0(年輕), 1(中間), 2(成熟)
    /// </summary>
    private string DetermineVoiceId(int gender, int personalityIndex, int ageGroupIndex)
    {
        if (gender == 1) // 女性
        {
            return ageGroupIndex switch
            {
                0 => f_young.GetIDByPersonality(personalityIndex),
                1 => f_middle.GetIDByPersonality(personalityIndex),
                2 => f_mature.GetIDByPersonality(personalityIndex),
                _ => f_middle.GetIDByPersonality(personalityIndex)
            };
        }
        else // 男性
        {
            return ageGroupIndex switch
            {
                0 => m_young.GetIDByPersonality(personalityIndex),
                1 => m_middle.GetIDByPersonality(personalityIndex),
                2 => m_mature.GetIDByPersonality(personalityIndex),
                _ => m_middle.GetIDByPersonality(personalityIndex)
            };
        }
    }

    public IEnumerator RequestAndPlaySpeech(string text, int gender, int personality, int ageGroup)
    {
        string selectedId = DetermineVoiceId(gender, personality, ageGroup);

        if (string.IsNullOrEmpty(selectedId))
        {
            Debug.LogError("無法找到對應的 Voice ID，請檢查 Inspector 設定！");
            yield break;
        }

        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{selectedId}";
        TTSData data = new TTSData { text = text };
        string jsonString = JsonUtility.ToJson(data);
        byte[] jsonBody = System.Text.Encoding.UTF8.GetBytes(jsonString);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(jsonBody);
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                voiceSource.clip = clip;
                voiceSource.Play();
            }
            else
            {
                Debug.LogError($"ElevenLabs Error: {request.error} | {request.downloadHandler.text}");
            }
        }
    }
}