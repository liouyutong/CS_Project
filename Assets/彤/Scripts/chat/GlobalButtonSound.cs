using UnityEngine;
using UnityEngine.UI;
using System.Collections; // 必須引用此命名空間才能使用協程

public class GlobalButtonSound : MonoBehaviour
{
    public AudioSource sfxSource;
    public AudioClip clickClip;
    public float playDuration = 0.5f; // 想要播放的長度（秒）

    void Start()
    {
        Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button btn in allButtons)
        {
            // 修改：點擊時啟動協程
            btn.onClick.AddListener(() => StartCoroutine(PlayAndStop()));
        }
    }

    // 使用協程控制播放與停止
    IEnumerator PlayAndStop()
    {
        if (sfxSource != null && clickClip != null)
        {
            sfxSource.clip = clickClip; // 先指定音訊檔
            sfxSource.Play();           // 開始播放

            yield return new WaitForSeconds(playDuration); // 等待設定的時間

            sfxSource.Stop(); // 強制停止
        }
    }
}