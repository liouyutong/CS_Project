using UnityEngine;
using System.Collections.Generic;

public class EyeColorConnector : MonoBehaviour
{
    public List<Renderer> eyeRenderers = new List<Renderer>();

    // 增加此屬性，讓 DataManager 存檔時可以抓到顏色
    public Color currentColor = Color.white;

    // 統一方法名稱為 SetColor
    public void SetColor(Color newColor)
    {
        currentColor = newColor; // 紀錄當前顏色

        if (eyeRenderers.Count == 0) return;

        foreach (Renderer r in eyeRenderers)
        {
            if (r == null) continue;
            Material[] allMaterials = r.materials;
            bool changed = false;

            foreach (Material m in allMaterials)
            {
                if (m == null) continue;
                // 根據你提供的 eyeClr 命名
                if (m.name.Contains("eyeClr"))
                {
                    if (m.HasProperty("_BaseColor")) { m.SetColor("_BaseColor", newColor); changed = true; }
                    else if (m.HasProperty("_Color")) { m.SetColor("_Color", newColor); changed = true; }
                }
            }
            if (changed) r.materials = allMaterials;
        }
    }

    // 為了相容你現有的色盤呼叫
    public void SetModelColor(Color newColor) => SetColor(newColor);

    public void ResetToDefault()
    {
        // 呼叫原本就有的 SetColor，傳入白色 (或是妳想要的預設膚色)
        SetColor(Color.white);
        Debug.Log("EyeColorConnector: 已恢復預設顏色");
    }

    public void ResetTargets() => eyeRenderers.Clear();

    public void AddTarget(Renderer newRenderer)
    {
        if (newRenderer != null && !eyeRenderers.Contains(newRenderer))
            eyeRenderers.Add(newRenderer);
    }
}