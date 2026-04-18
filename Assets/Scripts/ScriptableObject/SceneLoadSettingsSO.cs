using UnityEngine;

[CreateAssetMenu(fileName = "NewSceneLoadSettings", menuName = "SO文件/场景加载参数")]
public class SceneLoadSettingsSO : ScriptableObject
{
    [field: Header("渐隐渐显")]
    [field: SerializeField] public float FadeOutDuration { get; private set; }
    [field: SerializeField] public float FadeInDuration { get; private set; }
}
