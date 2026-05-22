using UnityEngine;

namespace HorrorGame
{
    public class NoiseManager : MonoBehaviour
    {
        public static NoiseManager Instance { get; private set; }

        [Header("Settings")]
        public float currentNoise = 0f;
        public float maxNoise = 100f;
        public float noiseDecayRate = 20f; // Noise reduction per second

        [Header("Multipliers")]
        public float crouchNoise = 5f;
        public float walkNoise = 25f;
        public float runNoise = 60f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            // Decay noise over time
            if (currentNoise > 0)
            {
                currentNoise -= noiseDecayRate * Time.deltaTime;
                currentNoise = Mathf.Max(0, currentNoise);
            }
        }

        public void AddNoise(float amount)
        {
            // We take the max of current or new noise to simulate "loudest sound wins" 
            // or just add it. For a "bar" it's usually better to lerp or set based on activity.
            currentNoise = Mathf.Max(currentNoise, amount);
        }
        
        public float GetNoiseLevel() => currentNoise / maxNoise;
    }
}
