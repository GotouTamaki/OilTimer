using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Switch;

/// <summary>
/// ProControllerGyroReaderの角速度を積分してGameObjectの回転に反映する
/// </summary>
public class GyroRotationApplier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProControllerGyroReader gyroReader;

    [Header("Settings")]
    [SerializeField] private float sensitivity = 1.0f;

    [Tooltip("最大角速度 (deg/s) これ以上は回転しない")]
    [SerializeField] private float maxAngularVelocity = 180f;

    [Tooltip("ドリフト補正の強さ (0=補正なし, 1=強い)")]
    [SerializeField, Range(0f, 1f)] private float driftCorrection = 0.02f;

    [Tooltip("リセット時に元の向きに戻る速さ")]
    [SerializeField] private float resetSpeed = 5.0f;

    [Tooltip("回転させる軸")]
    [SerializeField] private bool applyX = true;
    [SerializeField] private bool applyY = true;
    [SerializeField] private bool applyZ = true;

    private Rigidbody _rb;
    private Quaternion _rotation;

    // ゲーム開始時の初期角度
    private Quaternion _initialRotation;

    // リセット中かどうか
    private bool _isResetting = false;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // 初期角度を保存
        _initialRotation = transform.rotation;
    }

    private void OnEnable()
    {
        // 現在のGameObjectの向きから開始
        _rotation = transform.rotation;
    }

    private void Update()
    {
        // RキーまたはコントローラーのYボタンでリセット開始
        bool rKeyPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool yButtonPressed = SwitchProControllerNewHID.current != null && SwitchProControllerNewHID.current.buttonNorth.wasPressedThisFrame;

        if (rKeyPressed || yButtonPressed)
            ResetRotation();
    }

    private void FixedUpdate()
    {
        if (gyroReader == null || !gyroReader.IsConnected) return;

        // リセット中は初期角度へ滑らかに戻す
        if (_isResetting)
        {
            _rotation = Quaternion.Slerp(_rotation, _initialRotation, resetSpeed * Time.fixedDeltaTime);

            // 初期角度に十分近づいたらリセット完了
            if (Quaternion.Angle(_rotation, _initialRotation) < 0.5f)
            {
                _rotation = _initialRotation;
                _isResetting = false;
                Debug.Log("[GyroRotationApplier] リセット完了");
            }

            _rb.MoveRotation(_rotation);
            return;
        }

        // angularVelocity は deg/s
        Vector3 gyro = gyroReader.GyroValue * sensitivity;

        // 無効な軸をマスク
        if (!applyX) gyro.x = 0f;
        if (!applyY) gyro.y = 0f;
        if (!applyZ) gyro.z = 0f;

        // 最大角速度でClamp（貫通防止）
        if (gyro.magnitude > maxAngularVelocity)
            gyro = gyro.normalized * maxAngularVelocity;

        // 軸を入れ替え（コントローラーのX→UnityのZ）
        Vector3 deltaAngle = new Vector3(gyro.y, -gyro.x, gyro.z) * Time.fixedDeltaTime;

        // Quaternionとして積分
        Quaternion deltaRot = Quaternion.Euler(deltaAngle);
        _rotation = _rotation * deltaRot;

        // ドリフト補正：初期角度へ少しずつ引き戻す
        if (driftCorrection > 0f)
            _rotation = Quaternion.Slerp(_rotation, _initialRotation, driftCorrection * Time.fixedDeltaTime);

        // IsKinematic対応：MoveRotationで回転させる
        _rb.MoveRotation(_rotation);
    }

    /// <summary>
    /// 現在の向きをリセットする（初期角度へ滑らかに戻る）
    /// </summary>
    public void ResetRotation()
    {
        _isResetting = true;
        Debug.Log("[GyroRotationApplier] リセット開始");
    }
}
