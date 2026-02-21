using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// タブ切り替え対応のメインメニュー管理
/// タブ: ステージ選択 / 遊び方 / クレジット
/// </summary>
public class StageSelectMenu : MonoBehaviour
{
    private enum Tab { Stage, HowToPlay, Credit }

    [Header("UI References")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private CanvasGroup startButtonCanvasGroup;
    [SerializeField] private GameObject startButtonObject;

    [Header("Tab Buttons")]
    [SerializeField] private CanvasGroup tabBarCanvasGroup; // タブボタン群をまとめたCanvasGroup
    [SerializeField] private Button tabStageButton;
    [SerializeField] private Button tabHowToPlayButton;
    [SerializeField] private Button tabCreditButton;

    [Header("Tab Pages")]
    [SerializeField] private CanvasGroup stagePageCanvasGroup;
    [SerializeField] private CanvasGroup howToPlayPageCanvasGroup;
    [SerializeField] private CanvasGroup creditPageCanvasGroup;

    [Header("Stage Page")]
    [SerializeField] private string[] stageSceneNames;
    [SerializeField] private Button stageFirstSelected;

    [Header("HowToPlay Page - Paging")]
    [SerializeField] private CanvasGroup[] howToPlayPages;
    [SerializeField] private Button howToPlayPrevButton;
    [SerializeField] private Button howToPlayNextButton;

    [Header("Credit Page - Paging")]
    [SerializeField] private CanvasGroup[] creditPages;
    [SerializeField] private Button creditPrevButton;
    [SerializeField] private Button creditNextButton;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private float slideOffset = 100f;

    private bool _isVisible = false;
    private Vector2 _panelOriginalPos;
    private Tab _currentTab = Tab.Stage;
    private int _howToPlayCurrentPage = 0;
    private int _creditCurrentPage = 0;

    public bool IsVisible => _isVisible;

    private void Awake()
    {
        _panelOriginalPos = menuPanel.anchoredPosition;

        // メニュー全体・タブボタン・全ページを非表示
        HidePageImmediate(menuCanvasGroup);
        HidePageImmediate(tabBarCanvasGroup);
        HidePageImmediate(stagePageCanvasGroup);
        HidePageImmediate(howToPlayPageCanvasGroup);
        HidePageImmediate(creditPageCanvasGroup);

        menuPanel.anchoredPosition = _panelOriginalPos + Vector2.down * slideOffset;

        // 遊び方・クレジットの先頭ページのみ表示
        InitPages(howToPlayPages);
        InitPages(creditPages);
    }

    private void Start()
    {
        // タブボタンのイベント登録
        tabStageButton.onClick.AddListener(() => SwitchTab(Tab.Stage));
        tabHowToPlayButton.onClick.AddListener(() => SwitchTab(Tab.HowToPlay));
        tabCreditButton.onClick.AddListener(() => SwitchTab(Tab.Credit));

        // ページ送りボタンのイベント登録
        howToPlayPrevButton.onClick.AddListener(() => ChangeHowToPlayPage(-1));
        howToPlayNextButton.onClick.AddListener(() => ChangeHowToPlayPage(1));
        creditPrevButton.onClick.AddListener(() => ChangeCreditPage(-1));
        creditNextButton.onClick.AddListener(() => ChangeCreditPage(1));
    }

    // ---- メニュー開閉 ----

    public void ShowMenu()
    {
        if (_isVisible) return;
        _isVisible = true;

        // 現在のタブをステージ選択にリセット
        _currentTab = Tab.Stage;
        HidePageImmediate(howToPlayPageCanvasGroup);
        HidePageImmediate(creditPageCanvasGroup);
        ShowPageImmediate(stagePageCanvasGroup);

        // はじめるボタンを非表示
        startButtonCanvasGroup.DOFade(0f, fadeOutDuration);
        startButtonCanvasGroup.interactable = false;
        startButtonCanvasGroup.blocksRaycasts = false;

        // タブボタン・メニューを表示
        ShowPageImmediate(tabBarCanvasGroup);
        menuCanvasGroup.interactable = true;
        menuCanvasGroup.blocksRaycasts = true;

        menuCanvasGroup.DOFade(1f, fadeInDuration);
        menuPanel.DOAnchorPos(_panelOriginalPos, slideDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (stageFirstSelected != null)
                    EventSystem.current.SetSelectedGameObject(stageFirstSelected.gameObject);
            });
    }

    public void HideMenu()
    {
        if (!_isVisible) return;
        _isVisible = false;

        // はじめるボタンを再表示
        startButtonCanvasGroup.DOFade(1f, fadeInDuration);
        startButtonCanvasGroup.interactable = true;
        startButtonCanvasGroup.blocksRaycasts = true;

        menuCanvasGroup.interactable = false;
        menuCanvasGroup.blocksRaycasts = false;

        menuCanvasGroup.DOFade(0f, fadeOutDuration);
        menuPanel.DOAnchorPos(_panelOriginalPos + Vector2.down * slideOffset, slideDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                // 閉じ終わったらタブ・全ページを非表示にリセット
                HidePageImmediate(tabBarCanvasGroup);
                HidePageImmediate(stagePageCanvasGroup);
                HidePageImmediate(howToPlayPageCanvasGroup);
                HidePageImmediate(creditPageCanvasGroup);

                // 遊び方・クレジットのページを先頭に戻す
                _howToPlayCurrentPage = 0;
                _creditCurrentPage = 0;
                InitPages(howToPlayPages);
                InitPages(creditPages);

                EventSystem.current.SetSelectedGameObject(startButtonObject);
            });
    }

    // ---- タブ切り替え ----

    private void SwitchTab(Tab tab)
    {
        _currentTab = tab;

        FadePage(stagePageCanvasGroup, tab == Tab.Stage);
        FadePage(howToPlayPageCanvasGroup, tab == Tab.HowToPlay);
        FadePage(creditPageCanvasGroup, tab == Tab.Credit);

        switch (tab)
        {
            case Tab.Stage:
                if (stageFirstSelected != null)
                    EventSystem.current.SetSelectedGameObject(stageFirstSelected.gameObject);
                break;
            case Tab.HowToPlay:
                UpdatePageButtons(howToPlayPrevButton, howToPlayNextButton, _howToPlayCurrentPage, howToPlayPages.Length);
                // 先頭ページはPrevが非活性なのでNextを選択
                EventSystem.current.SetSelectedGameObject(howToPlayNextButton.gameObject);
                break;
            case Tab.Credit:
                UpdatePageButtons(creditPrevButton, creditNextButton, _creditCurrentPage, creditPages.Length);
                EventSystem.current.SetSelectedGameObject(creditNextButton.gameObject);
                break;
        }
    }

    // ---- ページ送り ----

    private void ChangeHowToPlayPage(int delta)
    {
        int next = Mathf.Clamp(_howToPlayCurrentPage + delta, 0, howToPlayPages.Length - 1);
        if (next == _howToPlayCurrentPage) return;

        FadePage(howToPlayPages[_howToPlayCurrentPage], false);
        _howToPlayCurrentPage = next;
        FadePage(howToPlayPages[_howToPlayCurrentPage], true);

        UpdatePageButtons(howToPlayPrevButton, howToPlayNextButton, _howToPlayCurrentPage, howToPlayPages.Length);
    }

    private void ChangeCreditPage(int delta)
    {
        int next = Mathf.Clamp(_creditCurrentPage + delta, 0, creditPages.Length - 1);
        if (next == _creditCurrentPage) return;

        FadePage(creditPages[_creditCurrentPage], false);
        _creditCurrentPage = next;
        FadePage(creditPages[_creditCurrentPage], true);

        UpdatePageButtons(creditPrevButton, creditNextButton, _creditCurrentPage, creditPages.Length);
    }

    private void UpdatePageButtons(Button prevBtn, Button nextBtn, int currentPage, int totalPages)
    {
        bool prevActive = currentPage > 0;
        bool nextActive = currentPage < totalPages - 1;

        var selected = EventSystem.current.currentSelectedGameObject;

        // 非活性にする前に選択を移す
        if (selected == nextBtn.gameObject && !nextActive)
        {
            if (prevActive)
                EventSystem.current.SetSelectedGameObject(prevBtn.gameObject);
        }
        else if (selected == prevBtn.gameObject && !prevActive)
        {
            if (nextActive)
                EventSystem.current.SetSelectedGameObject(nextBtn.gameObject);
        }

        // 選択を移した後に非活性化
        prevBtn.interactable = prevActive;
        nextBtn.interactable = nextActive;
    }
    // ---- ステージ遷移 ----

    public void LoadStage(int index)
    {
        if (index < 0 || index >= stageSceneNames.Length) return;

        StageDataHolder.CurrentStageIndex = index;
        StageDataHolder.StageSceneNames = stageSceneNames;

        // SceneTransitionでフェード遷移
        SceneTransition.Instance.LoadScene(stageSceneNames[index]);
    }

    // ---- ユーティリティ ----

    private void FadePage(CanvasGroup cg, bool show)
    {
        if (cg == null) return;
        cg.DOFade(show ? 1f : 0f, fadeInDuration);
        cg.interactable = show;
        cg.blocksRaycasts = show;
    }

    private void ShowPageImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    private void HidePageImmediate(CanvasGroup cg)
    {
        if (cg == null) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    private void InitPages(CanvasGroup[] pages)
    {
        if (pages == null) return;
        for (int i = 0; i < pages.Length; i++)
        {
            if (i == 0) ShowPageImmediate(pages[i]);
            else HidePageImmediate(pages[i]);
        }
    }
}
