using UnityEngine;
using System.Collections.Generic;

public class SkinColorConnector : MonoBehaviour
{
    [Header("顏色清單")]
    public List<Renderer> bodyRenderers = new List<Renderer>();

    [Header("預設膚色設定")]
    // 這裡設為 public，之後妳在 Inspector 也能手動微調
    public Color defaultSkinColor;
    public Color currentColor = Color.white;

    private void Awake()
    {
        // 在 Awake 時將妳指定的 Hex Code 轉換為 Unity Color
        // #FFC8AB 是一個很漂亮的粉嫩色
        if (ColorUtility.TryParseHtmlString("#FFC8AB", out Color skinColor))
        {
            defaultSkinColor = skinColor;
        }
        else
        {
            // 如果轉換失敗的保險設定
            defaultSkinColor = new Color(1f, 0.78f, 0.67f);
        }
    }

    void Start()
    {
        // 進入場景時，預設直接套用這個膚色
        SetColor(defaultSkinColor);
    }

    // 統一方法名稱為 SetColor，供外部調用
    public void SetColor(Color newColor)
    {
        currentColor = newColor; // 紀錄當前顏色

        if (bodyRenderers.Count == 0) return;

        foreach (Renderer r in bodyRenderers)
        {
            if (r == null) continue;

            // 取得所有材質
            Material[] allMaterials = r.materials;
            foreach (Material m in allMaterials)
            {
                // 只針對名稱包含 "body" 的材質球進行變色
                if (m.name.ToLower().Contains("body"))
                {
                    if (m.HasProperty("_BaseColor"))
                        m.SetColor("_BaseColor", newColor); // URP 常用屬性
                    else if (m.HasProperty("_Color"))
                        m.SetColor("_Color", newColor);      // Built-in 常用屬性
                }
            }
            // 必須重新賦值，修改才會生效
            r.materials = allMaterials;
        }
    }

    // 為了相容妳舊有的色盤呼叫
    public void SetModelColor(Color newColor) => SetColor(newColor);

    public void ResetToDefault()
    {
        // 重置時回歸到 FFC8AB
        SetColor(defaultSkinColor);
        Debug.Log("<color=yellow>SkinColorConnector: 已恢復預設膚色 #FFC8AB</color>");
    }

    public void ClearAndAddTarget(Renderer newRenderer)
    {
        if (newRenderer != null && !bodyRenderers.Contains(newRenderer))
            bodyRenderers.Add(newRenderer);
    }

    public void ResetTargets() => bodyRenderers.Clear();
}