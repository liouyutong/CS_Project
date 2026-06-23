using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class FaceAutoGenerator : MonoBehaviour
{
    [Header("目標設定")]
    public SkinnedMeshRenderer targetMesh;

    [Header("UI 模板與容器")]
    public GameObject sliderPrefab;
    public GameObject categoryBtnPrefab;
    public Transform contentParent;
    public Transform categoryParent;

    [Header("歷史管理")]
    public FaceHistoryManager historyManager;

    // 狀態控制：在 ChatScene 中請將此設為 True
    [Header("場景模式")]
    public bool isChatMode = false;

    private string[] allFeatures = {
        "eyebrowPosX", "eyebrowPosZ", "eyebrowRotate", "eyebrowSizeX",
        "eyePosX", "eyePosZ", "eyeSizeX", "eyeSizeZ", "eyeRotate",
        "eyeballPosX", "eyeballPosZ", "eyeballSize",
        "nosePosY", "nosePosZ", "noseSizeX", "noseSizeZ",
        "mouthPosZ", "mouthSizeX", "mouthSizeZ",
        "earPosY", "earPosZ", "earSize",
        "cheekUpPosX", "cheekUpPosZ", "cheekDownPosX", "cheekDownPosZ",
        "jawPosX", "jawPosZ", "neckPos", "neckSize",
        "shoulderWid", "chestWid", "waistWid"
    };

    private Dictionary<string, string> featureTranslation = new Dictionary<string, string>()
    {
        // 眉毛 (Eyebrow)
        { "eyebrowPosX", "左右" },
        { "eyebrowPosZ", "上下" },
        { "eyebrowRotate", "傾斜角度" },
        { "eyebrowSizeX", "長度" },

        // 眼睛 (Eye)
        { "eyePosX", "左右" },
        { "eyePosZ", "上下" },
        { "eyeSizeX", "寬度" },
        { "eyeSizeZ", "高度" },
        { "eyeRotate", "傾斜角度" },

        // 瞳孔 (Eyeball)
        { "eyeballPosX", "左右" },
        { "eyeballPosZ", "上下" },
        { "eyeballSize", "大小" },

        // 鼻子 (Nose)
        { "nosePosY", "前後" },
        { "nosePosZ", "上下" },
        { "noseSizeX", "寬度" },
        { "noseSizeZ", "高度" },

        // 嘴巴 (Mouth)
        { "mouthPosZ", "上下" },
        { "mouthSizeX", "寬度" },
        { "mouthSizeZ", "厚度" },

        // 耳朵 (Ear)
        { "earPosY", "前後" },
        { "earPosZ", "上下" },
        { "earSize", "大小" },

        // 臉頰與下顎 (Cheek & Jaw)
        { "cheekUpPosX", "臉頰寬度" },
        { "cheekUpPosZ", "臉頰上下" },
        { "cheekDownPosX", "下頷寬度" },
        { "cheekDownPosZ", "下頷上下" },
        { "jawPosX", "下巴寬度" },
        { "jawPosZ", "下巴上下" },

        // 脖子與身體 (Neck & Body)
        { "neckPos", "脖子長短" },
        { "neckSize", "脖子粗細" },
        { "shoulderWid", "肩膀寬度" },
        { "chestWid", "胸部寬度" },
        { "waistWid", "腰部寬度" }
    };

    // 建立分類翻譯字典
    private Dictionary<string, string> categoryTranslation = new Dictionary<string, string>()
    {
        { "eyebrow", "眉毛" },
        { "eye", "眼睛" },
        { "eyeball", "眼球" },
        { "nose", "鼻子" },
        { "mouth", "嘴巴" },
        { "ear", "耳朵" },
        { "face", "臉部" },
        { "body", "軀幹" }
    };

    private Dictionary<string, List<GameObject>> categoryGroups = new Dictionary<string, List<GameObject>>();
    private List<Button> allCategoryButtons = new List<Button>();
    public List<Slider> allSliders = new List<Slider>();

    public Color selectedColor = new Color(0.855f, 0.98f, 1f);
    public Color normalColor = Color.white;

    void Start()
    {
        // 如果是聊天模式，徹底跳過 UI 生成邏輯
        if (isChatMode)
        {
            Debug.Log("FaceAutoGenerator: 檢測到聊天模式，已跳過 UI 生成。");
            return;
        }
        GenerateSystem();
    }

    public void GenerateSystem()
    {
        if (contentParent == null || categoryParent == null) return;

        foreach (Transform child in contentParent) Destroy(child.gameObject);
        foreach (Transform child in categoryParent) Destroy(child.gameObject);

        allCategoryButtons.Clear();
        allSliders.Clear();
        categoryGroups.Clear();

        HashSet<string> categories = new HashSet<string>();
        foreach (string f in allFeatures) categories.Add(GetCategoryName(f));

        foreach (string cat in categories)
        {
            GameObject btnObj = Instantiate(categoryBtnPrefab, categoryParent);
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            // 在生成按鈕時修改：
            if (btnText != null)
            {
                if (categoryTranslation.ContainsKey(cat))
                    btnText.text = categoryTranslation[cat];
                else
                    btnText.text = char.ToUpper(cat[0]) + cat.Substring(1);
            }
            Button b = btnObj.GetComponent<Button>();
            allCategoryButtons.Add(b);
            string capturedCat = cat;
            b.onClick.AddListener(() => { ShowCategory(capturedCat); UpdateButtonColors(b); });
        }

        foreach (string feature in allFeatures)
        {
            string cat = GetCategoryName(feature);
            GameObject newSliderObj = Instantiate(sliderPrefab, contentParent);

            newSliderObj.name = feature;

            TMP_Text label = newSliderObj.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                // 如果字典裡有翻譯就用中文，沒有的話就顯示原始英文（防止漏寫報錯）
                if (featureTranslation.ContainsKey(feature))
                    label.text = featureTranslation[feature];
                else
                    label.text = feature;
            }
            Slider s = newSliderObj.GetComponent<Slider>();
            s.minValue = -100f; s.maxValue = 100f; s.value = 0f;
            allSliders.Add(s);
            string fName = feature;
            s.onValueChanged.AddListener((val) => { UpdateMeshBlendShapeByName(fName, val); });
            AddHistoryTrigger(s);
            if (!categoryGroups.ContainsKey(cat)) categoryGroups[cat] = new List<GameObject>();
            categoryGroups[cat].Add(newSliderObj);
        }

        if (allCategoryButtons.Count > 0)
        {
            ShowCategory(GetCategoryName(allFeatures[0]));
            UpdateButtonColors(allCategoryButtons[0]);
        }
    }

    // 補回缺失的方法，解決 CS1061 錯誤
    public void ChangeTarget(GameObject newModel)
    {
        if (newModel == null) return;
        targetMesh = newModel.GetComponentInChildren<SkinnedMeshRenderer>();
        Debug.Log($"FaceAutoGenerator: 目標已切換至 {newModel.name}");

        // 如果在聊天模式，切換目標後應直接套用數據（如果有的話）
        if (isChatMode && CharacterDataManager.Instance != null)
        {
            foreach (var record in CharacterDataManager.Instance.faceShapeData)
            {
                UpdateMeshBlendShapeByName(record.Key, record.Value);
            }
        }
    }

    public void UpdateMeshBlendShapeByName(string featureName, float val)
    {
        string maxN = featureName + "_max";
        string minN = featureName + "_min";
        UpdateMeshBlendShape(maxN, minN, val);
    }

    private void UpdateMeshBlendShape(string maxN, string minN, float val)
    {
        if (targetMesh == null || targetMesh.sharedMesh == null) return;
        int maxIdx = targetMesh.sharedMesh.GetBlendShapeIndex(maxN);
        int minIdx = targetMesh.sharedMesh.GetBlendShapeIndex(minN);
        if (val >= 0)
        {
            if (maxIdx != -1) targetMesh.SetBlendShapeWeight(maxIdx, val);
            if (minIdx != -1) targetMesh.SetBlendShapeWeight(minIdx, 0);
        }
        else
        {
            if (maxIdx != -1) targetMesh.SetBlendShapeWeight(maxIdx, 0);
            if (minIdx != -1) targetMesh.SetBlendShapeWeight(minIdx, -val);
        }
    }

    private void AddHistoryTrigger(Slider s)
    {
        float oldValue = s.value;
        EventTrigger trigger = s.gameObject.GetComponent<EventTrigger>() ?? s.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener((data) => { oldValue = s.value; });
        trigger.triggers.Add(pointerDown);
        EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((data) => {
            if (historyManager != null && !Mathf.Approximately(oldValue, s.value))
                historyManager.RecordAction(s, oldValue, s.value);
        });
        trigger.triggers.Add(pointerUp);
    }

    string GetCategoryName(string name)
    {
        if (name.Contains("eyebrow")) return "eyebrow";
        if (name.Contains("eyeball")) return "eyeball";
        if (name.Contains("eye")) return "eye";
        if (name.Contains("nose")) return "nose";
        if (name.Contains("mouth")) return "mouth";
        if (name.Contains("ear")) return "ear";
        if (name.Contains("cheek") || name.Contains("jaw")) return "face";
        return "body";
    }

    public void ShowCategory(string categoryName)
    {
        if (isChatMode) return;
        foreach (var group in categoryGroups)
            foreach (GameObject obj in group.Value)
                obj.SetActive(group.Key == categoryName);
    }

    void UpdateButtonColors(Button selectedBtn)
    {
        if (isChatMode) return;
        foreach (Button btn in allCategoryButtons)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = cb.selectedColor = (btn == selectedBtn) ? selectedColor : normalColor;
            btn.colors = cb;
        }
    }

    public void SetSliderValueByName(string sliderName, float value)
    {
        UpdateMeshBlendShapeByName(sliderName, value);
    }

    public void SaveAllFaceDataToManager()
    {
        if (CharacterDataManager.Instance == null) return;
        CharacterDataManager.Instance.faceShapeData.Clear();

        // 依照 allFeatures 的順序去抓對應的 Slider 數值
        for (int i = 0; i < allFeatures.Length; i++)
        {
            string englishName = allFeatures[i];
            float val = allSliders[i].value;
            CharacterDataManager.Instance.faceShapeData[englishName] = val;
        }
    }

    public void ResetAllSliders() { if (historyManager != null) historyManager.ResetAll(allSliders); }

    // 加入 FaceAutoGenerator.cs 中
    public void ResetToDefaultAppearance()
    {
        if (targetMesh == null || targetMesh.sharedMesh == null) return;

        // 1. 將模型權重全部歸零
        foreach (string feature in allFeatures)
        {
            int maxIdx = targetMesh.sharedMesh.GetBlendShapeIndex(feature + "_max");
            int minIdx = targetMesh.sharedMesh.GetBlendShapeIndex(feature + "_min");
            if (maxIdx != -1) targetMesh.SetBlendShapeWeight(maxIdx, 0);
            if (minIdx != -1) targetMesh.SetBlendShapeWeight(minIdx, 0);
        }

        // 2. 將 UI Slider 全部撥回 0
        foreach (Slider s in allSliders)
        {
            s.value = 0f;
        }

        // 3. 【新增】同步清空數據管理器中的字典，防止舊數據殘留
        if (CharacterDataManager.Instance != null)
        {
            CharacterDataManager.Instance.faceShapeData.Clear();
        }

        Debug.Log("FaceAutoGenerator: 臉部外觀與數據已完全重置。");
    }
}