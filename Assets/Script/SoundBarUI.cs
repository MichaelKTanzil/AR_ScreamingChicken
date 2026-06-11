using UnityEngine;
using UnityEngine.UI;

public class SoundBarUI : MonoBehaviour
{
    private MicDetection mic;

    [Header("UI")]
    public Image fillBar;

    [Header("Pengaturan Bar")]
    public float maxLoudness = 5f;
    public float smoothSpeed = 8f;

    [Header("Warna (Bawah → Atas)")]
    public Color colorLow    = Color.green;
    public Color colorMid    = Color.yellow;
    public Color colorHigh   = new Color(1f, 0.3f, 0f);
    public Color colorDanger = Color.red;

    private float currentFill = 0f;

    void Start()
    {
        mic = GameObject.Find("GameManager").GetComponent<MicDetection>();
    }

    void Update()
    {
        if (mic == null || fillBar == null) return;

        float targetFill = Mathf.Clamp01(mic.loudness / maxLoudness);
        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
        fillBar.fillAmount = currentFill;

        Color targetColor;
        if (currentFill < 0.4f)
            targetColor = Color.Lerp(colorLow, colorMid, currentFill / 0.4f);
        else if (currentFill < 0.6f)
            targetColor = Color.Lerp(colorMid, colorHigh, (currentFill - 0.4f) / 0.2f);
        else
            targetColor = Color.Lerp(colorHigh, colorDanger, (currentFill - 0.6f) / 0.4f);

        fillBar.color = targetColor;
    }
}