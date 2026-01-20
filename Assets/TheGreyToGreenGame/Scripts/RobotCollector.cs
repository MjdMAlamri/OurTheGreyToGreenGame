using System.Collections;
using UnityEngine;
using TMPro;

public class RobotCollector : MonoBehaviour
{
    // ---------- GAME STATES ----------
    private enum GameState { Map, HowToPlay, Playing, Transition, Ended }
    private GameState state = GameState.Map;

    [Header("Timer Settings")]
    public float gameDuration = 120f;
    private float timeRemaining;
    private bool isTimerRunning = false;

    [Header("Targets")]
    public int targetTires = 2;
    public int targetBoxes = 2;
    public int targetTrash = 2;

    private int tires = 0, boxes = 0, trash = 0;

    // Objectives completed => seed spawned
    private bool objectivesCompleted = false;

    // Seed picked => final transition started
    private bool seedPickedUp = false;

    // Track completion so audio/checkmark happens ONCE
    private bool trashDone = false, boxesDone = false, tiresDone = false;

    [Header("Folders")]
    public GameObject trashFolder;
    public GameObject forestFolder;
    public GameObject seedObject;

    [Header("Cameras")]
    public GameObject dirtyCamera;  // gray camera (Main Camera)
    public GameObject cleanCamera;  // colored camera (Clean Camera)

    [Header("Post Processing")]
    public GameObject globalVolumeObject; // drag "Global Volume" here

    [Header("Freeze Player (optional)")]
    public MonoBehaviour playerMovementScript; // drag RobotMovement script here
    public Rigidbody playerRb;                 // optional hard-freeze

    [Header("UI System")]
    public TextMeshProUGUI timerTextUI;   // ONLY time
    public TextMeshPro robotHeadText;

    [Header("UI Canvases / Panels")]
    public GameObject mapCanvas;          // MapCanvas
    public GameObject howToPlayCanvas;    // HowtoPlayCanvas
    public GameObject timerCanvas;        // TimerCanvas
    public GameObject todoCanvasOrPanel;  // Todo List
    public GameObject loseUIPanel;        // LoseCanvas (panel/canvas)
    public GameObject winUIPanel;         // WinCanvas (panel/canvas)

    [Header("To-Do List UI (Top)")]
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

    [Header("Seed Pickup Transition (Explosion/Loader)")]
    public GameObject transitionCanvas;     // full-screen loader/explosion UI
    public float transitionDuration = 2.0f; // seconds
    public AudioSource transitionSource;    // separate source recommended
    public AudioClip transitionClip;        // explosion / whoosh / etc

    void Start()
    {
        timeRemaining = gameDuration;

        // World default
        if (forestFolder != null) forestFolder.SetActive(false);
        if (seedObject != null) seedObject.SetActive(false);
        if (trashFolder != null) trashFolder.SetActive(true);

        // UI default
        if (loseUIPanel != null) loseUIPanel.SetActive(false);
        if (winUIPanel != null) winUIPanel.SetActive(false);
        if (transitionCanvas != null) transitionCanvas.SetActive(false);

        // Checkmarks default
        if (trashCheckMark != null) trashCheckMark.SetActive(false);
        if (boxesCheckMark != null) boxesCheckMark.SetActive(false);
        if (tiresCheckMark != null) tiresCheckMark.SetActive(false);

        StopTimerTick();
        UpdateTimerUI();
        UpdateRobotText();
        UpdateTodoUI();

        // Start at MAP state
        EnterMapState();
    }

    void Update()
    {
        if (state != GameState.Playing) return;
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

            if (!seedPickedUp)
                ShowLoseUI();
        }
    }

    // ---------------- BUTTONS ----------------
    // MapCanvas button -> calls this
    public void GoToHowToPlay()
    {
        if (state != GameState.Map) return;
        EnterHowToPlayState();
    }

    // HowtoPlayCanvas play button -> calls this
    public void StartGame()
    {
        if (state != GameState.HowToPlay) return;
        EnterPlayingState();
    }

    // ---------------- STATES ----------------
    void EnterMapState()
    {
        state = GameState.Map;

        // UI
        if (mapCanvas != null) mapCanvas.SetActive(true);
        if (howToPlayCanvas != null) howToPlayCanvas.SetActive(false);
        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (todoCanvasOrPanel != null) todoCanvasOrPanel.SetActive(false);
        if (transitionCanvas != null) transitionCanvas.SetActive(false);

        // Cameras: show clean preview
        if (cleanCamera != null) cleanCamera.SetActive(true);
        if (dirtyCamera != null) dirtyCamera.SetActive(false);

        // No gray post
        if (globalVolumeObject != null) globalVolumeObject.SetActive(false);

        // Freeze player
        FreezePlayer(true);

        // No timer
        isTimerRunning = false;
        StopTimerTick();
    }

    void EnterHowToPlayState()
    {
        state = GameState.HowToPlay;

        // UI
        if (mapCanvas != null) mapCanvas.SetActive(false);
        if (howToPlayCanvas != null) howToPlayCanvas.SetActive(true);
        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (todoCanvasOrPanel != null) todoCanvasOrPanel.SetActive(false);
        if (transitionCanvas != null) transitionCanvas.SetActive(false);

        // Show clean camera colors
        if (cleanCamera != null) cleanCamera.SetActive(true);
        if (dirtyCamera != null) dirtyCamera.SetActive(false);

        // Keep gray OFF
        if (globalVolumeObject != null) globalVolumeObject.SetActive(false);

        // Freeze player
        FreezePlayer(true);

        isTimerRunning = false;
        StopTimerTick();
    }

    void EnterPlayingState()
    {
        state = GameState.Playing;

        // UI
        if (mapCanvas != null) mapCanvas.SetActive(false);
        if (howToPlayCanvas != null) howToPlayCanvas.SetActive(false);
        if (timerCanvas != null) timerCanvas.SetActive(true);
        if (todoCanvasOrPanel != null) todoCanvasOrPanel.SetActive(true);

        if (loseUIPanel != null) loseUIPanel.SetActive(false);
        if (winUIPanel != null) winUIPanel.SetActive(false);
        if (transitionCanvas != null) transitionCanvas.SetActive(false);

        // Switch to gray camera
        if (cleanCamera != null) cleanCamera.SetActive(false);
        if (dirtyCamera != null) dirtyCamera.SetActive(true);

        // Enable gray post (Global Volume)
        if (globalVolumeObject != null) globalVolumeObject.SetActive(true);

        // Unfreeze player
        FreezePlayer(false);

        // Start timer + tick + dirty ambience
        timeRemaining = gameDuration;
        isTimerRunning = true;
        UpdateTimerUI();
        StartTimerTick();
        PlayEnvLoop(dirtyEnvLoop);
    }

    void FreezePlayer(bool freeze)
    {
        if (playerMovementScript != null)
            playerMovementScript.enabled = !freeze;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.isKinematic = freeze;
        }
    }

    // ---------------- COLLISIONS ----------------
    private void OnTriggerEnter(Collider other)
    {
        if (state != GameState.Playing) return;
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
            Destroy(other.gameObject);

            // Start explosion/loader transition then reveal green land
            StartCoroutine(SeedPickupTransition());
            return;
        }

        if (collectedSomething)
        {
            UpdateRobotText();
            UpdateTodoUI();
            CheckObjectiveCompletion();
            CheckProgress();
        }
    }

    // ---------------- TRANSITION SEQUENCE ----------------
    private IEnumerator SeedPickupTransition()
    {
        state = GameState.Transition;

        // Stop gameplay + freeze
        isTimerRunning = false;
        StopTimerTick();
        FreezePlayer(true);

        // Hide HUD
        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (todoCanvasOrPanel != null) todoCanvasOrPanel.SetActive(false);

        // Show transition UI
        if (transitionCanvas != null) transitionCanvas.SetActive(true);

        // Play explosion/transition sound
        if (transitionSource != null && transitionClip != null)
            transitionSource.PlayOneShot(transitionClip);

        // Wait
        yield return new WaitForSeconds(transitionDuration);

        // Hide transition UI
        if (transitionCanvas != null) transitionCanvas.SetActive(false);

        // Reveal green land + switch to clean view
        PlantTheSeedVisuals();

        // Play clean env + congrats together (sync)
        PlayCleanEnvAndCongratsTogether();

        // Show win UI at the end
        if (winUIPanel != null) winUIPanel.SetActive(true);

        state = GameState.Ended;
    }

    // ---------------- UI UPDATES ----------------
    void UpdateTimerUI()
    {
        if (timerTextUI == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerTextUI.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void UpdateRobotText()
    {
        if (robotHeadText == null) return;
        int remTires = Mathf.Max(0, targetTires - tires);
        int remBoxes = Mathf.Max(0, targetBoxes - boxes);
        int remTrash = Mathf.Max(0, targetTrash - trash);
        robotHeadText.text = $"Tires: {remTires}\nBoxes: {remBoxes}\nTrash: {remTrash}";
    }

    void UpdateTodoUI()
    {
        if (todoTrashText != null)
            todoTrashText.text = $"Collect {targetTrash} Trash ({Mathf.Clamp(trash, 0, targetTrash)}/{targetTrash})";
        if (todoBoxesText != null)
            todoBoxesText.text = $"Collect {targetBoxes} Boxes ({Mathf.Clamp(boxes, 0, targetBoxes)}/{targetBoxes})";
        if (todoTiresText != null)
            todoTiresText.text = $"Collect {targetTires} Tires ({Mathf.Clamp(tires, 0, targetTires)}/{targetTires})";
    }

    // ---------------- OBJECTIVES ----------------
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

    void CheckProgress()
    {
        if (objectivesCompleted) return;

        if (tires >= targetTires && boxes >= targetBoxes && trash >= targetTrash)
        {
            objectivesCompleted = true;

            // Stop timer when objectives complete (your behavior)
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

    // ---------------- WIN/LOSE ----------------
    void ShowLoseUI()
    {
        state = GameState.Ended;

        if (loseUIPanel != null) loseUIPanel.SetActive(true);
        FreezePlayer(true);

        // Optionally switch to clean camera for lose screen
        // if (dirtyCamera != null) dirtyCamera.SetActive(false);
        // if (cleanCamera != null) cleanCamera.SetActive(true);
    }

    // After transition, we show winUIPanel in coroutine

    // ---------------- WORLD SWITCH ----------------
    // Final stage reveal
    void PlantTheSeedVisuals()
    {
        if (trashFolder != null) trashFolder.SetActive(false);
        if (forestFolder != null) forestFolder.SetActive(true);

        // Switch to clean camera for final green stage
        if (dirtyCamera != null) dirtyCamera.SetActive(false);
        if (cleanCamera != null) cleanCamera.SetActive(true);

        // Turn off gray post processing
        if (globalVolumeObject != null) globalVolumeObject.SetActive(false);

        // Hide robot text if you want
        if (robotHeadText != null) robotHeadText.gameObject.SetActive(false);
    }

    // ---------------- AUDIO ----------------
    void PlayCleanEnvAndCongratsTogether()
    {
        if (envSource == null || cleanEnvLoop == null) return;
        if (congratsSource == null || congratsClip == null) return;

        double startTime = AudioSettings.dspTime + 0.05;

        envSource.Stop();
        envSource.clip = cleanEnvLoop;
        envSource.loop = true;
        envSource.PlayScheduled(startTime);

        congratsSource.Stop();
        congratsSource.clip = congratsClip;
        congratsSource.loop = false;
        congratsSource.PlayScheduled(startTime);
    }

    void PlayEnvLoop(AudioClip clip)
    {
        if (envSource == null || clip == null) return;
        if (envSource.clip == clip && envSource.isPlaying) return;

        envSource.Stop();
        envSource.clip = clip;
        envSource.loop = true;
        envSource.Play();
    }

    void PlayOneShot(AudioSource src, AudioClip clip)
    {
        if (src == null || clip == null) return;
        src.PlayOneShot(clip);
    }

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
