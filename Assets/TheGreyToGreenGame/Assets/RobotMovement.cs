using UnityEngine;

public class RobotMovement : MonoBehaviour
{
    // ·«ÕŸÌ: ·„ ‰÷⁄ √—ﬁ«„« Â‰«° ”‰ Õﬂ„ »Â« „‰ «·Œ«—Ã
    public float moveSpeed = 10f;
    public float turnSpeed = 100f;

    void Update()
    {
        // 1. «·ﬁ—«¡… «·ÿ»Ì⁄Ì… »œÊ‰ √Ì ⁄ﬂ”
        float moveForward = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        float turn = Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime;

        // 2.  ÿ»Ìﬁ «·Õ—ﬂ…
        transform.Translate(0, 0, moveForward);
        transform.Rotate(0, turn, 0);

        // 3. «·ÿÌ—«‰
        if (Input.GetKey(KeyCode.Space))
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.Translate(Vector3.down * moveSpeed * Time.deltaTime);
        }
    }
}