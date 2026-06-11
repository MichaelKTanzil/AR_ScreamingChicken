using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChickenCustomizePanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject customizePanel;

    [Header("Chicken Preview")]
    public RawImage   chickenPreviewDisplay;
    public Camera     previewCamera;
    // chickenPrefab tidak perlu lagi — diambil dari CharacterManager

    [Header("Color Wheel")]
    public RawImage colorWheelDisplay;
    public Slider   brightnessSlider;
    public Image    colorPreviewBox;

    [Header("Buttons")]
    public Button applyButton;
    public Button resetButton;
    public Button backButton;

    // ── Internal ──────────────────────────────────────────────────
    private Texture2D  wheelTexture;
    private GameObject previewInstance;
    private GameObject callerPanel;

    private float currentHue = 0f;
    private float currentSat = 0f;
    private float currentVal = 1f;

    private static readonly Vector3 PREVIEW_POS = new Vector3(500f, 0f, 500f);

    void Start()
    {
        BuildColorWheel();
        SetupEventTriggers();

        applyButton.onClick.AddListener(OnApply);
        resetButton.onClick.AddListener(OnReset);
        backButton.onClick.AddListener(ClosePanel);

        brightnessSlider.minValue = 0.1f;
        brightnessSlider.maxValue = 1f;
        brightnessSlider.value    = 1f;
        brightnessSlider.onValueChanged.AddListener(v => { currentVal = v; RefreshAll(); });

        SetupPreviewCamera();
        customizePanel.SetActive(false);
    }

    // ── Buka panel ────────────────────────────────────────────────
    public void OpenFromMainMenu() => Open(GameUIManager.instance.mainMenuPanel);
    public void OpenFromGameOver() => Open(GameUIManager.instance.gameOverPanel);

    void Open(GameObject fromPanel)
    {
        callerPanel = fromPanel;
        if (callerPanel != null) callerPanel.SetActive(false);
        GameUIManager.isPlaying = false;

        customizePanel.SetActive(true);
        SpawnPreviewChicken();
        RefreshAll();
    }

    public void ClosePanel()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
        customizePanel.SetActive(false);
        if (callerPanel != null) callerPanel.SetActive(true);
    }

    // ── Preview Camera ────────────────────────────────────────────
    void SetupPreviewCamera()
    {
        if (previewCamera == null) return;
        previewCamera.clearFlags      = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f);
        previewCamera.transform.position = PREVIEW_POS + new Vector3(0f, 0.3f, -0.8f);
        previewCamera.transform.LookAt(PREVIEW_POS + Vector3.up * 0.2f);
    }

    void SpawnPreviewChicken()
    {
        // Hapus preview lama dulu
        if (previewInstance != null) Destroy(previewInstance);

        GameObject prefab = CharacterManager.GetSelectedPrefab();
        if (prefab == null || previewCamera == null) return;

        previewInstance = Instantiate(prefab, PREVIEW_POS, Quaternion.Euler(0f, 90f, 0f));

        // Stop semua script & physics
        foreach (var mono in previewInstance.GetComponentsInChildren<MonoBehaviour>())
            mono.enabled = false;
        Rigidbody rb = previewInstance.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.linearVelocity = Vector3.zero; }

        foreach (var r in previewInstance.GetComponentsInChildren<Renderer>())
            r.enabled = true;
    }

    // Dipanggil CharacterManager saat user klik ← / →
    public void UpdatePreviewModel()
    {
        SpawnPreviewChicken();

        // Reset warna ke default karakter yang baru dipilih
        Color.RGBToHSV(CharacterManager.selectedColor, out currentHue, out currentSat, out currentVal);
        brightnessSlider.value = currentVal;
        RefreshAll();
    }

    // ── Color Wheel ───────────────────────────────────────────────
    void BuildColorWheel()
    {
        int size = 256;
        wheelTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = size / 2f;

        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            float dx = x - c, dy = y - c;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= c)
                wheelTexture.SetPixel(x, y, Color.HSVToRGB(
                    (Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 1f) % 1f,
                    dist / c, 1f));
            else
                wheelTexture.SetPixel(x, y, Color.clear);
        }
        wheelTexture.Apply();
        colorWheelDisplay.texture = wheelTexture;
    }

    void SetupEventTriggers()
    {
        var trigger = colorWheelDisplay.gameObject.AddComponent<EventTrigger>();
        void Add(EventTriggerType t)
        {
            var e = new EventTrigger.Entry { eventID = t };
            e.callback.AddListener(d => HandleWheel((PointerEventData)d));
            trigger.triggers.Add(e);
        }
        Add(EventTriggerType.PointerClick);
        Add(EventTriggerType.Drag);
    }

    void HandleWheel(PointerEventData data)
    {
        RectTransform rt = colorWheelDisplay.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, data.position, data.pressEventCamera, out Vector2 local)) return;

        Vector2 norm = new Vector2(local.x / (rt.rect.width / 2f),
                                   local.y / (rt.rect.height / 2f));
        if (norm.magnitude > 1f) return;

        currentHue = (Mathf.Atan2(norm.y, norm.x) / (2f * Mathf.PI) + 1f) % 1f;
        currentSat = norm.magnitude;
        RefreshAll();
    }

    // ── Apply / Reset ─────────────────────────────────────────────
    void OnApply()
    {
        ChickenCustomizer.selectedColor = CurrentColor();
        CharacterManager.selectedColor  = CurrentColor();
        CharacterManager.ApplyCharacterInScene();
        ClosePanel();
    }

    void OnReset()
    {
        currentHue = 0f; currentSat = 0f; currentVal = 1f;
        brightnessSlider.value = 1f;
        ChickenCustomizer.selectedColor = Color.white;
        RefreshAll();
    }

    Color CurrentColor() => Color.HSVToRGB(currentHue, currentSat, currentVal);

    void RefreshAll()
    {
        Color c = CurrentColor();
        if (colorPreviewBox != null) colorPreviewBox.color = c;
        if (previewInstance != null)
        {
            ChickenCustomizer.selectedColor = c;
            ChickenCustomizer.ApplyColorToChicken(previewInstance);
        }
    }
}