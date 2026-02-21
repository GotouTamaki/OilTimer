using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// すべての球が範囲内に入り、一定秒数経過するとゲームクリアになるシステム
/// ステージを動かして球を転がす構成を想定
/// </summary>
public class GameClearSystem : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("クリアに必要な範囲内滞在秒数")]
    [SerializeField] private float requiredTime = 3.0f;

    [Header("References")]
    [Tooltip("クリア判定の対象となる球のリスト（操作しない）")]
    [SerializeField] private List<GameObject> targetBalls;

    [Header("Events")]
    [Tooltip("残り時間の進捗 (0~1) を渡す。プログレスバーなどに接続する")]
    [SerializeField] private UnityEvent<float> onTimerUpdated;
    [Tooltip("全球が範囲内に入った瞬間")]
    [SerializeField] private UnityEvent onAllBallsInZone;
    [Tooltip("1つでも範囲外に出た時")]
    [SerializeField] private UnityEvent onBallLeftZone;
    [Tooltip("ゲームクリア")]
    [SerializeField] private UnityEvent onGameClear;

    private HashSet<GameObject> _ballsInZone = new HashSet<GameObject>();
    private float _timer     = 0f;
    private bool _isClearing = false;
    private bool _isCleared  = false;

    public float ClearProgress => _timer / requiredTime; // 0~1
    public bool IsClearing => _isClearing;
    public bool IsCleared  => _isCleared;

    private void Update()
    {
        if (_isCleared) return;

        bool allInZone = targetBalls.Count > 0 && _ballsInZone.Count >= targetBalls.Count;

        if (allInZone)
        {
            if (!_isClearing)
            {
                _isClearing = true;
                _timer      = 0f;
                onAllBallsInZone.Invoke();
                Debug.Log("[GameClear] 全球が範囲内に入りました。カウント開始");
            }

            _timer += Time.deltaTime;
            onTimerUpdated.Invoke(ClearProgress);

            if (_timer >= requiredTime)
            {
                _isCleared  = true;
                _isClearing = false;
                Debug.Log("[GameClear] ゲームクリア！");
                onGameClear.Invoke();
            }
        }
        else
        {
            if (_isClearing)
            {
                _isClearing = false;
                _timer      = 0f;
                onTimerUpdated.Invoke(0f);
                onBallLeftZone.Invoke();
                Debug.Log("[GameClear] 範囲外に出ました。カウントリセット");
            }
        }
    }

    public void OnBallEntered(GameObject ball)
    {
        if (!targetBalls.Contains(ball)) return;
        _ballsInZone.Add(ball);
        Debug.Log($"[GameClear] 範囲内: {ball.name} ({_ballsInZone.Count}/{targetBalls.Count})");
    }

    public void OnBallExited(GameObject ball)
    {
        if (!targetBalls.Contains(ball)) return;
        _ballsInZone.Remove(ball);
        Debug.Log($"[GameClear] 範囲外: {ball.name} ({_ballsInZone.Count}/{targetBalls.Count})");
    }
}
