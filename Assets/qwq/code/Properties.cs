using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Properties : MonoBehaviour
{
    public Propertie hp;
    private void Start()
    {
        hp.current = hp.max;
    }
    //public( int harm, bool isDeath) Injure(int attack)
    //{
    //    hp.current -= attack;
    //    bool isDeath;
    //    if (hp.current <= 0)
    //    {
    //        isDeath = true;
    //        gameObject.SetActive(false);
    //    }
    //    else
    //    {
    //        isDeath = false;
    //    }
    //    return (attack,isDeath);
    //}
}
