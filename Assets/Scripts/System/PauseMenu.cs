using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Switch;
using DG.Tweening;

/// <summary>
/// インゲームのポーズメニュー
/// WestButton(Xボタン)で開閉、リトライ・タイトルへ戻る
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private RectTransform pausePanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button resumeButton;

    [Header("References")]
    [SerializeField] private InGameManager inGameManager;
    [SerializeField] private GyroRotationApplier gyroRotationApplier;

    [Header("Stage Settings")]
    [SerializeField] private string titleSceneName;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration  = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float slideOffset     = 80f;

    private bool _isPaused  = false;
    private Vector2 _panelOriginalPos;
    private SwitchControllerHID _controller;

    public bool IsPaused => _isPaused;

    private void Awake()
    {
        _panelOriginalPos = pausePanel.anchoredPosition;

        // 初期非表示
        pauseCanvasGroup.alpha          = 0f;
        pauseCanvasGroup.interactable   = false;
        pauseCanvasGroup.blocksRaycasts = false;
        pausePanel.anchoredPosition     = _panelOriginalPos + Vector2.down * slideOffset;

        retryButton.onClick.AddListener(Retry);
        titleButton.onClick.AddListener(LoadTitle);
        resumeButton.onClick.AddListener(Resume);
    }

    private void OnEnable()
    {
        UnityEngine.InputSystem.InputSystem.onDeviceChange += OnDeviceChange;

        foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
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
        UnityEngine.InputSystem.InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.InputDeviceChange change)
    {
        if (device is not SwitchProControllerNewHID sc) return;

        if (change == UnityEngine.InputSystem.InputDeviceChange.Added || change == UnityEngine.InputSystem.InputDeviceChange.Reconnected)
            _controller = sc;
        else if (change == UnityEngine.InputSystem.InputDeviceChange.Removed || change == UnityEngine.InputSystem.InputDeviceChange.Disconnected)
            _controller = null;
    }

    private void Update()
    {
        if (_controller == null) return;

        // クリア済みの場合はポーズ不可
        if (inGameManager != null && inGameManager.IsCleared) return;

        // WestButton(Xボタン)でポーズ開閉
        if (_controller.buttonWest.wasPressedThisFrame)
        {
            if (_isPaused) Resume();
            else           Pause();
        }
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        // ゲームを一時停止
        Time.timeScale = 0f;

        // ジャイロ操作を無効化
        if (gyroRotationApplier != null)
            gyroRotationApplier.enabled = false;

        // パネルをスライドイン
        pausePanel.anchoredPosition     = _panelOriginalPos + Vector2.down * slideOffset;
        pauseCanvasGroup.interactable   = true;
        pauseCanvasGroup.blocksRaycasts = true;

        pauseCanvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
        pausePanel.DOAnchorPos(_panelOriginalPos, fadeInDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
            });
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;

        // ゲーム再開
        Time.timeScale = 1f;

        // ジャイロ操作を有効化
        if (gyroRotationApplier != null)
            gyroRotationApplier.enabled = true;

        pauseCanvasGroup.interactable   = false;
        pauseCanvasGroup.blocksRaycasts = false;

        pauseCanvasGroup.DOFade(0f, fadeOutDuration).SetUpdate(true);
        pausePanel.DOAnchorPos(_panelOriginalPos + Vector2.down * slideOffset, fadeOutDuration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true);
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneTransition.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadTitle()
    {
        Time.timeScale = 1f;
        SceneTransition.Instance.LoadScene(titleSceneName);
    }
}
