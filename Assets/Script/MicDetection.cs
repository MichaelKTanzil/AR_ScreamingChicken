using UnityEngine;
using System.Linq;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class MicDetection : MonoBehaviour
{
    public enum InputControlMode
    {
        Voice,
        Touch
    }

    public static MicDetection instance;

    [Header("Mode Kontrol")]
    [Tooltip("Pilih kontrol: Voice (Mikrofon) atau Touch (Layar Sentuh)")]
    public InputControlMode currentControlMode = InputControlMode.Voice;

    [Header("Emulasi Teriakan (Touch Mode)")]
    [Tooltip("Jika aktif (centang), ayam akan LANGSUNG melompat seketika saat jari pertama kali menyentuh layar tanpa perlu menunggu charge.")]
    public bool instantJumpOnTouch = false;

    [Tooltip("Nilai loudness awal saat pertama kali menyentuh layar. Set di bawah jumpThreshold (misal 0.5f) agar maju pelan dulu saat ditahan.")]
    public float initialTouchLoudness = 0.5f;

    [Tooltip("Batas maksimum loudness saat menahan sentuhan")]
    public float maxTouchLoudness = 3.5f;

    [Tooltip("Kecepatan naiknya suara saat ditahan dari initial menuju max (charge rate per detik)")]
    public float touchChargeSpeed = 3.0f;

    [Tooltip("Kecepatan turunnya suara saat sentuhan dilepas kembali ke 0")]
    public float touchDecaySpeed = 15f;

    [Tooltip("Nilai loudness saat menahan tombol JALAN (di bawah jumpThreshold 1.2f). Default 0.88f agar jalan terasa kencang dan responsif.")]
    public float touchWalkLoudness = 0.88f;

    private bool isTouchHolding = false;
    private bool isWalkHolding = false;
    private bool isJumpHolding = false;
    private float currentTouchLoudness = 0f;
    private bool wasTouchHoldingLastFrame = false;
    private bool wasJumpHoldingLastFrame = false;

    [Header("Output Suara Final")]
    [Tooltip("Nilai ini yang dibaca oleh Ayam dan Arena.")]
    public float loudness = 0f; 
    public float outputMultiplier = 3f; 

    [Header("Scream Meter (Peak dB Tracking)")]
    [Tooltip("Puncak desibel teriakan pemain yang tercatat selama run berlangsung.")]
    public float peakDecibelsInRun = 0f;

    [Tooltip("Offset kalibrasi akustik dB SPL agar akurat 1:1 dengan alat Sound Meter / Browser. Default 90f.")]
    public float decibelCalibrationOffset = 90f;

    [Header("Sensitivitas Mikrofon")]
    [Tooltip("Pengali sensitivitas mikrofon (0.5x s/d 2.5x). Default 1.0x. Nilai ini disimpan di PlayerPrefs.")]
    [Range(0.5f, 2.5f)]
    public float micSensitivity = 1.0f;

    private float smoothedMeterRMS = 0.003f;

    [Header("Anti-Kaget (Smoothing)")]
    [Tooltip("Semakin kecil angka ini, pergerakan makin mulus & kebal suara tajam mendadak")]
    public float smoothSpeed = 6f; 

    [Header("Auto-Kalibrasi (Real-time)")]
    public float noiseLevel = 0.02f; 
    public float voiceLevel = 0.16f;

    [Header("Kecepatan Adaptasi AI")]
    public float noiseAdaptSpeed = 0.5f; 
    public float voiceAdaptSpeed = 5f;   
    public float voiceDecaySpeed = 0.3f; 

    private AudioClip micClip;
    private string device;
    private int sampleWindow = 1024;
    private float[] waveData = new float[1024];

    private float baseNoiseFloor; 

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        baseNoiseFloor = noiseLevel; 

        // Muat preferensi kontrol yang tersimpan
        if (PlayerPrefs.HasKey("ScreamingChicken_ControlMode"))
        {
            currentControlMode = (InputControlMode)PlayerPrefs.GetInt("ScreamingChicken_ControlMode", 0);
        }

        // Muat preferensi sensitivitas mic yang tersimpan
        if (PlayerPrefs.HasKey("ScreamingChicken_MicSensitivity"))
        {
            micSensitivity = PlayerPrefs.GetFloat("ScreamingChicken_MicSensitivity", 1.0f);
        }

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        Invoke("InitMic", 1f);
    }

    public void SetControlMode(InputControlMode mode)
    {
        currentControlMode = mode;
        isTouchHolding = false;
        isWalkHolding = false;
        isJumpHolding = false;
        wasTouchHoldingLastFrame = false;
        wasJumpHoldingLastFrame = false;
        currentTouchLoudness = 0f;
        loudness = 0f;
        ResetPeakDecibels();
        PlayerPrefs.SetInt("ScreamingChicken_ControlMode", (int)mode);
        PlayerPrefs.Save();
    }

    public void SetMicSensitivity(float sensitivity)
    {
        micSensitivity = Mathf.Clamp(sensitivity, 0.5f, 2.5f);
        PlayerPrefs.SetFloat("ScreamingChicken_MicSensitivity", micSensitivity);
        PlayerPrefs.Save();
    }

    public void ToggleControlMode()
    {
        SetControlMode(currentControlMode == InputControlMode.Voice ? InputControlMode.Touch : InputControlMode.Voice);
    }

    public void SetTouchHolding(bool holding)
    {
        isTouchHolding = holding;
    }

    public void SetWalkHolding(bool holding)
    {
        isWalkHolding = holding;
    }

    public void SetJumpHolding(bool holding)
    {
        isJumpHolding = holding;
    }

    public bool IsTouchHolding() => isTouchHolding || isWalkHolding || isJumpHolding;

    public float GetTouchLoudnessRatio() => maxTouchLoudness > 0f ? Mathf.Clamp01(currentTouchLoudness / maxTouchLoudness) : 0f;

    public float GetCurrentDecibels()
    {
        // ── MODE TOUCH / TAP: Disable dB metering (hanya aktif di Mode Teriak / Voice) ──
        if (currentControlMode == InputControlMode.Touch)
        {
            return 0f;
        }

        // ── MODE VOICE / SUARA: Standar Sound Level Meter IEC 61672 (dBFS + Calibration Offset) ──
        // Menggunakan IEC FAST time-weighted RMS agar 1:1 akurat dengan Browser & Sound Meter App
        float rms = Mathf.Max(smoothedMeterRMS, 0.00001f);
        float dbfs = 20f * Mathf.Log10(rms);

        // Konversi ke perkiraan dB SPL menggunakan offset kalibrasi browser (default 90 dB)
        float baseDb = dbfs + decibelCalibrationOffset;

        // Baseline lantai kebisingan ruangan ambient (38 dB)
        if (baseDb <= 38f)
        {
            return 38f;
        }

        // Rentang suara bicara normal s/d teriakan sedang 80 dB (Sama persis dengan browser!)
        if (baseDb <= 80f)
        {
            return baseDb;
        }

        // Di atas 80 dB: Pada mic HP, level suara mendekati batas saturasi digital (-10 dBFS s/d 0 dBFS).
        // Berikan kurva ekspansi natural agar teriakan sangat kencang dapat mencapai 88 - 105 dB.
        float excess = baseDb - 80f; // Rentang 0 s/d ~10 dB (karena max baseDb di 0 dBFS adalah 90 dB)
        float finalDb = 80f + excess * 1.5f + (excess * excess) * 0.06f;

        return Mathf.Clamp(finalDb, 80f, 115f);
    }

    public void ResetPeakDecibels()
    {
        peakDecibelsInRun = 0f;
        smoothedMeterRMS = 0.003f;
    }

    /// <summary>
    /// Menghitung loudness khusus untuk pergerakan maju arena.
    /// Di Mode Touch: jika hanya menekan LOMPAT tanpa JALAN, ayam hanya melompat di tempat (kecepatan maju = 0).
    /// Untuk melompat maju, pemain harus menahan JALAN (kiri) sambil menekan LOMPAT (kanan).
    /// </summary>
    public float GetMoveLoudness()
    {
        if (currentControlMode == InputControlMode.Touch)
        {
            // Di mode touch, arena HANYA bergerak maju jika pemain menahan tombol/area JALAN
            if (!isWalkHolding) return 0f;

            // Jika menahan JALAN sekaligus menahan LOMPAT, beri akselerasi lompat maju yang mantap
            if (isJumpHolding) return 1.05f;

            return touchWalkLoudness;
        }

        return loudness;
    }

    void InitMic()
    {
        if (Microphone.devices.Length > 0)
        {
            device = Microphone.devices[0];
            micClip = Microphone.Start(device, true, 1, 44100);
        }
    }

    void Update()
    {
        // ── MODE TOUCH SCREEN: Suara mikrofon dinonaktifkan sepenuhnya ──
        if (currentControlMode == InputControlMode.Touch)
        {
            if (GameUIManager.isPlaying)
            {
                if (isJumpHolding)
                {
                    // LOMPAT: Frame pertama langsung meloncat seketika (loudness 2.2f di atas jumpThreshold 2.0f)
                    if (!wasJumpHoldingLastFrame)
                    {
                        currentTouchLoudness = Mathf.Max(currentTouchLoudness, 2.2f);
                    }

                    // Tahan tombol lompat untuk charge jump (semakin tinggi hingga batas max)
                    currentTouchLoudness += touchChargeSpeed * Time.deltaTime;
                    currentTouchLoudness = Mathf.Min(currentTouchLoudness, maxTouchLoudness);
                }
                else if (isWalkHolding)
                {
                    // JALAN: Stabil di touchWalkLoudness (default 0.88f)
                    // Memberikan laju jalan yang kencang & responsif, tanpa memicu lompat (di bawah jumpThreshold 1.2f)
                    currentTouchLoudness = Mathf.MoveTowards(currentTouchLoudness, touchWalkLoudness, 20f * Time.deltaTime);
                }
                else if (isTouchHolding)
                {
                    // Fallback satu tombol (jika ada pemanggilan SetTouchHolding)
                    if (!wasTouchHoldingLastFrame)
                    {
                        currentTouchLoudness = instantJumpOnTouch ? Mathf.Max(currentTouchLoudness, initialTouchLoudness) : initialTouchLoudness;
                    }
                    currentTouchLoudness += touchChargeSpeed * Time.deltaTime;
                    currentTouchLoudness = Mathf.Min(currentTouchLoudness, maxTouchLoudness);
                }
                else
                {
                    // Saat dilepas, suara turun kembali ke 0
                    currentTouchLoudness = Mathf.MoveTowards(currentTouchLoudness, 0f, touchDecaySpeed * Time.deltaTime);
                }
            }
            else
            {
                currentTouchLoudness = 0f;
            }

            wasJumpHoldingLastFrame = isJumpHolding;
            wasTouchHoldingLastFrame = isTouchHolding;
            loudness = currentTouchLoudness;
            return; // Mode Touch: tidak membaca mikrofon dan tidak ada metering dB!
        }

        wasTouchHoldingLastFrame = false;
        wasJumpHoldingLastFrame = false;

        // ── MODE VOICE CONTROL: Deteksi RMS dari mikrofon ──
        float rawRMS = GetRMSLoudnessFromMic() * micSensitivity;

        // IEC 61672 FAST time-weighting (tau = 125ms = 0.125s)
        // Standar industri Sound Level Meter: meredam spike transien sesaat (pop/koin/ketukan casing)
        // dan mengukur sound pressure level rata-rata secara akurat sesuai standar akustik
        float fastAlpha = 1f - Mathf.Exp(-Time.deltaTime / 0.125f);
        smoothedMeterRMS = Mathf.Lerp(smoothedMeterRMS, rawRMS, fastAlpha);

        if (rawRMS < noiseLevel * 2.5f)
        {
            noiseLevel = Mathf.Lerp(noiseLevel, rawRMS, Time.deltaTime * noiseAdaptSpeed);
        }

        if (noiseLevel < baseNoiseFloor)
        {
            noiseLevel = baseNoiseFloor;
        }

        if (rawRMS > voiceLevel)
        {
            voiceLevel = Mathf.Lerp(voiceLevel, rawRMS, Time.deltaTime * voiceAdaptSpeed);
        }
        else
        {
            voiceLevel = Mathf.Lerp(voiceLevel, noiseLevel + 0.02f, Time.deltaTime * voiceDecaySpeed);
        }

        float bottomLimit = noiseLevel * 1.5f; 
        float range = Mathf.Max(0.001f, voiceLevel - bottomLimit); 
        float normalized = Mathf.Max(0f, rawRMS - bottomLimit) / range;
        
        float targetLoudness = normalized * outputMultiplier;

        if (targetLoudness <= 0.05f) 
        {
            loudness = Mathf.Lerp(loudness, 0f, Time.deltaTime * 25f); 
        }
        else 
        {
            loudness = Mathf.Lerp(loudness, targetLoudness, Time.deltaTime * smoothSpeed);
        }

        // Track peak dB saat bermain di Voice mode (setelah loudness dismooth dan difilter dari noise)
        if (GameUIManager.isPlaying)
        {
            float currentDb = GetCurrentDecibels();
            if (currentDb > peakDecibelsInRun) peakDecibelsInRun = currentDb;
        }
    }

    float GetRMSLoudnessFromMic()
    {
        if (micClip == null) return 0f;

        int micPos = Microphone.GetPosition(device) - (sampleWindow + 1);
        if (micPos < 0) return 0f;

        micClip.GetData(waveData, micPos);

        float sum = 0f;
        for (int i = 0; i < sampleWindow; i++)
        {
            sum += waveData[i] * waveData[i];
        }
        return Mathf.Sqrt(sum / sampleWindow);
    }
}