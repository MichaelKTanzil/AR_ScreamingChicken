using UnityEngine;
using UnityEngine.EventSystems; // Wajib untuk mendeteksi Hover dan Click

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("SFX Klip Suara")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1f;

    // Pakai AudioSource statis agar semua tombol berbagi 1 sumber suara
    // (Biar tidak perlu capek pasang AudioSource di tiap tombol)
    private static AudioSource uiAudioSource;

    void Start()
    {
        // Otomatis membuat pemutar suara khusus UI jika belum ada
        if (uiAudioSource == null)
        {
            GameObject audioObj = new GameObject("UI_AudioPlayer");
            uiAudioSource = audioObj.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            
            // Opsional: Biar pemutar suaranya tidak hancur saat pindah scene
            DontDestroyOnLoad(audioObj); 
        }
    }

    // Fungsi ini terpanggil otomatis saat kursor/jari menyentuh area tombol (HOVER)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(hoverSound, volume);
        }
    }

    // Fungsi ini terpanggil otomatis saat tombol diklik/ditekan (CLICK)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(clickSound, volume);
        }
    }
}