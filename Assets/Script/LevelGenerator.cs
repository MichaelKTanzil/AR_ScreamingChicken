using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Pengaturan Block")]
    public GameObject blockPrefab;
    public Transform spawnPoint;

    [Header("Pengaturan Rintangan (Kaktus)")]
    public GameObject cactusPrefab;
    [Range(0f, 1f)] 
    [Tooltip("Peluang sebuah chunk memiliki kaktus (50% dari chunk memiliki kaktus)")]
    public float cactusChunkChance = 0.50f;
    [Range(0f, 1f)] 
    public float cactusChance = 0.50f;
    [Tooltip("Maksimal kaktus dalam 1 chunk")]
    public int maxCactusPerChunk = 1;
    [Tooltip("Jarak minimal balok antar kaktus dalam 1 chunk agar tidak menjadi tembok")]
    public int minCactusSpacing = 4;
    
    [Header("Pengaturan Collectible (Biji Jagung)")]
    public GameObject cornPrefab;
    [Range(0f, 1f)]
    [Tooltip("Peluang jagung muncul di atas balok (15% agar tidak terlalu ramai)")]
    public float cornChance = 0.15f;
    [Tooltip("Apakah memunculkan jagung melayang di atas jurang sebagai tantangan high-risk?")]
    public bool spawnCornInGap = true;
    
    [Header("Pengaturan Chunk (Jejeran Panjang)")]
    public int minBlocksPerChunk = 5;
    public int maxBlocksPerChunk = 12;
    
    [Header("Pengaturan Bolongan (Gap)")]
    public float minGap = 0.45f; 
    public float maxGap = 1.05f;   
    
    [Header("Skala Prefab")]
    public float blockSize = 0.2f; 

    private GameObject lastChunk; 
    private Vector3 lastSpawnPos;
    private float lastChunkWidth; 
    private float currentGap;

    void Awake()
    {
        // Sanitasi nilai agar jarak/gap dan ukuran chunk pas dan fair
        if (maxGap > 1.25f) maxGap = 1.05f;
        if (minGap < 0.35f) minGap = 0.45f;
        if (minBlocksPerChunk < 4) minBlocksPerChunk = 5;
        if (maxBlocksPerChunk < 8) maxBlocksPerChunk = 12;
    }

    void Start()
    {
        InitializeChunks();
    }

    void Update()
    {
        if (!GameUIManager.isPlaying) return;

        if (lastChunk != null && spawnPoint != null)
        {
            float distanceMoved = Vector3.Distance(lastSpawnPos, lastChunk.transform.position);
            
            if (distanceMoved > (lastChunkWidth + currentGap))
            {
                int totalBlocks = Random.Range(minBlocksPerChunk, maxBlocksPerChunk + 1);
                float nextGap = Random.Range(minGap, maxGap);
                GameObject chunk = SpawnChunkAt(spawnPoint.position, totalBlocks, nextGap, true);

                lastChunk = chunk;
                lastSpawnPos = spawnPoint.position; 
                lastChunkWidth = totalBlocks * blockSize; 
                currentGap = nextGap;
            }
        }
    }

    /// <summary>
    /// Menginisialisasi chunk sejak awal game: menyambung langsung dari ujung SpawnPoint (arena awal)
    /// hingga titik SpawnerPoint, sehingga celah pertama sama persis dengan celah normal dan tidak ada anomali jarak jauh!
    /// </summary>
    void InitializeChunks()
    {
        Vector3 trackDir = (spawnPoint != null) ? spawnPoint.right : Vector3.right;

        // Cari platform awal (SpawnPoint) di Wrapper
        Transform startPlatform = null;
        if (transform.parent != null)
        {
            startPlatform = transform.parent.Find("SpawnPoint");
        }

        Vector3 startEdge;
        if (startPlatform != null)
        {
            // Sembunyikan MeshRenderer kubus putih default pada SpawnPoint
            MeshRenderer startMr = startPlatform.GetComponent<MeshRenderer>();
            if (startMr != null) startMr.enabled = false;

            float platformLength = startPlatform.lossyScale.x;
            Vector3 startLeft = startPlatform.position - trackDir * (platformLength * 0.5f);
            if (spawnPoint != null) startLeft.y = spawnPoint.position.y;

            int startBlockCount = Mathf.RoundToInt(platformLength / blockSize);
            // Spawn balok rumput/tanah (PlatformBlock) untuk arena permulaan, aman tanpa kaktus
            GameObject startChunk = SpawnChunkAt(startLeft, startBlockCount, 0f, false);
            startChunk.name = "Chunk_StartingPlatform";

            // Ujung terluar dari platform balok awal tempat ayam berpijak
            startEdge = startPlatform.position + trackDir * (platformLength * 0.5f);
        }
        else
        {
            startEdge = (spawnPoint != null) ? spawnPoint.position - trackDir * 4.0f : transform.position;
        }

        Vector3 currentPos = startEdge;
        bool isFirst = true;

        // Isi jalur mulai dari ujung arena awal hingga mencapai atau melewati titik spawnPoint
        Vector3 targetEndPos = (spawnPoint != null) ? spawnPoint.position : startEdge + trackDir * 5.0f;
        while (Vector3.Dot(currentPos - targetEndPos, trackDir) < 0f)
        {
            float gap = Random.Range(minGap, maxGap);
            currentPos += trackDir * gap;

            int totalBlocks = Random.Range(minBlocksPerChunk, maxBlocksPerChunk + 1);
            // Chunk pertama dibuat aman tanpa kaktus sebagai area permulaan lari
            GameObject chunk = SpawnChunkAt(currentPos, totalBlocks, gap, !isFirst);

            float chunkLength = totalBlocks * blockSize;
            currentPos += trackDir * chunkLength;

            lastChunk = chunk;
            lastSpawnPos = chunk.transform.position;
            lastChunkWidth = chunkLength;
            isFirst = false;
        }

        currentGap = Random.Range(minGap, maxGap);
    }

    GameObject SpawnChunkAt(Vector3 worldPos, int totalBlocks, float gapUsed, bool allowCactus)
    {
        GameObject chunkWrapper = new GameObject("Chunk_" + totalBlocks);
        chunkWrapper.transform.position = worldPos;
        chunkWrapper.transform.rotation = (spawnPoint != null) ? spawnPoint.rotation : transform.rotation;

        chunkWrapper.AddComponent<ArenaMover>();

        bool chunkHasCactus = (allowCactus && cactusPrefab != null && totalBlocks >= 5 && Random.value <= cactusChunkChance);
        int spawnedCactusCount = 0;
        int lastCactusIndex = -999;
        int maxAllowedCactus = (totalBlocks >= 9) ? 2 : maxCactusPerChunk;

        for (int i = 0; i < totalBlocks; i++)
        {
            GameObject singleBlock = Instantiate(blockPrefab, chunkWrapper.transform);
            singleBlock.transform.localPosition = new Vector3(i * blockSize, 0, 0);
            singleBlock.transform.localRotation = Quaternion.identity;

            bool spawnedCactusOnThisBlock = false;

            // ── SPAWN KAKTUS (DUDUK TEGAK DI ATAS BALOK, TIDAK MELAYANG) ──
            if (chunkHasCactus && 
                spawnedCactusCount < maxAllowedCactus && 
                i >= 2 && 
                i < totalBlocks - 2)
            {
                if ((i - lastCactusIndex) >= minCactusSpacing && Random.value <= cactusChance)
                {
                    GameObject cactus = Instantiate(cactusPrefab, chunkWrapper.transform);
                    // 0.10f adalah permukaan atas balok rumput (singleBlock berada di y=0 dengan tinggi 0.2)
                    cactus.transform.localPosition = new Vector3(i * blockSize, 0.10f, 0);
                    cactus.transform.localRotation = Quaternion.identity;
                    lastCactusIndex = i;
                    spawnedCactusCount++;
                    spawnedCactusOnThisBlock = true;
                }
            }

            // ── SPAWN BIJI JAGUNG (COLLECTIBLE DIKURANGI AGAR TIDAK RAMAI) ──
            if (!spawnedCactusOnThisBlock && Random.value <= cornChance)
            {
                // Melayang pas di ketinggian badan/mata ayam (y = 0.18f)
                SpawnCorn(chunkWrapper.transform, new Vector3(i * blockSize, 0.18f, 0));
            }
        }

        // ── JAGUNG DI ATAS JURANG (HIGH RISK REWARD - 25% SAJA) ──
        if (spawnCornInGap && gapUsed >= 0.55f && Random.value <= 0.25f)
        {
            // Melayang di tengah jurang di titik lengkung lompatan
            float gapCenterX = (totalBlocks * blockSize) + (gapUsed * 0.5f);
            SpawnCorn(chunkWrapper.transform, new Vector3(gapCenterX, 0.32f, 0));
        }

        chunkWrapper.transform.SetParent(this.transform);
        return chunkWrapper;
    }

    void SpawnCorn(Transform parent, Vector3 localPos)
    {
        GameObject corn;
        if (cornPrefab != null)
        {
            corn = Instantiate(cornPrefab, parent);
        }
        else
        {
            // Fallback otomatis: jika prefab belum di-assign, buat dari CornCollectible langsung
            corn = new GameObject("CornCollectible");
            corn.transform.SetParent(parent, false);
            corn.AddComponent<CornCollectible>();
        }

        corn.transform.localPosition = localPos;
        corn.transform.localRotation = Quaternion.identity;
    }

    public void ResetLevel()
    {
        // Bersihkan semua chunk berjalan di scene
        ArenaMover[] allMovers = Object.FindObjectsByType<ArenaMover>(FindObjectsSortMode.None);
        foreach (ArenaMover mover in allMovers)
        {
            if (mover != null && mover.gameObject.name.StartsWith("Chunk_"))
            {
                Destroy(mover.gameObject);
            }
        }

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Chunk"))
            {
                Destroy(child.gameObject);
            }
        }
        lastChunk = null;
        InitializeChunks();
    }
}