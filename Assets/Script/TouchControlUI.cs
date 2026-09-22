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

    [Header("Opsi Mode di Pengaturan")]
    public Button voiceModeCardButton;
    public Image voiceCardBackground;
    public Image voiceCardBorder;
    public TextMeshProUGUI voiceStatusText;

    public Button touchModeCardButton;
    public Image touchCardBackground;
    public Image touchCardBorder;
    public TextMeshProUGUI touchStatusText;

    [Header("Panel Tahan Layar (Touch Screaming)")]
    public GameObject touchPadRoot;
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
    [Tooltip("Jika true, pemain dapat menahan area layar mana saja untuk berteriak saat dalam Touch Mode")]
    public bool enableFullScreenTouch = true;

    private bool isSettingsOpen = false;
    private bool isPadPressed = false;
    private bool isScreenHolding = false;
    private Vector3 originalPadScale = Vector3.one;
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

        if (touchPadTitleText != null)
        {
            touchPadTitleText.text = "TAHAN UNTUK TERIAK";
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
            if (isPadPressed || isScreenHolding)
            {
                isPadPressed = false;
                isScreenHolding = false;
                MicDetection.instance.SetTouchHolding(false);
            }
            return;
        }

        // Full Screen Touch handling
        if (enableFullScreenTouch)
        {
            HandleFullScreenTouch();
        }

        bool isHolding = isPadPressed || isScreenHolding;
        MicDetection.instance.SetTouchHolding(isHolding);

        UpdatePadVisualFeedback(isHolding);
    }

    // ──────────────────────────────────────────────────────────────────
    // KONTROL SETTINGS MODAL
    // ──────────────────────────────────────────────────────────────────
    public void OpenSettings()
    {
        isSettingsOpen = true;
        isPadPressed = false;
        isScreenHolding = false;
        if (MicDetection.instance != null) MicDetection.instance.SetTouchHolding(false);

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
    }

    // ──────────────────────────────────────────────────────────────────
    // FULL SCREEN TOUCH
    // ──────────────────────────────────────────────────────────────────
    private void HandleFullScreenTouch()
    {
        if (EventSystem.current == null || isSettingsOpen) return;

        bool touchingNow = false;

        // Touchscreen (Mobile)
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.isPressed)
                {
                    int touchId = touch.touchId.ReadValue();
                    if (EventSystem.current.IsPointerOverGameObject(touchId))
                    {
                        GameObject hovered = GetPointerOverObject(touch.position.ReadValue());
                        if (hovered != null && touchPadRoot != null && 
                            (hovered == touchPadRoot || hovered.transform.IsChildOf(touchPadRoot.transform)))
                        {
                            touchingNow = true;
                            break;
                        }
                    }
                    else
                    {
                        touchingNow = true;
                        break;
                    }
                }
            }
        }

        // Mouse (Editor / Standalone)
        if (!touchingNow && Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (EventSystem.current.IsPointerOverGameObject())
            {
                GameObject hovered = GetPointerOverObject(mousePos);
                if (hovered != null && touchPadRoot != null && 
                    (hovered == touchPadRoot || hovered.transform.IsChildOf(touchPadRoot.transform)))
                {
                    touchingNow = true;
                }
            }
            else
            {
                touchingNow = true;
            }
        }

        isScreenHolding = touchingNow;
    }

    private GameObject GetPointerOverObject(Vector2 screenPos)
    {
        var pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0 ? results[0].gameObject : null;
    }

    public void OnTouchPadDown()
    {
        if (isSettingsOpen) return;
        isPadPressed = true;
        if (MicDetection.instance != null) MicDetection.instance.SetTouchHolding(true);
    }

    public void OnTouchPadUp()
    {
        isPadPressed = false;
        if (!isScreenHolding && MicDetection.instance != null) MicDetection.instance.SetTouchHolding(false);
    }

    private void UpdatePadVisualFeedback(bool isHolding)
    {
        if (touchPadRoot == null) return;

        float ratio = MicDetection.instance.GetTouchLoudnessRatio();

        Vector3 targetScale = isHolding ? originalPadScale * 1.06f : originalPadScale;
        touchPadRoot.transform.localScale = Vector3.Lerp(touchPadRoot.transform.localScale, targetScale, Time.deltaTime * 12f);

        if (touchPadGlowBorder != null)
        {
            Color glowColor;
            if (ratio < 0.4f)
                glowColor = Color.Lerp(Color.green, Color.yellow, ratio / 0.4f);
            else if (ratio < 0.7f)
                glowColor = Color.Lerp(Color.yellow, new Color(1f, 0.4f, 0f), (ratio - 0.4f) / 0.3f);
            else
                glowColor = Color.Lerp(new Color(1f, 0.4f, 0f), Color.red, (ratio - 0.7f) / 0.3f);

            glowColor.a = isHolding ? 0.9f : 0.35f;
            touchPadGlowBorder.color = glowColor;
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
            cardRt.sizeDelta = new Vector2(760f, 780f);

            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.sprite = CreateRoundedRectSprite(256, 256, 32);
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -40f);
            titleRt.sizeDelta = new Vector2(650f, 60f);

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
            xRt.anchoredPosition = new Vector2(-25f, -25f);
            xRt.sizeDelta = new Vector2(60f, 60f);

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

            // Card 1: VOICE MODE
            BuildModeOptionCard(cardObj.transform, sharedFont, -150f,
                "KONTROL SUARA (MIC)", 
                "Gunakan suara/teriakan asli ke mikrofon untuk menggerakkan dan melompatkan ayam.",
                () => SelectControlMode(MicDetection.InputControlMode.Voice),
                out voiceModeCardButton, out voiceCardBackground, out voiceCardBorder, out voiceStatusText);

            // Card 2: TOUCH MODE
            BuildModeOptionCard(cardObj.transform, sharedFont, -345f,
                "LAYAR SENTUH (TOUCH)", 
                "Tahan layar untuk mengemulasikan teriakan (mic dimatikan). Makin lama ditahan, makin tinggi lompatnya!",
                () => SelectControlMode(MicDetection.InputControlMode.Touch),
                out touchModeCardButton, out touchCardBackground, out touchCardBorder, out touchStatusText);

            // Tombol Tutup di Bawah
            GameObject closeBtnObj = new GameObject("Button_CloseModal");
            closeBtnObj.transform.SetParent(cardObj.transform, false);
            RectTransform closeRt = closeBtnObj.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 40f);
            closeRt.sizeDelta = new Vector2(660f, 85f);

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

            settingsPanelRoot.SetActive(false);
        }

        // 3. Buat Touch Pad di Bawah Layar jika belum ada
        if (touchPadRoot == null)
        {
            touchPadRoot = new GameObject("TouchScreamPad");
            touchPadRoot.transform.SetParent(canvas.transform, false);

            RectTransform padRt = touchPadRoot.AddComponent<RectTransform>();
            padRt.anchorMin = new Vector2(0.5f, 0.5f);
            padRt.anchorMax = new Vector2(0.5f, 0.5f);
            padRt.pivot = new Vector2(0.5f, 0.5f);
            padRt.anchoredPosition = new Vector2(0f, -760f);
            padRt.sizeDelta = new Vector2(560f, 180f);

            touchPadBackground = touchPadRoot.AddComponent<Image>();
            touchPadBackground.sprite = CreateRoundedRectSprite(256, 128, 24);
            touchPadBackground.type = Image.Type.Sliced;
            touchPadBackground.color = new Color(0.08f, 0.1f, 0.15f, 0.85f);

            var padReceiver = touchPadRoot.AddComponent<TouchPadEventReceiver>();
            padReceiver.controller = this;

            // Glow Border
            GameObject borderObj = new GameObject("GlowBorder");
            borderObj.transform.SetParent(touchPadRoot.transform, false);

            RectTransform borderRt = borderObj.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(6f, 6f);

            touchPadGlowBorder = borderObj.AddComponent<Image>();
            touchPadGlowBorder.sprite = CreateBorderSprite(256, 128, 24, 6);
            touchPadGlowBorder.type = Image.Type.Sliced;
            touchPadGlowBorder.color = new Color(0.96f, 0.52f, 0.12f, 0.4f);
            touchPadGlowBorder.raycastTarget = false;

            // Title Text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(touchPadRoot.transform, false);

            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.4f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(10f, 0f);
            titleRt.offsetMax = new Vector2(-10f, -10f);

            touchPadTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) touchPadTitleText.font = sharedFont;
            touchPadTitleText.fontSize = 32f;
            touchPadTitleText.fontStyle = FontStyles.Bold;
            touchPadTitleText.alignment = TextAlignmentOptions.Center;
            touchPadTitleText.color = Color.white;
            touchPadTitleText.text = "TAHAN UNTUK TERIAK";
            touchPadTitleText.raycastTarget = false;

            // Subtitle Text
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(touchPadRoot.transform, false);

            RectTransform subRt = subObj.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0f);
            subRt.anchorMax = new Vector2(1f, 0.45f);
            subRt.offsetMin = new Vector2(10f, 10f);
            subRt.offsetMax = new Vector2(-10f, 0f);

            touchPadSubText = subObj.AddComponent<TextMeshProUGUI>();
            if (sharedFont != null) touchPadSubText.font = sharedFont;
            touchPadSubText.fontSize = 20f;
            touchPadSubText.alignment = TextAlignmentOptions.Center;
            touchPadSubText.color = new Color(0.85f, 0.88f, 0.92f, 0.9f);
            touchPadSubText.text = "Makin lama ditahan, makin tinggi lompatnya!";
            touchPadSubText.raycastTarget = false;

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
        rt.sizeDelta = new Vector2(660f, 160f);

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
        tRt.anchorMin = new Vector2(0f, 0.55f);
        tRt.anchorMax = new Vector2(0.7f, 1f);
        tRt.offsetMin = new Vector2(25f, 0f);
        tRt.offsetMax = new Vector2(-10f, -15f);

        TextMeshProUGUI tTxt = tObj.AddComponent<TextMeshProUGUI>();
        if (font != null) tTxt.font = font;
        tTxt.fontSize = 26f;
        tTxt.fontStyle = FontStyles.Bold;
        tTxt.alignment = TextAlignmentOptions.Left;
        tTxt.color = Color.white;
        tTxt.text = title;
        tTxt.raycastTarget = false;

        // Status Badge (PILIH / AKTIF)
        GameObject sObj = new GameObject("Status");
        sObj.transform.SetParent(cardObj.transform, false);
        RectTransform sRt = sObj.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.7f, 0.55f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.offsetMin = new Vector2(0f, 0f);
        sRt.offsetMax = new Vector2(-25f, -15f);

        statusTxt = sObj.AddComponent<TextMeshProUGUI>();
        if (font != null) statusTxt.font = font;
        statusTxt.fontSize = 22f;
        statusTxt.fontStyle = FontStyles.Bold;
        statusTxt.alignment = TextAlignmentOptions.Right;
        statusTxt.text = "PILIH";
        statusTxt.raycastTarget = false;

        // Description
        GameObject dObj = new GameObject("Desc");
        dObj.transform.SetParent(cardObj.transform, false);
        RectTransform dRt = dObj.AddComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 0.55f);
        dRt.offsetMin = new Vector2(25f, 15f);
        dRt.offsetMax = new Vector2(-25f, 0f);

        TextMeshProUGUI dTxt = dObj.AddComponent<TextMeshProUGUI>();
        if (font != null) dTxt.font = font;
        dTxt.fontSize = 18f;
        dTxt.alignment = TextAlignmentOptions.Left;
        dTxt.color = new Color(0.8f, 0.85f, 0.9f, 0.85f);
        dTxt.text = desc;
        dTxt.raycastTarget = false;
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
