using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menampilkan badge rekor desibel (dB) teriakan pemain secara dinamis tepat di atas tanda Game Over.
/// Dilengkapi dengan gelar/rating lucu berdasarkan seberapa kencang pemain berteriak selama run berlangsung.
/// </summary>
public class GameOverScreamMeter : MonoBehaviour
{
    public struct ScreamTierInfo
    {
        public string tierTitle;
        public string funnyQuote;
        public Color badgeColor;
        public Color barColor;
    }

    private GameObject badgeRoot;
    private RectTransform badgeRect;
    private Image badgeBg;
    private Image badgeBorder;
    private TextMeshProUGUI modeLabel;
    private TextMeshProUGUI dbNumberText;
    private TextMeshProUGUI funnyQuoteText;
    private RectTransform meterBarFill;
    private Image meterBarFillImg;

    private Coroutine activeAnimRoutine;

    public static ScreamTierInfo GetTierInfo(float db)
    {
        if (db < 50f)
        {
            return new ScreamTierInfo
            {
                tierTitle = "BISIKAN SANTAI",
                funnyQuote = "\"Ayamnya Masih Ngantuk...\"",
                badgeColor = new Color(0.20f, 0.85f, 0.55f, 1f),
                barColor = new Color(0.20f, 0.85f, 0.55f, 1f)
            };
        }
        else if (db < 70f)
        {
            return new ScreamTierInfo
            {
                tierTitle = "TERIAKAN SOPAN",
                funnyQuote = "\"Suara Kayak Mau Pesan Es Teh\"",
                badgeColor = new Color(0.25f, 0.75f, 1f, 1f),
                barColor = new Color(0.25f, 0.75f, 1f, 1f)
            };
        }
        else if (db < 85f)
        {
            return new ScreamTierInfo
            {
                tierTitle = "TERIAKAN NGEGAS!",
                funnyQuote = "\"Mulai Emosi Jiwa Nih Bos!\"",
                badgeColor = new Color(1f, 0.75f, 0.15f, 1f),
                barColor = new Color(1f, 0.75f, 0.15f, 1f)
            };
        }
        else if (db < 95f)
        {
            return new ScreamTierInfo
            {
                tierTitle = "TERIAKAN SUPER GAHAR!",
                funnyQuote = "\"Tetangga Sebelah Mulai Curiga!\"",
                badgeColor = new Color(1f, 0.45f, 0.15f, 1f),
                barColor = new Color(1f, 0.45f, 0.15f, 1f)
            };
        }
        else if (db < 105f)
        {
            return new ScreamTierInfo
            {
                tierTitle = "SUARA SIRINE!",
                funnyQuote = "\"Kaca Jendela Bergetar Hebat!!\"",
                badgeColor = new Color(1f, 0.20f, 0.45f, 1f),
                barColor = new Color(1f, 0.20f, 0.45f, 1f)
            };
        }
        else
        {
            return new ScreamTierInfo
            {
                tierTitle = "LEDAKAN NUKLIR!!",
                funnyQuote = "\"PITA SUARA RESMI JEBOL!\"",
                badgeColor = new Color(1f, 0.88f, 0.10f, 1f),
                barColor = new Color(1f, 0.25f, 0.20f, 1f)
            };
        }
    }

    public void ShowMeter()
    {
        EnsureUI();

        // ── MODE TOUCH / TAP: Disable dB metering dan sembunyikan meter teriakan sama sekali ──
        if (MicDetection.instance != null && MicDetection.instance.currentControlMode == MicDetection.InputControlMode.Touch)
        {
            HideMeter();
            return;
        }

        if (badgeRoot == null) return;

        badgeRoot.SetActive(true);

        float peakDb = 38f;
        string modeStr = "MIKROFON (VOICE)";
        if (MicDetection.instance != null)
        {
            peakDb = Mathf.Max(35f, MicDetection.instance.peakDecibelsInRun);
        }

        int finalScore = 0;
        if (ScoreManager.instance != null)
        {
            finalScore = ScoreManager.instance.GetCurrentScore();
        }

        if (activeAnimRoutine != null) StopCoroutine(activeAnimRoutine);
        activeAnimRoutine = StartCoroutine(AnimateMeter(peakDb, modeStr, finalScore));
    }

    public void HideMeter()
    {
        if (activeAnimRoutine != null)
        {
            StopCoroutine(activeAnimRoutine);
            activeAnimRoutine = null;
        }
        if (badgeRoot != null)
        {
            badgeRoot.SetActive(false);
        }
    }

    private void EnsureUI()
    {
        if (badgeRoot != null) return;

        Transform existing = transform.Find("ScreamMeterBadge");
        if (existing != null)
        {
            badgeRoot = existing.gameObject;
            badgeRect = badgeRoot.GetComponent<RectTransform>();
            Transform borderT = badgeRoot.transform.Find("Border");
            if (borderT != null) badgeBorder = borderT.GetComponent<Image>();
            Transform modeT = badgeRoot.transform.Find("Text_ModeLabel");
            if (modeT != null) modeLabel = modeT.GetComponent<TextMeshProUGUI>();
            Transform dbT = badgeRoot.transform.Find("Text_MainRating");
            if (dbT != null) dbNumberText = dbT.GetComponent<TextMeshProUGUI>();
            Transform quoteT = badgeRoot.transform.Find("Text_FunnyQuote");
            if (quoteT != null) funnyQuoteText = quoteT.GetComponent<TextMeshProUGUI>();
            Transform fillT = badgeRoot.transform.Find("MeterBar_Bg/MeterBar_Fill");
            if (fillT != null)
            {
                meterBarFill = fillT.GetComponent<RectTransform>();
                meterBarFillImg = fillT.GetComponent<Image>();
            }
            return;
        }

        TMP_FontAsset sharedFont = null;
        if (ScoreManager.instance != null && ScoreManager.instance.scoreText != null)
        {
            sharedFont = ScoreManager.instance.scoreText.font;
        }
        if (sharedFont == null)
        {
            TextMeshProUGUI anyTmp = Object.FindFirstObjectByType<TextMeshProUGUI>();
            if (anyTmp != null) sharedFont = anyTmp.font;
        }

        // ── Card Root ──
        badgeRoot = new GameObject("ScreamMeterBadge");
        badgeRoot.transform.SetParent(transform, false);

        badgeRect = badgeRoot.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
        badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(0f, 585f);
        badgeRect.sizeDelta = new Vector2(750f, 165f);

        badgeBg = badgeRoot.AddComponent<Image>();
        badgeBg.sprite = CreateRoundedRectSprite(128, 128, 24);
        badgeBg.type = Image.Type.Sliced;
        badgeBg.color = new Color(0.06f, 0.08f, 0.13f, 0.94f);

        // ── Glowing Border ──
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(badgeRoot.transform, false);
        RectTransform borderRt = borderObj.AddComponent<RectTransform>();
        borderRt.anchorMin = Vector2.zero;
        borderRt.anchorMax = Vector2.one;
        borderRt.sizeDelta = Vector2.zero;

        badgeBorder = borderObj.AddComponent<Image>();
        badgeBorder.sprite = CreateBorderSprite(128, 128, 24, 4);
        badgeBorder.type = Image.Type.Sliced;
        badgeBorder.color = new Color(1f, 0.55f, 0.15f, 0.9f);
        badgeBorder.raycastTarget = false;

        // ── 1. Top Mode Tag (e.g. "[ REKOR TERIAKAN (VOICE) ]") ──
        GameObject modeObj = new GameObject("Text_ModeLabel");
        modeObj.transform.SetParent(badgeRoot.transform, false);
        RectTransform modeRt = modeObj.AddComponent<RectTransform>();
        modeRt.anchorMin = new Vector2(0f, 1f);
        modeRt.anchorMax = new Vector2(1f, 1f);
        modeRt.pivot = new Vector2(0.5f, 1f);
        modeRt.anchoredPosition = new Vector2(0f, -12f);
        modeRt.sizeDelta = new Vector2(700f, 26f);

        modeLabel = modeObj.AddComponent<TextMeshProUGUI>();
        if (sharedFont != null) modeLabel.font = sharedFont;
        modeLabel.fontSize = 22f;
        modeLabel.alignment = TextAlignmentOptions.Center;
        modeLabel.color = new Color(0.70f, 0.75f, 0.85f, 0.85f);
        modeLabel.text = "[ REKOR TERIAKAN RUN INI ]";

        // ── 2. Main Decibel & Tier Title ──
        GameObject mainTxtObj = new GameObject("Text_MainRating");
        mainTxtObj.transform.SetParent(badgeRoot.transform, false);
        RectTransform mainRt = mainTxtObj.AddComponent<RectTransform>();
        mainRt.anchorMin = new Vector2(0f, 1f);
        mainRt.anchorMax = new Vector2(1f, 1f);
        mainRt.pivot = new Vector2(0.5f, 1f);
        mainRt.anchoredPosition = new Vector2(0f, -38f);
        mainRt.sizeDelta = new Vector2(700f, 48f);

        dbNumberText = mainTxtObj.AddComponent<TextMeshProUGUI>();
        if (sharedFont != null) dbNumberText.font = sharedFont;
        dbNumberText.fontSize = 36f;
        dbNumberText.fontStyle = FontStyles.Bold;
        dbNumberText.alignment = TextAlignmentOptions.Center;
        dbNumberText.color = Color.white;
        dbNumberText.text = "0.0 dB - BISIKAN SANTAI";

        // ── 3. Funny Quote Line ──
        GameObject quoteObj = new GameObject("Text_FunnyQuote");
        quoteObj.transform.SetParent(badgeRoot.transform, false);
        RectTransform quoteRt = quoteObj.AddComponent<RectTransform>();
        quoteRt.anchorMin = new Vector2(0f, 1f);
        quoteRt.anchorMax = new Vector2(1f, 1f);
        quoteRt.pivot = new Vector2(0.5f, 1f);
        quoteRt.anchoredPosition = new Vector2(0f, -86f);
        quoteRt.sizeDelta = new Vector2(700f, 32f);

        funnyQuoteText = quoteObj.AddComponent<TextMeshProUGUI>();
        if (sharedFont != null) funnyQuoteText.font = sharedFont;
        funnyQuoteText.fontSize = 23f;
        funnyQuoteText.fontStyle = FontStyles.Italic;
        funnyQuoteText.alignment = TextAlignmentOptions.Center;
        funnyQuoteText.color = new Color(0.88f, 0.92f, 0.98f, 0.95f);
        funnyQuoteText.text = "\"Ayamnya Masih Ngantuk...\"";

        // ── 4. Decibel Meter Bar (Track Background) ──
        GameObject barBgObj = new GameObject("MeterBar_Bg");
        barBgObj.transform.SetParent(badgeRoot.transform, false);
        RectTransform barBgRt = barBgObj.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.5f, 0f);
        barBgRt.anchorMax = new Vector2(0.5f, 0f);
        barBgRt.pivot = new Vector2(0.5f, 0f);
        barBgRt.anchoredPosition = new Vector2(0f, 14f);
        barBgRt.sizeDelta = new Vector2(660f, 10f);

        Image barBgImg = barBgObj.AddComponent<Image>();
        barBgImg.sprite = CreateRoundedRectSprite(64, 64, 5);
        barBgImg.type = Image.Type.Sliced;
        barBgImg.color = new Color(0.15f, 0.18f, 0.25f, 0.85f);

        // Meter Bar (Fill)
        GameObject barFillObj = new GameObject("MeterBar_Fill");
        barFillObj.transform.SetParent(barBgObj.transform, false);
        meterBarFill = barFillObj.AddComponent<RectTransform>();
        meterBarFill.anchorMin = new Vector2(0f, 0f);
        meterBarFill.anchorMax = new Vector2(0f, 1f);
        meterBarFill.pivot = new Vector2(0f, 0.5f);
        meterBarFill.anchoredPosition = Vector2.zero;
        meterBarFill.sizeDelta = new Vector2(0f, 0f);

        meterBarFillImg = barFillObj.AddComponent<Image>();
        meterBarFillImg.sprite = CreateRoundedRectSprite(64, 64, 5);
        meterBarFillImg.type = Image.Type.Sliced;
        meterBarFillImg.color = new Color(1f, 0.55f, 0.15f, 1f);
    }

    private IEnumerator AnimateMeter(float targetDb, string modeStr, int finalScore)
    {
        modeLabel.text = $"<color=#FFD700>SKOR: {finalScore} POIN</color>   •   <color=#B0C4DE>{modeStr}</color>";

        ScreamTierInfo finalTier = GetTierInfo(targetDb);
        badgeBorder.color = finalTier.badgeColor;
        meterBarFillImg.color = finalTier.barColor;
        funnyQuoteText.text = finalTier.funnyQuote;

        // Pop-in bounce scale
        badgeRect.localScale = Vector3.one * 0.6f;
        CanvasGroup cg = badgeRoot.GetComponent<CanvasGroup>();
        if (cg == null) cg = badgeRoot.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        float animDuration = 0.55f;
        float elapsed = 0f;
        float maxBarWidth = 660f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);

            // Pop bounce curve
            float scale;
            if (t < 0.65f)
            {
                scale = Mathf.Lerp(0.6f, 1.06f, t / 0.65f);
            }
            else
            {
                scale = Mathf.Lerp(1.06f, 1.0f, (t - 0.65f) / 0.35f);
            }
            badgeRect.localScale = Vector3.one * scale;
            cg.alpha = Mathf.Clamp01(t * 2.5f);

            // Roll number up
            float currentRollDb = Mathf.Lerp(30f, targetDb, Mathf.SmoothStep(0f, 1f, t));
            ScreamTierInfo currentTier = GetTierInfo(currentRollDb);

            dbNumberText.text = $"<color=white>{currentRollDb:F1} dB</color>  -  <color=#{ColorUtility.ToHtmlStringRGB(currentTier.badgeColor)}>{currentTier.tierTitle}</color>";

            // Bar fill ratio (normalized between 35 dB and 115 dB)
            float fillRatio = Mathf.Clamp01((currentRollDb - 35f) / 80f);
            meterBarFill.sizeDelta = new Vector2(fillRatio * maxBarWidth, 0f);

            yield return null;
        }

        // Finalize values
        badgeRect.localScale = Vector3.one;
        cg.alpha = 1f;
        dbNumberText.text = $"<color=white>{targetDb:F1} dB</color>  -  <color=#{ColorUtility.ToHtmlStringRGB(finalTier.badgeColor)}>{finalTier.tierTitle}</color>";
        funnyQuoteText.text = finalTier.funnyQuote;
        meterBarFill.sizeDelta = new Vector2(Mathf.Clamp01((targetDb - 35f) / 80f) * maxBarWidth, 0f);
        activeAnimRoutine = null;
    }

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
}
