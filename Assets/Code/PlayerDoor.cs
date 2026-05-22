using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerDoor : MonoBehaviour
{
    public enum DoorType { Normal, Locked, PermanentlyLocked }
    
    [Header("Door Status")]
    public DoorType type = DoorType.Normal;
    public bool isOpen = false;

    [Header("Movement Settings")]
    public float openAngle = -90f;
    public float smooth = 5f;

    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;

    private AudioSource audioSource;
    private Quaternion defaultRotation;
    private Quaternion openRotation;

    void Start()
    {
        defaultRotation = transform.localRotation;
        openRotation = Quaternion.Euler(0, openAngle, 0);
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isOpen)
            transform.localRotation = Quaternion.Slerp(transform.localRotation, openRotation, Time.deltaTime * smooth);
        else
            transform.localRotation = Quaternion.Slerp(transform.localRotation, defaultRotation, Time.deltaTime * smooth);
    }

    // แก้ไขให้ส่งข้อความแจ้งเตือนกลับไปที่ระบบ Interaction
    public string TryOpen(bool hasKey)
    {
        if (type == DoorType.PermanentlyLocked)
        {
            PlaySound(lockedSound);
            return "Door is locked."; // ข้อความสำหรับประตูที่เปิดไม่ได้เลย
        }

        if (type == DoorType.Locked)
        {
            if (hasKey)
            {
                type = DoorType.Normal;
                ToggleDoor();
                return ""; // สำเร็จ ไม่ต้องโชว์ข้อความแจ้งเตือน
            }
            else
            {
                PlaySound(lockedSound);
                return "I need a Key."; // ข้อความสำหรับประตูที่รอประแจ
            }
        }

        ToggleDoor();
        return "";
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        PlaySound(isOpen ? openSound : closeSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
