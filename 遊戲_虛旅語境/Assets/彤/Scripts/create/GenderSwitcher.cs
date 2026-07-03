using UnityEngine;

public class GenderSwitcher : MonoBehaviour
{
    public static GenderSwitcher Instance; // 單例模式，方便全域呼叫

    [Header("模型引用")]
    public GameObject maleModel;
    public GameObject femaleModel;

    [Header("系統引用")]
    public FaceAutoGenerator faceAutoGenerator;

    [Header("調色盤串接")]
    public SkinColorConnector skinColorConnector;
    public EyeColorConnector eyeColorConnector;
    public ColorPaletteController colorPalette;

    [Header("裝備系統引用")]
    public CharacterEquipmentHandler equipmentHandler;

    [HideInInspector]
    public GameObject currentActiveModel;

    private void Awake()
    {
        // ====== 1. 防止雙胞胎 (Singleton 邏輯) ======
        // 如果妳想要模型去聊天場景，就需要 DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果場景中已經有一個存在的 Instance，就毀滅這個新的，避免重複
            Destroy(gameObject);
            return;
        }
    }

    // ... 前方 Awake 部分保持不變 ...

    void Start()
    {
        // 稍微延遲一小下（0.1秒），確保 GameDataManager 單例已完全初始化
        Invoke("RefreshGender", 0.1f);
    }

    public void RefreshGender()
    {
        if (GameDataManager.Instance != null && !string.IsNullOrEmpty(GameDataManager.Instance.characterGender))
        {
            string savedGender = GameDataManager.Instance.characterGender;

            // 使用 Trim() 去除可能的空白，並統一判斷邏輯
            if (savedGender.Trim() == "男" || savedGender.Trim() == "Male")
            {
                ShowMale();
            }
            else if (savedGender.Trim() == "女" || savedGender.Trim() == "Female")
            {
                ShowFemale();
            }

            ResetModelVisuals();
            Debug.Log("GenderSwitcher: 已根據存檔加載模型 - " + savedGender);
        }
        else
        {
            // 如果真的沒資料，我們可以選擇關閉所有模型，直到有資料為止
            // 或者維持一個你最想要的預設，但要在 Console 噴出警告方便除錯
            Debug.LogWarning("GenderSwitcher: 找不到大腦中的性別資料，暫時執行預設。");
            ShowFemale();
            ResetModelVisuals();
        }
    }

    private void InitializeCurrentModel(GameObject target)
    {
        currentActiveModel = target;

        // 1. 確保捏臉組件抓到正確的模型目標
        if (faceAutoGenerator != null) faceAutoGenerator.ChangeTarget(target);

        // 2. 刷新渲染器列表
        UpdateGenderParts(target);

        // 3. 強化版相機綁定邏輯
        // 優先找 Camera.main，如果找不到就直接按名稱找（確保萬無一失）
        Camera targetCam = Camera.main;
        if (targetCam == null)
        {
            GameObject camObj = GameObject.Find("Camera_Create"); // 直接用妳場景中的相機名稱
            if (camObj != null) targetCam = camObj.GetComponent<Camera>();
        }

        if (targetCam != null)
        {
            ModelViewer mv = targetCam.GetComponent<ModelViewer>();
            if (mv != null)
            {
                mv.target = target.transform;
                Debug.Log($"成功綁定旋轉目標：{target.name} 到相機：{targetCam.name}");
            }
            else
            {
                Debug.LogError("相機上找不到 ModelViewer 腳本！請檢查 Camera_Create。");
            }
        }
        else
        {
            Debug.LogError("找不到任何有效相機來綁定模型旋轉！");
        }
    }

    public void ShowMale()
    {
        if (maleModel == null || femaleModel == null) return;
        maleModel.SetActive(true);
        femaleModel.SetActive(false);
        InitializeCurrentModel(maleModel);

        // 同步到數據大腦
        if (GameDataManager.Instance != null) GameDataManager.Instance.characterGender = "男";
        if (CharacterDataManager.Instance != null) CharacterDataManager.Instance.selectedGender = 0;
    }

    public void ShowFemale()
    {
        if (maleModel == null || femaleModel == null) return;
        maleModel.SetActive(false);
        femaleModel.SetActive(true);
        InitializeCurrentModel(femaleModel);

        // 同步到數據大腦
        if (GameDataManager.Instance != null) GameDataManager.Instance.characterGender = "女";
        if (CharacterDataManager.Instance != null) CharacterDataManager.Instance.selectedGender = 1;
    }

    public void ApplyColors(GameObject targetModel, Color skin, Color eye)
    {
        if (skinColorConnector == null || eyeColorConnector == null) return;
        UpdateGenderParts(targetModel);
        skinColorConnector.SetColor(skin);
        eyeColorConnector.SetColor(eye);
    }

    public void UpdateGenderParts(GameObject targetModel)
    {
        if (skinColorConnector == null || eyeColorConnector == null) return;

        skinColorConnector.ResetTargets();
        eyeColorConnector.ResetTargets();

        Renderer[] renderers = targetModel.GetComponentsInChildren<Renderer>(true);
        SkinnedMeshRenderer bodySMR = null;

        foreach (var r in renderers)
        {
            if (r.name.Contains("ca01") && r is SkinnedMeshRenderer)
            {
                bodySMR = (SkinnedMeshRenderer)r;
                break;
            }
        }

        foreach (Renderer r in renderers)
        {
            if (!r.gameObject.activeSelf) continue;
            string n = r.name.ToLower();

            if (n.Contains("cloth") && r is SkinnedMeshRenderer clothSMR && bodySMR != null)
            {
                clothSMR.bones = bodySMR.bones;
                clothSMR.rootBone = bodySMR.rootBone;
            }

            skinColorConnector.ClearAndAddTarget(r);
            eyeColorConnector.AddTarget(r);
        }

        // 套用當前存儲的顏色
        if (skinColorConnector != null)
            skinColorConnector.SetColor(skinColorConnector.currentColor);
    }

    public void ResetModelVisuals()
    {
        if (faceAutoGenerator != null) faceAutoGenerator.ResetToDefaultAppearance();
        if (skinColorConnector != null) skinColorConnector.ResetToDefault();
        if (eyeColorConnector != null) eyeColorConnector.ResetToDefault();
        if (equipmentHandler != null) equipmentHandler.ClearAllEquipment();
    }
}