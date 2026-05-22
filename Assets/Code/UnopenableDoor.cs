using UnityEngine;

namespace HorrorGame
{
    public class UnopenableDoor : BaseDoor
    {
        [Header("ข้อความแจ้งเตือน")]
        public string alertMessage = "Door is stuck.";

        public override string Interact(bool hasKey)
        {
            PlaySound(lockedSound);
            return alertMessage; 
        }
    }
}
