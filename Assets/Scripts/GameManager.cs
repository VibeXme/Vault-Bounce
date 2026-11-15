using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Settings")]
    public int keysRequired = 5;
    public int currentKeys = 0;
    public ExitDoor exitDoor;

    [Header("Detection Settings")]
    public float maxDetection = 1.0f;
    public float detectionDrainRate = 0.6f;
    private float currentDetection = 0f;
    private bool isBeingWatched = false;

    [Header("UI References")]
    public TextMeshProUGUI keysText;
    public TextMeshProUGUI levelText;
    public Slider detectionSlider;

    private bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();
        if (levelText != null)
            levelText.text = "LEVEL " + (SceneManager.GetActiveScene().buildIndex + 1);
    }

    void Update()
    {
        if (isGameOver) return;

        // Drain detection meter when safe from security spotlights
        if (!isBeingWatched && currentDetection > 0f)
        {
            currentDetection = Mathf.Max(0f, currentDetection - detectionDrainRate * Time.deltaTime);
            UpdateDetectionUI();
        }
        isBeingWatched = false; // Reset every frame; spotlights set this true while overlapping
    }

    public void CollectKey()
    {
        if (isGameOver) return;
        currentKeys++;
        UpdateUI();

        if (currentKeys >= keysRequired && exitDoor != null)
        {
            exitDoor.UnlockDoor();
        }
    }

    public void AddDetection(float ratePerSecond)
    {
        if (isGameOver) return;
        isBeingWatched = true;
        currentDetection += ratePerSecond * Time.deltaTime;
        UpdateDetectionUI();

        if (currentDetection >= maxDetection)
        {
            TriggerGameOver("DETECTED BY SECURITY!");
        }
    }

    public void TriggerGameOver(string reason = "LOCKDOWN TRIGGERED")
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log(reason);
        Invoke(nameof(RestartLevel), 0.6f);
    }

    public void CompleteLevel()
    {
        if (isGameOver) return;
        isGameOver = true;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            SceneManager.LoadScene(0); // Loop back to Level 1 after beating final stage
    }

    private void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void UpdateUI()
    {
        if (keysText != null)
            keysText.text = $"KEYS: {currentKeys} / {keysRequired}";
    }

    private void UpdateDetectionUI()
    {
        if (detectionSlider != null)
        {
            detectionSlider.gameObject.SetActive(currentDetection > 0.01f);
            detectionSlider.value = currentDetection / maxDetection;
        }
    }
}
