using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    public float mouseSensitivity = 0.5f;

    [Header("Head Settings")]
    public float headHeight = 1.6f; // ความสูงระดับสายตา

    [Header("References")]
    public Transform playerBody;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private InputAction lookAction;

    void Awake()
    {
        // รับค่า Mouse Delta
        lookAction = new InputAction("Look", binding: "<Mouse>/delta");
        lookAction.Enable();

        // ล็อกเมาส์
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 1. ย้ายกล้องไปที่ระดับหัวอัตโนมัติ
        transform.localPosition = new Vector3(0, headHeight, 0);

        // 2. ตั้งค่าการหมุนเริ่มต้นให้ตรงกับตัวละคร
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }

        if (playerBody != null)
        {
            yRotation = playerBody.eulerAngles.y;
        }
    }

    void OnDestroy()
    {
        lookAction.Disable();
    }

    void Update()
    {
        Vector2 delta = lookAction.ReadValue<Vector2>();

        if (delta != Vector2.zero)
        {
            float mouseX = delta.x * mouseSensitivity;
            float mouseY = delta.y * mouseSensitivity;

            // --- คำนวณแกน X (ก้ม-เงย) ---
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // --- คำนวณแกน Y (หันซ้าย-ขวา) ---
            if (playerBody != null)
            {
                // ใช้การหมุนที่ตัวละครโดยตรงเพื่อให้ทิศทางการเดินเปลี่ยนตามการหัน
                yRotation += mouseX;
                playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);
            }
        }
    }
}
