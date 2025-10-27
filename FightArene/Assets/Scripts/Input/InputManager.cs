using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Debug = Utilities.Debug;

public static class InputManager
{
    private static InputSystem_Actions inputAsset;
    public static InputActionMap playerMap = null;
    public static InputActionMap combatMap = null;
    public static PlayerActionContainer actions = null;
    
    private static bool isInitialized;
    
    public static void Init()
    {
        if (isInitialized) return;
        
        inputAsset = new InputSystem_Actions();
        playerMap = inputAsset.PlayerMovement;
        combatMap = inputAsset.Combat;
        
        actions = new PlayerActionContainer
        {
            jump = playerMap.FindAction("Jump"),
            move = playerMap.FindAction("Move"),
            sprint = playerMap.FindAction("Sprint"),
            look = playerMap.FindAction("Look"),
            armor = playerMap.FindAction("Armor"),
            magnet = playerMap.FindAction("Magnet"),
            
            // Combat Actions
            fire = combatMap.FindAction("Fire"),
            extraFire = combatMap.FindAction("ExtraFire")
            
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
        combatMap?.Disable();
        
        actions?.Disable();
        actions?.Dispose();
        
        if (inputAsset != null)
        {
            inputAsset.Disable();
            inputAsset.Dispose();
            inputAsset = null;
        }
        
        playerMap = null;
        combatMap = null;
        actions = null;
        isInitialized = false;
        
        Debug.Log("InputManager disposed");
    }

    
    public class PlayerActionContainer : IDisposable
    {
        public InputAction jump;
        public InputAction move;
        public InputAction sprint;
        public InputAction look;
        public InputAction armor;
        public InputAction magnet;
        
        // Combat Map
        public InputAction fire;
        public InputAction extraFire;
        
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
