using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
        [Tooltip("该档位下，所有出兵口临时开启持续时间（秒）")]
        public float spawnLaneOpenDuration;
    }

    [Header("测试档位（按键触发）")]
    [SerializeField] private RewindTier[] rewindTiers =
    {
        new RewindTier { name = "Low", triggerKey = KeyCode.Alpha1, energyCost = 10, rewindSeconds = 1.5f, playbackDuration = 0.25f, spawnLaneOpenDuration = 3f },
        new RewindTier { name = "Mid", triggerKey = KeyCode.Alpha2, energyCost = 20, rewindSeconds = 3f, playbackDuration = 0.4f, spawnLaneOpenDuration = 5f },
        new RewindTier { name = "High", triggerKey = KeyCode.Alpha3, energyCost = 35, rewindSeconds = 5f, playbackDuration = 0.6f, spawnLaneOpenDuration = 8f }
    };

    private readonly HashSet<int> tempOpenedLaneIndexes = new HashSet<int>();
    private Coroutine autoCloseCoroutine;

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
    //全局回溯调用
    private bool CastTier(RewindTier tier)
    {
        int energyCost = Mathf.Max(0, tier.energyCost);
        if (EnergyPoolRuntime.Instance != null && !EnergyPoolRuntime.Instance.TryConsume(energyCost))
            return false;

        float rewindSeconds = Mathf.Max(0.1f, tier.rewindSeconds);
        float playbackDuration = Mathf.Max(0.05f, tier.playbackDuration);
        GameEvent.TriggerEnemyRewindRequested(rewindSeconds, playbackDuration);
        OpenAllSpawnLanesTemporarily(tier);
        return true;
    }
    //开启塔
    private void OpenAllSpawnLanesTemporarily(RewindTier tier)
    {
        WaveManager waveManager = WaveManager.Instance;
        if (waveManager == null || waveManager.spawnLanes == null || waveManager.spawnLanes.Count == 0)
            return;

        for (int i = 0; i < waveManager.spawnLanes.Count; i++)
        {
            if (waveManager.IsSpawnLaneEnabled(i))
                continue;

            waveManager.SetSpawnLaneEnabled(i, true);
            tempOpenedLaneIndexes.Add(i);
        }

        if (tempOpenedLaneIndexes.Count == 0)
            return;

        if (autoCloseCoroutine != null)
            StopCoroutine(autoCloseCoroutine);

        float duration = Mathf.Max(0.05f, tier.spawnLaneOpenDuration);
        autoCloseCoroutine = StartCoroutine(AutoCloseSpawnLanesAfterDelay(duration));
    }
    /// <summary>
    /// 计算塔开启时间
    /// </summary>
    /// <param name="duration"></param>
    /// <returns></returns>
    private IEnumerator AutoCloseSpawnLanesAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);

        WaveManager waveManager = WaveManager.Instance;
        if (waveManager != null)
        {
            foreach (int laneIndex in tempOpenedLaneIndexes)
            {
                waveManager.SetSpawnLaneEnabled(laneIndex, false);
            }
        }

        tempOpenedLaneIndexes.Clear();
        autoCloseCoroutine = null;
    }
}
