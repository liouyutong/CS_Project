using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("UI 引用 (自動找回)")]
    public GameObject canvasCreate; // 前景 UI (按鈕、拉條)
    public GameObject canvasBack;   // 背景 UI (背景圖)
    public GameObject confirmationPopup;

    [Header("系統引用")]
    public GenderSwitcher genderSwitcher;
    public FaceAutoGenerator faceAutoGenerator;
    public VoicePanelController voicePanelController;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 確保只在捏臉場景執行找回邏輯
        if (scene.name == "CharacterCreationScene")
        {
            StartCoroutine(FindAllUI());
        }
    }

    private IEnumerator FindAllUI()
    {
        // 等待一幀確保場景物件加載完畢
        yield return new WaitForEndOfFrame();

        // 1. 動態找回兩個 Canvas
        if (canvasCreate == null) canvasCreate = GameObject.Find("Canvas_create");
        if (canvasBack == null) canvasBack = GameObject.Find("Canvas_back");

        // 2. 處理前景 UI：只啟動父層，不強制開啟子物件
        if (canvasCreate != null)
        {
            canvasCreate.SetActive(true);
            // 已移除：foreach (Transform child in canvasCreate.transform) child.gameObject.SetActive(true);
            Debug.Log("<color=cyan>成功找回前景 UI，維持預設子物件狀態。</color>");
        }

        // 3. 處理背景 UI：只啟動父層
        if (canvasBack != null)
        {
            canvasBack.SetActive(true);
            // 已移除：foreach (Transform child in canvasBack.transform) child.gameObject.SetActive(true);
            Debug.Log("<color=cyan>成功找回背景 UI。</color>");
        }

        // 4. 重新找回 Popup 並確保它是關閉的 (避免 Prefab 設定錯誤)
        if (confirmationPopup == null && canvasCreate != null)
        {
            // 在 Canvas 下尋找名稱為 "ConfirmationPopup" 的物件 (請確保名稱正確)
            Transform popupT = canvasCreate.transform.Find("ConfirmationPopup");
            if (popupT != null) confirmationPopup = popupT.gameObject;
        }

        if (confirmationPopup != null)
        {
            confirmationPopup.SetActive(false);
        }
    }

    public void OpenConfirmation()
    {
        if (confirmationPopup != null)
            confirmationPopup.SetActive(true);
    }

    public void CloseConfirmation()
    {
        if (confirmationPopup != null)
            confirmationPopup.SetActive(false);
    }

    public void ConfirmAndGoToChat()
    {
        SaveAllData();

        // 隱藏 UI 準備進入聊天室
        HideUI(canvasCreate);
        HideUI(canvasBack);

        SceneController.instance.LoadScene("ChatScene");
    }

    private void HideUI(GameObject canvasObj)
    {
        if (canvasObj != null)
        {
            // 這裡保留子物件隱藏，是因為我們要保留 Canvas 物件上的腳本運作
            foreach (Transform child in canvasObj.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void SaveAllData()
    {
        if (CharacterDataManager.Instance != null && genderSwitcher != null)
        {
            CharacterDataManager.Instance.selectedGender = genderSwitcher.maleModel.activeSelf ? 0 : 1;
            // ... 在此繼續妳的其他儲存邏輯 ...
        }
    }
}