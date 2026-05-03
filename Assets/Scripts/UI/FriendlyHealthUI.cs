using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 友军头顶/跟随血条，用法与 <see cref="EnemyHealthUI"/> 一致：拖 Slider，由 <see cref="FriendlyUnit"/> 调用 <see cref="PlayerHealthChange"/>。
/// </summary>
public class FriendlyHealthUI : MonoBehaviour
{
    public Slider FriendlyHealth;

    public void PlayerHealthChange(int currentHealth, int maxHealth)
    {
        if (FriendlyHealth == null)
            return;

        FriendlyHealth.maxValue = Mathf.Max(1, maxHealth);
        FriendlyHealth.value = Mathf.Clamp(currentHealth, 0, maxHealth);
    }
}
