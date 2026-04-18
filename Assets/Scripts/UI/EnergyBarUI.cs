using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将能量池同步到 Slider（0~1 映射 Current/Max），仅订阅事件，不直接依赖击杀逻辑。
/// </summary>
[RequireComponent(typeof(Slider))]
public class EnergyBarUI : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private bool hideWhenZeroMax = false;

    private void Reset()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        GameEvent.EnergyChanged += OnEnergyChanged;

        if (EnergyPoolRuntime.Instance != null)
            Apply(EnergyPoolRuntime.Instance.Current, EnergyPoolRuntime.Instance.Max);
    }

    private void OnDisable()
    {
        GameEvent.EnergyChanged -= OnEnergyChanged;
    }

    private void OnEnergyChanged(int current, int max)
    {
        Apply(current, max);
    }

    private void Apply(int current, int max)
    {
        if (slider == null) return;

        float denom = Mathf.Max(1, max);
        slider.value = Mathf.Clamp01(current / denom);
        if (hideWhenZeroMax)
            slider.gameObject.SetActive(max > 0);
    }
}
