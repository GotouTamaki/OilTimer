using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Switch;

public class ProControllerGyroReader : MonoBehaviour
{
    [SerializeField] private float sensitivity = 1.0f;

    public Vector3 GyroValue { get; private set; }
    public bool IsConnected => _controller != null;
    public System.Action<Vector3> OnGyroUpdated;

    private SwitchControllerHID _controller;

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;

        foreach (var device in InputSystem.devices)
        {
            if (device is SwitchProControllerNewHID sc)
            {
                InitController(sc);
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
            InitController(sc);
        else if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            _controller = null;
    }

    private void InitController(SwitchControllerHID sc)
    {
        _controller = sc;
        _controller.SetIMUEnabled(true);
        Debug.Log($"[ProController] 接続・IMU有効化: {sc.name}");
    }

    private void Update()
    {
        if (_controller == null) return;

        // キャリブレーション済みジャイロ (angular velocity, rad/s)
        Vector3 gyro = _controller.angularVelocity.value * sensitivity;
        GyroValue = gyro;
        OnGyroUpdated?.Invoke(gyro);
        // Debug.Log($"Gyro: {gyro}");
    }
}
