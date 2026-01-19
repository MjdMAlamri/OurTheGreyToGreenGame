using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RobotMovement : MonoBehaviour
{
    public float speed = 6f;
    public float verticalSpeed = 6f;
    public float turnSpeed = 120f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        // إعدادات الثبات
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // (مهم) رفعنا الاحتكاك برمجياً أيضاً للاحتياط
        rb.linearDamping = 10f;
        rb.angularDamping = 10f;
    }

    void FixedUpdate()
    {
        // 1. تصفير السرعة الفيزيائية تماماً (الفرامل) 🛑
        // هذا السطر يمنع الروبوت من الانزلاق أو الطيران بعد الاصطدام
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. قراءة المدخلات
        float forward = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");
        float vertical = 0f;
        if (Input.GetKey(KeyCode.E)) vertical = 1f;
        if (Input.GetKey(KeyCode.Q)) vertical = -1f;

        // 3. التحريك اليدوي
        Vector3 moveDirection =
            transform.forward * forward * speed +
            transform.up * vertical * verticalSpeed;

        // ننقله للموقع الجديد
        rb.MovePosition(rb.position + moveDirection * Time.fixedDeltaTime);

        // 4. الدوران
        if (Mathf.Abs(turn) > 0.01f)
        {
            Quaternion turnRotation = Quaternion.Euler(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }
}