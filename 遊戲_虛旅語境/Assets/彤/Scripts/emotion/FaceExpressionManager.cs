using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FaceExpressionManager : MonoBehaviour
{
    [Header("組件設定")]
    public SkinnedMeshRenderer faceMesh;
    public float transitionDuration = 0.3f;

    [System.Serializable]
    public class ExpressionEntry
    {
        public string name;
        public TextAsset jsonFile;
    }

    [Header("表情庫")]
    public List<ExpressionEntry> expressionLibrary;

    private Coroutine currentTransition;

    void Awake()
    {
        if (faceMesh == null)
        {
            faceMesh = GetComponent<SkinnedMeshRenderer>();
            if (faceMesh == null) faceMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        }
    }

    public void PlayExpression(string expressionName)
    {
        ExpressionEntry entry = expressionLibrary.Find(e => e.name == expressionName);
        if (entry != null)
        {
            if (currentTransition != null) StopCoroutine(currentTransition);
            currentTransition = StartCoroutine(ApplyExpressionRoutine(entry.jsonFile.text));
        }
        else
        {
            Debug.LogWarning($"[FaceManager] 找不到表情資源: {expressionName}");
        }
    }

    // --- 核心過渡邏輯 ---
    IEnumerator ApplyExpressionRoutine(string jsonText)
    {
        if (faceMesh == null) yield break;

        // targetWeights 只會存放「名稱包含 Expressions_」的 BlendShapes
        Dictionary<int, float> targetWeights = new Dictionary<int, float>();
        Dictionary<int, float> startWeights = new Dictionary<int, float>();

        // 1. 初始化：掃描所有 BlendShapes，找出所有「表情類」，預設目標為 0
        int totalCount = faceMesh.sharedMesh.blendShapeCount;
        for (int i = 0; i < totalCount; i++)
        {
            string shapeName = faceMesh.sharedMesh.GetBlendShapeName(i);
            if (shapeName.Contains("Expressions_"))
            {
                targetWeights[i] = 0f;
                startWeights[i] = faceMesh.GetBlendShapeWeight(i);
            }
        }

        // 2. 解析 JSON 並根據 0.5 中點映射到 _max / _min
        string[] lines = jsonText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            if (line.Contains("Expressions_"))
            {
                try
                {
                    string cleanLine = line.Trim().Replace("\"", "").Replace(",", "");
                    string[] parts = cleanLine.Split(':');
                    string baseKey = parts[0].Trim();
                    float rawVal = float.Parse(parts[1].Trim());

                    string targetMaxKey = baseKey + "_max";
                    string targetMinKey = baseKey + "_min";

                    float maxWeight = 0, minWeight = 0;
                    if (rawVal > 0.5f) maxWeight = (rawVal - 0.5f) * 2f * 100f;
                    else if (rawVal < 0.5f) minWeight = (0.5f - rawVal) * 2f * 100f;

                    // 更新字典裡的目標值
                    SetWeightInDictionary(targetMaxKey, maxWeight, targetWeights);
                    SetWeightInDictionary(targetMinKey, minWeight, targetWeights);
                }
                catch (System.Exception e) { Debug.LogError($"解析失敗: {line}, {e.Message}"); }
            }
        }

        // 3. 執行過渡動畫 (平滑 Lerp)
        float elapsed = 0;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / transitionDuration);

            foreach (var item in targetWeights)
            {
                if (!Mathf.Approximately(startWeights[item.Key], item.Value))
                {
                    float current = Mathf.Lerp(startWeights[item.Key], item.Value, t);
                    faceMesh.SetBlendShapeWeight(item.Key, current);
                }
            }
            yield return null;
        }

        // 4. 最後確認數值精準
        foreach (var item in targetWeights)
        {
            faceMesh.SetBlendShapeWeight(item.Key, item.Value);
        }
    }

    // 輔助函式：確保只修改已存在於 targetWeights（即符合前綴）的 Index
    void SetWeightInDictionary(string key, float weight, Dictionary<int, float> targets)
    {
        int index = faceMesh.sharedMesh.GetBlendShapeIndex(key);

        // 模糊匹配結尾
        if (index == -1)
        {
            for (int i = 0; i < faceMesh.sharedMesh.blendShapeCount; i++)
            {
                if (faceMesh.sharedMesh.GetBlendShapeName(i).EndsWith(key))
                {
                    index = i;
                    break;
                }
            }
        }

        // 核心保護：只有在第一步初始化時被加入 targets (符合 Expressions_ 前綴) 的 Index 才會被賦值
        if (index != -1 && targets.ContainsKey(index))
        {
            targets[index] = weight;
        }
    }
}