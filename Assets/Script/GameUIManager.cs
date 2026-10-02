using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;
    public static bool isPlaying = false;

    [Header("UI Panels")]
    public GameObject splashPanel;
    public GameObject mainMenuPanel;
    public GameObject gameOverPanel;

    [Header("AR References")]
    public ARPlaneManager arPlaneManager;
    public ARPlacementManager arPlacementManager;

    private GameObject activeLevelGenerator;
    private GameObject activePlayer;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        isPlaying = false;

        if (arPlaneManager != null)     arPlaneManager.enabled     = false;
        if (arPlacementManager != null) arPlacementManager.enabled = false;

        // Hide AR Hints initially so they don't block the screen raycasts on main menu
        if (arPlacementManager != null)
        {
            if (arPlacementManager.hintPanel != null)    arPlacementManager.hintPanel.SetActive(false);
            if (arPlacementManager.tapHintPanel != null) arPlacementManager.tapHintPanel.SetActive(false);
        }

        // Pastikan TouchControlUI aktif untuk mengelola mode suara/sentuh
        if (FindFirstObjectByType<TouchControlUI>() == null)
        {
            gameObject.AddComponent<TouchControlUI>();
        }

        splashPanel.SetActive(false);
        mainMenuPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        splashPanel.SetActive(true);
        Invoke("SwitchToMenu", 2.5f);
    }

    void SwitchToMenu()
    {
        splashPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnStartButtonPressed()
    {
        CancelInvoke("SwitchToMenu");
        splashPanel.SetActive(false);
        mainMenuPanel.SetActive(false);

        if (MicDetection.instance != null) MicDetection.instance.ResetPeakDecibels();

        if (arPlaneManager != null)     arPlaneManager.enabled     = true;
        if (arPlacementManager != null) arPlacementManager.enabled = true;

        isPlaying = true;
    }

    private void FindActiveGameObjects()
    {
        activeLevelGenerator = Object.FindFirstObjectByType<LevelGenerator>()?.gameObject;
        
        activePlayer = null;
        foreach (var player in Object.FindObjectsByType<ChickenController>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(player.transform.position, new Vector3(500f, 0f, 500f)) > 10f)
            {
                activePlayer = player.gameObject;
                break;
            }
        }
    }

    public void TriggerGameOver()
    {
        // Tangkap satu sampel desibel terakhir barangkali pemain teriak histeris saat mati (hanya mode Suara/Voice)
        if (MicDetection.instance != null && MicDetection.instance.currentControlMode == MicDetection.InputControlMode.Voice)
        {
            float lastMomentDb = MicDetection.instance.GetCurrentDecibels();
            if (lastMomentDb > MicDetection.instance.peakDecibelsInRun)
            {
                MicDetection.instance.peakDecibelsInRun = lastMomentDb;
            }
        }

        isPlaying = false;
        if (MicDetection.instance != null) MicDetection.instance.SetTouchHolding(false);
        FindActiveGameObjects();

        if (arPlaneManager != null)     arPlaneManager.enabled     = false;
        if (arPlacementManager != null) arPlacementManager.enabled = false;
        if (BGMManager.instance != null) BGMManager.instance.SetVolume(0.05f);

        gameOverPanel.SetActive(true);

        // Tampilkan badge Scream Meter rekor desibel tepat di atas tanda Game Over
        GameOverScreamMeter meter = gameOverPanel.GetComponent<GameOverScreamMeter>();
        if (meter == null) meter = gameOverPanel.AddComponent<GameOverScreamMeter>();
        meter.ShowMeter();
    }

    public void OnRestartButtonPressed()
    {
        if (MicDetection.instance != null)
        {
            MicDetection.instance.SetTouchHolding(false);
            MicDetection.instance.ResetPeakDecibels();
        }

        GameOverScreamMeter meter = gameOverPanel.GetComponent<GameOverScreamMeter>();
        if (meter != null) meter.HideMeter();

        gameOverPanel.SetActive(false);
        FindActiveGameObjects();
        ScoreManager.instance.ResetScore(); 

        if (activeLevelGenerator != null && activePlayer != null)
        {
            ArenaMover[] movers = Object.FindObjectsByType<ArenaMover>(FindObjectsSortMode.None);
            foreach (ArenaMover mover in movers) mover.ResetPosisi();

            activeLevelGenerator.GetComponent<LevelGenerator>().ResetLevel();

            activePlayer.GetComponent<ChickenController>().ResetChicken();

            if (BGMManager.instance != null) BGMManager.instance.SetVolume(0.3f);

            isPlaying = true;
        }
    }

    public void OnExitButtonPressed()
    {
        Debug.Log("Game Keluar...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}