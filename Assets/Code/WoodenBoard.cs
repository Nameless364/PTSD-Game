using UnityEngine;

namespace HorrorGame
{
    public class WoodenBoard : MonoBehaviour
    {
        public AudioClip breakSound;
        private AudioSource audioSource;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        public void Break()
        {
            if (breakSound != null) audioSource.PlayOneShot(breakSound);
            
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForce(transform.up * 2f + transform.forward * 2f, ForceMode.Impulse); 
            }
            
            Destroy(this);
            Destroy(gameObject, 10f);
        }
    }
}
