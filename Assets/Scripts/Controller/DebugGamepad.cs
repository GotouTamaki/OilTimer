using UnityEngine;
using UnityEngine.InputSystem;

public class DebugGamepad : MonoBehaviour
{
    void Update()
    {
        foreach (var device in InputSystem.devices)
        {
            Debug.Log(device.layout);
        }
    }
}
