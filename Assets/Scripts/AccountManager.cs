using UnityEngine;

public class AccountManager : MonoBehaviour
{
    public static AccountManager Instance;

    public AccountData CurrentAccount;

    private const string SAVE_KEY = "ACCOUNT_DATA";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAccount();
    }

    // 建立帳號（LoginScene 用）
    public void CreateAccount(string userId,string playerName)
    {
        CurrentAccount = new AccountData();
        CurrentAccount.userId = userId;
        CurrentAccount.playerName = playerName;

        SaveAccount();
    }

    // 儲存資料
    public void SaveAccount()
    {
        if (CurrentAccount == null) return;

        string json = JsonUtility.ToJson(CurrentAccount);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    // 讀取資料
    public void LoadAccount()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        CurrentAccount = JsonUtility.FromJson<AccountData>(json);
    }

    public bool HasAccount()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    public void ClearAccount()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        CurrentAccount = null;
    }
}
