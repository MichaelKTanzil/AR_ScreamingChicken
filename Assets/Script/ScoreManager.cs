using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    [Header("Pengaturan Score")]
    public float scoreMultiplier = 10f;

    private float accumulatedDistance = 0f;
    private MicDetection mic;
    private float threshold = 0.15f;
    private float moveSpeedMultiplier = 6f;
    private float maxSpeed = 6f;

    // [BARU] Gembok untuk menahan skor sebelum arena diletakkan
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

        if (scoreText != null)
        {
            scoreText.text = Mathf.FloorToInt(accumulatedDistance * scoreMultiplier).ToString();
        }
    }

    public void ResetScore()
    {
        accumulatedDistance = 0f;
        // isArenaPlaced = false; // Kunci kembali gemboknya saat game di-reset
        if (scoreText != null) scoreText.text = "0";
    }

    // [BARU] Fungsi untuk membuka gembok skor dari script AR
    public void StartCountingScore()
    {
        isArenaPlaced = true;
    }
}