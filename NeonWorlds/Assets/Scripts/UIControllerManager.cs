using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class UIControllerManager : MonoBehaviour
{
    public static UIControllerManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureEventSystem();
    }

    void Start()
    {
        EnsureEventSystem();
    }

    public static void EnsureEventSystem()
    {
        EventSystem es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
        }

        // Remove deprecated StandaloneInputModule if present
        StandaloneInputModule standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Object.DestroyImmediate(standalone);
        }

        // Ensure InputSystemUIInputModule is present with default actions
        InputSystemUIInputModule inputModule = es.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        if (inputModule.actionsAsset == null)
        {
            inputModule.AssignDefaultActions();
        }

        es.sendNavigationEvents = true;
    }

    void Update()
    {
        EnsureEventSystem();

        GameObject activeMenu = GetActiveMenuPanel();
        if (activeMenu == null) return;

        GameObject current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        bool hasValidSelection = current != null && current.activeInHierarchy && current.transform.IsChildOf(activeMenu.transform);

        bool navInput = HasNavigationInput();

        // If no element is selected or current selection is invalid, immediately focus the first interactable
        if (!hasValidSelection && (navInput || current == null))
        {
            FocusFirstInteractable(activeMenu);
        }

        HandleMenuShortcuts(activeMenu);
    }

    private bool HasNavigationInput()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.left.wasPressedThisFrame ||
                Gamepad.current.dpad.right.wasPressedThisFrame ||
                Gamepad.current.dpad.up.wasPressedThisFrame ||
                Gamepad.current.dpad.down.wasPressedThisFrame ||
                Gamepad.current.leftStick.left.wasPressedThisFrame ||
                Gamepad.current.leftStick.right.wasPressedThisFrame ||
                Gamepad.current.leftStick.up.wasPressedThisFrame ||
                Gamepad.current.leftStick.down.wasPressedThisFrame ||
                Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                Keyboard.current.upArrowKey.wasPressedThisFrame ||
                Keyboard.current.downArrowKey.wasPressedThisFrame ||
                Keyboard.current.wKey.wasPressedThisFrame ||
                Keyboard.current.aKey.wasPressedThisFrame ||
                Keyboard.current.sKey.wasPressedThisFrame ||
                Keyboard.current.dKey.wasPressedThisFrame ||
                Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    public static GameObject GetActiveMenuPanel()
    {
        GameObject canvasObj = GameObject.Find("CanvasHUD");
        if (canvasObj == null) canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null) return null;

        Transform canvas = canvasObj.transform;

        // 1. Hangar Panel
        Transform hangar = canvas.Find("HangarPanel");
        if (hangar != null && hangar.gameObject.activeInHierarchy) return hangar.gameObject;

        // 2. Synergy Codex
        Transform codex = canvas.Find("SynergyCodexPanel");
        if (codex != null && codex.gameObject.activeInHierarchy) return codex.gameObject;

        // 3. Level Up Panel
        Transform levelUp = canvas.Find("LevelUpPanel");
        if (levelUp != null && levelUp.gameObject.activeInHierarchy) return levelUp.gameObject;

        // 4. Pause Menu
        Transform pause = canvas.Find("PauseMenuPanel");
        if (pause != null && pause.gameObject.activeInHierarchy) return pause.gameObject;

        // 5. Game Over / Victory Panels
        Transform gameOver = canvas.Find("GameOverPanel");
        if (gameOver != null && gameOver.gameObject.activeInHierarchy) return gameOver.gameObject;

        Transform victory = canvas.Find("VictoryPanel");
        if (victory != null && victory.gameObject.activeInHierarchy) return victory.gameObject;

        return null;
    }

    public static void FocusFirstInteractable(GameObject panel)
    {
        if (panel == null || EventSystem.current == null) return;

        Button[] buttons = panel.GetComponentsInChildren<Button>(false);
        foreach (var btn in buttons)
        {
            if (btn.interactable && btn.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
                UISelectionFeedback fb = btn.GetComponent<UISelectionFeedback>();
                if (fb != null) fb.SetHighlight(true);
                return;
            }
        }
    }

    private void HandleMenuShortcuts(GameObject activeMenu)
    {
        bool cancelPressed = false;
        if (Gamepad.current != null && Gamepad.current.bButton.wasPressedThisFrame) cancelPressed = true;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) cancelPressed = true;

        if (cancelPressed)
        {
            string pName = activeMenu.name;
            if (pName == "HangarPanel" || pName == "SynergyCodexPanel")
            {
                Destroy(activeMenu);
                // If pause menu is open underneath, re-focus its buttons
                Transform pause = activeMenu.transform.parent.Find("PauseMenuPanel");
                if (pause != null && pause.gameObject.activeInHierarchy)
                {
                    FocusFirstInteractable(pause.gameObject);
                }
                return;
            }
            else if (pName == "PauseMenuPanel")
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TogglePauseMenu();
                }
                return;
            }
        }

        // Hangar Tab switching with Bumpers (LB / RB)
        if (activeMenu.name == "HangarPanel" && Gamepad.current != null)
        {
            bool lb = Gamepad.current.leftShoulder.wasPressedThisFrame;
            bool rb = Gamepad.current.rightShoulder.wasPressedThisFrame;
            if (lb || rb)
            {
                Transform tabs = activeMenu.transform.Find("Tabs");
                if (tabs != null)
                {
                    Button[] tabBtns = tabs.GetComponentsInChildren<Button>();
                    if (tabBtns.Length >= 2)
                    {
                        if (lb) tabBtns[0].onClick.Invoke();
                        if (rb) tabBtns[1].onClick.Invoke();
                    }
                }
            }
        }
    }
}
