using UnityEngine;
using UnityEngine.InputSystem;

public class GyroDebugTrigger : MonoBehaviour
{
    private ProControllerGyroReader _reader;

    private void Awake()
    {
        _reader = GetComponent<ProControllerGyroReader>();
    }

    private void Update()
    {
        // スペースキーでバイト列を出力
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("#Press Space");
            // _reader.DebugPrintBytes();
        }

        // ジャイロ値をリアルタイム表示
        Debug.Log($"Gyro: {_reader.GyroValue}");
    }
}
