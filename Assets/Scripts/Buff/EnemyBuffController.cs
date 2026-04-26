using System.Collections.Generic;
using UnityEngine;
using qwq;

[DisallowMultipleComponent]
public class EnemyBuffController : MonoBehaviour
{
    private sealed class ActiveBuffState
    {
        public string GroupKey;
        public BuffDataSO Data;
        public float RemainingDuration;
    }

    private readonly List<ActiveBuffState> activeBuffs = new List<ActiveBuffState>();
    private Enemy owner;

    private void Awake()
    {
        owner = GetComponent<Enemy>();
    }

    private void Update()
    {
        if (activeBuffs.Count == 0)
            return;

        float delta = Time.deltaTime;
        bool hasChanged = false;
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            //在内部判断当前buff是不是过时了
            ActiveBuffState state = activeBuffs[i];
            state.RemainingDuration -= delta;
            if (state.RemainingDuration > 0f) continue;

            activeBuffs.RemoveAt(i);
            hasChanged = true;
        }
        if (hasChanged) NotifyBuffChanged();
    }

    public void ApplyBuff(BuffDataSO buff)
    {
        if (buff == null)
            return;

        EnsureOwner();
        string groupKey = BuildBuffKey(buff);
        float duration = Mathf.Max(0.05f, buff.Duration);

        if (!buff.CanStack)
        {
            ActiveBuffState existing = FindFirstByGroupKey(groupKey);
            if (existing != null)
            {
                existing.Data = buff;
                existing.RemainingDuration = duration;
            }
            else
            {
                activeBuffs.Add(new ActiveBuffState
                {
                    GroupKey = groupKey,
                    Data = buff,
                    RemainingDuration = duration
                });
            }
        }
        else
        {
            activeBuffs.Add(new ActiveBuffState
            {
                GroupKey = groupKey,
                Data = buff,
                RemainingDuration = duration
            });
        }

        if (buff.HealOnApply > 0)
            owner?.Heal(buff.HealOnApply);

        NotifyBuffChanged();
    }

    public void ApplyBuffSet(BuffSetSO buffSet)
    {
        if (buffSet == null || buffSet.Buffs == null || buffSet.Buffs.Length == 0)
            return;

        for (int i = 0; i < buffSet.Buffs.Length; i++)
            ApplyBuff(buffSet.Buffs[i]);
    }

    public float GetFlatValue(BuffTargetStat targetStat)
    {
        return SumValue(targetStat, BuffModifyMode.Flat);
    }

    public float GetPercentValue(BuffTargetStat targetStat)
    {
        return SumValue(targetStat, BuffModifyMode.Percent);
    }

    private float SumValue(BuffTargetStat targetStat, BuffModifyMode modifyMode)
    {
        if (activeBuffs.Count == 0)
            return 0f;

        float total = 0f;
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            BuffDataSO data = activeBuffs[i].Data;
            if (data == null)
                continue;
            if (data.TargetStat != targetStat || data.ModifyMode != modifyMode)
                continue;

            total += data.Value;
        }
        return total;
    }

    private string BuildBuffKey(BuffDataSO buff)
    {
        if (!string.IsNullOrWhiteSpace(buff.BuffId))
            return buff.BuffId.Trim();
        return $"buff_{buff.GetInstanceID()}";
    }

    private void NotifyBuffChanged()
    {
        EnsureOwner();
        owner?.RefreshStatsByBuff();
    }

    private void EnsureOwner()
    {
        if (owner == null)
            owner = GetComponent<Enemy>();
    }

    private ActiveBuffState FindFirstByGroupKey(string groupKey)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            if (activeBuffs[i].GroupKey == groupKey)
                return activeBuffs[i];
        }
        return null;
    }
}
