using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

namespace HorrorGame
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        public float interactDistance = 3f;
        public float rayOffset = 0.5f;
        public LayerMask ignoreLayer;

        [Header("UI Settings")]
        public TextMeshProUGUI interactText; 

        [Header("Inventory Settings")]
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public int selectedIndex = 0; 

        [Header("Hand Visuals")]
        public GameObject crowbarInHand;
        public List<KeyVisual> keyVisuals = new List<KeyVisual>();

        private InputAction interactAction;
        private string currentMessage = "";
        private Texture2D circleTexture;
        private HidingSpot currentHidingSpot;

        [System.Serializable]
        public class InventoryItem {
            public string id;
            public string name;
            public bool isCrowbar;
            public bool isKey;
        }

        [System.Serializable]
        public class KeyVisual {
            public string keyID;
            public GameObject model;
        }

        void Awake()
        {
            interactAction = new InputAction("Interact", binding: "<Keyboard>/e");
            interactAction.Enable();
            CreateCircleTexture();
            UpdateHandVisuals();
            Debug.Log("🎒 [PlayerInteraction] ระบบ Inventory และ Interaction พร้อมใช้งาน");
        }

        void CreateCircleTexture()
        {
            int size = 8;
            circleTexture = new Texture2D(size, size);
            float radius = size / 2f;
            for (int y = 0; y < size; y++) {
                for (int x = 0; x < size; x++) {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    circleTexture.SetPixel(x, y, (dist <= radius) ? Color.white : Color.clear);
                }
            }
            circleTexture.Apply();
        }

        void Update()
        {
            if (currentHidingSpot != null && currentHidingSpot.IsHiding()) {
                if (interactAction.WasPressedThisFrame()) {
                    Debug.Log("🚪 [PlayerInteraction] ออกจากที่ซ่อน");
                    currentHidingSpot.ToggleHide(transform.parent.gameObject);
                    currentHidingSpot = null;
                }
                return;
            }

            HandleInventorySwitch();
            CheckInteraction();
            if (interactAction.WasPressedThisFrame()) PerformInteraction();
        }

        void HandleInventorySwitch()
        {
            int oldIndex = selectedIndex;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedIndex = 0;
            if (Keyboard.current.digit2Key.wasPressedThisFrame && inventory.Count >= 1) selectedIndex = 1;
            if (Keyboard.current.digit3Key.wasPressedThisFrame && inventory.Count >= 2) selectedIndex = 2;
            if (Keyboard.current.digit4Key.wasPressedThisFrame && inventory.Count >= 3) selectedIndex = 3;
            
            if (oldIndex != selectedIndex) {
                Debug.Log("✋ [PlayerInteraction] สลับไอเทมเป็นช่องที่: " + (selectedIndex == 0 ? "มือเปล่า" : selectedIndex.ToString()));
                UpdateHandVisuals();
            }
        }

        public void UpdateHandVisuals()
        {
            if (crowbarInHand != null) crowbarInHand.SetActive(false);
            foreach (var kv in keyVisuals) if (kv.model != null) kv.model.SetActive(false);

            if (selectedIndex > 0 && selectedIndex <= inventory.Count)
            {
                var item = inventory[selectedIndex - 1];
                bool isCrowbar = item.isCrowbar || item.id.ToLower().Contains("crowbar");
                if (isCrowbar && crowbarInHand != null) crowbarInHand.SetActive(true);
                if (item.isKey) {
                    foreach (var kv in keyVisuals) {
                        if (kv.keyID == item.id && kv.model != null) kv.model.SetActive(true);
                    }
                }
            }
        }

        void OnGUI()
        {
            GUI.color = (interactText != null && interactText.text != "") ? Color.red : Color.white;
            GUI.DrawTexture(new Rect((Screen.width/2)-4, (Screen.height/2)-4, 8, 8), circleTexture);
            GUI.color = Color.white;
            float startY = Screen.height - 150;
            GUI.Box(new Rect(10, startY, 200, 130), "INVENTORY");
            GUI.Label(new Rect(20, startY + 30, 180, 20), "1. " + ((selectedIndex == 0) ? "> Empty Hand <" : "Empty Hand"));
            for (int i = 0; i < inventory.Count; i++) {
                string text = (selectedIndex == i + 1) ? "> " + inventory[i].name + " <" : inventory[i].name;
                GUI.Label(new Rect(20, startY + 55 + (i * 20), 180, 20), (i + 2) + ". " + text);
            }
        }

        void CheckInteraction()
        {
            if (!string.IsNullOrEmpty(currentMessage)) { if (interactText != null) interactText.text = currentMessage; return; }
            Vector3 rayStart = transform.position + (transform.forward * rayOffset);
            RaycastHit hit;
            if (Physics.Raycast(rayStart, transform.forward, out hit, interactDistance, ~ignoreLayer)) {
                if (hit.collider.GetComponentInParent<BaseDoor>() || 
                    hit.collider.GetComponentInParent<KeyItem>() || 
                    hit.collider.GetComponentInParent<CrowbarItem>() || 
                    hit.collider.GetComponentInParent<WoodenBoard>() ||
                    hit.collider.GetComponentInParent<HidingSpot>())
                {
                    if (interactText != null) interactText.text = "Press [E]";
                    return;
                }
            }
            if (interactText != null) interactText.text = "";
        }

        void PerformInteraction()
        {
            Vector3 rayStart = transform.position + (transform.forward * rayOffset);
            RaycastHit hit;
            if (Physics.Raycast(rayStart, transform.forward, out hit, interactDistance, ~ignoreLayer))
            {
                Debug.Log("🎯 [PlayerInteraction] เล็งโดน: " + hit.collider.gameObject.name);

                HidingSpot spot = hit.collider.GetComponentInParent<HidingSpot>();
                if (spot != null) {
                    Debug.Log("📦 [PlayerInteraction] กำลังเข้าที่ซ่อน");
                    currentHidingSpot = spot;
                    currentHidingSpot.ToggleHide(transform.parent.gameObject);
                    return;
                }

                KeyItem key = hit.collider.GetComponentInParent<KeyItem>();
                if (key != null) { 
                    Debug.Log("🔑 [PlayerInteraction] เก็บกุญแจ: " + key.keyID);
                    inventory.Add(new InventoryItem { id = key.keyID, name = key.keyID + " Key", isKey = true });
                    Destroy(key.gameObject);
                    selectedIndex = inventory.Count; UpdateHandVisuals(); return; 
                }

                CrowbarItem crowbar = hit.collider.GetComponentInParent<CrowbarItem>();
                if (crowbar != null) { 
                    Debug.Log("⛏️ [PlayerInteraction] เก็บชะแลง");
                    inventory.Add(new InventoryItem { id = "crowbar", name = "Crowbar", isCrowbar = true });
                    Destroy(crowbar.gameObject);
                    selectedIndex = inventory.Count; UpdateHandVisuals(); return; 
                }

                InventoryItem heldItem = (selectedIndex > 0 && selectedIndex <= inventory.Count) ? inventory[selectedIndex - 1] : null;
                bool isHoldingCrowbar = heldItem != null && (heldItem.isCrowbar || heldItem.id.ToLower().Contains("crowbar"));

                WoodenBoard board = hit.collider.GetComponentInParent<WoodenBoard>();
                if (board != null) {
                    if (isHoldingCrowbar) {
                        BarricadedDoor bDoor = hit.collider.GetComponentInParent<BarricadedDoor>();
                        if (bDoor != null) bDoor.TryRemoveBoard(true);
                        else { Debug.Log("🔨 [PlayerInteraction] ทำลายแผ่นไม้"); board.Break(); }
                    } else StartCoroutine(ShowTempMessage("Need Crowbar!"));
                    return;
                }

                BaseDoor door = hit.collider.GetComponentInParent<BaseDoor>();
                if (door != null) {
                    if (door is LockedDoor lockedDoor) {
                        if (lockedDoor.isLocked) {
                            if (heldItem != null && heldItem.isKey && heldItem.id == lockedDoor.requiredKeyID) {
                                Debug.Log("🔓 [PlayerInteraction] ปลดล็อกประตูด้วยกุญแจ: " + heldItem.id);
                                lockedDoor.Unlock();
                                inventory.Remove(heldItem);
                                selectedIndex = 0; UpdateHandVisuals();
                                StartCoroutine(ShowTempMessage("Unlocked"));
                            } else {
                                Debug.Log("🚫 [PlayerInteraction] ประตูล็อกอยู่ (ต้องการกุญแจ: " + lockedDoor.requiredKeyID + ")");
                                StartCoroutine(ShowTempMessage("Need Correct Key"));
                            }
                        } else lockedDoor.Interact(true);
                    }
                    else if (door is BarricadedDoor barricadedDoor) {
                        if (barricadedDoor.IsStillBarricaded) {
                            Debug.Log("🚧 [PlayerInteraction] ประตูถูกไม้กั้นไว้");
                            StartCoroutine(ShowTempMessage("It's barricaded!"));
                        } else door.Interact(true);
                    }
                    else {
                        Debug.Log("🚪 [PlayerInteraction] เปิด/ปิดประตูปกติ");
                        door.Interact(true);
                    }
                }
            }
        }

        IEnumerator ShowTempMessage(string msg) {
            currentMessage = msg; yield return new WaitForSeconds(2f); currentMessage = "";
        }
    }
}
