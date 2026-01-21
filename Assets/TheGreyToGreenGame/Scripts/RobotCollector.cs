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
    private bool seedPickedUp = false;

    // Track completion
    private bool trashDone = false, boxesDone = false, tiresDone = false;

    [Header("Folders")]
    public GameObject trashFolder;
    public GameObject forestFolder;
    public GameObject seedObject;

    [Header("Cameras")]
    public GameObject dirtyCamera;
    public GameObject cleanCamera;

    [Header("Post Processing")]
    public GameObject globalVolumeObject;

    [Header("Freeze Player")]
    public MonoBehaviour playerMovementScript;
    public Rigidbody playerRb;

    [Header("UI System")]
    public TextMeshProUGUI timerTextUI;

    [Header("Todo List Texts (Top Right)")]
    public TextMeshProUGUI todoTrashText;
    public TextMeshProUGUI todoBoxesText;
    public TextMeshProUGUI todoTiresText;

    [Header("UI Canvases / Panels")]
    public GameObject mapCanvas;
    public GameObject howToPlayCanvas;
    public GameObject timerCanvas;
    public GameObject todoCanvasOrPanel;
    public GameObject loseUIPanel;
    public GameObject winUIPanel;

    [Header("Checkmarks")]
    public GameObject trashCheckMark;
    public GameObject boxesCheckMark;
    public GameObject tiresCheckMark;

    [Header("SFX & Audio")]
    public AudioSource sfxSource;

    // ✅ NEW: plays every time you pick up Tire/Box/Trash
    public AudioClip pickupClip;

    // existing
    public AudioClip objectiveCompleteClip;
    public AudioClip seedAppearClip;

    // ✅ OPTIONAL: different sound when picking up the seed
    public AudioClip seedPickupClip;

    public AudioSource congratsSource;
    public AudioClip congratsClip;

    public AudioSource envSource;
    public AudioClip dirtyEnvLoop;
    public AudioClip cleanEnvLoop;

    public AudioSource timerTickSource;
    public AudioClip timerTickClip;

    [Header("Seed Pickup Transition")]
    public GameObject transitionCanvas;
    public float transitionDuration = 2.0f;
    public AudioSource transitionSource;
    public AudioClip transitionClip;

    void Start()
    {
        timeRemaining = gameDuration;

        if (forestFolder != null) forestFolder.SetActive(false);
        if (seedObject != null) seedObject.SetActive(false);
        if (trashFolder != null) trashFolder.SetActive(true);

        if (loseUIPanel != null) loseUIPanel.SetActive(false);
        if (winUIPanel != null) winUIPanel.SetActive(false);
        if (transitionCanvas != null) transitionCanvas.SetActive(false);

        if (trashCheckMark != null) trashCheckMark.SetActive(false);
        if (boxesCheckMark != null) boxesCheckMark.SetActive(false);
        if (tiresCheckMark != null) tiresCheckMark.SetActive(false);

        StopTimerTick();
        UpdateTimerUI();
        UpdateTodoUI();

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
    public void GoToHowToPlay()
    {
        if (state != GameState.Map) return;
        EnterHowToPlayState();
    }

    public void StartGame()
    {
        if (state != GameState.HowToPlay) return;
        EnterPlayingState();
    }

    // ---------------- STATES ----------------
    void EnterMapState()
    {
        state = GameState.Map;
        SetUIState(mapCanvas: true, cleanCam: true, freeze: true);
    }

    void EnterHowToPlayState()
    {
        state = GameState.HowToPlay;
        SetUIState(howToPlay: true, cleanCam: true, freeze: true);
    }

    void EnterPlayingState()
    {
        state = GameState.Playing;
        SetUIState(timer: true, todo: true, dirtyCam: true, freeze: false);

        if (globalVolumeObject != null) globalVolumeObject.SetActive(true);

        timeRemaining = gameDuration;
        isTimerRunning = true;
        UpdateTimerUI();
        StartTimerTick();
        PlayEnvLoop(dirtyEnvLoop);

        UpdateTodoUI();
    }

    void SetUIState(bool mapCanvas = false, bool howToPlay = false, bool timer = false, bool todo = false,
                    bool cleanCam = false, bool dirtyCam = false, bool freeze = false)
    {
        if (this.mapCanvas != null) this.mapCanvas.SetActive(mapCanvas);
        if (this.howToPlayCanvas != null) this.howToPlayCanvas.SetActive(howToPlay);
        if (this.timerCanvas != null) this.timerCanvas.SetActive(timer);
        if (this.todoCanvasOrPanel != null) this.todoCanvasOrPanel.SetActive(todo);

        if (this.cleanCamera != null) this.cleanCamera.SetActive(cleanCam);
        if (this.dirtyCamera != null) this.dirtyCamera.SetActive(dirtyCam);

        FreezePlayer(freeze);
    }

    void FreezePlayer(bool freeze)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = !freeze;
        if (playerRb != null)
        {
            // NOTE: if you get errors here, replace linearVelocity with velocity
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
            collectedSomething = true;
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Box"))
        {
            boxes++;
            collectedSomething = true;
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Trash"))
        {
            trash++;
            collectedSomething = true;
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("FinalSeed"))
        {
            seedPickedUp = true;

            // ✅ Play seed pickup sound (optional)
            PlayOneShot(sfxSource, seedPickupClip != null ? seedPickupClip : pickupClip);

            Destroy(other.gameObject);
            StartCoroutine(SeedPickupTransition());
            return;
        }

        // ✅ Play pickup sound every time you collect something
        if (collectedSomething)
        {
            PlayOneShot(sfxSource, pickupClip);

            CheckObjectiveCompletion();
            CheckProgress();
            UpdateTodoUI();
        }
    }

    // ---------------- TRANSITION SEQUENCE ----------------
    private IEnumerator SeedPickupTransition()
    {
        state = GameState.Transition;
        isTimerRunning = false;
        StopTimerTick();
        FreezePlayer(true);

        if (timerCanvas != null) timerCanvas.SetActive(false);
        if (todoCanvasOrPanel != null) todoCanvasOrPanel.SetActive(false);
        if (transitionCanvas != null) transitionCanvas.SetActive(true);

        if (transitionSource != null && transitionClip != null)
            transitionSource.PlayOneShot(transitionClip);

        yield return new WaitForSeconds(transitionDuration);

        if (transitionCanvas != null) transitionCanvas.SetActive(false);

        PlantTheSeedVisuals();
        PlayCleanEnvAndCongratsTogether();

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

    void UpdateTodoUI()
    {
        bool taskFinished = (tires >= targetTires && boxes >= targetBoxes && trash >= targetTrash);

        if (taskFinished)
        {
            if (todoTrashText != null)
            {
                todoTrashText.text = "Great Job!\nFind the Seed ";
                todoTrashText.color = Color.green;
            }
            if (todoBoxesText != null) todoBoxesText.text = "";
            if (todoTiresText != null) todoTiresText.text = "";

            if (trashCheckMark != null) trashCheckMark.SetActive(false);
            if (boxesCheckMark != null) boxesCheckMark.SetActive(false);
            if (tiresCheckMark != null) tiresCheckMark.SetActive(false);
        }
        else
        {
            if (todoTrashText != null)
            {
                todoTrashText.text = $"Collect Trash ({Mathf.Clamp(trash, 0, targetTrash)}/{targetTrash})";
                todoTrashText.color = Color.white;
            }

            if (todoBoxesText != null)
            {
                todoBoxesText.text = $"Collect Boxes ({Mathf.Clamp(boxes, 0, targetBoxes)}/{targetBoxes})";
                todoBoxesText.color = Color.white;
            }

            if (todoTiresText != null)
            {
                todoTiresText.text = $"Collect Tires ({Mathf.Clamp(tires, 0, targetTires)}/{targetTires})";
                todoTiresText.color = Color.white;
            }
        }
    }

    // ---------------- HELPER FUNCTIONS ----------------
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
            if (seedObject != null && !seedObject.activeSelf)
            {
                seedObject.SetActive(true);
                PlayOneShot(sfxSource, seedAppearClip);
            }
            UpdateTodoUI();
        }
    }

    void ShowLoseUI()
    {
        state = GameState.Ended;
        if (loseUIPanel != null) loseUIPanel.SetActive(true);
        FreezePlayer(true);
    }

    void PlantTheSeedVisuals()
    {
        if (trashFolder != null) trashFolder.SetActive(false);
        if (forestFolder != null) forestFolder.SetActive(true);
        if (dirtyCamera != null) dirtyCamera.SetActive(false);
        if (cleanCamera != null) cleanCamera.SetActive(true);
        if (globalVolumeObject != null) globalVolumeObject.SetActive(false);
    }

    void PlayCleanEnvAndCongratsTogether()
    {
        if (envSource != null && cleanEnvLoop != null)
        {
            envSource.Stop();
            envSource.clip = cleanEnvLoop;
            envSource.loop = true;
            envSource.Play();
        }
        if (congratsSource != null && congratsClip != null)
            congratsSource.PlayOneShot(congratsClip);
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
        timerTickSource.clip = timerTickClip;
        timerTickSource.loop = true;
        timerTickSource.Play();
    }

    void StopTimerTick()
    {
        if (timerTickSource != null) timerTickSource.Stop();
    }
}
