using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ModelViewer : MonoBehaviour
{
    [Header("目標對象")]
    public Transform target;

    [Header("旋轉設定")]
    public float rotateSpeed = 0.2f;
    private Vector2 lastMousePos;
    private bool isDragging = false;

    [Header("視角參數設定")]
    public float farFOV = 60f;
    public float farHeight = 3.04f;
    public float closeFOV = 20f;
    public float closeHeight = 3.29f;

    [Header("平滑過渡速度")]
    public float duration = 0.5f;

    private Camera cam;
    private Coroutine transitionCoroutine;

    void Start()
    {
        cam = GetComponent<Camera>();

        // 初始化相機位置
        cam.fieldOfView = farFOV;
        Vector3 pos = transform.position;
        pos.y = farHeight;
        transform.position = pos;

        // 初始化尋找模型
        RefreshTargetFromSwitcher();
        BindButtonActions();
    }

    private void BindButtonActions()
    {
        GameObject farBtnObj = GameObject.Find("Btn_far");
        if (farBtnObj != null)
        {
            Button b = farBtnObj.GetComponent<Button>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(SetFarView);
        }

        GameObject closeBtnObj = GameObject.Find("Btn_close");
        if (closeBtnObj != null)
        {
            Button b = closeBtnObj.GetComponent<Button>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(SetCloseUpView);
        }
    }

    private void RefreshTargetFromSwitcher()
    {
        GenderSwitcher gs = Object.FindAnyObjectByType<GenderSwitcher>();
        if (gs != null && gs.currentActiveModel != null)
        {
            target = gs.currentActiveModel.transform;
        }
    }

    void Update()
    {
        // 如果 target 遺失或被隱藏（例如重建角色時），重新抓取
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            RefreshTargetFromSwitcher();
        }

        // 防止點擊 UI 時觸發旋轉
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            isDragging = false;
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePos = mouse.position.ReadValue();
        }
        if (mouse.leftButton.wasReleasedThisFrame) isDragging = false;

        // 滑鼠拖拽旋轉邏輯
        if (isDragging && target != null)
        {
            Vector2 currentMousePos = mouse.position.ReadValue();
            float deltaX = currentMousePos.x - lastMousePos.x;
            target.Rotate(Vector3.up, -deltaX * rotateSpeed, Space.World);
            lastMousePos = currentMousePos;
        }
    }

    public void SetCloseUpView() => StartTransition(closeHeight, closeFOV, false);

    // 進入聊天或遠景時，強制觸發「回正」旋轉
    public void SetFarView() => StartTransition(farHeight, farFOV, true);

    private void StartTransition(float targetHeight, float targetFOV, bool shouldResetRotation)
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(SmoothMove(targetHeight, targetFOV, shouldResetRotation));
    }

    IEnumerator SmoothMove(float targetHeight, float targetFOV, bool shouldResetRotation)
    {
        float elapsed = 0f;
        float startHeight = transform.position.y;
        float startFOV = cam.fieldOfView;

        // 旋轉重置相關變數
        Quaternion startRotation = (target != null) ? target.rotation : Quaternion.identity;
        Quaternion targetRotation = Quaternion.identity; // 正對鏡頭 (0,0,0)

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curve = Mathf.SmoothStep(0, 1, t); // 平滑曲線

            // 1. 移動相機高度
            Vector3 currentPos = transform.position;
            currentPos.y = Mathf.Lerp(startHeight, targetHeight, curve);
            transform.position = currentPos;

            // 2. 改變相機視角 (FOV)
            cam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, curve);

            // 3. 【新增重點】平滑轉回正面
            if (shouldResetRotation && target != null)
            {
                target.rotation = Quaternion.Slerp(startRotation, targetRotation, curve);
            }

            yield return null;
        }

        // 確保最終值正確
        Vector3 finalPos = transform.position;
        finalPos.y = targetHeight;
        transform.position = finalPos;
        cam.fieldOfView = targetFOV;

        if (shouldResetRotation && target != null)
        {
            target.rotation = targetRotation;
        }
    }

    // 當妳「重新建立角色」按鈕按下時，可以手動呼叫這個方法確保萬無一失
    public void ResetModelToFront()
    {
        if (target != null) target.rotation = Quaternion.identity;
    }
}