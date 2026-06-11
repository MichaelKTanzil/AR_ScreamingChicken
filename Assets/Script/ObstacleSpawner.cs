using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Pengaturan Seed")]
    public int gameSeed = 12345; 
    public bool gunakanSeedAcak = false;

    [Header("Pengaturan Rintangan")]
    public GameObject obstaclePrefab;
    public float spawnInterval = 2f;
    private float timer;

    void Start()
    {
        if (!gunakanSeedAcak)
        {
            Random.InitState(gameSeed);
            Debug.Log("Game pakai Seed: " + gameSeed);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnObstacle();
            timer = 0;
        }
    }

    void SpawnObstacle()
    {
        float randomY = Random.Range(0f, 1.5f);
        
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y + randomY, transform.position.z);
        
        GameObject rintanganBaru = Instantiate(obstaclePrefab, spawnPos, transform.rotation);
        
    }
}