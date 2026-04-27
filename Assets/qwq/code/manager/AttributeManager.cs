using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class AttributeManager : MonoBehaviour
{
    [SerializeField]private AttributeTable attribute;
    public AttributeTable _attribute => attribute;

    public static AttributeManager Instance;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

    }

    private void Start()
    {
        MoneyC.Instance.MoneyUI();
    }
    public bool SpendMoney(int fertilizer, int diamond)
    {
        if (attribute.fertilizer >= fertilizer && attribute.diamond >= diamond)
        {
            attribute.fertilizer -= fertilizer;
            attribute.diamond -= diamond;
            MoneyC.Instance.MoneyUI();
            return true;
        }
        return false;
    }

}

[Serializable]
public class AttributeTable
{
    [Header("»õ±Ò")]
    public int fertilizer = 500;
    public int diamond = 300;
}

