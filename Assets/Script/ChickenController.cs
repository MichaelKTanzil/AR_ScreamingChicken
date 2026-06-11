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

    [Header("Coyote Time")]
    public float coyoteTime = 0.15f;
    private float coyoteTimeCounter; 

    public LayerMask groundLayer;

    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mic = GameObject.Find("GameManager").GetComponent<MicDetection>();
        
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

        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
        Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (coyoteTimeCounter > 0f && mic.loudness > jumpThreshold)
        {
            Lompat();
        }

        if (transform.position.y < startPosition.y - 2f)
        {
            GameOver();
        }
    }

    void Lompat()
    {
        coyoteTimeCounter = 0f;
        
        rb.linearVelocity = new Vector3(0, 0, 0);

        float dynamicJumpForce = (mic.loudness - jumpThreshold) * jumpPowerMultiplier;
        dynamicJumpForce = Mathf.Max(dynamicJumpForce, 4f); 
        rb.AddForce(Vector3.up * dynamicJumpForce, ForceMode.Impulse);
    }

    void GameOver()
    {
        Debug.Log("NYUNGSEP BRO!");
        rb.linearVelocity = Vector3.zero;
        GameUIManager.instance.TriggerGameOver(); 
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!GameUIManager.isPlaying) return;
        if (other.CompareTag("Obstacle"))
        {
            Debug.Log("AWWW! KENA KAKTUS!");
            GameOver();
        }
    }

    public void ResetChicken()
    {
        transform.position = startPosition;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.identity;

        transform.rotation = startRotation; 

        ChickenCustomizer.ApplyColorToChicken(gameObject);
    }
}