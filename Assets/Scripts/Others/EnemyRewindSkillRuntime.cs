using UnityEngine;

/// <summary>
/// 敌人回溯技能入口：外部调用 Cast() 即可触发全场敌人回溯。
/// </summary>
public class EnemyRewindSkillRuntime : MonoBehaviour
{
    [System.Serializable]
    private struct RewindTier
    {
        public string name;
        public KeyCode triggerKey;
        public int energyCost;
        [Tooltip("回到过去的秒数（决定回到多早的位置）")]
        public float rewindSeconds;
        [Tooltip("实际倒放动画时长（秒）")]
        public float playbackDuration;
    }

    [Header("测试档位（按键触发）")]
    [SerializeField] private RewindTier[] rewindTiers =
    {
        new RewindTier { name = "Low", triggerKey = KeyCode.Alpha1, energyCost = 10, rewindSeconds = 1.5f, playbackDuration = 0.25f },
        new RewindTier { name = "Mid", triggerKey = KeyCode.Alpha2, energyCost = 20, rewindSeconds = 3f, playbackDuration = 0.4f },
        new RewindTier { name = "High", triggerKey = KeyCode.Alpha3, energyCost = 35, rewindSeconds = 5f, playbackDuration = 0.6f }
    };

    private void Update()
    {
        for (int i = 0; i < rewindTiers.Length; i++)
        {
            if (rewindTiers[i].triggerKey == KeyCode.None)
                continue;

            if (!Input.GetKeyDown(rewindTiers[i].triggerKey))
                continue;

            CastTier(rewindTiers[i]);
            break;
        }
    }

    public bool Cast()
    {
        if (rewindTiers == null || rewindTiers.Length == 0)
            return false;
        return CastTier(rewindTiers[0]);
    }

    private bool CastTier(RewindTier tier)
    {
        int energyCost = Mathf.Max(0, tier.energyCost);
        if (EnergyPoolRuntime.Instance != null && !EnergyPoolRuntime.Instance.TryConsume(energyCost))
            return false;

        float rewindSeconds = Mathf.Max(0.1f, tier.rewindSeconds);
        float playbackDuration = Mathf.Max(0.05f, tier.playbackDuration);
        GameEvent.TriggerEnemyRewindRequested(rewindSeconds, playbackDuration);
        return true;
    }
}
