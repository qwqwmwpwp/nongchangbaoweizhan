using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 单场景异步切换：DOTween 控制 CanvasGroup 渐隐 → 加载 → 渐显。
/// 挂在带 CanvasGroup 的全屏遮罩上，置于首场景并随 DontDestroyOnLoad 常驻。
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private SceneLoadSettingsSO loadSettings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        //初始化关闭所有动画，透明，且不可交互
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.DOKill();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.DOKill();
    }

    private float FadeOutSec => loadSettings.FadeInDuration;
    private float FadeInSec => loadSettings.FadeInDuration;

    /// <summary>按场景名替换当前场景（Build Settings 中需已加入）。</summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (fadeCanvasGroup == null)
        {
            Debug.LogError("SceneLoadManager: 未指定 Fade Canvas Group。", this);
            yield break;
        }

        fadeCanvasGroup.DOKill();
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.interactable = true;

        yield return fadeCanvasGroup.DOFade(1f, FadeOutSec).SetUpdate(true).WaitForCompletion(true);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (op == null)
        {
            Debug.LogError($"SceneLoadManager: 无法加载场景 \"{sceneName}\"，请检查 Build Settings。", this);
            fadeCanvasGroup.DOKill();
            yield return fadeCanvasGroup.DOFade(0f, FadeInSec).SetUpdate(true).WaitForCompletion(true);
            ApplyFadeInteractable(0f);
            yield break;
        }

        while (!op.isDone)
            yield return null;

        fadeCanvasGroup.DOKill();
        yield return fadeCanvasGroup.DOFade(0f, FadeInSec).SetUpdate(true).WaitForCompletion(true);
        ApplyFadeInteractable(0f);
    }

    private void ApplyFadeInteractable(float alpha)
    {
        if (fadeCanvasGroup == null) return;
        bool block = alpha > 0.01f;
        fadeCanvasGroup.blocksRaycasts = block;
        fadeCanvasGroup.interactable = block;
    }
}
