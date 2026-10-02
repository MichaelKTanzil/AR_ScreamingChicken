using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class TouchControlUI : MonoBehaviour
{
    public static TouchControlUI instance;

    [Header("Tombol Pengaturan")]
    [Tooltip("Tombol ikon gear untuk membuka panel Pengaturan")]
    public Button openSettingsButton;
    [Tooltip("Sprite icon untuk tombol Settings. Kamu bisa drag gambar icon apa pun dari Project ke sini di Inspector. Jika kosong, otomatis memakai icon roda gigi.")]
    public Sprite settingsIconSprite;
    [Tooltip("Posisi tombol gear di layar. Default (440, 1020) di pojok kanan atas.")]
    public Vector2 settingsButtonPosition = new Vector2(440f, 1020f);

    [Header("Panel Pengaturan (Modal)")]
    public GameObject settingsPanelRoot;
    public Button closeSettingsButton;
    public Button closeXButton;
    public RectTransform settingsCardRect;

    [Header("Slider Sensitivitas Mikrofon")]
    public Slider micSensitivitySlider;
    public TextMeshProUGUI micSensitivityValueText;
    public GameObject micSensitivitySectionRoot;

    [Header("Opsi Mode di Pengaturan")]
    public Button voiceModeCardButton;
    public Image voiceCardBackground;
    public Image voiceCardBorder;
    public TextMeshProUGUI voiceStatusText;

    public Button touchModeCardButton;
    public Image touchCardBackground;
    public Image touchCardBorder;
    public TextMeshProUGUI touchStatusText;

    [Header("Panel Kontrol Ganda (Dual Touch Controls)")]
    public GameObject touchPadRoot;
    public GameObject walkButtonRoot;
    public Image walkButtonBackground;
    public Image walkButtonBorder;
    public TextMeshProUGUI walkButtonTitle;
    public TextMeshProUGUI walkButtonSub;

    public GameObject jumpButtonRoot;
    public Image jumpButtonBackground;
    public Image jumpButtonBorder;
    public TextMeshProUGUI jumpButtonTitle;
    public TextMeshProUGUI jumpButtonSub;

    [Header("Legacy Panel Tahan Layar (Fallback)")]
    public Image touchPadBackground;
    public TextMeshProUGUI touchPadTitleText;
    public TextMeshProUGUI touchPadSubText;
    public Image touchPadGlowBorder;

    [Header("Tema Warna")]
    public Color voiceActiveColor = new Color(0.06f, 0.72f, 0.51f, 1f); // Modern Emerald Green (#10B981)
    public Color touchActiveColor = new Color(0.96f, 0.52f, 0.12f, 1f); // Vibrant Coral Orange (#F97316)
    public Color cardInactiveColor = new Color(0.12f, 0.15f, 0.22f, 0.9f);
    public Color cardActiveVoiceColor = new Color(0.04f, 0.28f, 0.2f, 0.95f);
    public Color cardActiveTouchColor = new Color(0.35f, 0.16f, 0.05f, 0.95f);

    [Header("Fitur Layar")]
    [Tooltip("Jika true, pemain dapat menahan area layar mana saja untuk kontrol: Kiri = Jalan, Kanan = Lompat")]
    public bool enableFullScreenTouch = true;

    private bool isSettingsOpen = false;
    private bool isWalkPressed = false;
    private bool isJumpPressed = false;
    private bool isPadPressed = false;
    private bool isScreenHolding = false;
    private Vector3 originalPadScale = Vector3.one;
    private Vector3 originalWalkScale = Vector3.one;
    private Vector3 originalJumpScale = Vector3.one;
    private GameObject soundBarRootObject;
    private GameObject scoreBoardObject;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        EnsureUIExists();

        if (openSettingsButton != null)
        {
            RectTransform rt = openSettingsButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = settingsButtonPosition;
            }

            RefreshSettingsButtonIcon();
        }

        if (touchPadRoot != null)
        {
            originalPadScale = touchPadRoot.transform.localScale;
        }

        if (walkButtonRoot != null)
        {
            originalWalkScale = walkButtonRoot.transform.localScale;
        }

        if (jumpButtonRoot != null)
        {
            originalJumpScale = jumpButtonRoot.transform.localScale;
        }

        CleanupLegacySectionLabel();
        UpdateSettingsVisuals();
    }

    private void Update()
    {
        if (MicDetection.instance == null) return;

        bool isSplash = (GameUIManager.instance != null && GameUIManager.instance.splashPanel != null && GameUIManager.instance.splashPanel.activeSelf);
        bool isMainMenu = (GameUIManager.instance != null && GameUIManager.instance.mainMenuPanel != null && GameUIManager.instance.mainMenuPanel.activeSelf);
        bool isGameOver = (GameUIManager.instance != null && GameUIManager.instance.gameOverPanel != null && GameUIManager.instance.gameOverPanel.activeSelf);
        bool isPlaying = GameUIManager.isPlaying && !isGameOver && !isMainMenu && !isSplash;

        bool isCustomize = false;
        ChickenCustomizePanel custPanel = FindFirstObjectByType<ChickenCustomizePanel>();
        if (custPanel != null && custPanel.customizePanel != null && custPanel.customizePanel.activeSelf)
        {
            isCustomize = true;
        }

        // 1. Tombol Gear (Settings): Muncul di Main Menu & Game Over (Mati), HILANG saat Play
        if (openSettingsButton != null)
        {
            bool shouldShowGear = !isSplash && !isCustomize && !isSettingsOpen && (isMainMenu || isGameOver);
            if (openSettingsButton.gameObject.activeSelf != shouldShowGear)
            {
                openSettingsButton.gameObject.SetActive(shouldShowGear);
            }
        }

        // 2. Screaming Bar (Sound Bar): Hanya muncul saat Play
        EnsureSoundBarRef();
        if (soundBarRootObject != null)
        {
            bool shouldShowSoundBar = isPlaying && !isCustomize && !isSettingsOpen;
            if (soundBarRootObject.activeSelf != shouldShowSoundBar)
            {
                soundBarRootObject.SetActive(shouldShowSoundBar);
            }
        }

        // 3. Score Board: Sembunyi saat Main Menu & Splash, selalu muncul saat Play dan saat Mati (Game Over)
        EnsureScoreBoardRef();
        if (scoreBoardObject != null)
        {
            bool shouldShowScore = (isPlaying || isGameOver) && !isMainMenu && !isSplash && !isCustomize && !isSettingsOpen;
            if (scoreBoardObject.activeSelf != shouldShowScore)
            {
                scoreBoardObject.SetActive(shouldShowScore);
            }
        }

        bool isTouchMode = (MicDetection.instance.currentControlMode == MicDetection.InputControlMode.Touch);
        bool isGameActive = GameUIManager.isPlaying && 
                            (ARPlacementManager.instance == null || ARPlacementManager.instance.IsArenaPlaced()) &&
                            (GameUIManager.instance == null || !GameUIManager.instance.gameOverPanel.activeSelf) &&
                            !isSettingsOpen;

        // Tampilkan Touch Pad hanya saat dalam Mode Touch dan game aktif
        if (touchPadRoot != null)
        {
            bool shouldShowPad = isTouchMode && isGameActive;
            if (touchPadRoot.activeSelf != shouldShowPad)
            {
                touchPadRoot.SetActive(shouldShowPad);
            }
        }

        if (!isTouchMode || !isGameActive)
        {
            if (isWalkPressed || isJumpPressed || isPadPressed || isScreenHolding)
            {
                isWalkPressed = false;
                isJumpPressed = false;
                isPadPressed = false;
                isScreenHolding = false;
                if (MicDetection.instance != null)
                {
                    MicDetection.instance.SetWalkHolding(false);
                    MicDetection.instance.SetJumpHolding(false);
                    MicDetection.instance.SetTouchHolding(false);
                }
            }
            return;
        }

        // Full Screen Touch handling
        if (enableFullScreenTouch)
        {
            HandleFullScreenTouch();
        }

        UpdatePadVisualFeedback();
    }

    // ──────────────────────────────────────────────────────────────────
    // KONTROL SETTINGS MODAL
    // ──────────────────────────────────────────────────────────────────
    public void OpenSettings()
    {
        isSettingsOpen = true;
        isWalkPressed = false;
        isJumpPressed = false;
        isPadPressed = false;
        isScreenHolding = false;
        if (MicDetection.instance != null)
        {
            MicDetection.instance.SetWalkHolding(false);
            MicDetection.instance.SetJumpHolding(false);
            MicDetection.instance.SetTouchHolding(false);
        }

        if (settingsPanelRoot != null)
        {
            CleanupLegacySectionLabel();
            settingsPanelRoot.SetActive(true);
        }

        UpdateSettingsVisuals();
    }

    private void CleanupLegacySectionLabel()
    {
        if (settingsPanelRoot != null)
        {
            Transform oldSec = settingsPanelRoot.transform.Find("SettingsCard/SectionLabel");
            if (oldSec != null)
            {
                if (Application.isPlaying) Destroy(oldSec.gameObject);
                else DestroyImmediate(oldSec.gameObject);
            }
        }
    }

    public void CloseSettings()
    {
        isSettingsOpen = false;
        if (settingsPanelRoot != null)
        {
            settingsPanelRoot.SetActive(false);
        }
    }

    public void SelectControlMode(MicDetection.InputControlMode mode)
    {
        if (MicDetection.instance != null)
        {
            MicDetection.instance.SetControlMode(mode);
            UpdateSettingsVisuals();
        }
    }

    public void UpdateSettingsVisuals()
    {
        if (MicDetection.instance == null) return;

        bool isVoice = (MicDetection.instance.currentControlMode == MicDetection.InputControlMode.Voice);

        // Update Card Voice
        if (voiceCardBackground != null)
            voiceCardBackground.color = isVoice ? cardActiveVoiceColor : cardInactiveColor;

        if (voiceCardBorder != null)
        {
            voiceCardBorder.gameObject.SetActive(isVoice);
            voiceCardBorder.color = voiceActiveColor;
        }

        if (voiceStatusText != null)
        {
            voiceStatusText.text = isVoice ? "AKTIF" : "PILIH";
            voiceStatusText.color = isVoice ? voiceActiveColor : new Color(0.7f, 0.75f, 0.8f, 0.8f);
        }

        // Update Card Touch
        if (touchCardBackground != null)
            touchCardBackground.color = !isVoice ? cardActiveTouchColor : cardInactiveColor;

        if (touchCardBorder != null)
        {
            touchCardBorder.gameObject.SetActive(!isVoice);
            touchCardBorder.color = touchActiveColor;
        }

        if (touchStatusText != null)
        {
            touchStatusText.text = !isVoice ? "AKTIF" : "PILIH";
            touchStatusText.color = !isVoice ? touchActiveColor : new Color(0.7f, 0.75f, 0.8f, 0.8f);
        }

        // ── SLIDER SENSITIVITAS MIC: Hanya muncul jika Mode Mic (Voice) aktif! ──
        if (micSensitivitySectionRoot != null)
        {
            micSensitivitySectionRoot.SetActive(isVoice);
        }

        // Terapkan layout posisi dan margin lega (40-50px) ke semua elemen modal
        ApplySettingsLayoutPositions();

        if (micSensitivitySlider != null)
        {
            micSensitivitySlider.SetValueWithoutNotify(MicDetection.instance.micSensitivity);
            UpdateSensitivityDisplay(MicDetection.instance.micSensitivity);
        }
    }

    /// <summary>
    /// Memastikan semua elemen dalam SettingsCard memiliki margin lega (40-50px)
    /// dan tidak ada tombol yang nempel atau berdempetan.
    /// </summary>
    public void ApplySettingsLayoutPositions()
    {
        if (settingsCardRect == null && settingsPanelRoot != null)
        {
            Transform card = settingsPanelRoot.transform.Find("SettingsCard");
            if (card != null) settingsCardRect = card.GetComponent<RectTransform>();
        }

        if (settingsCardRect == null) return;

        bool isVoice = (MicDetection.instance == null || MicDetection.instance.currentControlMode == MicDetection.InputControlMode.Voice);
        
        // Ukuran Modal Card: 780px lebar, 920px (Voice) / 660px (Touch) tinggi
        settingsCardRect.sizeDelta = new Vector2(780f, isVoice ? 920f : 660f);

        // 1. Header Title: top -40f, height 55f (bottom = -95f)
        Transform titleT = settingsCardRect.Find("Title");
        if (titleT != null)
        {
            RectTransform tRt = titleT.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0f, -40f);
            tRt.sizeDelta = new Vector2(660f, 55f);
        }

        // Tombol Close X di sudut kanan atas
        Transform xBtnT = settingsCardRect.Find("Button_CloseX");
        if (xBtnT != null)
        {
            RectTransform xRt = xBtnT.GetComponent<RectTransform>();
            xRt.anchoredPosition = new Vector2(-28f, -28f);
            xRt.sizeDelta = new Vector2(56f, 56f);
        }

        // 2. Card 1: Voice Mode (top -140f, height 150f, bottom -290f) -> Margin dari Title = 45px!
        Transform c1 = settingsCardRect.Find("Card_KONTROL SUARA (MIC)");
        if (c1 != null)
        {
            RectTransform c1Rt = c1.GetComponent<RectTransform>();
            c1Rt.anchoredPosition = new Vector2(0f, -140f);
            c1Rt.sizeDelta = new Vector2(660f, 150f);
            UpdateCardInternalLayout(c1);
        }

        // 3. Card 2: Touch Mode (top -335f, height 150f, bottom -485f) -> Margin dari Card 1 = 45px!
        Transform c2 = settingsCardRect.Find("Card_LAYAR SENTUH (TOUCH)");
        if (c2 != null)
        {
            RectTransform c2Rt = c2.GetComponent<RectTransform>();
            c2Rt.anchoredPosition = new Vector2(0f, -335f);
            c2Rt.sizeDelta = new Vector2(660f, 150f);
            UpdateCardInternalLayout(c2);
        }

        // 4. Section 3: Slider Sensitivitas (top -530f, height 215f, bottom -745f) -> Margin dari Card 2 = 45px!
        Transform sec = settingsCardRect.Find("Section_MicSensitivity");
        if (sec != null)
        {
            micSensitivitySectionRoot = sec.gameObject;
            micSensitivitySectionRoot.SetActive(isVoice);
            RectTransform secRt = sec.GetComponent<RectTransform>();
            secRt.anchoredPosition = new Vector2(0f, -530f);
            secRt.sizeDelta = new Vector2(660f, 215f);

            if (micSensitivitySlider == null) 
                micSensitivitySlider = sec.GetComponentInChildren<Slider>();

            if (micSensitivityValueText == null)
            {
                Transform valT = sec.Find("ValueText");
                if (valT != null) micSensitivityValueText = valT.GetComponent<TextMeshProUGUI>();
            }
        }

        // 5. Tombol Close Modal ("TUTUP"): anchored bottom (y = 45f, height 80f)
        // -> Margin dari Section 3 (Voice) atau Card 2 (Touch) = 50px!
        Transform closeBtn = settingsCardRect.Find("Button_CloseModal");
        if (closeBtn != null)
        {
            RectTransform closeRt = closeBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 45f);
            closeRt.sizeDelta = new Vector2(660f, 80f);
        }
    }

    private void UpdateCardInternalLayout(Transform cardTrans)
    {
        Transform titleT = cardTrans.Find("Title");
        if (titleT != null)
        {
            RectTransform tRt = titleT.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.50f);
            tRt.anchorMax = new Vector2(0.72f, 1f);
            tRt.offsetMin = new Vector2(28f, 0f);
            tRt.offsetMax = new Vector2(-10f, -16f);
        }

        Transform statusT = cardTrans.Find("Status");
        if (statusT != null)
        {
            RectTransform sRt = statusT.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.72f, 0.50f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.offsetMin = new Vector2(0f, 0f);
            sRt.offsetMax = new Vector2(-28f, -16f);
        }

        Transform descT = cardTrans.Find("Desc");
        if (descT != null)
        {
            RectTransform dRt = descT.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0f, 0f);
            dRt.anchorMax = new Vector2(1f, 0.50f);
            dRt.offsetMin = new Vector2(28f, 14f);
            dRt.offsetMax = new Vector2(-28f, -2f);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // FULL SCREEN TOUCH & BUTTON HANDLERS
    // ──────────────────────────────────────────────────────────────────
    private void HandleFullScreenTouch()
    {
        if (EventSystem.current == null || isSettingsOpen) return;

        bool screenWalk = false;
        bool screenJump = false;

        // Touchscreen (Mobile)
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                {
                    int touchId = touch.touchId.ReadValue();
                    Vector2 pos = touch.position.ReadValue();
                    if (!EventSystem.current.IsPointerOverGameObject(touchId))
                    {
                        // Touch di luar tombol UI: Sisi Kiri = Jalan, Sisi Kanan = Lompat
                        if (pos.x < Screen.width * 0.48f) screenWalk = true;
                        else screenJump = true;
                    }
                }
            }
        }

        // Mouse (Editor / Standalone)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (mousePos.x < Screen.width * 0.48f) screenWalk = true;
                else screenJump = true;
            }
        }

        if (MicDetection.instance != null)
        {
            bool finalWalk = screenWalk || isWalkPressed;
            bool finalJump = screenJump || isJumpPressed;

            MicDetection.instance.SetWalkHolding(finalWalk);
            MicDetection.instance.SetJumpHolding(finalJump);
        }
    }

    public void OnWalkDown()
    {
        if (isSettingsOpen) return;
        isWalkPressed = true;
        if (MicDetection.instance != null) MicDetection.instance.SetWalkHolding(true);
    }

    public void OnWalkUp()
    {
        isWalkPressed = false;
        if (MicDetection.instance != null) MicDetection.instance.SetWalkHolding(false);
    }

    public void OnJumpDown()
    {
        if (isSettingsOpen) return;
        isJumpPressed = true;
        if (MicDetection.instance != null) MicDetection.instance.SetJumpHolding(true);
    }

    public void OnJumpUp()
    {
        isJumpPressed = false;
        if (MicDetection.instance != null) MicDetection.instance.SetJumpHolding(false);
    }

    public void OnTouchPadDown()
    {
        OnJumpDown();
    }

    public void OnTouchPadUp()
    {
        OnJumpUp();
    }

    private void UpdatePadVisualFeedback()
    {
        if (walkButtonRoot != null)
        {
            Vector3 targetWalkScale = isWalkPressed ? originalWalkScale * 1.06f : originalWalkScale;
            walkButtonRoot.transform.localScale = Vector3.Lerp(walkButtonRoot.transform.localScale, targetWalkScale, Time.deltaTime * 14f);
        }

        if (jumpButtonRoot != null)
        {
            Vector3 targetJumpScale = isJumpPressed ? originalJumpScale * 1.06f : originalJumpScale;
            jumpButtonRoot.transform.localScale = Vector3.Lerp(jumpButtonRoot.transform.localScale, targetJumpScale, Time.deltaTime * 14f);
        }
    }

    public void RefreshSettingsButtonIcon()
    {
        if (openSettingsButton == null) return;

        Transform iconTrans = openSettingsButton.transform.Find("Icon");
        if (iconTrans == null)
        {
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(openSettingsButton.transform, false);
            iconTrans = iconObj.transform;
        }

        // Hapus TextMeshProUGUI lama jika ada (agar tidak muncul kotak kosong/tofu akibat font tidak support ⚙️)
        TextMeshProUGUI oldTmp = iconTrans.GetComponent<TextMeshProUGUI>();
        if (oldTmp != null)
        {
            if (Application.isPlaying) Destroy(oldTmp);
            else DestroyImmediate(oldTmp);
        }

        RectTransform iconRt = iconTrans.GetComponent<RectTransform>();
        if (iconRt == null) iconRt = iconTrans.gameObject.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.2f, 0.2f);
        iconRt.anchorMax = new Vector2(0.8f, 0.8f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = Vector2.zero;
        iconRt.anchoredPosition = Vector2.zero;

        Image iconImg = iconTrans.GetComponent<Image>();
        if (iconImg == null) iconImg = iconTrans.gameObject.AddComponent<Image>();

        iconImg.raycastTarget = false;
        iconImg.sprite = GetSettingsSprite();
        iconImg.color = Color.white;
    }

    private Sprite GetSettingsSprite()
    {
        if (settingsIconSprite != null) return settingsIconSprite;
        Sprite resSprite = Resources.Load<Sprite>("settings");
        if (resSprite != null) return resSprite;
        return CreateGearIconSprite(128);
    }

    private void EnsureSoundBarRef()
    {
        if (soundBarRootObject != null) return;

        SoundBarUI sb = FindFirstObjectByType<SoundBarUI>();
        if (sb != null)
        {
            if (sb.fillBar != null)
            {
                soundBarRootObject = sb.fillBar.gameObject;
            }
            else
            {
                soundBarRootObject = sb.gameObject;
            }
        }
    }

    private void EnsureScoreBoardRef()
    {
        if (scoreBoardObject != null) return;

        if (ScoreManager.instance != null && ScoreManager.instance.scoreText != null)
        {
            scoreBoardObject = ScoreManager.instance.scoreText.gameObject;
        }
        else
        {
            ScoreManager sm = FindFirstObjectByType<ScoreManager>();
            if (sm != null && sm.scoreText != null)
            {
                scoreBoardObject = sm.scoreText.gameObject;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (openSettingsButton != null)
        {
            RefreshSettingsButtonIcon();
        }
    }
#endif

    // ──────────────────────────────────────────────────────────────────
    // AUTO-SETUP UI (Otomatis dibuat jika belum ada di dalam Scene)
    // ──────────────────────────────────────────────────────────────────
    private void EnsureUIExists()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        TMP_FontAsset sharedFont = null;
        if (ScoreManager.instance != null && ScoreManager.instance.scoreText != null)
        {
            sharedFont = ScoreManager.instance.scoreText.font;
        }

        // 1. Tombol Buka Pengaturan (⚙️ Icon Button di pojok kanan atas)
        if (openSettingsButton == null)
        {
            GameObject btnObj = new GameObject("Button_OpenSettings");
            btnObj.transform.SetParent(canvas.transform, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = settingsButtonPosition; // Default (377, 600) tepat di bawah Sound Bar
            rt.sizeDelta = new Vector2(85f, 85f);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = CreateRoundedRectSprite(128, 128, 36);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.1f, 0.13f, 0.2f, 0.85f);

            openSettingsButton = btnObj.AddComponent<Button>();
            openSettingsButton.targetGraphic = img;
            openSettingsButton.onClick.AddListener(OpenSettings);
            btnObj.AddComponent<UIButtonSound>();

            // Icon Image (Ganti TextMeshPro yang berpotensi jadi kotak/tofu jika font tidak ada unicode ⚙️)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            RectTransform iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.2f, 0.2f);
            iconRt.anchorMax = new Vector2(0.8f, 0.8f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = Vector2.zero;
            iconRt.anchoredPosition = Vector2.zero;

            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.sprite = GetSettingsSprite();
            iconImg.color = Color.white;
        }

        // 2. Modal Panel Pengaturan (Panel_Settings)
        if (settingsPanelRoot == null)
        {
            settingsPanelRoot = new GameObject("Panel_Settings");
            settingsPanelRoot.transform.SetParent(canvas.transform, false);

            RectTransform rootRt = settingsPanelRoot.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // Backdrop
            Image backdrop = settingsPanelRoot.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.72f);

            // Modal Card
            GameObject cardObj = new GameObject("SettingsCard");
            cardObj.transform.SetParent(settingsPanelRoot.transform, false);

            RectTransform cardRt = cardObj.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = Vector2.zero;
            cardRt.sizeDelta = new Vector2(780f, 920f);
            settingsCardRect = cardRt;

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = CreateRoundedRectSprite(256, 256, 32);
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);

            // Title (top -40f, height 55f, bottom -95f)
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -40f);
            titleRt.sizeDelta = new Vector2(660f, 55f);

            TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) titleTxt.font = sharedFont;
            titleTxt.fontSize = 38f;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;
            titleTxt.text = "PENGATURAN";

            // Tombol X di pojok kanan atas modal
            GameObject xBtnObj = new GameObject("Button_CloseX");
            xBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform xRt = xBtnObj.AddComponent<RectTransform>();
            xRt.anchorMin = new Vector2(1f, 1f);
            xRt.anchorMax = new Vector2(1f, 1f);
            xRt.pivot = new Vector2(1f, 1f);
            xRt.anchoredPosition = new Vector2(-28f, -28f);
            xRt.sizeDelta = new Vector2(56f, 56f);

            Image xImg = xBtnObj.AddComponent<Image>();
            xImg.color = new Color(0.2f, 0.25f, 0.35f, 0.6f);
            xImg.sprite = CreateRoundedRectSprite(64, 64, 16);
            xImg.type = Image.Type.Sliced;

            closeXButton = xBtnObj.AddComponent<Button>();
            closeXButton.targetGraphic = xImg;
            closeXButton.onClick.AddListener(CloseSettings);
            xBtnObj.AddComponent<UIButtonSound>();

            GameObject xTxtObj = new GameObject("Text");
            xTxtObj.transform.SetParent(xBtnObj.transform, false);
            RectTransform xTxtRt = xTxtObj.AddComponent<RectTransform>();
            xTxtRt.anchorMin = Vector2.zero;
            xTxtRt.anchorMax = Vector2.one;
            xTxtRt.sizeDelta = Vector2.zero;
            TextMeshProUGUI xTxt = xTxtObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) xTxt.font = sharedFont;
            xTxt.fontSize = 28f;
            xTxt.fontStyle = FontStyles.Bold;
            xTxt.alignment = TextAlignmentOptions.Center;
            xTxt.color = Color.white;
            xTxt.text = "X";

            // Card 1: VOICE MODE (y = -140f, height = 150f, bottom = -290f, margin dari Title = 45px)
            BuildModeOptionCard(cardObj.transform, sharedFont, -140f,
                "KONTROL SUARA (MIC)", 
                "Gunakan suara/teriakan asli ke mikrofon untuk menggerakkan dan melompatkan ayam.",
                () => SelectControlMode(MicDetection.InputControlMode.Voice),
                out voiceModeCardButton, out voiceCardBackground, out voiceCardBorder, out voiceStatusText);

            // Card 2: TOUCH MODE (y = -335f, height = 150f, bottom = -485f, margin dari Card 1 = 45px!)
            BuildModeOptionCard(cardObj.transform, sharedFont, -335f,
                "LAYAR SENTUH (TOUCH)", 
                "Tahan layar untuk mengemulasikan teriakan (mic dimatikan). Tombol Kiri = Jalan, Kanan = Lompat.",
                () => SelectControlMode(MicDetection.InputControlMode.Touch),
                out touchModeCardButton, out touchCardBackground, out touchCardBorder, out touchStatusText);

            // Section 3: SLIDER SENSITIVITAS MIC (y = -530f, height = 215f, bottom = -745f, margin dari Card 2 = 45px!)
            BuildMicSensitivitySliderSection(cardObj.transform, sharedFont, -530f);

            // Tombol Tutup di Bawah (y = 45f dari bawah, height = 80f, margin dari Section 3 = 50px!)
            GameObject closeBtnObj = new GameObject("Button_CloseModal");
            closeBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 45f);
            closeRt.sizeDelta = new Vector2(660f, 80f);

            Image closeImg = closeBtnObj.AddComponent<Image>();
            closeImg.sprite = CreateRoundedRectSprite(256, 64, 20);
            closeImg.type = Image.Type.Sliced;
            closeImg.color = new Color(0.2f, 0.28f, 0.4f, 1f);

            closeSettingsButton = closeBtnObj.AddComponent<Button>();
            closeSettingsButton.targetGraphic = closeImg;
            closeSettingsButton.onClick.AddListener(CloseSettings);
            closeBtnObj.AddComponent<UIButtonSound>();

            GameObject closeTxtObj = new GameObject("Text");
            closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
            RectTransform closeTxtRt = closeTxtObj.AddComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero;
            closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI closeTxt = closeTxtObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) closeTxt.font = sharedFont;
            closeTxt.fontSize = 28f;
            closeTxt.fontStyle = FontStyles.Bold;
            closeTxt.alignment = TextAlignmentOptions.Center;
            closeTxt.color = Color.white;
            closeTxt.text = "TUTUP";

            ApplySettingsLayoutPositions();
            settingsPanelRoot.SetActive(false);
        }
        else
        {
            if (settingsCardRect == null)
            {
                Transform card = settingsPanelRoot.transform.Find("SettingsCard");
                if (card != null) settingsCardRect = card.GetComponent<RectTransform>();
            }

            if (settingsCardRect != null)
            {
                Transform sec = settingsCardRect.Find("Section_MicSensitivity");
                if (sec == null)
                {
                    BuildMicSensitivitySliderSection(settingsCardRect, sharedFont, -530f);
                }
                ApplySettingsLayoutPositions();
            }
        }

        // 3. Buat Panel Kontrol Ganda (Dual Touch Controls: JALAN & LOMPAT) di Bawah Layar
        Transform oldPad = canvas.transform.Find("TouchScreamPad");
        if (oldPad != null)
        {
            if (Application.isPlaying) Destroy(oldPad.gameObject);
            else DestroyImmediate(oldPad.gameObject);
            touchPadRoot = null;
        }

        if (touchPadRoot == null)
        {
            touchPadRoot = new GameObject("DualTouchControls");
            touchPadRoot.transform.SetParent(canvas.transform, false);

            RectTransform containerRt = touchPadRoot.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.5f, 0f);
            containerRt.anchorMax = new Vector2(0.5f, 0f);
            containerRt.pivot = new Vector2(0.5f, 0f);
            containerRt.anchoredPosition = new Vector2(0f, 70f);
            containerRt.sizeDelta = new Vector2(900f, 180f);

            // A. Tombol JALAN (Kiri)
            walkButtonRoot = new GameObject("Btn_Walk");
            walkButtonRoot.transform.SetParent(touchPadRoot.transform, false);

            RectTransform walkRt = walkButtonRoot.AddComponent<RectTransform>();
            walkRt.anchorMin = new Vector2(0.5f, 0.5f);
            walkRt.anchorMax = new Vector2(0.5f, 0.5f);
            walkRt.pivot = new Vector2(0.5f, 0.5f);
            walkRt.anchoredPosition = new Vector2(-230f, 0f);
            walkRt.sizeDelta = new Vector2(410f, 160f);

            walkButtonBackground = walkButtonRoot.AddComponent<Image>();
            walkButtonBackground.sprite = CreateRoundedRectSprite(256, 128, 24);
            walkButtonBackground.type = Image.Type.Sliced;
            walkButtonBackground.color = new Color(0.06f, 0.20f, 0.16f, 0.90f);

            var walkReceiver = walkButtonRoot.AddComponent<DualButtonEventReceiver>();
            walkReceiver.controller = this;
            walkReceiver.buttonType = DualButtonEventReceiver.ButtonType.Walk;

            // Border JALAN
            GameObject walkBorderObj = new GameObject("Border");
            walkBorderObj.transform.SetParent(walkButtonRoot.transform, false);
            RectTransform wbRt = walkBorderObj.AddComponent<RectTransform>();
            wbRt.anchorMin = Vector2.zero;
            wbRt.anchorMax = Vector2.one;
            wbRt.sizeDelta = new Vector2(6f, 6f);
            walkButtonBorder = walkBorderObj.AddComponent<Image>();
            walkButtonBorder.sprite = CreateBorderSprite(256, 128, 24, 6);
            walkButtonBorder.type = Image.Type.Sliced;
            walkButtonBorder.color = new Color(0.18f, 0.82f, 0.50f, 0.75f);
            walkButtonBorder.raycastTarget = false;

            // Title JALAN
            GameObject walkTitleObj = new GameObject("Title");
            walkTitleObj.transform.SetParent(walkButtonRoot.transform, false);
            RectTransform wtRt = walkTitleObj.AddComponent<RectTransform>();
            wtRt.anchorMin = new Vector2(0f, 0.38f);
            wtRt.anchorMax = new Vector2(1f, 1f);
            wtRt.offsetMin = new Vector2(10f, 0f);
            wtRt.offsetMax = new Vector2(-10f, -8f);

            walkButtonTitle = walkTitleObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) walkButtonTitle.font = sharedFont;
            walkButtonTitle.fontSize = 32f;
            walkButtonTitle.fontStyle = FontStyles.Bold;
            walkButtonTitle.alignment = TextAlignmentOptions.Center;
            walkButtonTitle.color = Color.white;
            walkButtonTitle.text = "JALAN";
            walkButtonTitle.raycastTarget = false;

            // Subtitle JALAN
            GameObject walkSubObj = new GameObject("Subtitle");
            walkSubObj.transform.SetParent(walkButtonRoot.transform, false);
            RectTransform wsRt = walkSubObj.AddComponent<RectTransform>();
            wsRt.anchorMin = new Vector2(0f, 0f);
            wsRt.anchorMax = new Vector2(1f, 0.44f);
            wsRt.offsetMin = new Vector2(10f, 8f);
            wsRt.offsetMax = new Vector2(-10f, 0f);

            walkButtonSub = walkSubObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) walkButtonSub.font = sharedFont;
            walkButtonSub.fontSize = 19f;
            walkButtonSub.alignment = TextAlignmentOptions.Center;
            walkButtonSub.color = new Color(0.70f, 0.95f, 0.82f, 0.90f);
            walkButtonSub.text = "Tahan untuk Maju";
            walkButtonSub.raycastTarget = false;

            // B. Tombol LOMPAT (Kanan)
            jumpButtonRoot = new GameObject("Btn_Jump");
            jumpButtonRoot.transform.SetParent(touchPadRoot.transform, false);

            RectTransform jumpRt = jumpButtonRoot.AddComponent<RectTransform>();
            jumpRt.anchorMin = new Vector2(0.5f, 0.5f);
            jumpRt.anchorMax = new Vector2(0.5f, 0.5f);
            jumpRt.pivot = new Vector2(0.5f, 0.5f);
            jumpRt.anchoredPosition = new Vector2(230f, 0f);
            jumpRt.sizeDelta = new Vector2(410f, 160f);

            jumpButtonBackground = jumpButtonRoot.AddComponent<Image>();
            jumpButtonBackground.sprite = CreateRoundedRectSprite(256, 128, 24);
            jumpButtonBackground.type = Image.Type.Sliced;
            jumpButtonBackground.color = new Color(0.24f, 0.12f, 0.04f, 0.90f);

            var jumpReceiver = jumpButtonRoot.AddComponent<DualButtonEventReceiver>();
            jumpReceiver.controller = this;
            jumpReceiver.buttonType = DualButtonEventReceiver.ButtonType.Jump;

            // Border LOMPAT
            GameObject jumpBorderObj = new GameObject("Border");
            jumpBorderObj.transform.SetParent(jumpButtonRoot.transform, false);
            RectTransform jbRt = jumpBorderObj.AddComponent<RectTransform>();
            jbRt.anchorMin = Vector2.zero;
            jbRt.anchorMax = Vector2.one;
            jbRt.sizeDelta = new Vector2(6f, 6f);
            jumpButtonBorder = jumpBorderObj.AddComponent<Image>();
            jumpButtonBorder.sprite = CreateBorderSprite(256, 128, 24, 6);
            jumpButtonBorder.type = Image.Type.Sliced;
            jumpButtonBorder.color = new Color(0.96f, 0.52f, 0.12f, 0.80f);
            jumpButtonBorder.raycastTarget = false;

            // Title LOMPAT
            GameObject jumpTitleObj = new GameObject("Title");
            jumpTitleObj.transform.SetParent(jumpButtonRoot.transform, false);
            RectTransform jtRt = jumpTitleObj.AddComponent<RectTransform>();
            jtRt.anchorMin = new Vector2(0f, 0.38f);
            jtRt.anchorMax = new Vector2(1f, 1f);
            jtRt.offsetMin = new Vector2(10f, 0f);
            jtRt.offsetMax = new Vector2(-10f, -8f);

            jumpButtonTitle = jumpTitleObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) jumpButtonTitle.font = sharedFont;
            jumpButtonTitle.fontSize = 32f;
            jumpButtonTitle.fontStyle = FontStyles.Bold;
            jumpButtonTitle.alignment = TextAlignmentOptions.Center;
            jumpButtonTitle.color = Color.white;
            jumpButtonTitle.text = "LOMPAT";
            jumpButtonTitle.raycastTarget = false;

            // Subtitle LOMPAT
            GameObject jumpSubObj = new GameObject("Subtitle");
            jumpSubObj.transform.SetParent(jumpButtonRoot.transform, false);
            RectTransform jsRt = jumpSubObj.AddComponent<RectTransform>();
            jsRt.anchorMin = new Vector2(0f, 0f);
            jsRt.anchorMax = new Vector2(1f, 0.44f);
            jsRt.offsetMin = new Vector2(10f, 8f);
            jsRt.offsetMax = new Vector2(-10f, 0f);

            jumpButtonSub = jumpSubObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) jumpButtonSub.font = sharedFont;
            jumpButtonSub.fontSize = 19f;
            jumpButtonSub.alignment = TextAlignmentOptions.Center;
            jumpButtonSub.color = new Color(1.0f, 0.85f, 0.65f, 0.90f);
            jumpButtonSub.text = "Tap / Tahan Lompat";
            jumpButtonSub.raycastTarget = false;

            touchPadRoot.SetActive(false);
        }
    }

    private void BuildModeOptionCard(Transform parent, TMP_FontAsset font, float yPos, 
                                     string title, string desc, UnityEngine.Events.UnityAction onClick,
                                     out Button cardButton, out Image cardBg, out Image cardBorder, out TextMeshProUGUI statusTxt)
    {
        GameObject cardObj = new GameObject("Card_" + title);
        cardObj.transform.SetParent(parent, false);

        RectTransform rt = cardObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(660f, 150f);

        cardBg = cardObj.AddComponent<Image>();
        cardBg.sprite = CreateRoundedRectSprite(256, 128, 20);
        cardBg.type = Image.Type.Sliced;
        cardBg.color = cardInactiveColor;

        cardButton = cardObj.AddComponent<Button>();
        cardButton.targetGraphic = cardBg;
        cardButton.onClick.AddListener(onClick);
        cardObj.AddComponent<UIButtonSound>();

        // Border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(cardObj.transform, false);
        RectTransform bRt = borderObj.AddComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.sizeDelta = new Vector2(4f, 4f);

        cardBorder = borderObj.AddComponent<Image>();
        cardBorder.sprite = CreateBorderSprite(256, 128, 20, 4);
        cardBorder.type = Image.Type.Sliced;
        cardBorder.raycastTarget = false;
        cardBorder.gameObject.SetActive(false);

        // Header Title
        GameObject tObj = new GameObject("Title");
        tObj.transform.SetParent(cardObj.transform, false);
        RectTransform tRt = tObj.AddComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.50f);
        tRt.anchorMax = new Vector2(0.72f, 1f);
        tRt.offsetMin = new Vector2(28f, 0f);
        tRt.offsetMax = new Vector2(-10f, -16f);

        TextMeshProUGUI tTxt = tObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tTxt.font = font;
        tTxt.fontSize = 24f;
        tTxt.fontStyle = FontStyles.Bold;
        tTxt.alignment = TextAlignmentOptions.Left;
        tTxt.color = Color.white;
        tTxt.text = title;
        tTxt.raycastTarget = false;

        // Status Badge (PILIH / AKTIF)
        GameObject sObj = new GameObject("Status");
        sObj.transform.SetParent(cardObj.transform, false);
        RectTransform sRt = sObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.72f, 0.50f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.offsetMin = new Vector2(0f, 0f);
        sRt.offsetMax = new Vector2(-28f, -16f);

        statusTxt = sObj.AddComponent<TextMeshProUGUI>();
        if (font != null) statusTxt.font = font;
        statusTxt.fontSize = 21f;
        statusTxt.fontStyle = FontStyles.Bold;
        statusTxt.alignment = TextAlignmentOptions.Right;
        statusTxt.text = "PILIH";
        statusTxt.raycastTarget = false;

        // Description
        GameObject dObj = new GameObject("Desc");
        dObj.transform.SetParent(cardObj.transform, false);
        RectTransform dRt = dObj.AddComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 0.50f);
        dRt.offsetMin = new Vector2(28f, 14f);
        dRt.offsetMax = new Vector2(-28f, -2f);

        TextMeshProUGUI dTxt = dObj.AddComponent<TextMeshProUGUI>();
        if (font != null) dTxt.font = font;
        dTxt.fontSize = 17f;
        dTxt.alignment = TextAlignmentOptions.Left;
        dTxt.color = new Color(0.8f, 0.85f, 0.9f, 0.85f);
        dTxt.text = desc;
        dTxt.raycastTarget = false;
    }

    private void BuildMicSensitivitySliderSection(Transform parent, TMP_FontAsset font, float yPos)
    {
        micSensitivitySectionRoot = new GameObject("Section_MicSensitivity");
        micSensitivitySectionRoot.transform.SetParent(parent, false);

        RectTransform secRt = micSensitivitySectionRoot.AddComponent<RectTransform>();
        secRt.anchorMin = new Vector2(0.5f, 1f);
        secRt.anchorMax = new Vector2(0.5f, 1f);
        secRt.pivot = new Vector2(0.5f, 1f);
        secRt.anchoredPosition = new Vector2(0f, yPos);
        secRt.sizeDelta = new Vector2(660f, 215f);

        Image secBg = micSensitivitySectionRoot.AddComponent<Image>();
        secBg.sprite = CreateRoundedRectSprite(256, 128, 20);
        secBg.type = Image.Type.Sliced;
        secBg.color = new Color(0.10f, 0.13f, 0.19f, 0.95f);

        // Border
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(micSensitivitySectionRoot.transform, false);
        RectTransform bRt = borderObj.AddComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.sizeDelta = new Vector2(4f, 4f);
        Image bImg = borderObj.AddComponent<Image>();
        bImg.sprite = CreateBorderSprite(256, 128, 20, 2);
        bImg.type = Image.Type.Sliced;
        bImg.color = new Color(0.18f, 0.25f, 0.38f, 0.7f);
        bImg.raycastTarget = false;

        // Title Label (Left)
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(micSensitivitySectionRoot.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0.6f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(28f, -18f);
        titleRt.sizeDelta = new Vector2(350f, 32f);

        TextMeshProUGUI titleTxt = titleObj.AddComponent<TextMeshProUGUI>();
        if (font != null) titleTxt.font = font;
        titleTxt.fontSize = 24f;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Left;
        titleTxt.color = Color.white;
        titleTxt.text = "SENSITIVITAS MIC";

        // Value Text Badge (Right)
        GameObject valObj = new GameObject("ValueText");
        valObj.transform.SetParent(micSensitivitySectionRoot.transform, false);
        RectTransform valRt = valObj.AddComponent<RectTransform>();
        valRt.anchorMin = new Vector2(0.55f, 1f);
        valRt.anchorMax = new Vector2(1f, 1f);
        valRt.pivot = new Vector2(1f, 1f);
        valRt.anchoredPosition = new Vector2(-28f, -18f);
        valRt.sizeDelta = new Vector2(280f, 32f);

        micSensitivityValueText = valObj.AddComponent<TextMeshProUGUI>();
        if (font != null) micSensitivityValueText.font = font;
        micSensitivityValueText.fontSize = 22f;
        micSensitivityValueText.fontStyle = FontStyles.Bold;
        micSensitivityValueText.alignment = TextAlignmentOptions.Right;

        // Slider Component Root
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(micSensitivitySectionRoot.transform, false);
        RectTransform sliderRt = sliderObj.AddComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 1f);
        sliderRt.anchorMax = new Vector2(0.5f, 1f);
        sliderRt.pivot = new Vector2(0.5f, 1f);
        sliderRt.anchoredPosition = new Vector2(0f, -72f);
        sliderRt.sizeDelta = new Vector2(604f, 44f);

        micSensitivitySlider = sliderObj.AddComponent<Slider>();
        micSensitivitySlider.direction = Slider.Direction.LeftToRight;
        micSensitivitySlider.minValue = 0.5f;
        micSensitivitySlider.maxValue = 2.5f;

        // Track Background
        GameObject trackObj = new GameObject("Background");
        trackObj.transform.SetParent(sliderObj.transform, false);
        RectTransform trackRt = trackObj.AddComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0f, 0.28f);
        trackRt.anchorMax = new Vector2(1f, 0.72f);
        trackRt.sizeDelta = Vector2.zero;
        trackRt.anchoredPosition = Vector2.zero;

        Image trackImg = trackObj.AddComponent<Image>();
        trackImg.sprite = CreateRoundedRectSprite(64, 64, 12);
        trackImg.type = Image.Type.Sliced;
        trackImg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        // Fill Area & Fill
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.28f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.72f);
        fillAreaRt.offsetMin = new Vector2(6f, 0f);
        fillAreaRt.offsetMax = new Vector2(-6f, 0f);

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.sizeDelta = Vector2.zero;

        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.sprite = CreateRoundedRectSprite(64, 64, 10);
        fillImg.type = Image.Type.Sliced;
        fillImg.color = voiceActiveColor;

        micSensitivitySlider.fillRect = fillRt;

        // Handle Slide Area & Handle Knob
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRt = handleArea.AddComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(22f, 0f);
        handleAreaRt.offsetMax = new Vector2(-22f, 0f);

        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handleObj.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(44f, 44f);

        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.sprite = CreateRoundedRectSprite(64, 64, 32);
        handleImg.type = Image.Type.Simple;
        handleImg.color = Color.white;

        micSensitivitySlider.handleRect = handleRt;
        micSensitivitySlider.targetGraphic = handleImg;

        // Min/Max Scale Labels under slider track
        GameObject minLabelObj = new GameObject("Label_Min");
        minLabelObj.transform.SetParent(micSensitivitySectionRoot.transform, false);
        RectTransform minRt = minLabelObj.AddComponent<RectTransform>();
        minRt.anchorMin = new Vector2(0f, 1f);
        minRt.anchorMax = new Vector2(0.5f, 1f);
        minRt.pivot = new Vector2(0f, 1f);
        minRt.anchoredPosition = new Vector2(28f, -122f);
        minRt.sizeDelta = new Vector2(220f, 24f);

        TextMeshProUGUI minTxt = minLabelObj.AddComponent<TextMeshProUGUI>();
        if (font != null) minTxt.font = font;
        minTxt.fontSize = 17f;
        minTxt.color = new Color(0.60f, 0.66f, 0.78f, 0.85f);
        minTxt.text = "Kurang Peka (50%)";

        GameObject maxLabelObj = new GameObject("Label_Max");
        maxLabelObj.transform.SetParent(micSensitivitySectionRoot.transform, false);
        RectTransform maxRt = maxLabelObj.AddComponent<RectTransform>();
        maxRt.anchorMin = new Vector2(0.5f, 1f);
        maxRt.anchorMax = new Vector2(1f, 1f);
        maxRt.pivot = new Vector2(1f, 1f);
        maxRt.anchoredPosition = new Vector2(-28f, -122f);
        maxRt.sizeDelta = new Vector2(220f, 24f);

        TextMeshProUGUI maxTxt = maxLabelObj.AddComponent<TextMeshProUGUI>();
        if (font != null) maxTxt.font = font;
        maxTxt.fontSize = 17f;
        maxTxt.alignment = TextAlignmentOptions.Right;
        maxTxt.color = new Color(0.60f, 0.66f, 0.78f, 0.85f);
        maxTxt.text = "Sangat Peka (250%)";

        // Hint Description Text at bottom of card
        GameObject hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(micSensitivitySectionRoot.transform, false);
        RectTransform hintRt = hintObj.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 14f);
        hintRt.sizeDelta = new Vector2(604f, 36f);

        TextMeshProUGUI hintTxt = hintObj.AddComponent<TextMeshProUGUI>();
        if (font != null) hintTxt.font = font;
        hintTxt.fontSize = 17f;
        hintTxt.fontStyle = FontStyles.Italic;
        hintTxt.alignment = TextAlignmentOptions.Center;
        hintTxt.color = new Color(0.70f, 0.80f, 0.92f, 0.90f);
        hintTxt.text = "Geser ke kanan jika di HP ini ayam susah melompat saat teriak.";

        // Init initial value and listener
        float currentSens = 1.0f;
        if (MicDetection.instance != null)
        {
            currentSens = MicDetection.instance.micSensitivity;
        }
        micSensitivitySlider.value = currentSens;
        UpdateSensitivityDisplay(currentSens);

        micSensitivitySlider.onValueChanged.AddListener(OnMicSensitivitySliderChanged);
    }

    private void OnMicSensitivitySliderChanged(float val)
    {
        float stepped = Mathf.Round(val * 20f) / 20f;
        if (MicDetection.instance != null)
        {
            MicDetection.instance.SetMicSensitivity(stepped);
        }
        UpdateSensitivityDisplay(stepped);
    }

    private void UpdateSensitivityDisplay(float sens)
    {
        if (micSensitivityValueText == null) return;

        int percent = Mathf.RoundToInt(sens * 100f);
        string qualityTag;
        string colorHex;

        if (percent < 90)
        {
            qualityTag = "Rendah";
            colorHex = "#38BDF8";
        }
        else if (percent <= 115)
        {
            qualityTag = "Normal";
            colorHex = "#10B981";
        }
        else if (percent <= 175)
        {
            qualityTag = "Tinggi";
            colorHex = "#F59E0B";
        }
        else
        {
            qualityTag = "Ekstra Peka";
            colorHex = "#EF4444";
        }

        micSensitivityValueText.text = $"<color={colorHex}>{percent}%</color>  <size=17><color=#94A3B8>({qualityTag})</color></size>";
    }

    // ──────────────────────────────────────────────────────────────────
    // GENERATOR TEKSTUR UI DINAMIS
    // ──────────────────────────────────────────────────────────────────
    private static Sprite CreateRoundedRectSprite(int w, int h, int r)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dx = (x < r) ? r - x : ((x >= w - r) ? x - (w - r - 1) : 0);
                int dy = (y < r) ? r - y : ((y >= h - r) ? y - (h - r - 1) : 0);

                if (dx * dx + dy * dy <= r * r)
                    cols[y * w + x] = Color.white;
                else
                    cols[y * w + x] = Color.clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    private static Sprite CreateBorderSprite(int w, int h, int r, int borderWidth)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dxOuter = (x < r) ? r - x : ((x >= w - r) ? x - (w - r - 1) : 0);
                int dyOuter = (y < r) ? r - y : ((y >= h - r) ? y - (h - r - 1) : 0);
                bool insideOuter = (dxOuter * dxOuter + dyOuter * dyOuter <= r * r);

                int innerR = Mathf.Max(1, r - borderWidth);
                int innerW = w - 2 * borderWidth;
                int innerH = h - 2 * borderWidth;
                int inX = x - borderWidth;
                int inY = y - borderWidth;

                bool insideInner = false;
                if (inX >= 0 && inX < innerW && inY >= 0 && inY < innerH)
                {
                    int dxInner = (inX < innerR) ? innerR - inX : ((inX >= innerW - innerR) ? inX - (innerW - innerR - 1) : 0);
                    int dyInner = (inY < innerR) ? innerR - inY : ((inY >= innerH - innerR) ? inY - (innerH - innerR - 1) : 0);
                    insideInner = (dxInner * dxInner + dyInner * dyInner <= innerR * innerR);
                }

                cols[y * w + x] = (insideOuter && !insideInner) ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    private static Sprite CreateGearIconSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        float center = (size - 1) / 2f;
        float outerRadius = size * 0.45f;
        float toothRootRadius = size * 0.32f;
        float holeRadius = size * 0.14f;
        int teethCount = 6;
        float toothPitch = (Mathf.PI * 2f) / teethCount;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > outerRadius + 1f || dist < holeRadius - 1f)
                {
                    cols[y * size + x] = Color.clear;
                    continue;
                }

                float holeAlpha = Mathf.Clamp01(dist - holeRadius + 0.5f);

                float angle = Mathf.Atan2(dy, dx);
                if (angle < 0) angle += Mathf.PI * 2f;
                float toothPhase = Mathf.Repeat(angle, toothPitch) / toothPitch;

                // Lebar gigi: 42% dari slot sektor
                bool inToothSector = (toothPhase >= 0.04f && toothPhase <= 0.46f);
                float currentRadius = inToothSector ? outerRadius : toothRootRadius;

                float radialAlpha = Mathf.Clamp01(currentRadius - dist + 0.5f);
                float angularAlpha = 1f;
                if (dist > toothRootRadius && inToothSector)
                {
                    float distToEdge = Mathf.Min(Mathf.Abs(toothPhase - 0.04f), Mathf.Abs(toothPhase - 0.46f)) * toothPitch * dist;
                    angularAlpha = Mathf.Clamp01(distToEdge);
                }

                float finalAlpha = radialAlpha * angularAlpha * holeAlpha;
                cols[y * size + x] = new Color(1f, 1f, 1f, finalAlpha);
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}

public class TouchPadEventReceiver : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public TouchControlUI controller;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller != null) controller.OnTouchPadDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (controller != null) controller.OnTouchPadUp();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (controller != null) controller.OnTouchPadUp();
    }
}

public class DualButtonEventReceiver : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum ButtonType
    {
        Walk,
        Jump
    }

    public TouchControlUI controller;
    public ButtonType buttonType;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller == null) return;
        if (buttonType == ButtonType.Walk) controller.OnWalkDown();
        else controller.OnJumpDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (controller == null) return;
        if (buttonType == ButtonType.Walk) controller.OnWalkUp();
        else controller.OnJumpUp();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (controller == null) return;
        if (buttonType == ButtonType.Walk) controller.OnWalkUp();
        else controller.OnJumpUp();
    }
}
