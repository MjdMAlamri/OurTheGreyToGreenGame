using UnityEngine;
using TMPro; // ضروري جداً

public class RobotCollector : MonoBehaviour
{
    [Header("Timer Settings")]
    public float gameDuration = 120f;
    private float timeRemaining;
    private bool isTimerRunning = true;

    [Header("Targets")]
    public int targetTires = 2;
    public int targetBoxes = 2;
    public int targetTrash = 2;

    // العدادات الداخلية
    int tires = 0;
    int boxes = 0;
    int trash = 0;

    [Header("Folders")]
    public GameObject trashFolder;
    public GameObject forestFolder;
    public GameObject seedObject;

    [Header("Cameras")]
    public GameObject dirtyCamera;
    public GameObject cleanCamera;

    [Header("UI System (نظام النصوص)")]
    // 1. النص المثبت في الكانفس (للتايمر)
    public TextMeshProUGUI timerTextUI;

    // 2. النص العائم فوق الروبوت (للعدادات)
    public TextMeshPro robotHeadText;

    void Start()
    {
        timeRemaining = gameDuration;

        // إعدادات البيئة
        if (forestFolder != null) forestFolder.SetActive(false);
        if (seedObject != null) seedObject.SetActive(false);
        if (trashFolder != null) trashFolder.SetActive(true);

        if (dirtyCamera != null) dirtyCamera.SetActive(true);
        if (cleanCamera != null) cleanCamera.SetActive(false);

        // تحديث النص فوق الروبوت لأول مرة
        UpdateRobotText();
    }

    void Update()
    {
        // تحديث التايمر (في الكانفس)
        if (isTimerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerUI(); // دالة خاصة للتايمر
            }
            else
            {
                timeRemaining = 0;
                isTimerRunning = false;
                UpdateTimerUI();

                // عند الخسارة
                if (timerTextUI != null) timerTextUI.text = "TIME'S UP!";
            }
        }
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
            PlantTheSeed();
            Destroy(other.gameObject);
        }

        if (collectedSomething)
        {
            UpdateRobotText(); // تحديث النص فوق الروبوت عند الجمع
            CheckProgress();
        }
    }

    // --- دالة 1: تحديث التايمر فقط (في الكانفس) ---
    void UpdateTimerUI()
    {
        if (timerTextUI != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerTextUI.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    // --- دالة 2: تحديث العدادات (فوق الروبوت) ---
    void UpdateRobotText()
    {
        if (robotHeadText != null)
        {
            int remTires = Mathf.Max(0, targetTires - tires);
            int remBoxes = Mathf.Max(0, targetBoxes - boxes);
            int remTrash = Mathf.Max(0, targetTrash - trash);

            robotHeadText.text = $"Tires: {remTires}\n" +
                                 $"Boxes: {remBoxes}\n" +
                                 $"Trash: {remTrash}";
        }
    }

    void CheckProgress()
    {
        if (tires >= targetTires && boxes >= targetBoxes && trash >= targetTrash)
        {
            if (seedObject != null && !seedObject.activeSelf)
            {
                seedObject.SetActive(true);
                isTimerRunning = false;

                // رسائل الفوز
                if (timerTextUI != null)
                {
                    timerTextUI.color = Color.green;
                    timerTextUI.text = "YOU WIN!";
                }

                if (robotHeadText != null)
                {
                    robotHeadText.color = Color.green;
                    robotHeadText.text = "Find Seed! 🌱";
                }
            }
        }
    }

    void PlantTheSeed()
    {
        if (trashFolder != null) trashFolder.SetActive(false);
        if (forestFolder != null) forestFolder.SetActive(true);

        if (dirtyCamera != null) dirtyCamera.SetActive(false);
        if (cleanCamera != null) cleanCamera.SetActive(true);

        // إخفاء النصوص عند النهاية
        if (timerTextUI != null) timerTextUI.gameObject.SetActive(false);
        if (robotHeadText != null) robotHeadText.gameObject.SetActive(false);
    }
}