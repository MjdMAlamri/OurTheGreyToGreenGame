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

    // Objectives completed => seed spawned
    private bool objectivesCompleted = false;

    // Seed picked => final win
    private bool seedPickedUp = false;

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
    public TextMeshPro robotHeadText;

    [Header("UI Panels")]
    public GameObject startUIPanel;
    public GameObject loseUIPanel;
    public GameObject winUIPanel; // show AFTER seed is picked

    [Header("To-Do List UI (Top)")]
    public GameObject todoCanvasOrPanel; // whole ToDo canvas/panel to hide on win
    public TextMeshProUGUI todoTrashText;
    public TextMeshProUGUI todoBoxesText;
    public TextMeshProUGUI todoTiresText;

    [Header("Checkmarks")]
    public GameObject trashCheckMark;
    public GameObject boxesCheckMark;
    public GameObject tiresCheckMark;

    [Header("SFX (one-shots)")]
    public AudioSource sfxSource; // objective dings + seed appear
    public AudioClip objectiveCompleteClip;
    public AudioClip seedAppearClip;

    [Header("Congrats SFX (scheduled)")]
    public AudioSource congratsSource; // separate source for scheduling
    public AudioClip congratsClip;     // plays with clean env at same time

    [Header("Environment Audio (looping)")]
    public AudioSource envSource;      // loop ambience
    public AudioClip dirtyEnvLoop;
    public AudioClip cleanEnvLoop;

    [Header("Timer Tick Audio (looping)")]
    public AudioSource timerTickSource; // separate loop for "tic tic"
    public AudioClip timerTickClip;

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

        // Start dirty ambience
        PlayEnvLoop(dirtyEnvLoop);

        // Timer is paused until Start button
        StopTimerTick();

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
            UpdateTimerUI();

            StopTimerTick();

            // Lose only if seed not picked
            if (!seedPickedUp)
                ShowLoseUI();
        }
    }

    // Button calls this
    public void StartTimer()
    {
        timeRemaining = gameDuration;
        isTimerRunning = true;
        UpdateTimerUI();

        StartTimerTick();

        if (startUIPanel != null) startUIPanel.SetActive(false);
        if (loseUIPanel != null) loseUIPanel.SetActive(false);
        if (winUIPanel != null) winUIPanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (seedPickedUp) return;

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
            seedPickedUp = true;

            // Stop ticking once final action happens
            StopTimerTick();

            // Visual/world switch first
            PlantTheSeedVisuals();

            // Start clean env + congrats EXACTLY together
            PlayCleanEnvAndCongratsTogether();

            // Hide todo + show win
            HideTodoAndShowWinUI();

            Destroy(other.gameObject);
            return;
        }

        if (collectedSomething)
        {
            UpdateRobotText();
            UpdateTodoUI();
            CheckObjectiveCompletion();
            CheckProgress(); // spawns seed
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
            PlayOneShot(sfxSource, objectiveCompleteClip);
        }

        if (!boxesDone && boxes >= targetBoxes)
        {
            boxesDone = true;
            if (boxesCheckMark != null) boxesCheckMark.SetActive(true);
            PlayOneShot(sfxSource, objectiveCompleteClip);
        }

        if (!tiresDone && tires >= targetTires)
        {
            tiresDone = true;
            if (tiresCheckMark != null) tiresCheckMark.SetActive(true);
            PlayOneShot(sfxSource, objectiveCompleteClip);
        }
    }

    // When all objectives complete: seed appears + play seed appear sound
    void CheckProgress()
    {
        if (objectivesCompleted) return;

        if (tires >= targetTires && boxes >= targetBoxes && trash >= targetTrash)
        {
            objectivesCompleted = true;

            // Keep your current behavior: stop timer here
            isTimerRunning = false;
            StopTimerTick();

            if (seedObject != null && !seedObject.activeSelf)
            {
                seedObject.SetActive(true);
                PlayOneShot(sfxSource, seedAppearClip);
            }

            if (robotHeadText != null)
            {
                robotHeadText.color = Color.green;
                robotHeadText.text = "Find Seed! 🌱";
            }
        }
    }

    void ShowLoseUI()
    {
        if (loseUIPanel != null) loseUIPanel.SetActive(true);
        if (robotHeadText != null) robotHeadText.gameObject.SetActive(false);

        // Optional: hide todo on lose too
        // if (todoCanvasOrPanel != null) todoCanvasOrPanel.SetActive(false);
    }

    void HideTodoAndShowWinUI()
    {
        if (todoCanvasOrPanel != null) todoCanvasOrPanel.SetActive(false);
        if (winUIPanel != null) winUIPanel.SetActive(true);
        isTimerRunning = false;
    }

    // Visual/world switch ONLY (no audio here)
    void PlantTheSeedVisuals()
    {
        if (trashFolder != null) trashFolder.SetActive(false);
        if (forestFolder != null) forestFolder.SetActive(true);

        if (dirtyCamera != null) dirtyCamera.SetActive(false);
        if (cleanCamera != null) cleanCamera.SetActive(true);

        // Optional: hide texts at the end
        if (timerTextUI != null) timerTextUI.gameObject.SetActive(false);
        if (robotHeadText != null) robotHeadText.gameObject.SetActive(false);
    }

    // Start clean env loop + congrats sound at the same DSP time (overlap guaranteed)
    void PlayCleanEnvAndCongratsTogether()
    {
        if (envSource == null || cleanEnvLoop == null) return;
        if (congratsSource == null || congratsClip == null) return;

        double startTime = AudioSettings.dspTime + 0.05;

        // ENV (loop)
        envSource.Stop();
        envSource.clip = cleanEnvLoop;
        envSource.loop = true;
        envSource.PlayScheduled(startTime);

        // Congrats (one-shot)
        congratsSource.Stop();
        congratsSource.clip = congratsClip;
        congratsSource.loop = false;
        congratsSource.PlayScheduled(startTime);
    }

    // Dirty/clean ambience helper
    void PlayEnvLoop(AudioClip clip)
    {
        if (envSource == null || clip == null) return;
        if (envSource.clip == clip && envSource.isPlaying) return;

        envSource.Stop();
        envSource.clip = clip;
        envSource.loop = true;
        envSource.Play();
    }

    // One-shot helper
    void PlayOneShot(AudioSource src, AudioClip clip)
    {
        if (src == null || clip == null) return;
        src.PlayOneShot(clip);
    }

    // Timer tick helpers
    void StartTimerTick()
    {
        if (timerTickSource == null || timerTickClip == null) return;
        if (timerTickSource.isPlaying) return;

        timerTickSource.Stop();
        timerTickSource.clip = timerTickClip;
        timerTickSource.loop = true;
        timerTickSource.Play();
    }

    void StopTimerTick()
    {
        if (timerTickSource == null) return;
        if (timerTickSource.isPlaying) timerTickSource.Stop();
    }
}
