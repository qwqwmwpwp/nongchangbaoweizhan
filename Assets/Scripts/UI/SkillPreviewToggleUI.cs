using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using qwq;

/// <summary>
/// 技能预览交互：按键进入预览、左键施法退出。
/// 可仅用 Canvas 占位，或指定 <see cref="worldRangePreview"/> 使用与关卡同平面的世界空间圆形预览（与 <see cref="rewindRadius"/> 检测一致）。
/// </summary>
public class SkillPreviewToggleUI : MonoBehaviour
{
    private enum SkillCastMode
    {
        RewindEnemiesInTowerRange,
        CatalyzeTowersInRange
    }

    [Header("引用")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform previewRoot;
    [Tooltip("指定后：用该物体在世界坐标跟随鼠标，并以其位置为施法圆心；不再自动创建 Canvas 方块（可关闭 autoCreatePlaceholder）。")]
    [SerializeField] private Transform worldRangePreview;
    [Tooltip("为 true 时按 Sprite 原始尺寸与 rewindRadius 自动设置 worldRangePreview 的均匀 localScale。")]
    [SerializeField] private bool syncWorldPreviewScaleFromRadius = true;

    [Header("按键")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q;
    [SerializeField] private int placeMouseButton = 0;

    [Header("Effect")]
    [SerializeField] private SkillCastMode castMode = SkillCastMode.RewindEnemiesInTowerRange;
    [SerializeField] private float catalysisSeconds = 3f;

    [Header("倒放技能")]
    [SerializeField] private int energyCost = 20;  // 能量消耗
    [SerializeField] private float rewindRadius = 4f;  // 技能作用半径
    [SerializeField] private float rewindSeconds = 3f;  // 倒流的时间长度
    [SerializeField] private float playbackDuration = 0.4f;  // 倒流动画时长
    [SerializeField] private LayerMask enemyLayer = ~0;  // 检测的层级
    [SerializeField] private LayerMask BatteryLayer = ~0;  // 检测的层级

    [SerializeField] private int maxOverlapResults = 64;
    [SerializeField] private float groundHeight = 0f;
    [SerializeField] private bool use2DWorldPoint = true;
    [SerializeField] private float worldZ = 0f;
    [SerializeField] private BuffSetSO buffSetOnCast;

    [Header("自动占位")]
    [SerializeField] private bool autoCreatePlaceholder = true;
    [SerializeField] private Vector2 placeholderSize = new Vector2(80f, 80f);
    [SerializeField] private Color placeholderColor = new Color(0.3f, 0.8f, 1f, 0.4f);

    private RectTransform _canvasRect;
    private bool _isPreviewing;
    private SkillCastMode _activeCastMode;
    private Collider2D[] _overlapResults;
    private readonly HashSet<Plants> _affectedTowers = new HashSet<Plants>();
    private readonly HashSet<Enemy> _towerRangeEnemies = new HashSet<Enemy>();
    private readonly HashSet<EnemyRewindRecorder> _rewindAppliedRecorders = new HashSet<EnemyRewindRecorder>();

    private void Awake()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        if (targetCanvas != null)
            _canvasRect = targetCanvas.transform as RectTransform;

        if (previewRoot == null && autoCreatePlaceholder && !UsesWorldRangePreview())
            previewRoot = CreatePlaceholder();

        _overlapResults = new Collider2D[Mathf.Max(8, maxOverlapResults)];
        _activeCastMode = castMode;
        EnsureTowerLayerMask();
        SetPreviewVisible(false);
    }

    private void Update()
    {
        if (!_isPreviewing)
        {
            if (Input.GetKeyDown(triggerKey))
                EnterPreview();
            return;
        }

        UpdatePreviewPosition();

        if (Input.GetMouseButtonDown(placeMouseButton))
            CastAndExit();
    }

    public void EnterPreview()
    {
        EnterPreview(castMode);
    }

    public void EnterRewindPreview()
    {
        EnterPreview(SkillCastMode.RewindEnemiesInTowerRange);
    }

    public void EnterCatalysisPreview()
    {
        EnterPreview(SkillCastMode.CatalyzeTowersInRange);
    }

    public void ConfigureAsCatalysisPreview(KeyCode key, Color previewColor)
    {
        castMode = SkillCastMode.CatalyzeTowersInRange;
        _activeCastMode = castMode;
        triggerKey = key;
        ApplyWorldPreviewColor(previewColor);
        SetPreviewVisible(false);
    }

    private void EnterPreview(SkillCastMode activeMode)
    {
        _activeCastMode = activeMode;
        _isPreviewing = true;
        SetPreviewVisible(true);
        UpdatePreviewPosition();
    }

    public void ExitPreview()
    {
        _isPreviewing = false;
        _activeCastMode = castMode;
        SetPreviewVisible(false);
    }
    //鼠标点击后触发该方法尝试回溯和关闭提示框
    private void CastAndExit()
    {
        TryCastCurrentEffectInRange();
        ExitPreview();
    }

    private void UpdatePreviewPosition()
    {
        if (UsesWorldRangePreview())
        {
            if (!TryGetMouseWorldPoint(out Vector3 worldPoint))
                return;
            worldRangePreview.position = worldPoint;
            if (syncWorldPreviewScaleFromRadius)
                ApplyWorldPreviewScaleToMatchRadius();
            return;
        }

        if (previewRoot == null || targetCanvas == null)
            return;

        Vector2 screenPoint = Input.mousePosition;
        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;

        if (_canvasRect == null)
        {
            previewRoot.position = screenPoint;
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            previewRoot.anchoredPosition = localPoint;
    }

    private bool UsesWorldRangePreview()
    {
        return worldRangePreview != null;
    }

    private void ApplyWorldPreviewScaleToMatchRadius()
    {
        if (worldRangePreview == null)
            return;

        float radius = Mathf.Max(0.1f, rewindRadius);
        float diameterWorld = 2f * radius;

        var sr = worldRangePreview.GetComponent<SpriteRenderer>();
        float baseDiameter = 1f;
        if (sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;
            baseDiameter = Mathf.Max(b.size.x, b.size.y);
        }
        if (baseDiameter < 1e-4f)
            baseDiameter = 1f;

        Transform parent = worldRangePreview.parent;
        float parentMaxScale = 1f;
        if (parent != null)
        {
            Vector3 pls = parent.lossyScale;
            parentMaxScale = Mathf.Max(Mathf.Abs(pls.x), Mathf.Abs(pls.y));
            if (parentMaxScale < 1e-4f)
                parentMaxScale = 1f;
        }

        float uniformLocal = diameterWorld / (baseDiameter * parentMaxScale);
        worldRangePreview.localScale = new Vector3(uniformLocal, uniformLocal, 1f);
    }

    private void ApplyWorldPreviewColor(Color color)
    {
        if (worldRangePreview == null)
            return;

        SpriteRenderer sr = worldRangePreview.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = color;
    }

    private void SetPreviewVisible(bool visible)
    {
        if (UsesWorldRangePreview())
        {
            worldRangePreview.gameObject.SetActive(visible);
            if (previewRoot != null)
                previewRoot.gameObject.SetActive(false);
            return;
        }

        if (previewRoot != null)
            previewRoot.gameObject.SetActive(visible);
    }

    private RectTransform CreatePlaceholder()
    {
        if (targetCanvas == null)
            return null;

        GameObject go = new GameObject("SkillPreviewPlaceholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(targetCanvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = placeholderSize;

        Image image = go.GetComponent<Image>();
        image.color = placeholderColor;
        image.raycastTarget = false;
        return rect;
    }
    //在范围内挂载EnemyRewindRecorder的敌人开始倒放
    private bool TryCastCurrentEffectInRange()
    {
        switch (_activeCastMode)
        {
            case SkillCastMode.CatalyzeTowersInRange:
                return TryCastCatalysisInRange();
            case SkillCastMode.RewindEnemiesInTowerRange:
            default:
                return TryCastRewindInRange();
        }
    }

    private bool TryCastRewindInRange()
    {
        if (!TryGetCastCenter(out Vector3 center))
            return false;

        float radius = Mathf.Max(0.1f, rewindRadius);
        float finalRewindSeconds = Mathf.Max(0.1f, rewindSeconds);
        float finalPlaybackDuration = Mathf.Max(0.05f, playbackDuration);

        _towerRangeEnemies.Clear();
        _rewindAppliedRecorders.Clear();
        CollectTowersInRadius(center, radius);

        if (_affectedTowers.Count <= 0)
            return false;

        foreach (Plants tower in _affectedTowers)
        {
            CollectTowerRangeEnemies(tower, _towerRangeEnemies);
        }

        int finalCost = Mathf.Max(0, energyCost);
        bool energyOk = EnergyPoolRuntime.Instance == null || EnergyPoolRuntime.Instance.TryConsume(finalCost);
        if (!energyOk)
            return false;

        foreach (Plants tower in _affectedTowers)
        {
            if (tower != null)
                tower.Backward(finalRewindSeconds);
        }

        foreach (Enemy enemy in _towerRangeEnemies)
        {
            if (enemy == null || !enemy.IsInteractable)
                continue;

            EnemyRewindRecorder recorder = enemy.GetComponent<EnemyRewindRecorder>();
            if (recorder != null && !_rewindAppliedRecorders.Contains(recorder))
            {
                _rewindAppliedRecorders.Add(recorder);
                recorder.StartRewindBySkill(finalRewindSeconds, finalPlaybackDuration);
            }
        }

        return true;
    }

    private bool TryCastCatalysisInRange()
    {
        if (!TryGetCastCenter(out Vector3 center))
            return false;

        float radius = Mathf.Max(0.1f, rewindRadius);
        CollectTowersInRadius(center, radius);

        if (_affectedTowers.Count <= 0)
            return false;

        int finalCost = Mathf.Max(0, energyCost);
        bool energyOk = EnergyPoolRuntime.Instance == null || EnergyPoolRuntime.Instance.TryConsume(finalCost);
        if (!energyOk)
            return false;

        float duration = Mathf.Max(0.05f, catalysisSeconds);
        foreach (Plants tower in _affectedTowers)
        {
            if (tower != null)
                tower.Catalysis(duration);
        }

        return true;
    }

    private bool TryGetCastCenter(out Vector3 center)
    {
        if (UsesWorldRangePreview())
        {
            center = worldRangePreview.position;
            return true;
        }

        return TryGetMouseWorldPoint(out center);
    }

    private void CollectTowersInRadius(Vector3 center, float radius)
    {
        EnsureOverlapBuffer();
        _affectedTowers.Clear();

        int towerHitCount = Physics2D.OverlapCircleNonAlloc(center, radius, _overlapResults, BatteryLayer);
        for (int i = 0; i < towerHitCount; i++)
        {
            Collider2D collider2D = _overlapResults[i];
            if (collider2D == null)
                continue;

            Plants tower = collider2D.GetComponentInParent<Plants>();
            if (tower != null)
                _affectedTowers.Add(tower);

            _overlapResults[i] = null;
        }
    }

    private void EnsureOverlapBuffer()
    {
        int targetSize = Mathf.Max(8, maxOverlapResults);
        if (_overlapResults != null && _overlapResults.Length == targetSize)
            return;
        _overlapResults = new Collider2D[targetSize];
    }

    private void CollectTowerRangeEnemies(Plants tower, HashSet<Enemy> results)
    {
        if (tower == null || results == null)
            return;

        if (tower is Bamboo bamboo)
        {
            bamboo.CollectRewindTargetEnemies(results);
            return;
        }

        List<IDamageable> enemies = tower.plantsCtx?.enemys;
        if (enemies == null)
            return;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            IDamageable target = enemies[i];
            if (target == null || target.obj == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            if (target is Enemy enemy)
            {
                if (enemy.IsInteractable)
                    results.Add(enemy);
                else
                    enemies.RemoveAt(i);
            }
        }
    }

    private void EnsureTowerLayerMask()
    {
        if (BatteryLayer.value != 0)
            return;

        int towerLayer = LayerMask.NameToLayer("Palyer");
        BatteryLayer = towerLayer >= 0 ? 1 << towerLayer : ~0;
    }

    private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            worldPoint = Vector3.zero;
            return false;
        }

        if (use2DWorldPoint)
        {
            float distance = Mathf.Abs(worldZ - cam.transform.position.z);
            worldPoint = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance));
            worldPoint.z = worldZ;
            return true;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
        if (!ground.Raycast(ray, out float enter))
        {
            worldPoint = Vector3.zero;
            return false;
        }

        worldPoint = ray.GetPoint(enter);
        return true;
    }
}
