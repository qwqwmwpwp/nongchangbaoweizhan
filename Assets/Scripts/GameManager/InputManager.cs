using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public PlayerAction input;
    public float moveInput;
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        input = new PlayerAction();
        Instance = this;
        input.Action.CameraControl.performed += ctx => moveInput = ctx.ReadValue<float>();
        input.Action.CameraControl.canceled += ctx => moveInput = 0;
    }
    public void OnEnable()
    {
        input?.Enable();
    }

    public void OnDisable()
    {
        input?.Disable();
    }
}
