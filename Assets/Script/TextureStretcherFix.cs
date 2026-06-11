using UnityEngine;

public class TextureStretcherFix : MonoBehaviour
{
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateTiling();
    }

    void Update()
    {
        UpdateTiling();
    }

    void UpdateTiling()
    {
        float scaleX = transform.lossyScale.x;
        float scaleZ = transform.lossyScale.z;

        rend.material.mainTextureScale = new Vector2(scaleX, scaleZ);
    }
}