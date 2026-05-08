using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using qwq;

/// <summary>
/// 敌人回溯技能入口：外部调用 Cast() 即可触发全场敌人回溯。
/// </summary>
public class EnemyRewindSkillRuntime : MonoBehaviour
{
    [Header("回溯附加 Buff（可选）")]
    [Tooltip("与局部 Q 技能相同：可拖入 Buff 组合；成功触发全场回溯时给所有在场敌人施加。留空则不附加。")]
    [SerializeField] private BuffSetSO buffSetOnCast;

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

    [Header("隐藏兵口淡入淡出")]
    [SerializeField] private Transform[] spawnLaneFadeRoots;
    [SerializeField] private float spawnLaneFadeDuration = 0.35f;

    private readonly HashSet<int> tempOpenedLaneIndexes = new HashSet<int>();
    private readonly Dictionary<int, Coroutine> fadeCoroutines = new Dictionary<int, Coroutine>();
    private Coroutine autoCloseCoroutine;

    private struct SpriteRendererAlpha
    {
        public SpriteRenderer renderer;
        public float alpha;
    }

    private void Start()
    {
        ApplyInitialSpawnLaneVisualAlpha();
    }

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

    /// <summary>按档位索引施放全局回溯（与键盘档位共用同一份 <see cref="rewindTiers"/>）。</summary>
    public bool CastRewindTier(int tierIndex)
    {
        if (rewindTiers == null || rewindTiers.Length == 0)
            return false;
        if (tierIndex < 0 || tierIndex >= rewindTiers.Length)
            return false;
        return CastTier(rewindTiers[tierIndex]);
    }

    //全局回溯调用
    private bool CastTier(RewindTier tier)
    {
        int energyCost = Mathf.Max(0, tier.energyCost);
        if (EnergyPoolRuntime.Instance != null && !EnergyPoolRuntime.Instance.TryConsume(energyCost))
            return false;

        float rewindSeconds = Mathf.Max(0.1f, tier.rewindSeconds);
        float playbackDuration = Mathf.Max(0.05f, tier.playbackDuration);
        ApplyBuffSetToAllActiveEnemies();
        GameEvent.TriggerEnemyRewindRequested(rewindSeconds, playbackDuration);
        OpenAllSpawnLanesTemporarily(tier);
        return true;
    }

    private void ApplyBuffSetToAllActiveEnemies()
    {
        if (buffSetOnCast == null)
            return;

        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && enemies[i].isActiveAndEnabled)
                enemies[i].ApplyBuffSet(buffSetOnCast);
        }
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
            FadeSpawnLaneVisual(i, true);
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
                FadeSpawnLaneVisual(laneIndex, false);
            }
        }

        tempOpenedLaneIndexes.Clear();
        autoCloseCoroutine = null;
    }

    private void FadeSpawnLaneVisual(int laneIndex, bool visible)
    {
        Transform fadeRoot = GetSpawnLaneFadeRoot(laneIndex);
        if (fadeRoot == null)
            return;

        if (fadeCoroutines.TryGetValue(laneIndex, out Coroutine running) && running != null)
            StopCoroutine(running);

        fadeCoroutines[laneIndex] = StartCoroutine(FadeSpawnLaneVisualRoutine(laneIndex, fadeRoot, visible));
    }

    private void ApplyInitialSpawnLaneVisualAlpha()
    {
        WaveManager waveManager = WaveManager.Instance;
        if (waveManager == null || waveManager.spawnLanes == null)
            return;

        for (int i = 0; i < waveManager.spawnLanes.Count; i++)
        {
            Transform fadeRoot = GetSpawnLaneFadeRoot(i);
            if (fadeRoot == null)
                continue;

            SetSpawnLaneVisualAlpha(fadeRoot, waveManager.IsSpawnLaneEnabled(i) ? 1f : 0f);
        }
    }

    private Transform GetSpawnLaneFadeRoot(int laneIndex)
    {
        if (spawnLaneFadeRoots != null && laneIndex >= 0 && laneIndex < spawnLaneFadeRoots.Length && spawnLaneFadeRoots[laneIndex] != null)
            return spawnLaneFadeRoots[laneIndex];

        WaveManager waveManager = WaveManager.Instance;
        if (waveManager == null || waveManager.spawnLanes == null || laneIndex < 0 || laneIndex >= waveManager.spawnLanes.Count)
            return null;

        SpawnLaneConfig lane = waveManager.spawnLanes[laneIndex];
        return lane != null ? lane.spawnPoint : null;
    }

    private IEnumerator FadeSpawnLaneVisualRoutine(int laneIndex, Transform fadeRoot, bool visible)
    {
        SpriteRenderer[] renderers = fadeRoot.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            fadeCoroutines.Remove(laneIndex);
            yield break;
        }

        SpriteRendererAlpha[] startAlphas = new SpriteRendererAlpha[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            startAlphas[i] = new SpriteRendererAlpha
            {
                renderer = renderers[i],
                alpha = renderers[i] != null ? renderers[i].color.a : 0f
            };
        }

        float targetAlpha = visible ? 1f : 0f;
        float duration = Mathf.Max(0.01f, spawnLaneFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < startAlphas.Length; i++)
            {
                SpriteRenderer renderer = startAlphas[i].renderer;
                if (renderer == null)
                    continue;

                Color color = renderer.color;
                color.a = Mathf.Lerp(startAlphas[i].alpha, targetAlpha, t);
                renderer.color = color;
            }

            yield return null;
        }

        for (int i = 0; i < startAlphas.Length; i++)
        {
            SpriteRenderer renderer = startAlphas[i].renderer;
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = targetAlpha;
            renderer.color = color;
        }

        fadeCoroutines.Remove(laneIndex);
    }

    private static void SetSpawnLaneVisualAlpha(Transform fadeRoot, float alpha)
    {
        SpriteRenderer[] renderers = fadeRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
