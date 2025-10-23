using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public static class InputManager
{
    private static InputSystem_Actions inputAsset;
    public static InputActionMap playerMap = null;
    public static PlayerActionContainer actions = null;
    
    private static bool isInitialized;
    
    public static void Init()
    {
        if (isInitialized) return;
        
        inputAsset = new InputSystem_Actions();
        playerMap = inputAsset.Player;
        
        actions = new PlayerActionContainer
        {
            // Actions will be add here
        };
       
        
        playerMap.Enable();
        
        isInitialized = true;
        Debug.Log("InputManager initialized");
    }

    public static void Enable()
    {
        if (!isInitialized) Init();
        actions?.Enable();
    }
    
    public static void Disable()
    {
        actions?.Disable();
    }
    
    public static void Dispose()
    {
        playerMap?.Disable();
        
        actions?.Disable();
        actions?.Dispose();
        
        if (inputAsset != null)
        {
            inputAsset.Disable();
            inputAsset.Dispose();
            inputAsset = null;
        }
        
        playerMap = null;
        actions = null;
        isInitialized = false;
        
        Debug.Log("InputManager disposed");
    }

    
    public class PlayerActionContainer : IDisposable
    {
        public InputAction jump;
        public InputAction moveRight;
        public InputAction moveLeft;
        public InputAction push;
        public InputAction ghost;
        public InputAction pressButton;
        
        private HashSet<InputAction> _allActions = new HashSet<InputAction>();
        
        public void RegisterAction(InputAction action)
        {
            _allActions.Add(action);
        }
        
        public void Dispose()
        {
            foreach (var action in _allActions)
            {
                action?.Dispose();
            }
            _allActions.Clear();
        }
        
        public void Enable()
        {
            foreach (var action in _allActions)
            {
                action?.Enable();
            }
        }
        
        public void Disable()
        {
            foreach (var action in _allActions)
            {
                action?.Disable();
            }
        }
        
    }
}
