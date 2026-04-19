using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoneyC : MonoBehaviour
{
    [SerializeField] MoneyM moneyM;
    public static MoneyC Instance;

    private void Awake()
    {
        if (Instance)
            Destroy(this);
        else
            Instance = this;
    }

public void MoneyUI()
    {
        AttributeTable attribute= AttributeManager.Instance._attribute;
        moneyM.UiUpdate(attribute.fertilizer, attribute.diamond);
    }
}
