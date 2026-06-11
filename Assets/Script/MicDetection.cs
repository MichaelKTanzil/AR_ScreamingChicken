using UnityEngine;
using System.Linq;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class MicDetection : MonoBehaviour
{
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

    void Start()
    {
        baseNoiseFloor = noiseLevel; 

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        Invoke("InitMic", 1f);
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