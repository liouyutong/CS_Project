using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    [Header("UI 元件")]
    public GameObject loadingCanvas;
    public Slider progressBar;
    public Text progressText;

    [Header("數值設定")]
    public float minimumDuration = 3f;
    public float fadeDuration = 1.5f;  // 音效淡入淡出的時間

    [Header("音訊設定")]
    public AudioSource loadingAudioSource;

    private AudioSource backgroundMusic;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            loadingCanvas.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        StopAllCoroutines();
        StartCoroutine(LoadAsynchronously(sceneName));
    }

    IEnumerator LoadAsynchronously(string sceneName)
    {
        // 1. 處理音訊：偵測 Main Camera 上的背景音樂並執行「淡出」
        if (Camera.main != null)
        {
            backgroundMusic = Camera.main.GetComponent<AudioSource>();
            if (backgroundMusic != null)
            {
                // 啟動獨立的淡出協程，不干擾主流程
                StartCoroutine(FadeOutMusic(backgroundMusic, fadeDuration));
                Debug.Log("背景音樂開始淡出...");
            }
        }

        // 2. 顯示 Loading UI 並讓 Loading 音效「淡入」
        loadingCanvas.SetActive(true);
        if (loadingAudioSource != null)
        {
            loadingAudioSource.Play();
            StartCoroutine(FadeInMusic(loadingAudioSource, 1.0f));
        }

        // 3. 開始異步載入場景
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        // 4. 強制計時 5 秒
        while (timer < minimumDuration)
        {
            timer += Time.deltaTime;
            float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float visualProgress = Mathf.Min(loadProgress, timer / minimumDuration);

            if (progressBar != null) progressBar.value = visualProgress;
            if (progressText != null) progressText.text = (visualProgress * 100).ToString("F0") + "%";

            yield return null;
        }

        // 5. 5秒時間到，允許跳轉
        operation.allowSceneActivation = true;

        while (!operation.isDone)
        {
            yield return null;
        }

        // 6. 抵達新場景後的清理
        if (loadingAudioSource != null) loadingAudioSource.Stop();
        loadingCanvas.SetActive(false);

        Debug.Log("場景載入完成，Loading 畫面關閉");
    }

    // --- 音訊工具函式 ---

    IEnumerator FadeOutMusic(AudioSource source, float duration)
    {
        float startVolume = source.volume;

        // 逐漸降低音量
        while (source.volume > 0)
        {
            source.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; // 還原初始音量，確保下次讀取該場景時正常
    }

    IEnumerator FadeInMusic(AudioSource source, float duration)
    {
        float targetVolume = 1.0f; // 假設目標音量是 1
        source.volume = 0;

        while (source.volume < targetVolume)
        {
            source.volume += Time.deltaTime / duration;
            yield return null;
        }
        source.volume = targetVolume;
    }
}