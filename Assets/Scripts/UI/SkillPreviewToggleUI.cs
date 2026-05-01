using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using qwq;
using System.IO;

/// <summary>
/// 最小技能预览交互：
/// - 按键进入预览模式
/// - 预览框跟随鼠标
/// - 鼠标左键点击后退出预览模式
/// </summary>
public class SkillPreviewToggleUI : MonoBehaviour
{
    private const string DebugLogPath = "D:/Unity Project/nongchangbaoweizhan/debug-b67a13.log";
    private const string DebugSessionId = "b67a13";

    [Header("引用")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform previewRoot;

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

        if (previewRoot == null && autoCreatePlaceholder)
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

    private void SetPreviewVisible(bool visible)
    {
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
        WriteDebugLog("H8", "Before TryGetMouseWorldPoint", "{}");
        // #endregion
        if (!TryGetMouseWorldPoint(out Vector3 center))
            return false;

        // #region agent log
        WriteDebugLog("H1", "TryCastRewindInRange center resolved",
            "{\"centerX\":" + center.x.ToString("F4")
            + ",\"centerY\":" + center.y.ToString("F4")
            + ",\"centerZ\":" + center.z.ToString("F4")
            + ",\"rewindRadius\":" + rewindRadius.ToString("F4")
            + ",\"enemyLayer\":" + enemyLayer.value
            + ",\"batteryLayer\":" + BatteryLayer.value + "}");
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
