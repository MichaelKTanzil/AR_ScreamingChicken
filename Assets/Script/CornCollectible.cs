using UnityEngine;

public class CornCollectible : MonoBehaviour
{
    [Header("Pengaturan Skor")]
    [Tooltip("Jumlah bonus skor yang didapat saat mengambil jagung")]
    public int scoreValue = 10;

    [Header("Audio Pickup")]
    [Tooltip("Efek suara saat diambil. Jika kosong, akan menggunakan suara ding prosedural.")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.30f;

    [Header("Animasi Melayang & Berputar")]
    public float rotateSpeed = 120f;
    public float bobSpeed = 3.5f;
    public float bobAmplitude = 0.025f;

    private Vector3 baseLocalPos;
    private bool isCollected = false;
    private static AudioClip defaultDingClip;
    private static AudioSource sharedAudioSource;

    void Awake()
    {
        // Pastikan ada collider trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.12f;
            sc.center = new Vector3(0, 0.05f, 0);
        }
        else
        {
            col.isTrigger = true;
        }

        // Jika belum ada visual child atau mesh, bangun visual jagung Minecraft voxel
        if (transform.childCount == 0 && GetComponent<MeshFilter>() == null)
        {
            BuildVoxelCornVisual();
        }
    }

    void Start()
    {
        baseLocalPos = transform.localPosition;
    }

    void Update()
    {
        // Animasi rotasi horizontal
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);

        // Animasi naik-turun halus (bobbing)
        float newY = baseLocalPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = new Vector3(baseLocalPos.x, newY, baseLocalPos.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || !GameUIManager.isPlaying) return;

        // Cek apakah yang menyentuh adalah Ayam / Player
        ChickenController chicken = other.GetComponent<ChickenController>();
        if (chicken == null) chicken = other.GetComponentInParent<ChickenController>();

        if (chicken != null || other.CompareTag("Player"))
        {
            isCollected = true;
            Collect();
        }
    }

    void Collect()
    {
        // Tambahkan skor bonus
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddBonusScore(scoreValue);
        }

        // Mainkan SFX
        PlaySound();

        // Hancurkan objek jagung
        Destroy(gameObject);
    }

    void PlaySound()
    {
        AudioClip clipToPlay = pickupSound != null ? pickupSound : GetOrCreateDingClip();
        if (clipToPlay == null) return;

        if (sharedAudioSource == null)
        {
            GameObject sfxHost = new GameObject("_GlobalCornSFX");
            DontDestroyOnLoad(sfxHost);
            sharedAudioSource = sfxHost.AddComponent<AudioSource>();
            sharedAudioSource.playOnAwake = false;
        }

        sharedAudioSource.PlayOneShot(clipToPlay, soundVolume);
    }

    private static AudioClip GetOrCreateDingClip()
    {
        if (defaultDingClip != null) return defaultDingClip;

        int sampleRate = 44100;
        float duration = 0.22f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Arpeggio ceria nada tinggi (987Hz B5 -> 1318Hz E6)
            float freq = t < 0.07f ? 987.77f : 1318.51f;
            float envelope = Mathf.Exp(-14f * (t < 0.07f ? t : t - 0.07f));
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.20f;
        }

        defaultDingClip = AudioClip.Create("CornPickupDing", sampleCount, 1, sampleRate, false);
        defaultDingClip.SetData(samples, 0);
        return defaultDingClip;
    }

    /// <summary>
    /// Membangun visual jagung bergaya voxel 3D Minecraft yang imut
    /// </summary>
    void BuildVoxelCornVisual()
    {
        GameObject visualRoot = new GameObject("Visual_Corn");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.transform.localPosition = Vector3.zero;

        // Material Jagung Kuning Emas dengan fallback shader yang aman
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Mobile/Diffuse");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material yellowMat = shader != null ? new Material(shader) : null;
        if (yellowMat != null)
        {
            Color yColor = new Color(1.0f, 0.82f, 0.12f);
            yellowMat.color = yColor;
            if (yellowMat.HasProperty("_BaseColor")) yellowMat.SetColor("_BaseColor", yColor);
        }

        Material orangeMat = shader != null ? new Material(shader) : null;
        if (orangeMat != null)
        {
            Color oColor = new Color(0.95f, 0.65f, 0.08f);
            orangeMat.color = oColor;
            if (orangeMat.HasProperty("_BaseColor")) orangeMat.SetColor("_BaseColor", oColor);
        }

        Material greenMat = shader != null ? new Material(shader) : null;
        if (greenMat != null)
        {
            Color gColor = new Color(0.28f, 0.72f, 0.22f);
            greenMat.color = gColor;
            if (greenMat.HasProperty("_BaseColor")) greenMat.SetColor("_BaseColor", gColor);
        }

        // 1. Batang Jagung Utama (Yellow Cob)
        CreateCube(visualRoot.transform, new Vector3(0, 0.06f, 0), new Vector3(0.065f, 0.11f, 0.065f), yellowMat);

        // 2. Kernel oranye pemanis (aksen voxel)
        CreateCube(visualRoot.transform, new Vector3(0, 0.065f, 0), new Vector3(0.072f, 0.04f, 0.072f), orangeMat);

        // 3. Daun Kulit Jagung Bawah Kiri & Kanan (Green Husk Leaves)
        GameObject leaf1 = CreateCube(visualRoot.transform, new Vector3(-0.035f, 0.02f, 0), new Vector3(0.025f, 0.065f, 0.05f), greenMat);
        leaf1.transform.localRotation = Quaternion.Euler(0, 0, 18f);

        GameObject leaf2 = CreateCube(visualRoot.transform, new Vector3(0.035f, 0.02f, 0), new Vector3(0.025f, 0.065f, 0.05f), greenMat);
        leaf2.transform.localRotation = Quaternion.Euler(0, 0, -18f);
    }

    GameObject CreateCube(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "VoxelPart";
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = localScale;

        // Hapus collider pada bagian visual agar tidak bentrok dengan trigger utama
        Collider c = cube.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Renderer r = cube.GetComponent<Renderer>();
        if (r != null && mat != null) r.material = mat;

        return cube;
    }
}
