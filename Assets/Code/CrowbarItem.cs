using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// แนบ script นี้ไว้ที่ Root GameObject ของชะแลง (Crowbar)
    ///
    /// ✅ Checklist สำหรับตั้งค่าใน Inspector:
    ///   1. Root GameObject ต้องมี Collider (BoxCollider / MeshCollider) ที่ isTrigger = FALSE
    ///   2. ถ้าโมเดล 3D อยู่ใน Child ให้แน่ใจว่า MeshRenderer enabled = true
    ///   3. Layer ของ object ต้องไม่ถูก ignoreLayer ของ PlayerInteraction กรอง
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CrowbarItem : MonoBehaviour
    {
        void Awake()
        {
            ValidateSetup();
        }

        void ValidateSetup()
        {
            // เช็ค Collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError($"❌ [CrowbarItem] '{name}' ไม่มี Collider! เพิ่ม BoxCollider หรือ MeshCollider ที่ isTrigger=false");
                return;
            }
            if (col.isTrigger)
            {
                Debug.LogWarning($"⚠️ [CrowbarItem] '{name}' Collider เป็น isTrigger=true — Raycast จะโดนไม่ได้! ให้ปิด isTrigger");
            }

            // เช็ค Renderer (รวม child)
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend == null)
            {
                Debug.LogWarning($"⚠️ [CrowbarItem] '{name}' ไม่พบ Renderer ใน GameObject หรือ Child — โมเดลอาจไม่แสดงผล");
            }
            else if (!rend.enabled)
            {
                Debug.LogWarning($"⚠️ [CrowbarItem] '{name}' Renderer ถูกปิดอยู่ (enabled=false)");
            }
            else
            {
                Debug.Log($"⛏️ [CrowbarItem] '{name}' พร้อมใช้งาน");
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.3f, 0.1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.2f);
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, "⛏️ Crowbar");
        }
#endif
    }
}
