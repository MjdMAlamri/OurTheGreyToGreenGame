using UnityEngine;

public class RobotCollector : MonoBehaviour
{
    // --- الأهداف ---
    public int targetTires = 2;
    public int targetBoxes = 2;
    public int targetTrash = 2;

    // --- العدادات ---
    int tires = 0;
    int boxes = 0;
    int trash = 0;

    [Header("اسحبي المجلدات هنا")]
    public GameObject trashFolder;      // مجلد النفايات
    public GameObject forestFolder;     // مجلد الطبيعة
    public GameObject seedObject;       // البذرة

    [Header("نظام الكاميرات")]
    public GameObject dirtyCamera;      // ضعي هنا الكاميرا الأساسية (Main Camera)
    public GameObject cleanCamera;      // ضعي هنا الكاميرا الجديدة (Clean Camera)

    void Start()
    {
        // 1. إعدادات البداية
        if (forestFolder != null) forestFolder.SetActive(false);
        if (seedObject != null) seedObject.SetActive(false);
        if (trashFolder != null) trashFolder.SetActive(true);

        // 2. ضبط الكاميرات (نبدأ بالكاميرا الملوثة)
        if (dirtyCamera != null) dirtyCamera.SetActive(true);
        if (cleanCamera != null) cleanCamera.SetActive(false);
    }

    // --- دالة الجمع ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tire"))
        {
            tires++;
            Destroy(other.gameObject);
            CheckProgress();
        }
        else if (other.CompareTag("Box"))
        {
            boxes++;
            Destroy(other.gameObject);
            CheckProgress();
        }
        else if (other.CompareTag("Trash"))
        {
            trash++;
            Destroy(other.gameObject);
            CheckProgress();
        }
        else if (other.CompareTag("FinalSeed"))
        {
            PlantTheSeed();
            Destroy(other.gameObject);
        }
    }

    void CheckProgress()
    {
        if (tires >= targetTires && boxes >= targetBoxes && trash >= targetTrash)
        {
            if (seedObject != null && !seedObject.activeSelf)
            {
                Debug.Log("🎉 ظهرت البذرة!");
                seedObject.SetActive(true);
            }
        }
    }

    // --- لحظة التحول (التبديل بين الكاميرات) ---
    void PlantTheSeed()
    {
        Debug.Log("🌿 تحول العالم!");

        // تبديل البيئة
        if (trashFolder != null) trashFolder.SetActive(false);
        if (forestFolder != null) forestFolder.SetActive(true);

        // 📸 تبديل الكاميرا (هنا السحر!)
        if (dirtyCamera != null) dirtyCamera.SetActive(false); // طفي القديمة
        if (cleanCamera != null) cleanCamera.SetActive(true);  // شغلي النظيفة
    }
}