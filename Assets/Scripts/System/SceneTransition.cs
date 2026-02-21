using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

/// <summary>
/// シーン遷移時のフェードイン・フェードアウトを管理する
/// DontDestroyOnLoadで全シーンで使い回す
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float fadeOutDuration = 0.5f; // 暗くなる時間
    [SerializeField] private float fadeInDuration  = 0.5f; // 明るくなる時間

    private Image _fadeImage;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        // シングルトン
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // フェード用Imageを取得
        _fadeImage    = GetComponentInChildren<Image>();
        _canvasGroup  = GetComponentInChildren<CanvasGroup>();

        // 起動時はフェードイン（黒→透明）
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOFade(0f, fadeInDuration)
            .OnComplete(() => _canvasGroup.blocksRaycasts = false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // シーン読み込み完了後にフェードイン
        _canvasGroup.alpha          = 1f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOFade(0f, fadeInDuration)
            .OnComplete(() => _canvasGroup.blocksRaycasts = false);
    }

    /// <summary>
    /// フェードアウトしてシーン遷移する
    /// </summary>
    public void LoadScene(string sceneName)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOFade(1f, fadeOutDuration)
            .OnComplete(() => SceneManager.LoadScene(sceneName));
    }

    /// <summary>
    /// フェードアウトしてシーン遷移する（インデックス指定）
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOFade(1f, fadeOutDuration)
            .OnComplete(() => SceneManager.LoadScene(sceneIndex));
    }
}
