using System.Collections.Generic;
using UnityEngine;
using qwq;

/// <summary>
/// 按时间顺序记录敌人位置（有上限），回溯时沿历史向旧插值移动；结束后裁剪掉「被抹去的未来」快照并继续录制，
/// 从而可多次回溯到更早轨迹，直到历史不足。
/// </summary>
[DisallowMultipleComponent]
public class EnemyRewindRecorder : MonoBehaviour
{
    [Header("记录")]
    [SerializeField] private float recordInterval = 0.1f;
    [Tooltip("最多保留的快照条数。连续多次回溯（如每次约 5s、间隔 0.1s）需要约 50 点/次，请按需加大。")]
    [SerializeField] private int maxRecords = 120;

    [Header("倒放")]
    [SerializeField] private float rewindDuration = 0.5f;
    [SerializeField] private AnimationCurve rewindEase = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private readonly List<Vector3> _history = new List<Vector3>();
    private float _recordTimer;

    private bool _isRewinding;
    private int _currentLogicalStep;
    private int _targetLogicalStep;
    private int _stepsToRewind;
    private float _segmentElapsed;
    private float _segmentDuration;
    private Vector3 _segmentStart;
    private Vector3 _segmentEnd;

    private EnemyMove _enemyMove;
    private Enemy _enemy;

    public bool IsRewinding => _isRewinding;

    private void Awake()
    {
        _enemyMove = GetComponent<EnemyMove>();
        _enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        GameEvent.EnemyRewindRequested += HandleRewindRequested;
    }

    private void OnDisable()
    {
        GameEvent.EnemyRewindRequested -= HandleRewindRequested;
    }

    private void Update()
    {
        if (_isRewinding)
        {
            TickRewind(Time.deltaTime);
            return;
        }

        TickRecord(Time.deltaTime);
    }

    public void TriggerRewind()
    {
        StartRewind(rewindDuration);
    }

    public void StartRewind(float duration)
    {
        float rewindSeconds = Mathf.Max(recordInterval, (_history.Count - 1) * recordInterval);
        StartRewindBySeconds(rewindSeconds, duration);
    }

    public void StartRewindBySkill(float rewindSeconds, float playbackDuration)
    {
        StartRewindBySeconds(rewindSeconds, playbackDuration);
    }

    public void ClearHistory()
    {
        _history.Clear();
        _recordTimer = 0f;
    }

    private void HandleRewindRequested(float rewindSeconds, float playbackDuration)
    {
        StartRewindBySeconds(rewindSeconds, Mathf.Max(0.05f, playbackDuration));
    }

    private void TickRecord(float deltaTime)
    {
        int cap = Mathf.Max(2, maxRecords);

        _recordTimer += deltaTime;
        if (_recordTimer < recordInterval)
            return;

        _recordTimer -= recordInterval;
        PushPosition(transform.position);

        while (_history.Count > cap)
            _history.RemoveAt(0);
    }

    private void TickRewind(float deltaTime)
    {
        _segmentElapsed += deltaTime;
        float t = Mathf.Clamp01(_segmentElapsed / _segmentDuration);
        float easedT = rewindEase != null ? rewindEase.Evaluate(t) : t;
        transform.position = Vector3.LerpUnclamped(_segmentStart, _segmentEnd, easedT);

        if (t < 1f)
            return;

        _currentLogicalStep--;
        if (_currentLogicalStep <= _targetLogicalStep)
        {
            transform.position = GetPositionByLogicalIndex(_targetLogicalStep);
            FinishRewind();
            return;
        }

        _segmentStart = _segmentEnd;
        _segmentEnd = GetPositionByLogicalIndex(_currentLogicalStep - 1);
        _segmentElapsed = 0f;
    }

    private void StartRewindBySeconds(float rewindSeconds, float playbackDuration)
    {
        if (_isRewinding)
            return;

        if (_enemy != null && _enemy.HasRewindResistance)
            return;

        if (_history.Count <= 1)
            return;

        float clampedRewindSeconds = Mathf.Max(recordInterval, rewindSeconds);
        int count = _history.Count;
        _stepsToRewind = Mathf.Clamp(Mathf.RoundToInt(clampedRewindSeconds / recordInterval), 1, count - 1);
        _targetLogicalStep = (count - 1) - _stepsToRewind;

        _isRewinding = true;
        _enemyMove?.SetMovementPaused(true);
        GameEvent.TriggerEnemyRewindStarted(transform);

        _segmentDuration = Mathf.Max(0.01f, playbackDuration / _stepsToRewind);
        _currentLogicalStep = count - 1;
        _segmentElapsed = 0f;

        _segmentStart = transform.position;
        _segmentEnd = GetPositionByLogicalIndex(_currentLogicalStep - 1);
    }

    private void FinishRewind()
    {
        _isRewinding = false;
        _enemyMove?.RebindPathFromCurrentPosition();
        _enemyMove?.SetMovementPaused(false);

        // 回溯后若仍停在 Battle（OnUpdate 为空）会卡死移动；并释放对友军的近战占用，避免友军槽位被锁死。
        EnemyStateController stateCtrl = GetComponent<EnemyStateController>();
        if (stateCtrl != null)
            stateCtrl.SwitchToPathMove(false);

        int removeStart = _targetLogicalStep + 1;
        int removeCount = _history.Count - removeStart;
        if (removeCount > 0)
            _history.RemoveRange(removeStart, removeCount);

        _recordTimer = 0f;
        GameEvent.TriggerEnemyRewindEnded(transform);
    }

    private void PushPosition(Vector3 position)
    {
        _history.Add(position);
    }

    private Vector3 GetPositionByLogicalIndex(int logicalIndex)
    {
        if (_history.Count <= 0)
            return transform.position;

        int clampedIndex = Mathf.Clamp(logicalIndex, 0, _history.Count - 1);
        return _history[clampedIndex];
    }
}
