using UnityEngine;

public class ArenaMover : MonoBehaviour
{
    private MicDetection mic;
    
    [Header("Pengaturan Kecepatan")]
    public float moveSpeedMultiplier = 6f;
    public float threshold = 0.15f;
    public float maxSpeed = 6f;

    public Vector3 startPos;

    void Start()
    {
        mic = GameObject.Find("GameManager").GetComponent<MicDetection>();
        startPos = transform.localPosition;

    }

    void Update()
    {
        if (!GameUIManager.isPlaying) return; 

        if (mic.loudness > threshold)
        {
            float calculatedSpeed = (mic.loudness - threshold) * moveSpeedMultiplier;
            calculatedSpeed = Mathf.Clamp(calculatedSpeed, 0f, maxSpeed);
            transform.Translate(-calculatedSpeed * Time.deltaTime, 0, 0); 
        }
    }

    public void ResetPosisi()
    {
        transform.localPosition = startPos;
    }
}