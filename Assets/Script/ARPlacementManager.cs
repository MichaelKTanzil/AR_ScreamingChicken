using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ARRaycastManager))]
[RequireComponent(typeof(ARPlaneManager))]
public class ARPlacementManager : MonoBehaviour
{
    [Header("Prefab Arena")]
    public GameObject arenaPrefab;

    [Header("Pengaturan Posisi")]
    [Tooltip("Rotasi offset arena dalam derajat. Nilai -90 membuat lintasan (sumbu +X) membentang lurus ke depan menghadap kamera.")]
    public float kemiringanArena = -90f;

    [Header("AR Hint UI")]
    public GameObject hintPanel;
    public GameObject tapHintPanel;

    private GameObject        spawnedArena;
    private ARRaycastManager  raycastManager;
    private ARPlaneManager    planeManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    public static ARPlacementManager instance;

    private bool planeFound  = false;
    private bool arenaPlaced = false;

    public bool IsArenaPlaced() => arenaPlaced;

    void Awake()
    {
        instance = this;
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager   = GetComponent<ARPlaneManager>();

        // Ensure hints are deactivated initially to prevent blocking UI interactions
        if (hintPanel    != null) hintPanel.SetActive(false);
        if (tapHintPanel != null) tapHintPanel.SetActive(false);
    }

    void OnEnable()
    {
        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        ShowScanHint();
    }

    void OnDisable()
    {
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (!planeFound && args.added.Count > 0)
        {
            planeFound = true;
            ShowTapHint();
        }
    }

    void Update()
    {
        if (arenaPlaced) return;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();

            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                // Gunakan arah hadap horizontal kamera agar rotasi arena selalu konsisten lurus ke depan pandangan pemain
                // dan TIDAK miring/terdistorsi hanya karena pemain mengetuk sisi kiri atau kanan layar
                Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                cameraForward.y = 0;
                if (cameraForward.sqrMagnitude < 0.001f && Camera.main != null)
                {
                    cameraForward = Camera.main.transform.up;
                    cameraForward.y = 0;
                }
                cameraForward.Normalize();

                Quaternion rotasiDasar   = Quaternion.LookRotation(cameraForward);
                Quaternion rotasiMiring  = rotasiDasar * Quaternion.Euler(0, kemiringanArena, 0);

                spawnedArena = Instantiate(arenaPrefab, hitPose.position, rotasiMiring);

                spawnedArena.AddComponent<ARAnchor>();

                // Immediately swap default chicken with the chosen custom character model
                CharacterManager.ApplyCharacterInScene();

                arenaPlaced = true;
                HideAllHints();
                LockPlanes();

                // ---> [TAMBAHAN BARU] Buka gembok skor setelah arena terkunci di lantai! <---
                if (ScoreManager.instance != null)
                {
                    ScoreManager.instance.StartCountingScore();
                }
            }
        }
    }

    void ShowScanHint()
    {
        if (hintPanel    != null) hintPanel.SetActive(true);
        if (tapHintPanel != null) tapHintPanel.SetActive(false);
    }

    void ShowTapHint()
    {
        if (hintPanel    != null) hintPanel.SetActive(false);
        if (tapHintPanel != null) tapHintPanel.SetActive(true);
    }

    void HideAllHints()
    {
        if (hintPanel    != null) hintPanel.SetActive(false);
        if (tapHintPanel != null) tapHintPanel.SetActive(false);
    }

    void LockPlanes()
    {
        planeManager.enabled = false;
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(false);
        Debug.Log("Plane locked!");
    }
}