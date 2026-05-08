using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Tool UI Data", menuName = "Date/Tool UI Data")]
public class ToolUIData : ScriptableObject
{
    [Header("声音设置数据")]
    [Range(0f, 1f)]
    [FormerlySerializedAs("MasterVolumeDate")]
    public float masterVolumeDate = 1f;

    [Range(0f, 1f)]
    [FormerlySerializedAs("MusicVolumeDate")]
    public float musicVolumeDate = 1f;

    [Range(0f, 1f)]
    [FormerlySerializedAs("SFXVolume")]
    public float SFXVolumeDate = 1f;

    public bool masterMuted;
    public bool sfxMuted;
}
