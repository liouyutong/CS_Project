using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable] public class ColorChangeEvent : UnityEvent<Color> { }
[System.Serializable] public class HueChangeEvent : UnityEvent<float> { }

public class ColorPaletteController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    public enum ColorTargetMode { Skin, Eyes }
    public ColorTargetMode currentMode = ColorTargetMode.Skin;
    public SkinColorConnector skinConnector;
    public EyeColorConnector eyeConnector;

    [Header("UI 引用")]
    [SerializeField] RectTransform picker;
    [SerializeField] Image pickedColorImage;
    [SerializeField] Material colorWheelMat;

    [Header("色盤參數設定")]
    [SerializeField] int totalNumberofColors = 128;
    [SerializeField] int wheelsCount = 1;
    [SerializeField][Range(0, 360)] float startingAngle = 0;

    private float currentHue = 0;
    private float currentRadialS = 0.5f;
    private Vector2 centerPoint;
    private float paletteRadius;
    private float pickerHueOffset;
    private bool dragging = false;

    private Color selectedColor;
    public Color SelectedColor
    {
        get => selectedColor;
        private set { if (value != selectedColor) { selectedColor = value; OnColorChange?.Invoke(selectedColor); } }
    }
    public float Value { get; set; } = 1f;
    public float Saturation { get; set; } = 1f;
    public float Hue { get => currentHue; private set => currentHue = value; }

    public ColorChangeEvent OnColorChange;
    public HueChangeEvent OnHueChange;

    void Awake()
    {
        CalculatePresets();
        UpdateMaterialInitialValues();

        Vector2 dir = (Vector2)picker.localPosition;
        float angle = Vector2.SignedAngle(Vector2.right, dir);
        currentHue = (angle < 0 ? angle + 360 : angle) / 360f;
        currentRadialS = Mathf.Clamp01(dir.magnitude / paletteRadius);

        UpdateMaterial();
        //UpdateColor();
    }

    public void SetTargetMode(int modeIndex)
    {
        currentMode = (ColorTargetMode)modeIndex;
        //UpdateColor();
    }

    public void UpdateColor()
    {
        float shiftedH = (pickerHueOffset + (currentHue / wheelsCount)) % 1.0f;
        float discretedH = Mathf.Floor(shiftedH * totalNumberofColors) / (totalNumberofColors - 1.0f);

        float finalVal = 1.0f, finalSat = 1.0f;

        if (currentRadialS <= 0.7f)
        {
            float t = Mathf.InverseLerp(0.01f, 0.7f, currentRadialS);
            finalVal = Mathf.Lerp(0.0f, 1.0f, Mathf.Pow(t, 0.4f));
            finalSat = 1.0f;
        }
        else
        {
            float t = Mathf.InverseLerp(0.7f, 0.98f, currentRadialS);
            finalVal = 1.0f;
            finalSat = Mathf.Lerp(1.0f, 0.35f, t);
        }

        Color color = Color.HSVToRGB(discretedH, finalSat, finalVal);
        if (pickedColorImage) pickedColorImage.color = color;
        SelectedColor = color;

        if (currentMode == ColorTargetMode.Skin && skinConnector != null) skinConnector.SetModelColor(color);
        else if (currentMode == ColorTargetMode.Eyes && eyeConnector != null) eyeConnector.SetModelColor(color);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        Vector2 mousePos = eventData.position;
        Vector2 dirFromCenter = mousePos - centerPoint;

        // --- 核心修正：將角度取反，使旋轉方向與鼠標同步 ---
        float angle = -Vector2.SignedAngle(Vector2.right, dirFromCenter);
        currentHue = (angle < 0 ? angle + 360 : angle) / 360f;
        OnHueChange?.Invoke(currentHue);

        float dist = dirFromCenter.magnitude;
        currentRadialS = Mathf.Clamp01(dist / paletteRadius);

        UpdateMaterial();
        UpdateColor();
    }

    public void UpdateMaterial()
    {
        if (colorWheelMat == null) return;
        colorWheelMat.SetFloat("_Hue", currentHue);
        colorWheelMat.SetFloat("_GlobalS", currentRadialS);
    }

    void CalculatePresets()
    {
        centerPoint = RectTransformUtility.WorldToScreenPoint(null, transform.position);
        paletteRadius = GetComponent<RectTransform>().rect.width / 2f;
        Vector2 pickerDir = (Vector2)picker.localPosition;
        float angle = Vector2.SignedAngle(Vector2.right, pickerDir);
        pickerHueOffset = (angle < 0 ? angle + 360 : angle) / 360f;
    }

    void UpdateMaterialInitialValues()
    {
        if (colorWheelMat == null) return;
        colorWheelMat.SetFloat("_StartingAngle", startingAngle);
        colorWheelMat.SetInt("_ColorsCount", totalNumberofColors);
        colorWheelMat.SetInt("_WheelsCount", wheelsCount);
    }

    public void OnBeginDrag(PointerEventData eventData) { dragging = true; }
    public void OnEndDrag(PointerEventData eventData) { dragging = false; }
    public void OnInitializePotentialDrag(PointerEventData eventData) { }
}