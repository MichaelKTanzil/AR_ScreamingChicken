using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    [Header("Pengaturan Score")]
    [Tooltip("Pengali skor jarak tempuh. Diset 1f agar skor maksimal di kisaran ratusan untuk kemudahan sistem hadiah.")]
    public float scoreMultiplier = 1f;

    private float accumulatedDistance = 0f;
    private MicDetection mic;
    private float threshold = 0.15f;
    private float moveSpeedMultiplier = 3.5f;
    private float maxSpeed = 3.8f;

    // [BARU] Gembok untuk menahan skor sebelum arena diletakkbs
    private bool isArenaPlaced = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GameObject gm = GameObject.Find("GameManager");
        if (gm != null) mic = gm.GetComponent<MicDetection>();
    }

    private int bonusScore = 0;

    void Update()
    {
        // [UBAH] Tahan perhitungan kalau game belum mulai ATAU arena belum diletakkan
        if (!GameUIManager.isPlaying || !isArenaPlaced) return;

        if (mic == null)
        {
            GameObject gm = GameObject.Find("GameManager");
            if (gm != null) mic = gm.GetComponent<MicDetection>();
            return;
        }

        if (mic.loudness > threshold)
        {
            float calculatedSpeed = (mic.loudness - threshold) * moveSpeedMultiplier;
            calculatedSpeed = Mathf.Clamp(calculatedSpeed, 0f, maxSpeed);
            accumulatedDistance += calculatedSpeed * Time.deltaTime;
        }

        UpdateScoreDisplay();
    }

    public void AddBonusScore(int bonus)
    {
        bonusScore += bonus;
        UpdateScoreDisplay();
    }

    public int GetCurrentScore()
    {
        return Mathf.FloorToInt(accumulatedDistance * scoreMultiplier) + bonusScore;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = GetCurrentScore().ToString();
        }
    }

    public void ResetScore()
    {
        accumulatedDistance = 0f;
        bonusScore = 0;
        // isArenaPlaced = false; // Kunci kembali gemboknya saat game di-reset
        if (scoreText != null) scoreText.text = "0";
    }

    // [BARU] Fungsi untuk membuka gembok skor dari script AR
    public void StartCountingScore()
    {
        isArenaPlaced = true;
    }
}