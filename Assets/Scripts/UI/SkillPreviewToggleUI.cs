using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 最小技能预览交互：
/// - 按键进入预览模式
/// - 预览框跟随鼠标
/// - 鼠标左键点击后退出预览模式
/// </summary>
public class SkillPreviewToggleUI : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform previewRoot;

    [Header("按键")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q;
    [SerializeField] private int placeMouseButton = 0;

    [Header("倒放技能")]
    [SerializeField] private int energyCost = 20;
    [SerializeField] private float rewindRadius = 4f;
    [SerializeField] private float rewindSeconds = 3f;
    [SerializeField] private float playbackDuration = 0.4f;
    [SerializeField] private LayerMask enemyLayer = ~0;
    [SerializeField] private int maxOverlapResults = 64;
    [SerializeField] private float groundHeight = 0f;
    [SerializeField] private bool use2DWorldPoint = true;
    [SerializeField] private float worldZ = 0f;

    [Header("自动占位")]
    [SerializeField] private bool autoCreatePlaceholder = true;
    [SerializeField] private Vector2 placeholderSize = new Vector2(80f, 80f);
    [SerializeField] private Color placeholderColor = new Color(0.3f, 0.8f, 1f, 0.4f);

    private RectTransform _canvasRect;
    private bool _isPreviewing;
    private Collider2D[] _overlapResults;

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
        int finalCost = Mathf.Max(0, energyCost);
        if (EnergyPoolRuntime.Instance != null && !EnergyPoolRuntime.Instance.TryConsume(finalCost))
            return false;

        if (!TryGetMouseWorldPoint(out Vector3 center))
            return false;

        EnsureOverlapBuffer();

        float radius = Mathf.Max(0.1f, rewindRadius);
        float finalRewindSeconds = Mathf.Max(0.1f, rewindSeconds);
        float finalPlaybackDuration = Mathf.Max(0.05f, playbackDuration);
        int hitCount = Physics2D.OverlapCircleNonAlloc(center, radius, _overlapResults, enemyLayer);
        if (hitCount <= 0)
            return false;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D collider2D = _overlapResults[i];
            if (collider2D == null) continue;
            EnemyRewindRecorder recorder = collider2D.GetComponentInParent<EnemyRewindRecorder>();
            if (recorder == null) continue;
            recorder.StartRewindBySkill(finalRewindSeconds, finalPlaybackDuration);
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
