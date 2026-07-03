using UnityEngine;

public class ClothesFitter : MonoBehaviour
{
    [Header("目標設定")]
    [Tooltip("請拖入場景中的【主要骨架】(f_ca01_skeleton)")]
    public GameObject mainSkeleton;

    [ContextMenu("執行最終綁定")]
    public void FitClothes()
    {
        if (mainSkeleton == null)
        {
            Debug.LogError("請先在 Inspector 拖入 Main Skeleton (f_ca01_skeleton)！");
            return;
        }

        // 1. 抓取這件衣服物件下的 SkinnedMeshRenderer (即 dress_geo)
        SkinnedMeshRenderer clothesRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        if (clothesRenderer == null)
        {
            Debug.LogError("找不到衣服的 SkinnedMeshRenderer (dress_geo)！");
            return;
        }

        // 2. 獲取主要骨架中所有的骨骼轉向資訊
        Transform[] allTargetBones = mainSkeleton.GetComponentsInChildren<Transform>();

        // 3. 準備新的骨骼容器
        Transform[] newBones = new Transform[clothesRenderer.bones.Length];

        for (int i = 0; i < clothesRenderer.bones.Length; i++)
        {
            string boneName = clothesRenderer.bones[i].name;
            bool found = false;

            foreach (var tBone in allTargetBones)
            {
                if (tBone.name == boneName)
                {
                    newBones[i] = tBone;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning("注意：骨骼 [" + boneName + "] 在主要骨架中找不到對應，保留原樣。");
                newBones[i] = clothesRenderer.bones[i];
            }
        }

        // 4. 重新指定
        clothesRenderer.bones = newBones;

        // 5. 嘗試對齊 Root Bone (通常是 Hips)
        if (clothesRenderer.rootBone != null)
        {
            foreach (var tBone in allTargetBones)
            {
                if (tBone.name == clothesRenderer.rootBone.name)
                {
                    clothesRenderer.rootBone = tBone;
                    break;
                }
            }
        }

        Debug.Log("<color=cyan>【綁定成功】</color> " + clothesRenderer.name + " 現在已連結至主要骨架！");
    }
}