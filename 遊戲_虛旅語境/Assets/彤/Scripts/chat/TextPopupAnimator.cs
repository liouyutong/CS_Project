using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(TMP_Text))]
public class TextPopupAnimator : MonoBehaviour
{
    private TMP_Text textComponent;
    private TMP_TextInfo textInfo;

    [Header("彈跳設定")]
    public float popupHeight = 15.0f;
    public float popupDuration = 0.2f;
    public float delayBetweenChars = 0.1f;
    public float loopDelay = 0.5f;

    private List<Vector3[]> originalVertices = new List<Vector3[]>();

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    // --- 關鍵修改處：將 Start 改為 OnEnable ---
    private void OnEnable()
    {
        // 每次 Loading 畫面顯示時，重新啟動動畫
        StopAllCoroutines(); // 保險起見，先停止所有舊的防止重複
        StartCoroutine(AnimatePopupSequence());
    }

    // 當物件被隱藏時，清理資料（選配，增加穩定性）
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator AnimatePopupSequence()
    {
        // 等待一幀確保 TMP 已經抓到文字資訊
        yield return null;

        while (true)
        {
            textComponent.ForceMeshUpdate();
            textInfo = textComponent.textInfo;

            originalVertices.Clear();
            int materialCount = textInfo.materialCount;
            for (int i = 0; i < materialCount; i++)
            {
                originalVertices.Add((Vector3[])textInfo.meshInfo[i].vertices.Clone());
            }

            int characterCount = textInfo.characterCount;
            for (int charIndex = 0; charIndex < characterCount; charIndex++)
            {
                var charInfo = textInfo.characterInfo[charIndex];
                if (!charInfo.isVisible) continue;

                yield return StartCoroutine(PopupSingleCharacter(charIndex));
                yield return new WaitForSeconds(delayBetweenChars);
            }

            yield return new WaitForSeconds(loopDelay);
        }
    }

    private IEnumerator PopupSingleCharacter(int charIndex)
    {
        // 這裡需要重新抓取資訊，因為 ForceMeshUpdate 可能改變了索引
        var charInfo = textComponent.textInfo.characterInfo[charIndex];
        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Vector3[] sourceVertices = originalVertices[materialIndex];
        Vector3[] meshVertices = textComponent.textInfo.meshInfo[materialIndex].vertices;

        float elapsedTime = 0f;

        while (elapsedTime < popupDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.PingPong(elapsedTime / (popupDuration / 2.0f), 1.0f);
            float offsetY = normalizedTime * popupHeight;

            for (int v = 0; v < 4; v++)
            {
                meshVertices[vertexIndex + v].y = sourceVertices[vertexIndex + v].y + offsetY;
            }

            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
            yield return null;
        }

        // 回歸原位
        for (int v = 0; v < 4; v++)
        {
            meshVertices[vertexIndex + v].y = sourceVertices[vertexIndex + v].y;
        }
        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
    }
}