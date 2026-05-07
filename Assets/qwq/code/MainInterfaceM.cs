using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainInterfaceM : MonoBehaviour
{
    [SerializeField] Button openSetUp;
    [SerializeField] Button closeSetUp;
    [SerializeField] GameObject setUpUI;

    private void OnEnable()
    {
        openSetUp.onClick.AddListener(() => SetUp(true));
        closeSetUp.onClick.AddListener(() => SetUp(false));
    }

    private void OnDisable()
    {
        
    }
    public void   SetUp(bool isActive)
    {
        setUpUI.SetActive(isActive);
    }
}
