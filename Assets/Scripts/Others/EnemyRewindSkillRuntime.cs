using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using qwq;
using TMPro;
using UnityEngine.UI;

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
        [Tooltip("Independent cooldown time for this tier, in seconds.")]
        public float cooldownSeconds;
    }

    [Header("测试档位（按键触发）")]
    [SerializeField] private RewindTier[] rewindTiers =
    {
        new RewindTier { name = "Low", triggerKey = KeyCode.Alpha1, energyCost = 10, rewindSeconds = 1.5f, playbackDuration = 0.25f, spawnLaneOpenDuration = 3f, cooldownSeconds = 5f },
        new RewindTier { name = "Mid", triggerKey = KeyCode.Alpha2, energyCost = 20, rewindSeconds = 3f, playbackDuration = 0.4f, spawnLaneOpenDuration = 5f, cooldownSeconds = 8f },
        new RewindTier { name = "High", triggerKey = KeyCode.Alpha3, energyCost = 35, rewindSeconds = 5f, playbackDuration = 0.6f, spawnLaneOpenDuration = 8f, cooldownSeconds = 12f }
    };

    [Header("隐藏兵口淡入淡出")]
    [Header("Cooldown UI")]
    [Tooltip("Shared TMP text used to show the currently relevant effect cooldown.")]
    [SerializeField] private TMP_Text cooldownText;
    [Tooltip("Shared cooldown mask Image. It will be configured as Filled/Radial360/Clockwise.")]
    [SerializeField] private Image cooldownMask;

    [SerializeField] private Transform[] spawnLaneFadeRoots;
    [SerializeField] private float spawnLaneFadeDuration = 0.35f;

    private readonly HashSet<int> tempOpenedLaneIndexes = new HashSet<int>();
    private readonly Dictionary<int, Coroutine> fadeCoroutines = new Dictionary<int, Coroutine>();
    private Coroutine autoCloseCoroutine;
    private float[] cooldownRemainingByTier;
    private int activeCooldownTierIndex = -1;

    private struct SpriteRendererAlpha
    {
        public SpriteRenderer renderer;
        public float alpha;
    }

    private void Start()
    {
        EnsureCooldownState();
        ConfigureCooldownView();
        ApplyInitialSpawnLaneVisualAlpha();
    }

    private void Update()
    {
        TickCooldowns(Time.deltaTime);
        if (rewindTiers == null)
            return;

        for (int i = 0; i < rewindTiers.Length; i++)
        {
            if (rewindTiers[i].triggerKey == KeyCode.None)
                continue;

            if (!Input.GetKeyDown(rewindTiers[i].triggerKey))
                continue;

            CastTier(i);
            break;
        }
    }

    public bool Cast()
    {
        if (rewindTiers == null || rewindTiers.Length == 0)
            return false;
        return CastTier(0);
    }

    /// <summary>按档位索引施放全局回溯（与键盘档位共用同一份 <see cref="rewindTiers"/>）。</summary>
    public bool CastRewindTier(int tierIndex)
    {
        if (rewindTiers == null || rewindTiers.Length == 0)
            return false;
        if (tierIndex < 0 || tierIndex >= rewindTiers.Length)
            return false;
        return CastTier(tierIndex);
    }

    //全局回溯调用
    private bool CastTier(int tierIndex)
    {
        EnsureCooldownState();
        if (tierIndex < 0 || tierIndex >= rewindTiers.Length)
            return false;

        if (TryGetActiveCooldownTierIndex(out _))
        {
            UpdateCooldownView();
            return false;
        }

        RewindTier tier = rewindTiers[tierIndex];
        int energyCost = Mathf.Max(0, tier.energyCost);
        if (EnergyPoolRuntime.Instance != null && !EnergyPoolRuntime.Instance.TryConsume(energyCost))
            return false;

        float rewindSeconds = Mathf.Max(0.1f, tier.rewindSeconds);
        float playbackDuration = Mathf.Max(0.05f, tier.playbackDuration);
        ApplyBuffSetToAllActiveEnemies();
        GameEvent.TriggerEnemyRewindRequested(rewindSeconds, playbackDuration);
        OpenAllSpawnLanesTemporarily(tier);
        StartTierCooldown(tierIndex);
        return true;
    }

    public bool IsTierCoolingDown(int tierIndex)
    {
        EnsureCooldownState();
        return tierIndex >= 0
            && tierIndex < cooldownRemainingByTier.Length
            && cooldownRemainingByTier[tierIndex] > 0f;
    }

    public bool IsAnyTierCoolingDown()
    {
        EnsureCooldownState();
        return TryGetActiveCooldownTierIndex(out _);
    }

    public float GetTierCooldownRemaining(int tierIndex)
    {
        EnsureCooldownState();
        if (tierIndex < 0 || tierIndex >= cooldownRemainingByTier.Length)
            return 0f;

        return Mathf.Max(0f, cooldownRemainingByTier[tierIndex]);
    }

    private void StartTierCooldown(int tierIndex)
    {
        if (tierIndex < 0 || tierIndex >= rewindTiers.Length)
            return;

        float cooldown = Mathf.Max(0f, rewindTiers[tierIndex].cooldownSeconds);
        cooldownRemainingByTier[tierIndex] = cooldown;
        activeCooldownTierIndex = tierIndex;
        UpdateCooldownView();
    }

    private void TickCooldowns(float deltaTime)
    {
        EnsureCooldownState();
        if (cooldownRemainingByTier.Length == 0)
            return;

        for (int i = 0; i < cooldownRemainingByTier.Length; i++)
        {
            if (cooldownRemainingByTier[i] > 0f)
                cooldownRemainingByTier[i] = Mathf.Max(0f, cooldownRemainingByTier[i] - deltaTime);

        }

        UpdateCooldownView();
    }

    private void EnsureCooldownState()
    {
        int tierCount = rewindTiers != null ? rewindTiers.Length : 0;
        if (cooldownRemainingByTier != null && cooldownRemainingByTier.Length == tierCount)
            return;

        float[] previous = cooldownRemainingByTier;
        cooldownRemainingByTier = new float[tierCount];
        if (previous == null)
            return;

        int copyCount = Mathf.Min(previous.Length, cooldownRemainingByTier.Length);
        for (int i = 0; i < copyCount; i++)
            cooldownRemainingByTier[i] = previous[i];
    }

    private void ConfigureCooldownView()
    {
        if (cooldownMask != null)
        {
            cooldownMask.type = Image.Type.Filled;
            cooldownMask.fillMethod = Image.FillMethod.Radial360;
            cooldownMask.fillOrigin = (int)Image.Origin360.Top;
            cooldownMask.fillClockwise = true;
        }

        UpdateCooldownView();
    }

    private void UpdateCooldownView()
    {
        int tierIndex = ResolveDisplayedCooldownTierIndex();
        bool hasDisplayedTier = rewindTiers != null && tierIndex >= 0 && tierIndex < rewindTiers.Length;
        float remaining = hasDisplayedTier ? GetTierCooldownRemaining(tierIndex) : 0f;
        float cooldown = hasDisplayedTier ? Mathf.Max(0f, rewindTiers[tierIndex].cooldownSeconds) : 0f;
        bool coolingDown = remaining > 0f && cooldown > 0f;

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(coolingDown);
            cooldownText.text = coolingDown ? Mathf.CeilToInt(remaining).ToString() : string.Empty;
        }

        if (cooldownMask != null)
        {
            cooldownMask.gameObject.SetActive(coolingDown);
            cooldownMask.fillAmount = coolingDown ? Mathf.Clamp01(remaining / cooldown) : 0f;
        }
    }

    private int ResolveDisplayedCooldownTierIndex()
    {
        if (!TryGetActiveCooldownTierIndex(out int tierIndex))
        {
            activeCooldownTierIndex = -1;
            return -1;
        }

        return tierIndex;
    }

    private bool TryGetActiveCooldownTierIndex(out int tierIndex)
    {
        tierIndex = -1;
        if (rewindTiers == null || cooldownRemainingByTier == null || cooldownRemainingByTier.Length == 0)
            return false;

        if (activeCooldownTierIndex >= 0
            && activeCooldownTierIndex < cooldownRemainingByTier.Length
            && cooldownRemainingByTier[activeCooldownTierIndex] > 0f)
        {
            tierIndex = activeCooldownTierIndex;
            return true;
        }

        for (int i = 0; i < cooldownRemainingByTier.Length; i++)
        {
            if (cooldownRemainingByTier[i] > 0f)
            {
                activeCooldownTierIndex = i;
                tierIndex = i;
                return true;
            }
        }

        activeCooldownTierIndex = -1;
        return false;
    }

    public void SetDisplayedCooldownTier(int tierIndex)
    {
        if (rewindTiers == null || tierIndex < 0 || tierIndex >= rewindTiers.Length)
            return;

        if (TryGetActiveCooldownTierIndex(out _))
        {
            UpdateCooldownView();
            return;
        }

        activeCooldownTierIndex = tierIndex;
        UpdateCooldownView();
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
