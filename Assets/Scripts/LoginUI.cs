using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class LoginUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameInputField;
    public TMP_Text warningText;

    private const string PLAYER_ID_KEY = "PLAYER_ID";

    private void Start()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);

        Invoke(nameof(FocusInputField), 0.05f);
    }

    private void Update()
    {
        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == null)
        {
            FocusInputField();
        }
    }

    public void OnClickStart()
    {
        string playerName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            ShowWarning("未輸入名字，無法開始");
            return;
        }

        // 取得或產生簡短 Player ID
        string playerId;

        if (PlayerPrefs.HasKey(PLAYER_ID_KEY))
        {
            playerId = PlayerPrefs.GetString(PLAYER_ID_KEY);
        }
        else
        {
            playerId = GenerateShortId();
            PlayerPrefs.SetString(PLAYER_ID_KEY, playerId);
            PlayerPrefs.Save();
        }

        // 建立帳號
        AccountManager.Instance.CreateAccount(playerId, playerName);

        // 存到 GameDataManager
        GameDataManager.Instance.playerId = playerId;
        GameDataManager.Instance.playerName = playerName;

        SceneController.instance.LoadScene("CharacterSetupScene");
    }

    // 生成 5 碼英數混合 ID
    private string GenerateShortId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        string id = "";

        for (int i = 0; i < 5; i++)
        {
            id += chars[random.Next(chars.Length)];
        }

        return id;
    }

    private void ShowWarning(string msg)
    {
        if (warningText == null) return;

        warningText.text = msg;
        warningText.color = Color.red;
        warningText.gameObject.SetActive(true);

        FocusInputField();
    }

    private void FocusInputField()
    {
        if (nameInputField == null) return;
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(nameInputField.gameObject);

        nameInputField.interactable = true;
        nameInputField.ActivateInputField();
        nameInputField.Select();
    }
}