using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Switch;

public class ProControllerUINavigation : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private float repeatDelay = 0.5f;
    [SerializeField] private float repeatInterval = 0.1f;
    [SerializeField] private float stickSensitivity = 0.5f;

    [Header("Stage Select Menu")]
    [SerializeField] private StageSelectMenu stageSelectMenu;

    [Header("In Game")]
    [SerializeField] private InGameUI inGameUI;

    private SwitchControllerHID _controller;
    private MoveDirection _lastMoveDir = MoveDirection.None;
    private float _repeatTimer;

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;

        foreach (var device in InputSystem.devices)
        {
            if (device is SwitchProControllerNewHID sc)
            {
                _controller = sc;
                break;
            }
        }
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is not SwitchProControllerNewHID sc) return;

        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            _controller = sc;
        else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            _controller = null;
    }

    private void Update()
    {
        if (_controller == null || EventSystem.current == null) return;

        // クリア画面表示中はメニュー状態と同様に操作を有効にする
        bool menuOrClearVisible = (stageSelectMenu != null && stageSelectMenu.IsVisible)
                               || (inGameUI != null && inGameUI.IsClearScreenVisible);

        if (menuOrClearVisible)
        {
            if (_controller.buttonSouth.wasPressedThisFrame)
            {
                if (stageSelectMenu != null && stageSelectMenu.IsVisible)
                    stageSelectMenu.HideMenu();
                return;
            }

            if (_controller.buttonEast.wasPressedThisFrame)
                ExecuteSubmit();

            HandleNavigation();
        }
        else
        {
            if (_controller.buttonEast.wasPressedThisFrame)
                ExecuteSubmit();
        }
    }

    private void HandleNavigation()
    {
        // 十字キー or 左スティック
        Vector2 direction = _controller.dpad.ReadValue();
        if (direction == Vector2.zero)
        {
            Vector2 stick = _controller.leftStick.ReadValue();
            if (stick.magnitude > 0.5f)
                direction = stick * stickSensitivity;
        }

        // デッドゾーン
        if (direction.magnitude < 0.5f)
        {
            _lastMoveDir = MoveDirection.None;
            _repeatTimer = 0f;
            return;
        }

        // 4方向に正規化して比較
        MoveDirection moveDir;
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            moveDir = direction.x > 0 ? MoveDirection.Right : MoveDirection.Left;
        else
            moveDir = direction.y > 0 ? MoveDirection.Up : MoveDirection.Down;

        if (moveDir != _lastMoveDir)
        {
            _lastMoveDir = moveDir;
            _repeatTimer = repeatDelay;
            ExecuteMove(direction, moveDir);
        }
        else
        {
            _repeatTimer -= Time.deltaTime;
            if (_repeatTimer <= 0f)
            {
                _repeatTimer = repeatInterval;
                ExecuteMove(direction, moveDir);
            }
        }
    }

    private void ExecuteMove(Vector2 direction, MoveDirection moveDir)
    {
        var eventData = new AxisEventData(EventSystem.current);
        eventData.moveVector = direction;
        eventData.moveDir = moveDir;

        ExecuteEvents.Execute(
            EventSystem.current.currentSelectedGameObject,
            eventData,
            ExecuteEvents.moveHandler
        );
    }

    private void ExecuteSubmit()
    {
        var eventData = new BaseEventData(EventSystem.current);
        ExecuteEvents.Execute(
            EventSystem.current.currentSelectedGameObject,
            eventData,
            ExecuteEvents.submitHandler
        );
    }
}
