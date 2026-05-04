using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using qwq;
using System.IO;

/// <summary>
/// 技能预览交互：按键进入预览、左键施法退出。
/// 可仅用 Canvas 占位，或指定 <see cref="worldRangePreview"/> 使用与关卡同平面的世界空间圆形预览（与 <see cref="rewindRadius"/> 检测一致）。
/// </summary>
public class SkillPreviewToggleUI : MonoBehaviour
{
    private const string DebugLogPath = "D:/Unity Project/nongchangbaoweizhan/debug-b67a13.log";
    private const string DebugSessionId = "b67a13";

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
    private Collider2D[] _overlapResults;
    private readonly HashSet<Enemy> _buffAppliedEnemies = new HashSet<Enemy>();
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
        _isPreviewing = true;
        // #region agent log
        WriteDebugLog("H1", "EnterPreview called", "{}");
        // #endregion
        SetPreviewVisible(true);
        UpdatePreviewPosition();
    }

    public void ExitPreview()
    {
        _isPreviewing = false;
        SetPreviewVisible(false);
    }
    //鼠标点击后触发该方法尝试回溯和关闭提示框
    private void CastAndExit()
    {
        // #region agent log
        WriteDebugLog("H6", "CastAndExit called",
            "{\"mouseButton\":\"" + placeMouseButton
            + "\",\"isPreviewing\":" + (_isPreviewing ? "true" : "false") + "}");
        // #endregion
        TryCastRewindInRange();
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
    private bool TryCastRewindInRange()
    {
        // #region agent log
        WriteDebugLog("H7", "TryCastRewindInRange entered", "{}");
        // #endregion

        int finalCost = Mathf.Max(0, energyCost);
        bool energyOk = EnergyPoolRuntime.Instance == null || EnergyPoolRuntime.Instance.TryConsume(finalCost);
        // #region agent log
        WriteDebugLog("H7", "Energy check result",
            "{\"energyPoolExists\":" + (EnergyPoolRuntime.Instance != null ? "true" : "false")
            + ",\"finalCost\":" + finalCost
            + ",\"energyOk\":" + (energyOk ? "true" : "false")
            + ",\"currentEnergy\":" + (EnergyPoolRuntime.Instance != null ? EnergyPoolRuntime.Instance.current : -1) + "}");
        // #endregion
        if (!energyOk)
            return false;

        // #region agent log
        WriteDebugLog("H8", "Before resolve cast center", "{}");
        // #endregion

        Vector3 center;
        if (UsesWorldRangePreview())
            center = worldRangePreview.position;
        else if (!TryGetMouseWorldPoint(out center))
            return false;

        // #region agent log
        WriteDebugLog("H1", "TryCastRewindInRange center resolved",
            "{\"centerX\":" + center.x.ToString("F4")
            + ",\"centerY\":" + center.y.ToString("F4")
            + ",\"centerZ\":" + center.z.ToString("F4")
            + ",\"rewindRadius\":" + rewindRadius.ToString("F4")
            + ",\"enemyLayer\":" + enemyLayer.value
            + ",\"batteryLayer\":" + BatteryLayer.value
            + ",\"fromWorldPreview\":" + (UsesWorldRangePreview() ? "true" : "false") + "}");
        // #endregion

        EnsureOverlapBuffer();

        float radius = Mathf.Max(0.1f, rewindRadius);
        float finalRewindSeconds = Mathf.Max(0.1f, rewindSeconds);
        float finalPlaybackDuration = Mathf.Max(0.05f, playbackDuration);

        int batteryHitCount = Physics2D.OverlapCircleNonAlloc(center, radius, _overlapResults, BatteryLayer);
        // #region agent log
        WriteDebugLog("H4", "Battery overlap result",
            "{\"hitCount\":" + batteryHitCount
            + ",\"centerX\":" + center.x.ToString("F4")
            + ",\"centerY\":" + center.y.ToString("F4")
            + ",\"centerZ\":" + center.z.ToString("F4")
            + ",\"radius\":" + radius.ToString("F4") + "}");
        // #endregion

        for (int i = 0; i < batteryHitCount; i++)
        {
            if (_overlapResults[i].GetComponent<IBatteryBackward>() is IBatteryBackward batteryBackward)
            {

                batteryBackward.Backward(3f);
            }
        }


        int hitCount = Physics2D.OverlapCircleNonAlloc(center, radius, _overlapResults, enemyLayer);
        // #region agent log
        WriteDebugLog("H3", "Enemy overlap result",
            "{\"hitCount\":" + hitCount
            + ",\"centerX\":" + center.x.ToString("F4")
            + ",\"centerY\":" + center.y.ToString("F4")
            + ",\"centerZ\":" + center.z.ToString("F4")
            + ",\"radius\":" + radius.ToString("F4") + "}");
        // #endregion
        if (hitCount <= 0)
            return false;

        _buffAppliedEnemies.Clear();
        _rewindAppliedRecorders.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D collider2D = _overlapResults[i];
            if (collider2D == null) continue;

            Enemy enemy = collider2D.GetComponentInParent<Enemy>();
            if (enemy != null && !_buffAppliedEnemies.Contains(enemy))
            {
                _buffAppliedEnemies.Add(enemy);
                if (buffSetOnCast != null)
                    enemy.ApplyBuffSet(buffSetOnCast);
            }

            EnemyRewindRecorder recorder = collider2D.GetComponentInParent<EnemyRewindRecorder>();
            if (recorder != null && !_rewindAppliedRecorders.Contains(recorder))
            {
                _rewindAppliedRecorders.Add(recorder);
                recorder.StartRewindBySkill(finalRewindSeconds, finalPlaybackDuration);
            }

            _overlapResults[i] = null;




        }

     
        return true;
    }

    private void EnsureOverlapBuffer()
    {
        int targetSize = Mathf.Max(8, maxOverlapResults);
        if (_overlapResults != null && _overlapResults.Length == targetSize)
            return;
        _overlapResults = new Collider2D[targetSize];
    }

    private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            worldPoint = Vector3.zero;
            return false;
        }

        // #region agent log
        WriteDebugLog("H2", "Camera snapshot before world point",
            "{\"camPosX\":" + cam.transform.position.x.ToString("F4")
            + ",\"camPosY\":" + cam.transform.position.y.ToString("F4")
            + ",\"camPosZ\":" + cam.transform.position.z.ToString("F4")
            + ",\"camRotX\":" + cam.transform.eulerAngles.x.ToString("F4")
            + ",\"camRotY\":" + cam.transform.eulerAngles.y.ToString("F4")
            + ",\"camRotZ\":" + cam.transform.eulerAngles.z.ToString("F4")
            + ",\"camOrthographic\":" + (cam.orthographic ? "true" : "false")
            + ",\"mouseX\":" + Input.mousePosition.x.ToString("F2")
            + ",\"mouseY\":" + Input.mousePosition.y.ToString("F2")
            + ",\"use2DWorldPoint\":" + (use2DWorldPoint ? "true" : "false")
            + ",\"configuredWorldZ\":" + worldZ.ToString("F4")
            + ",\"configuredGroundHeight\":" + groundHeight.ToString("F4") + "}");
        // #endregion

        if (use2DWorldPoint)
        {
            float distance = Mathf.Abs(worldZ - cam.transform.position.z);
            worldPoint = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, distance));
            worldPoint.z = worldZ;
            // #region agent log
            WriteDebugLog("H2", "World point resolved via ScreenToWorldPoint",
                "{\"distance\":" + distance.ToString("F4")
                + ",\"worldX\":" + worldPoint.x.ToString("F4")
                + ",\"worldY\":" + worldPoint.y.ToString("F4")
                + ",\"worldZ\":" + worldPoint.z.ToString("F4") + "}");
            // #endregion
            return true;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
        if (!ground.Raycast(ray, out float enter))
        {
            worldPoint = Vector3.zero;
            // #region agent log
            WriteDebugLog("H5", "Ground raycast failed",
                "{\"rayOriginX\":" + ray.origin.x.ToString("F4")
                + ",\"rayOriginY\":" + ray.origin.y.ToString("F4")
                + ",\"rayOriginZ\":" + ray.origin.z.ToString("F4")
                + ",\"rayDirX\":" + ray.direction.x.ToString("F4")
                + ",\"rayDirY\":" + ray.direction.y.ToString("F4")
                + ",\"rayDirZ\":" + ray.direction.z.ToString("F4")
                + ",\"groundHeight\":" + groundHeight.ToString("F4") + "}");
            // #endregion
            return false;
        }

        worldPoint = ray.GetPoint(enter);
        // #region agent log
        WriteDebugLog("H5", "World point resolved via ray-plane",
            "{\"enter\":" + enter.ToString("F4")
            + ",\"worldX\":" + worldPoint.x.ToString("F4")
            + ",\"worldY\":" + worldPoint.y.ToString("F4")
            + ",\"worldZ\":" + worldPoint.z.ToString("F4") + "}");
        // #endregion
        return true;
    }

    private void WriteDebugLog(string hypothesisId, string message, string dataJson)
    {
        try
        {
            long timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string payload = "{\"sessionId\":\"" + DebugSessionId
                + "\",\"runId\":\"pre-fix\""
                + ",\"hypothesisId\":\"" + hypothesisId
                + "\",\"location\":\"SkillPreviewToggleUI.cs\""
                + ",\"message\":\"" + EscapeForJson(message)
                + "\",\"data\":" + (string.IsNullOrEmpty(dataJson) ? "{}" : dataJson)
                + ",\"timestamp\":" + timestamp + "}";
            File.AppendAllText(DebugLogPath, payload + "\n");
        }
        catch
        {
        }
    }

    private static string EscapeForJson(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;
        return source.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
