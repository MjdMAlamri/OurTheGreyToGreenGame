using UnityEngine;

public class RobotCollector : MonoBehaviour
{
    // أعداد الأشياء التي جمعناها
    int tires = 0;
    int boxes = 0;
    int trash = 0;

    // دالة الشفط: تعمل تلقائياً عندما يدخل شيء في المنطقة السفلية
    private void OnTriggerEnter(Collider other)
    {
        // 1. إذا شفطنا كفر
        if (other.CompareTag("Tire"))
        {
            tires = tires + 1;
            Destroy(other.gameObject); // اخفاء الكفر
            Debug.Log("تم شفط كفر! 🍩 العدد: " + tires);
        }
        // 2. إذا شفطنا كرتون
        else if (other.CompareTag("Box"))
        {
            boxes = boxes + 1;
            Destroy(other.gameObject); // اخفاء الكرتون
            Debug.Log("تم شفط كرتون! 📦 العدد: " + boxes);
        }
        // 3. إذا شفطنا نفايات
        else if (other.CompareTag("Trash"))
        {
            trash = trash + 1;
            Destroy(other.gameObject); // اخفاء النفايات
            Debug.Log("تم تنظيف نفايات! 🗑️ العدد: " + trash);
        }

        // التحقق من الفوز
        if (tires >= 10 && boxes >= 5 && trash >= 10)
        {
            Debug.Log("🎉 مبروك! الروبوت أنهى جميع مهام التنظيف!");
        }
    }
}