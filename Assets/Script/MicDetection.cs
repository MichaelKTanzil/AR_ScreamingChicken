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

    private bool isTouchHolding = false;
    private float currentTouchLoudness = 0f;
    private bool wasTouchHoldingLastFrame = false;

    [Header("Output Suara Final")]
    [Tooltip("Nilai ini yang dibaca oleh Ayam dan Arena.")]
    public float loudness = 0f; 
    public float outputMultiplier = 3f; 

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
    private int sampleWindow = 256;

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
        wasTouchHoldingLastFrame = false;
        currentTouchLoudness = 0f;
        loudness = 0f;
        PlayerPrefs.SetInt("ScreamingChicken_ControlMode", (int)mode);
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

    public bool IsTouchHolding() => isTouchHolding;

    public float GetTouchLoudnessRatio() => maxTouchLoudness > 0f ? Mathf.Clamp01(currentTouchLoudness / maxTouchLoudness) : 0f;

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
            if (isTouchHolding && GameUIManager.isPlaying)
            {
                // Frame pertama saat ditekan/di-hold: mulai dari initialTouchLoudness agar langsung jalan pelan
                if (!wasTouchHoldingLastFrame)
                {
                    currentTouchLoudness = instantJumpOnTouch ? Mathf.Max(currentTouchLoudness, initialTouchLoudness) : initialTouchLoudness;
                }

                // Semakin lama ditahan, suara emulasi semakin tinggi hingga batas max
                currentTouchLoudness += touchChargeSpeed * Time.deltaTime;
                currentTouchLoudness = Mathf.Min(currentTouchLoudness, maxTouchLoudness);
            }
            else
            {
                // Saat dilepas, suara turun kembali ke 0
                currentTouchLoudness = Mathf.MoveTowards(currentTouchLoudness, 0f, touchDecaySpeed * Time.deltaTime);
            }

            wasTouchHoldingLastFrame = isTouchHolding;
            loudness = currentTouchLoudness;
            return; // Tidak membaca mikrofon sama sekali!
        }

        wasTouchHoldingLastFrame = false;

        // ── MODE VOICE CONTROL: Deteksi RMS dari mikrofon ──
        float rawRMS = GetRMSLoudnessFromMic();

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
    }

    float GetRMSLoudnessFromMic()
    {
        if (micClip == null) return 0f;

        float[] waveData = new float[sampleWindow];
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