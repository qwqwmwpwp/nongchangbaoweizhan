using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable 
{
    public GameObject obj { get; }
    void TakeDamage(int amount);
}

public static class DamageableTargetUtility
{
    public static bool TryGetGameObject(IDamageable target, out GameObject targetObject)
    {
        targetObject = null;
        if (target == null)
            return false;

        if (target is Object unityObject && unityObject == null)
            return false;

        try
        {
            targetObject = target.obj;
        }
        catch (MissingReferenceException)
        {
            return false;
        }

        return targetObject != null;
    }

    public static bool IsValid(IDamageable target)
    {
        return TryGetGameObject(target, out _);
    }
}

public interface IWeapon
{
    void Fire(IDamageable target);
}
