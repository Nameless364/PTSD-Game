using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace HorrorGame
{
    public class EndTrigger : MonoBehaviour
    {
        [Header("End Game Settings")]
        public string escapeMessage = "YOU ESCAPED!";
        public float delayBeforeRestart = 3f;
        public Color triggerColor = new Color(0, 1, 0, 0.3f); // สีสำหรับวาดในหน้า Scene

        private bool hasEnded = false;

        // ฟังก์ชันนี้จะทำงานเมื่อมีวัตถุวิ่งเข้ามาชน (ต้องตั้งค่า Collider เป็น Is Trigger)
        private void OnTriggerEnter(Collider other)
        {
            if (hasEnded) return;

            // ตรวจสอบว่าเป็นผู้เล่นหรือไม่ (เช็คจาก Tag หรือสคริปต์ Movement)
            if (other.CompareTag("Player") || other.GetComponentInParent<Movement>() != null)
            {
                Debug.Log("🏁 [EndTrigger] ผู้เล่นเข้าสู่จุดจบเกม!");
                StartCoroutine(EndGameRoutine());
            }
        }

        IEnumerator EndGameRoutine()
        {
            hasEnded = true;
            
            // แสดงข้อความใน Console
            Debug.Log("🏁 " + escapeMessage);

            // คุณสามารถเพิ่ม UI สำหรับแสดงข้อความชนะตรงนี้ได้ครับ
            yield return new WaitForSeconds(delayBeforeRestart);

            Debug.Log("🔄 กำลังเริ่มเกมใหม่...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ช่วยให้มองเห็นขอบเขตของ Trigger ในหน้า Scene (จะไม่เห็นในเกม)
        private void OnDrawGizmos()
        {
            Gizmos.color = triggerColor;
            BoxCollider box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
        }
    }
}
