using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

/// <summary>
/// インゲームのタイマー・カウントダウン・クリア管理
/// GameClearSystemのOnGameClearイベントと接続する
/// </summary>
public class InGameManager : MonoBehaviour
{
    [Header("Countdown Settings")]
    [SerializeField] private float countdownDuration = 3f;

    [Header("References")]
    [SerializeField] private GyroRotationApplier gyroRotationApplier;

    [Header("Events")]
    [Tooltip("カウントダウン中の値を渡す (3, 2, 1, 0=GO)")]
    [SerializeField] private UnityEvent<int> onCountdownTick;
    [Tooltip("ゲーム開始")]
    [SerializeField] private UnityEvent onGameStart;
    [Tooltip("タイマー更新 (秒数を渡す)")]
    [SerializeField] private UnityEvent<float> onTimerUpdated;
    [Tooltip("ゲームクリア (クリアタイムを渡す)")]
    [SerializeField] private UnityEvent<float> onGameClearWithTime;

    private float _timer = 0f;
    private bool _isRunning = false;
    private bool _isCleared = false;

    public float CurrentTime => _timer;

    public bool IsCleared => _isCleared;

    private void Start()
    {
        // ゲーム開始時はジャイロ操作を無効化
        if (gyroRotationApplier != null)
            gyroRotationApplier.enabled = false;

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        int count = (int)countdownDuration;
        while (count > 0)
        {
            onCountdownTick.Invoke(count);
            yield return new WaitForSeconds(1f);
            count--;
        }

        // GO!
        onCountdownTick.Invoke(0);
        yield return new WaitForSeconds(0.5f);

        // ゲームスタート
        if (gyroRotationApplier != null)
            gyroRotationApplier.enabled = true;

        _isRunning = true;
        onGameStart.Invoke();
    }

    private void Update()
    {
        if (!_isRunning || _isCleared) return;

        _timer += Time.deltaTime;
        onTimerUpdated.Invoke(_timer);
        // Debug.Log($"[InGameManager] Timer: {_timer:F2} isRunning={_isRunning}");
    }

    /// <summary>
    /// GameClearSystem の OnGameClear から呼ぶ
    /// </summary>
    public void OnGameClear()
    {
        if (_isCleared) return;
        _isCleared = true;
        _isRunning = false;

        // ジャイロ操作を無効化
        if (gyroRotationApplier != null)
            gyroRotationApplier.enabled = false;

        onGameClearWithTime.Invoke(_timer);
    }

    /// <summary>
    /// タイムを "00:00.00" 形式にフォーマット
    /// </summary>
    public static string FormatTime(float time)
    {
        int minutes = (int)(time / 60f);
        int seconds = (int)(time % 60f);
        int millis = (int)((time % 1f) * 100f);
        return $"{minutes:D2}:{seconds:D2}.{millis:D2}";
    }
}
