using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Pengaturan Block")]
    public GameObject blockPrefab;
    public Transform spawnPoint;

    [Header("Pengaturan Rintangan (Kaktus)")]
    public GameObject cactusPrefab;
    [Range(0f, 1f)] 
    public float cactusChance = 0.3f;
    
    [Header("Pengaturan Chunk (Jejeran Panjang)")]
    public int minBlocksPerChunk = 4;
    public int maxBlocksPerChunk = 8;
    
    [Header("Pengaturan Bolongan (Gap)")]
    public float minGap = 1.0f; 
    public float maxGap = 2.5f;   
    
    [Header("Skala Prefab")]
    public float blockSize = 0.2f; 

    private GameObject lastChunk; 
    private Vector3 lastSpawnPos;
    private float lastChunkWidth; 
    private float currentGap;

    void Start()
    {
        SpawnChunk();
    }

    void Update()
    {
        if (!GameUIManager.isPlaying) return;

        if (lastChunk != null)
        {
            float distanceMoved = Vector3.Distance(lastSpawnPos, lastChunk.transform.position);
            
            if (distanceMoved > (lastChunkWidth + currentGap))
            {
                SpawnChunk();
            }
        }
    }


   void SpawnChunk()
    {
        int totalBlocks = Random.Range(minBlocksPerChunk, maxBlocksPerChunk + 1);
        currentGap = Random.Range(minGap, maxGap);

        GameObject chunkWrapper = new GameObject("Chunk_" + totalBlocks);
        chunkWrapper.transform.position = spawnPoint.position;
        chunkWrapper.transform.rotation = spawnPoint.rotation;

        chunkWrapper.AddComponent<ArenaMover>();

        for (int i = 0; i < totalBlocks; i++)
        {
            GameObject singleBlock = Instantiate(blockPrefab, chunkWrapper.transform);
            singleBlock.transform.localPosition = new Vector3(i * blockSize, 0, 0);
            singleBlock.transform.localRotation = Quaternion.identity;

            if (i > 0 && Random.value <= cactusChance)
            {
                GameObject cactus = Instantiate(cactusPrefab, chunkWrapper.transform);
                
                cactus.transform.localPosition = new Vector3(i * blockSize, 0.08f, 0);
                cactus.transform.localRotation = Quaternion.identity;
            }
        }

        lastChunk = chunkWrapper;
        lastSpawnPos = spawnPoint.position; 
        lastChunkWidth = totalBlocks * blockSize; 

        chunkWrapper.transform.SetParent(this.transform);
    }

    public void ResetLevel()
    {
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Chunk"))
            {
                Destroy(child.gameObject);
            }
        }
        lastChunk = null;
        SpawnChunk();
    }   
}