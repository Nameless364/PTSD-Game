using UnityEngine;

namespace HorrorGame
{
    public abstract class BaseDoor : MonoBehaviour
    {
        public bool open = false;
        public float openAngle = 90f;
        public float closeAngle = 0f;
        public float smooth = 2f;

        public AudioSource audioSource;
        public AudioClip openSound, closeSound, lockedSound;

        // ลบ NavMeshObstacle ออกแล้ว เพราะ Carve ตัด NavMesh ทำให้ผีหาทางไม่ได้
        // ผีจัดการประตูผ่าน HandleDoorSequence โดยตรงอยู่แล้ว
        // วิธีที่ถูกต้อง: Bake NavMesh โดยเปิดประตูทิ้งไว้ก่อน Bake

        void Start()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        void Update()
        {
            float targetAngle = open ? openAngle : closeAngle;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
        }

        public void ToggleDoor()
        {
            open = !open;
            PlaySound(open ? openSound : closeSound);
        }

        public virtual string Interact(bool hasKey)
        {
            ToggleDoor();
            return "";
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
        }
    }
}
