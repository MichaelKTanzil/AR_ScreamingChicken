using UnityEngine;

public class ChickenController : MonoBehaviour
{
    private Rigidbody rb;
    private MicDetection mic;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isStartTransformSet = false;

    public void SetStartTransform(Vector3 pos, Quaternion rot)
    {
        startPosition = pos;
        startRotation = rot;
        isStartTransformSet = true;
    }

    public Vector3 GetStartPosition() => startPosition;
    public Quaternion GetStartRotation() => startRotation;
    public bool IsStartTransformSet() => isStartTransformSet;

    [Header("Pengaturan Lompat")]
    public float jumpPowerMultiplier = 2f;
    public float jumpThreshold = 2f;
    public float groundCheckDistance = 0.5f;

    [Header("Audio Suara Ayam")]
    [Tooltip("Klip suara ayam saat melompat. Jika kosong, otomatis menghasilkan suara kokok/kotek ayam ceria.")]
    public AudioClip jumpSound;
    public AudioClip[] randomJumpSounds;
    [Range(0f, 1f)]
    public float jumpSoundVolume = 0.35f;

    private AudioSource chickenAudioSource;
    private static AudioClip defaultChickenJumpClip;

    [Header("Coyote Time")]
    public float coyoteTime = 0.15f;
    private float coyoteTimeCounter; 

    public LayerMask groundLayer;

    private bool isGrounded;

    private Collider chickenCollider;
    private float groundSurfaceY = -999f;
    private bool hasLandedOnGround = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mic = GameObject.Find("GameManager").GetComponent<MicDetection>();

        // AudioSource khusus ayam
        chickenAudioSource = GetComponent<AudioSource>();
        if (chickenAudioSource == null)
        {
            chickenAudioSource = gameObject.AddComponent<AudioSource>();
        }
        chickenAudioSource.playOnAwake = false;
        chickenAudioSource.spatialBlend = 0f;
        
        // Utamakan BoxCollider yang aktif (bukan MeshCollider yang disabled)
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol != null) chickenCollider = boxCol;
        else chickenCollider = GetComponent<Collider>();
        
        if (chickenCollider != null)
        {
            PhysicsMaterial pm = new PhysicsMaterial("ChickenNoFriction");
            pm.dynamicFriction = 0f;
            pm.staticFriction = 0f;
            pm.frictionCombine = PhysicsMaterialCombine.Minimum;
            pm.bounciness = 0f;
            chickenCollider.material = pm;
        }

        if (!isStartTransformSet)
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        ChickenCustomizer.ApplyColorToChicken(gameObject);
    }

    void Update()
    {
        if (!GameUIManager.isPlaying) return; 

        // Cek apakah ayam sedang menapak di atas tanah (Layer 6 = Ground / Platform)
        int mask = groundLayer.value != 0 ? groundLayer.value : (1 << 6);
        RaycastHit groundHit;
        // Jarak 0.25f pas: dari pusat ayam (0.16m ke kaki) + 9cm ke bawah
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, 0.25f, mask, QueryTriggerInteraction.Ignore);
        Debug.DrawRay(transform.position, Vector3.down * 0.25f, isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            // Catat ketinggian ASLI permukaan tanah tempat berpijak
            groundSurfaceY = groundHit.point.y;
            hasLandedOnGround = true;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (coyoteTimeCounter > 0f && mic.loudness > jumpThreshold)
        {
            Lompat();
        }

        // Cek jatuh ke jurang (hanya aktif setelah ayam pernah mendarat di arena)
        if (hasLandedOnGround)
        {
            // Jika ayam jatuh 0.35m di bawah permukaan lantai tempatnya berpijak
            if (transform.position.y < groundSurfaceY - 0.35f)
            {
                GameOver("Jatuh ke dalam jurang");
            }
        }
        else
        {
            // Fallback saat pertama kali spawn di udara jika entah kenapa meleset dari arena
            if (transform.position.y < startPosition.y - 3f)
            {
                GameOver("Spawn di udara meleset dari arena");
            }
        }
    }

    void Lompat()
    {
        coyoteTimeCounter = 0f;
        
        rb.linearVelocity = new Vector3(0, 0, 0);

        float dynamicJumpForce = (mic.loudness - jumpThreshold) * jumpPowerMultiplier;
        dynamicJumpForce = Mathf.Max(dynamicJumpForce, 4f); 
        rb.AddForce(Vector3.up * dynamicJumpForce, ForceMode.Impulse);

        PlayJumpChickenSound();
    }

    void PlayJumpChickenSound()
    {
        if (chickenAudioSource == null) return;

        AudioClip clipToPlay = null;
        if (randomJumpSounds != null && randomJumpSounds.Length > 0)
        {
            clipToPlay = randomJumpSounds[Random.Range(0, randomJumpSounds.Length)];
        }
        if (clipToPlay == null)
        {
            clipToPlay = jumpSound != null ? jumpSound : GetOrCreateChickenJumpClip();
        }

        if (clipToPlay != null)
        {
            // Sedikit variasi pitch agar tiap lompatan terdengar hidup, dinamis dan kocak!
            chickenAudioSource.pitch = Random.Range(0.94f, 1.14f);
            chickenAudioSource.PlayOneShot(clipToPlay, jumpSoundVolume);
        }
    }

    private static AudioClip GetOrCreateChickenJumpClip()
    {
        if (defaultChickenJumpClip != null) return defaultChickenJumpClip;

        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Sweep nada kokok ayam ceria (naik cepat 420Hz -> 720Hz lalu meluncur turun ke 360Hz)
            float pitch = t < 0.045f
                ? Mathf.Lerp(420f, 720f, t / 0.045f)
                : Mathf.Lerp(720f, 360f, (t - 0.045f) / 0.175f);

            // Modulasi getaran tenggorokan ayam (flutter)
            float flutter = 1.0f + 0.25f * Mathf.Sin(2f * Mathf.PI * 34f * t);

            // Resonansi paruh & harmonik khas unggas
            float phase = 2f * Mathf.PI * pitch * t;
            float wave = (
                0.58f * Mathf.Sin(phase) +
                0.32f * Mathf.Sin(phase * 2f) +
                0.18f * Mathf.Sin(phase * 3f) +
                0.10f * Mathf.Sin(phase * 4f)
            ) * flutter;

            // Envelope suara (attack cepat, decay empuk)
            float env = t < 0.015f ? (t / 0.015f) : Mathf.Exp(-16f * (t - 0.015f));
            samples[i] = wave * env * 0.30f;
        }

        defaultChickenJumpClip = AudioClip.Create("ChickenJumpCluck", sampleCount, 1, sampleRate, false);
        defaultChickenJumpClip.SetData(samples, 0);
        return defaultChickenJumpClip;
    }

    void GameOver(string reason = "Unknown")
    {
        if (!GameUIManager.isPlaying) return;
        Debug.Log("GAME OVER: " + reason);
        rb.linearVelocity = Vector3.zero;

        // Nonaktifkan collider saat mati agar ayam jatuh bebas ke bawah dan tidak tersangkut balok
        foreach (var col in GetComponents<Collider>())
        {
            col.enabled = false;
        }

        GameUIManager.instance.TriggerGameOver(); 
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!GameUIManager.isPlaying) return;
        if (other.CompareTag("Obstacle"))
        {
            Debug.Log("AWWW! KENA KAKTUS: " + other.gameObject.name);
            GameOver("Menabrak Kaktus");
        }
    }

    public void ResetChicken()
    {
        transform.position = startPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.rotation = startRotation; 

        hasLandedOnGround = false;
        groundSurfaceY = -999f;

        // Aktifkan kembali BoxCollider
        foreach (var col in GetComponents<Collider>())
        {
            if (col is BoxCollider)
            {
                col.enabled = true;
                chickenCollider = col;
            }
        }

        ChickenCustomizer.ApplyColorToChicken(gameObject);
    }
}