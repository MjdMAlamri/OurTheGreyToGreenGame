using UnityEngine;
using TMPro;

public class RobotCollector : MonoBehaviour
{
    [Header("Timer Settings")]
    public float gameDuration = 120f;
    private float timeRemaining;
    private bool isTimerRunning = false;

    [Header("Targets")]
    public int targetTires = 2;
    public int targetBoxes = 2;
    public int targetTrash = 2;

    private int tires = 0;
    private int boxes = 0;
    private int trash = 0;

    private bool missionCompleted = false;

    // Track completion so audio/checkmark happens ONCE
    private bool trashDone = false;
    private bool boxesDone = false;
    private bool tiresDone = false;

    [Header("Folders")]
    public GameObject trashFolder;
    public GameObject forestFolder;
    public GameObject seedObject;

    [Header("Cameras")]
    public GameObject dirtyCamera;
    public GameObject cleanCamera;

    [Header("UI System")]
    public TextMeshProUGUI timerTextUI;   // ONLY time
    public TextMeshPro robotHeadText;     // optional

    [Header("UI Panels")]
    public GameObject startUIPanel;
    public GameObject loseUIPanel;
    public GameObject winUIPanel; // optional

    [Header("To-Do List UI (Top)")]
    public TextMeshProUGUI todoTrashText;
    public TextMeshProUGUI todoBoxesText;
    public TextMeshProUGUI todoTiresText;

    [Header("Checkmarks (enable when done)")]
    public GameObject trashCheckMark;
    public GameObject boxesCheckMark;
    public GameObject tiresCheckMark;

    [Header("SFX (one-shots)")]
    public AudioSource sfxSource;
    public AudioClip objectiveCompleteClip; // plays when each objective completes (trash/box/tire)

    [Header("Seed SFX (two stages)")]
    public AudioClip seedAppearClip;   // plays when seed appears (after all objectives)
    public AudioClip seedPlantedClip;  // plays when player collides with FinalSeed

    [Header("Environment Audio (looping)")]
    public AudioSource envSource;      // separate AudioSource recommended (loop)
    public AudioClip dirtyEnvLoop;     // plays while cleaning
    public AudioClip cleanEnvLoop;     // plays after planting seed

    void Start()
    {
        timeRemaining = gameDuration;

        // Environment setup
        if (forestFolder != null) forestFolder.SetActive(false);
        if (seedObject != null) seedObject.SetActive(false);
        if (trashFolder != null) trashFolder.SetActive(true);

        if (dirtyCamera != null) dirtyCamera.SetActive(true);
        if (cleanCamera != null) cleanCamera.SetActive(false);

        if (loseUIPanel != null) loseUIPanel.SetActive(false);
        if (winUIPanel != null) winUIPanel.SetActive(false);

        // Hide checkmarks at start
        if (trashCheckMark != null) trashCheckMark.SetActive(false);
        if (boxesCheckMark != null) boxesCheckMark.SetActive(false);
        if (tiresCheckMark != null) tiresCheckMark.SetActive(false);

        // Start dirty environment loop (player is in cleaning phase)
        PlayEnvLoop(dirtyEnvLoop);

        UpdateTimerUI();
        UpdateRobotText();
        UpdateTodoUI();
    }

    void Update()
    {
        if (!isTimerRunning) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0) timeRemaining = 0;
            UpdateTimerUI();
        }
        else
        {
            timeRemaining = 0;
            isTimerRunning = false;
            UpdateTimerUI(); // stays "00:00" only

            if (!missionCompleted)
            {
                ShowLoseUI();
            }
        }
    }

    // Button calls this
    public void StartTimer()
    {
        timeRemaining = gameDuration;
        isTimerRunning = true;
        UpdateTimerUI();

        if (startUIPanel != null) startUIPanel.SetActive(false);
        if (loseUIPanel != null) loseUIPanel.SetActive(false);
        if (winUIPanel != null) winUIPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        bool collectedSomething = false;

        if (other.CompareTag("Tire"))
        {
            tires++;
            Destroy(other.gameObject);
            collectedSomething = true;
        }
        else if (other.CompareTag("Box"))
        {
            boxes++;
            Destroy(other.gameObject);
            collectedSomething = true;
        }
        else if (other.CompareTag("Trash"))
        {
            trash++;
            Destroy(other.gameObject);
            collectedSomething = true;
        }
        else if (other.CompareTag("FinalSeed"))
        {
            // Second stage: seed planted
            if (sfxSource != null && seedPlantedClip != null)
                sfxSource.PlayOneShot(seedPlantedClip);

            PlantTheSeed();
            Destroy(other.gameObject);
        }

        if (collectedSomething)
        {
            UpdateRobotText();
            UpdateTodoUI();
            CheckObjectiveCompletion(); // audio + checkmarks
            CheckProgress();            // seed appear logic
        }
    }

    // Timer UI: ONLY time
    void UpdateTimerUI()
    {
        if (timerTextUI == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerTextUI.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Optional text above robot
    void UpdateRobotText()
    {
        if (robotHeadText == null) return;

        int remTires = Mathf.Max(0, targetTires - tires);
        int remBoxes = Mathf.Max(0, targetBoxes - boxes);
        int remTrash = Mathf.Max(0, targetTrash - trash);

        robotHeadText.text = $"Tires: {remTires}\nBoxes: {remBoxes}\nTrash: {remTrash}";
    }

    // To-do list UI
    void UpdateTodoUI()
    {
        if (todoTrashText != null)
            todoTrashText.text = $"Collect {targetTrash} Trash ({Mathf.Clamp(trash, 0, targetTrash)}/{targetTrash})";

        if (todoBoxesText != null)
            todoBoxesText.text = $"Collect {targetBoxes} Boxes ({Mathf.Clamp(boxes, 0, targetBoxes)}/{targetBoxes})";

        if (todoTiresText != null)
            todoTiresText.text = $"Collect {targetTires} Tires ({Mathf.Clamp(tires, 0, targetTires)}/{targetTires})";
    }

    // Objective completion: play sound + show checkmark ONCE per objective
    void CheckObjectiveCompletion()
    {
        if (!trashDone && trash >= targetTrash)
        {
            trashDone = true;
            if (trashCheckMark != null) trashCheckMark.SetActive(true);
            PlayObjectiveCompleteSFX();
        }

        if (!boxesDone && boxes >= targetBoxes)
        {
            boxesDone = true;
            if (boxesCheckMark != null) boxesCheckMark.SetActive(true);
            PlayObjectiveCompleteSFX();
        }

        if (!tiresDone && tires >= targetTires)
        {
            tiresDone = true;
            if (tiresCheckMark != null) tiresCheckMark.SetActive(true);
            PlayObjectiveCompleteSFX();
        }
    }

    void PlayObjectiveCompleteSFX()
    {
        if (sfxSource != null && objectiveCompleteClip != null)
            sfxSource.PlayOneShot(objectiveCompleteClip);
    }

    // When all objectives complete: seed appears + play "before seed" sound
    void CheckProgress()
    {
        if (missionCompleted) return;

        if (tires >= targetTires && boxes >= targetBoxes && trash >= targetTrash)
        {
            missionCompleted = true;
            isTimerRunning = false; // stop timer when objectives complete (your original behavior)

            if (seedObject != null && !seedObject.activeSelf)
            {
                seedObject.SetActive(true);

                if (sfxSource != null && seedAppearClip != null)
                    sfxSource.PlayOneShot(seedAppearClip);
            }

            // Do NOT change timer text. Show a win panel if you want:
            if (winUIPanel != null) winUIPanel.SetActive(true);

            if (robotHeadText != null)
            {
                robotHeadText.color = Color.green;
                robotHeadText.text = "Find Seed! 🌱";
            }
        }
    }

    void ShowLoseUI()
    {
        // timerTextUI remains time only
        if (loseUIPanel != null) loseUIPanel.SetActive(true);

        if (robotHeadText != null) robotHeadText.gameObject.SetActive(false);
    }

    // After player collides with FinalSeed:
    // - switch environment objects/cameras
    // - switch environment loop to clean sound
    void PlantTheSeed()
    {
        if (trashFolder != null) trashFolder.SetActive(false);
        if (forestFolder != null) forestFolder.SetActive(true);

        if (dirtyCamera != null) dirtyCamera.SetActive(false);
        if (cleanCamera != null) cleanCamera.SetActive(true);

        // Switch looping environment sound to "clean"
        PlayEnvLoop(cleanEnvLoop);

        // Hide texts at the end
        if (timerTextUI != null) timerTextUI.gameObject.SetActive(false);
        if (robotHeadText != null) robotHeadText.gameObject.SetActive(false);
    }

    // Helper: play a looping environment clip safely
    void PlayEnvLoop(AudioClip clip)
    {
        if (envSource == null || clip == null) return;

        // If already playing this clip, do nothing
        if (envSource.clip == clip && envSource.isPlaying) return;

        envSource.Stop();
        envSource.clip = clip;
        envSource.loop = true;
        envSource.Play();
    }
}
