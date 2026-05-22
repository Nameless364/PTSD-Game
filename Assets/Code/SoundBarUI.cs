using UnityEngine;
using UnityEngine.UI;

namespace HorrorGame
{
    public class SoundBarUI : MonoBehaviour
    {
        public Slider soundSlider;
        public Image fillImage;
        public Color lowNoiseColor = Color.green;
        public Color highNoiseColor = Color.red;

        void Start()
        {
            if (soundSlider == null) soundSlider = GetComponent<Slider>();
        }

        void Update()
        {
            if (NoiseManager.Instance == null || soundSlider == null) return;

            float noiseLevel = NoiseManager.Instance.GetNoiseLevel();
            soundSlider.value = noiseLevel;

            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(lowNoiseColor, highNoiseColor, noiseLevel);
            }
        }
    }
}
