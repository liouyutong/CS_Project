using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class FaceHistoryManager : MonoBehaviour
{
    // 用來儲存動作的類別
    [System.Serializable]
    public class FaceAction
    {
        public Slider slider;
        public float oldValue;
        public float newValue;
    }

    private Stack<FaceAction> undoStack = new Stack<FaceAction>();
    private Stack<FaceAction> redoStack = new Stack<FaceAction>();
    private bool isInternalChange = false; // 防止 Undo 時觸發新的紀錄

    // 紀錄一個新的動作
    public void RecordAction(Slider slider, float oldVal, float newVal)
    {
        if (isInternalChange) return;

        FaceAction action = new FaceAction { slider = slider, oldValue = oldVal, newValue = newVal };
        undoStack.Push(action);
        redoStack.Clear(); // 只要有新動作，就清空重做棧
        Debug.Log("已紀錄動作：" + slider.gameObject.name);
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            isInternalChange = true;
            FaceAction action = undoStack.Pop();
            redoStack.Push(action);
            action.slider.value = action.oldValue;
            isInternalChange = false;
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            isInternalChange = true;
            FaceAction action = redoStack.Pop();
            undoStack.Push(action);
            action.slider.value = action.newValue;
            isInternalChange = false;
        }
    }

    // 一鍵重置
    public void ResetAll(List<Slider> allSliders)
    {
        isInternalChange = true;
        foreach (var slider in allSliders)
        {
            slider.value = 0f; // 假設 0 是你的中間初始值
        }
        undoStack.Clear();
        redoStack.Clear();
        isInternalChange = false;
        Debug.Log("所有數值已重置");
    }
}