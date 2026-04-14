using UnityEngine;

/// <summary>
/// 固定容量记录敌人历史位置，并在触发时平滑倒放到最早快照点。
/// </summary>
[DisallowMultipleComponent]
public class EnemyRewindRecorder : MonoBehaviour
{
    [Header("记录")]
    [SerializeField] private float recordInterval = 0.1f;
    [SerializeField] private int maxRecords = 50;

    [Header("倒放")]
    [SerializeField] private float rewindDuration = 0.5f;
    [SerializeField] private AnimationCurve rewindEase = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    //写入固定大小数组
    private Vector3[] _positions;
    //通过指针操作，head为下一次要写入的位置
    private int _head;
    private int _count;
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

    public bool IsRewinding => _isRewinding;

    private void Awake()
    {
        _enemyMove = GetComponent<EnemyMove>();
        EnsureBuffer();
    }

    private void OnEnable()
    {
        GameEvent.EnemyRewindRequested += HandleRewindRequested;
    }

    private void OnDisable()
    {
        GameEvent.EnemyRewindRequested -= HandleRewindRequested;
    }
    //在更新方法中回溯中就不记录位置了
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
        float rewindSeconds = Mathf.Max(recordInterval, (_count - 1) * recordInterval);
        StartRewindBySeconds(rewindSeconds, duration);
    }

    public void ClearHistory()
    {
        _head = 0;
        _count = 0;
        _recordTimer = 0f;
    }

    private void HandleRewindRequested(float rewindSeconds, float playbackDuration)
    {
        StartRewindBySeconds(rewindSeconds, Mathf.Max(0.05f, playbackDuration));
    }
    private void TickRecord(float deltaTime)
    {
        if (_positions == null || _positions.Length != maxRecords)
            EnsureBuffer();

        _recordTimer += deltaTime;
        if (_recordTimer < recordInterval)
            return;

        _recordTimer -= recordInterval;
        PushPosition(transform.position);
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

        if (_count <= 1)
            return;

        EnsureBuffer();

        float clampedRewindSeconds = Mathf.Max(recordInterval, rewindSeconds);
        _stepsToRewind = Mathf.Clamp(Mathf.RoundToInt(clampedRewindSeconds / recordInterval), 1, _count - 1);
        _targetLogicalStep = (_count - 1) - _stepsToRewind;

        _isRewinding = true;
        _enemyMove?.SetMovementPaused(true);
        GameEvent.TriggerEnemyRewindStarted(transform);

        _segmentDuration = Mathf.Max(0.01f, playbackDuration / _stepsToRewind);
        _currentLogicalStep = _count - 1;
        _segmentElapsed = 0f;

        _segmentStart = transform.position;
        _segmentEnd = GetPositionByLogicalIndex(_currentLogicalStep - 1);
    }

    private void FinishRewind()
    {
        _isRewinding = false;
        _enemyMove?.RebindPathFromCurrentPosition();
        _enemyMove?.SetMovementPaused(false);
        ClearHistory();
        GameEvent.TriggerEnemyRewindEnded(transform);
    }

    private void EnsureBuffer()
    {
        int capacity = Mathf.Max(2, maxRecords);
        if (_positions != null && _positions.Length == capacity)
            return;

        _positions = new Vector3[capacity];
        _head = 0;
        _count = 0;
        _recordTimer = 0f;
    }

    private void PushPosition(Vector3 position)
    {
        _positions[_head] = position;
        _head = (_head + 1) % _positions.Length;
        if (_count < _positions.Length)
            _count++;
    }

    private Vector3 GetPositionByLogicalIndex(int logicalIndex)
    {
        if (_count <= 0)
            return transform.position;

        int clampedIndex = Mathf.Clamp(logicalIndex, 0, _count - 1);
        int oldestPhysical = (_head - _count + _positions.Length) % _positions.Length;
        int physicalIndex = (oldestPhysical + clampedIndex) % _positions.Length;
        return _positions[physicalIndex];
    }
}
