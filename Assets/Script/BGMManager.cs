using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager instance;
    private AudioSource bgmSource;

    void Awake()
    {
        // Memastikan hanya ada satu BGMManager (Singleton)
        if (instance == null)
        {
            instance = this;
            bgmSource = GetComponent<AudioSource>();
            
            // Opsional: Jika kamu berencana menambah Scene baru nanti, 
            // ini mencegah lagunya terputus saat pindah Scene.
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Fungsi tambahan kalau kamu mau mengecilkan volume saat ayam mati
    public void SetVolume(float newVolume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = newVolume;
        }
    }
}