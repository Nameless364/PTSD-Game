using UnityEngine;
using System.Collections;

namespace HorrorGame
{
    public class HidingSpot : MonoBehaviour
    {
        [Header("Points")]
        public Transform insidePoint;
        public Transform outsidePoint;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;

        private bool isHiding = false;
        private AudioSource audioSource;
        private GameObject hidingPlayer;

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }

        void Update()
        {
            if (isHiding && hidingPlayer != null && insidePoint != null)
            {
                hidingPlayer.transform.position = insidePoint.position;
            }
        }

        public void ToggleHide(GameObject player)
        {
            isHiding = !isHiding;
            hidingPlayer = player;
            
            if (isHiding)
                StartCoroutine(EnterHiding(player));
            else
                StartCoroutine(ExitHiding(player));
        }

        IEnumerator EnterHiding(GameObject player)
        {
            PlaySound(openSound);
            yield return new WaitForSeconds(0.2f);
            
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }

            player.transform.position = insidePoint.position;
            player.transform.rotation = insidePoint.rotation;

            var movement = player.GetComponent<Movement>();
            if (movement != null) movement.SetHiding(true);

            PlaySound(closeSound);
        }

        IEnumerator ExitHiding(GameObject player)
        {
            PlaySound(openSound);
            yield return new WaitForSeconds(0.2f);

            player.transform.position = outsidePoint.position;

            var movement = player.GetComponent<Movement>();
            if (movement != null) movement.SetHiding(false);

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            hidingPlayer = null;
            PlaySound(closeSound);
        }

        // เพิ่มฟังก์ชันที่หายไปกลับมาครับ
        void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null) 
                audioSource.PlayOneShot(clip);
        }

        public bool IsHiding() => isHiding;
    }
}
