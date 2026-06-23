using UnityEngine;
using System.Collections.Generic; // 必須引用，才能使用 Dictionary

public class CharacterDataManager : MonoBehaviour
{
    public static CharacterDataManager Instance;

    [Header("基礎數據")]
    public int selectedGender; // 0: Male, 1: Female
    public int outfitIndex = -1;
    public int hairIndex = -1;
    public Color skinColor = Color.white;
    public Color eyeColor = Color.white;

    [Header("臉部微調數據")]
    // Key 儲存 Slider 的名字 (如 "eyeSizeX")，Value 儲存數值 (-100 ~ 100)
    public Dictionary<string, float> faceShapeData = new Dictionary<string, float>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetAllData()
    {
        // 1. 重置基礎索引
        selectedGender = 0; // 預設回男生
        outfitIndex = -1;
        hairIndex = -1;

        // 2. 重置顏色 (回到預設白色或妳自訂的膚色)
        skinColor = Color.white;
        eyeColor = Color.white;

        // 3. 【最關鍵】清空臉部微調的字典數據
        if (faceShapeData != null)
        {
            faceShapeData.Clear();
        }

        Debug.Log("<color=yellow>[CharacterDataManager]</color> 數據已完全重置！");
    }
}