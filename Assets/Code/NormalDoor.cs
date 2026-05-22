using UnityEngine;

namespace HorrorGame
{
    public class NormalDoor : BaseDoor
    {
        public override string Interact(bool hasKey)
        {
            ToggleDoor();
            return "";
        }
    }
}
