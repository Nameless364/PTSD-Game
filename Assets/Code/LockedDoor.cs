using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame
{
    public class LockedDoor : BaseDoor
    {
        public string requiredKeyID = "Room1";
        public string needKeyMessage = "I need a Key.";
        public string unlockMessage = "Door Unlocked.";

        [Header("Status")]
        public bool isLocked = true; // เพิ่มตัวแปรนี้เพื่อให้สคริปต์อื่น (เช่น ผี) เช็คได้ครับ

        public override string Interact(bool hasKey) 
        {
            if (isLocked)
            {
                PlaySound(lockedSound);
                return needKeyMessage;
            }
            
            ToggleDoor();
            return "";
        }

        // ฟังก์ชันสำหรับปลดล็อก
        public void Unlock()
        {
            isLocked = false;
            Debug.Log("🔓 ประตูถูกปลดล็อกแล้ว");
        }
    }
}
