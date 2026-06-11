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

        if (arPlaneManager != null)     arPlaneManager.enabled     = true;
        if (arPlacementManager != null) arPlacementManager.enabled = true;

        isPlaying = true;
    }

    private void FindActiveGameObjects()
    {
        activeLevelGenerator = GameObject.FindObjectOfType<LevelGenerator>()?.gameObject;
        
        activePlayer = null;
        foreach (var player in GameObject.FindObjectsOfType<ChickenController>())
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
        isPlaying = false;
        FindActiveGameObjects();

        if (arPlaneManager != null)     arPlaneManager.enabled     = false;
        if (arPlacementManager != null) arPlacementManager.enabled = false;
        if (BGMManager.instance != null) BGMManager.instance.SetVolume(0.05f);

        gameOverPanel.SetActive(true);
    }

    public void OnRestartButtonPressed()
    {
        gameOverPanel.SetActive(false);
        FindActiveGameObjects();
        ScoreManager.instance.ResetScore(); 

        if (activeLevelGenerator != null && activePlayer != null)
        {
            ArenaMover[] movers = GameObject.FindObjectsOfType<ArenaMover>();
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