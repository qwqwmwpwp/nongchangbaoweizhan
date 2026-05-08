using UnityEngine;
using UnityEngine.UI;

namespace qwq
{
    [DisallowMultipleComponent]
    public class PlantGrowthSliderUI : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        private void Awake()
        {
            EnsureSlider();
            ConfigureSlider();
        }

        private void OnValidate()
        {
            EnsureSlider();
            ConfigureSlider();
        }

        public void SetProgress01(float value)
        {
            if (slider == null)
                return;

            ConfigureSlider();
            slider.value = Mathf.Clamp01(value);
        }

        public void SetFull()
        {
            SetProgress01(1f);
        }

        public void Clear()
        {
            SetProgress01(0f);
        }

        private void EnsureSlider()
        {
            if (slider == null)
                slider = GetComponent<Slider>();
        }

        private void ConfigureSlider()
        {
            if (slider == null)
                return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
        }
    }
}
