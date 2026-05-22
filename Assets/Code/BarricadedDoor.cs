using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame
{
    public class BarricadedDoor : BaseDoor
    {
        [Header("Wooden Boards")]
        public List<WoodenBoard> boards = new List<WoodenBoard>();

        private void Awake()
        {
            if (boards.Count == 0)
            {
                WoodenBoard[] childBoards = GetComponentsInChildren<WoodenBoard>();
                foreach (var b in childBoards) boards.Add(b);
            }
        }

        // สร้าง Property เพื่อให้คนอื่นเช็คได้ว่ายังมีไม้เหลืออยู่ไหมจริงๆ
        public bool IsStillBarricaded
        {
            get {
                CleanBoardList();
                return boards.Count > 0;
            }
        }

        public override string Interact(bool hasKey)
        {
            if (IsStillBarricaded)
            {
                return "Barricaded";
            }
            
            ToggleDoor();
            return "";
        }

        public bool TryRemoveBoard(bool hasCrowbar)
        {
            if (!hasCrowbar)
            {
                PlaySound(lockedSound);
                return false;
            }

            if (IsStillBarricaded)
            {
                int lastIndex = boards.Count - 1;
                if (boards[lastIndex] != null)
                {
                    boards[lastIndex].Break();
                }
                boards.RemoveAt(lastIndex);
                
                if (boards.Count == 0)
                {
                    Invoke("DelayedOpen", 0.5f);
                }
                return true;
            }

            return false;
        }

        void DelayedOpen()
        {
            if (!open) ToggleDoor();
        }

        private void CleanBoardList()
        {
            for (int i = boards.Count - 1; i >= 0; i--)
            {
                if (boards[i] == null)
                {
                    boards.RemoveAt(i);
                }
            }
        }
    }
}
