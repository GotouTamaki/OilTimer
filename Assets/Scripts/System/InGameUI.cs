using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// インゲームのUI管理
/// タイマー / カウントダウン / ランプ(3つ) / クリア画面
/// </summary>
public class InGameUI : MonoBehaviour
{
    [Header("Countdown UI")]
    [SerializeField] private CanvasGroup countdownCanvasGroup;
    [SerializeField] private Text countdownText;

    [Header("Timer UI")]
    [SerializeField] private Text timerText;

    [Header("Lamp UI (3つ)")]
    [SerializeField] private List<Image> lamps;
    [SerializeField] private Color lampOffColor = Color.gray;
    [SerializeField] private Color lampOnColor = Color.green;

    [Header("Clear Screen")]
    [SerializeField] private CanvasGroup clearScreenCanvasGroup;
    [SerializeField] private Text clearTimeText;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button titleButton;

    [Header("Stage Settings")]
    [SerializeField] private string[] stageSceneNames;
    [SerializeField] private string titleSceneName;

    [Header("Clear Animation")]
    [SerializeField] private float clearFadeInDuration = 0.5f;
    [SerializeField] private float clearSlideOffset = 80f;
    [SerializeField] private RectTransform clearPanel;

    private int _currentStageIndex = 0;
    private Vector2 _clearPanelOriginalPos;

    public bool IsClearScreenVisible => clearScreenCanvasGroup != null
                                     && clearScreenCanvasGroup.alpha > 0f
                                     && clearScreenCanvasGroup.interactable;

    private void Awake()
    {
        _currentStageIndex = StageDataHolder.CurrentStageIndex;

        if (StageDataHolder.StageSceneNames.Length > 0)
            stageSceneNames = StageDataHolder.StageSceneNames;

        HideImmediate(clearScreenCanvasGroup);
        ShowImmediate(countdownCanvasGroup);

        if (timerText != null) timerText.text = "00:00.00";

        foreach (var lamp in lamps)
            lamp.color = lampOffColor;

        if (clearPanel != null)
            _clearPanelOriginalPos = clearPanel.anchoredPosition;

        nextStageButton.onClick.AddListener(LoadNextStage);
        retryButton.onClick.AddListener(Retry);
        titleButton.onClick.AddListener(LoadTitle);
    }

    // ---- カウントダウン ----

    /// <summary>
    /// InGameManager の OnCountdownTick から呼ぶ (3, 2, 1, 0=GO)
    /// </summary>
    public void OnCountdownTick(int count)
    {
        if (countdownText == null) return;

        countdownText.transform.DOKill();
        countdownText.transform.localScale = Vector3.one * 1.5f;
        countdownText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

        countdownText.text = count > 0 ? count.ToString() : "GO!";

        if (count == 0)
        {
            countdownCanvasGroup.DOFade(0f, 0.5f)
                .SetDelay(0.4f)
                .OnComplete(() => HideImmediate(countdownCanvasGroup));
        }
    }

    // ---- タイマー ----

    /// <summary>
    /// InGameManager の OnTimerUpdated から呼ぶ
    /// </summary>
    public void OnTimerUpdated(float time)
    {
        if (timerText != null)
            timerText.text = InGameManager.FormatTime(time);
    }

    // ---- ランプ ----

    /// <summary>
    /// GameClearSystem の OnTimerUpdated から呼ぶ (0~1)
    /// </summary>
    public void OnClearProgressUpdated(float progress)
    {
        int litCount = Mathf.FloorToInt(progress * lamps.Count);
        for (int i = 0; i < lamps.Count; i++)
        {
            Color target = i < litCount ? lampOnColor : lampOffColor;
            lamps[i].DOColor(target, 0.1f);
        }
    }

    /// <summary>
    /// GameClearSystem の OnBallLeftZone から呼ぶ
    /// </summary>
    public void OnBallLeftZone()
    {
        foreach (var lamp in lamps)
            lamp.DOColor(lampOffColor, 0.2f);
    }

    // ---- クリア画面 ----

    /// <summary>
    /// InGameManager の OnGameClearWithTime から呼ぶ
    /// </summary>
    public void ShowClearScreen(float clearTime)
    {
        if (clearTimeText != null)
            clearTimeText.text = InGameManager.FormatTime(clearTime);

        foreach (var lamp in lamps)
            lamp.DOColor(lampOnColor, 0.1f);

        if (clearPanel != null)
            clearPanel.anchoredPosition = _clearPanelOriginalPos + Vector2.down * clearSlideOffset;

        clearScreenCanvasGroup.alpha = 0f;
        clearScreenCanvasGroup.interactable = true;
        clearScreenCanvasGroup.blocksRaycasts = true;

        clearScreenCanvasGroup.DOFade(1f, clearFadeInDuration);
        clearPanel?.DOAnchorPos(_clearPanelOriginalPos, clearFadeInDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                EventSystem.current?.SetSelectedGameObject(nextStageButton.gameObject);
            });
    }

    // ---- ボタン処理 ----

    private void LoadNextStage()
    {
        int nextIndex = _currentStageIndex + 1;
        if (nextIndex < stageSceneNames.Length)
        {
            StageDataHolder.CurrentStageIndex = nextIndex;
            StageDataHolder.StageSceneNames = stageSceneNames;
            SceneTransition.Instance.LoadScene(stageSceneNames[nextIndex]);
        }
        else
            SceneTransition.Instance.LoadScene(titleSceneName);
    }

    private void Retry()
    {
        SceneTransition.Instance.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void LoadTitle()
    {
        SceneTransition.Instance.LoadScene(titleSceneName);
    }

    // ---- ユーティリティ ----

    private void ShowImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void HideImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }
}
