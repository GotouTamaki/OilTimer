using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using Unity.Profiling;
using TMPro;

/// <summary>
/// Android実機/PCでのパフォーマンス計測用モニター
/// シーンのCanvasにアタッチして使用
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [Header("表示設定")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private TextMeshProUGUI _text;
    // [SerializeField] private Color _textColor = Color.white;
    // [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);

    [Header("計測対象")]
    [SerializeField] private WaterRenderer _waterRenderer;

    // FPS計測用
    private float _deltaTime = 0.0f;
    private float _fps = 0.0f;
    private float _updateInterval = 0.5f;
    private float _timeSinceUpdate = 0.0f;

    // GPU計測用
    private float _gpuTime = 0.0f;
    private float _cpuTime = 0.0f;
    
    // プラットフォーム別のProfilerRecorder
    private ProfilerRecorder _mainThreadRecorder;
    private ProfilerRecorder _renderThreadRecorder;
    private ProfilerRecorder _gpuRecorder; // PC用

    // メモリ計測用
    private long _totalMemory = 0;
    private long _usedMemory = 0;

    // 統計情報
    private float _minFps = float.MaxValue;
    private float _maxFps = 0f;
    private float _avgFps = 0f;
    private int _frameCount = 0;
    private float _fpsSum = 0f;

    private GUIStyle _labelStyle;
    private GUIStyle _boxStyle;
    private StringBuilder _sb = new StringBuilder();

    private void Awake()
    {
        // デバイスの最大リフレッシュレートを取得して設定
        int maxRefreshRate = Screen.currentResolution.refreshRate;
        Application.targetFrameRate = maxRefreshRate;

        // VSync無効化（targetFrameRateを優先）
        // QualitySettings.vSyncCount = 0;

        // Debug.Log($"Target FPS set to: {maxRefreshRate}");
    }

    private void Start()
    {
        // ProfilerRecorderの初期化
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        _mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
        
        // プラットフォーム別のGPU計測設定
        if (Application.isMobilePlatform)
        {
            // モバイル用：WaitForPresent
            _renderThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread", 15);
        }
        else
        {
            // PC用：GPU全体の処理時間
            _gpuRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Total", 15);
            
            // 代替案としてRenderThread全体を計測
            if (!_gpuRecorder.Valid)
            {
                _renderThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Render Thread", 15);
            }
        }
#endif
    }

    private void Update()
    {
        if (!_showDebugInfo) return;

        // FPS計測
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        _timeSinceUpdate += Time.unscaledDeltaTime;

        if (_timeSinceUpdate >= _updateInterval)
        {
            _fps = 1.0f / _deltaTime;
            _timeSinceUpdate = 0.0f;

            // 統計情報更新
            UpdateStatistics();

            // メモリ情報更新
            UpdateMemoryInfo();

            // GPU/CPU時間計測
            UpdateTimingInfo();

            UpdateText();
        }
    }

    private void UpdateStatistics()
    {
        if (_fps < _minFps) _minFps = _fps;
        if (_fps > _maxFps) _maxFps = _fps;

        _frameCount++;
        _fpsSum += _fps;
        _avgFps = _fpsSum / _frameCount;
    }

    private void UpdateMemoryInfo()
    {
        _totalMemory = Profiler.GetTotalReservedMemoryLong() / 1048576; // MB
        _usedMemory = Profiler.GetTotalAllocatedMemoryLong() / 1048576; // MB
    }

    private void UpdateTimingInfo()
    {
        // CPU時間(ミリ秒)
        _cpuTime = Time.deltaTime * 1000f;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // GPU時間(ミリ秒) - プラットフォーム別
        if (Application.isMobilePlatform)
        {
            // モバイル：WaitForPresent
            if (_renderThreadRecorder.Valid)
            {
                _gpuTime = (float)(GetRecorderAverage(_renderThreadRecorder) / 1000000.0);
            }
        }
        else
        {
            // PC：GPU Total または Render Thread
            if (_gpuRecorder.Valid)
            {
                _gpuTime = (float)(GetRecorderAverage(_gpuRecorder) / 1000000.0);
            }
            else if (_renderThreadRecorder.Valid)
            {
                _gpuTime = (float)(GetRecorderAverage(_renderThreadRecorder) / 1000000.0);
            }
        }
#endif
    }

    // ProfilerRecorderの平均値取得
    private static double GetRecorderAverage(ProfilerRecorder recorder)
    {
        if (!recorder.Valid || recorder.Count == 0)
            return 0;

        // 最新のサンプル値を使用（平均ではなく）
        return recorder.LastValue;
    }

    private void UpdateText()
    {
        if (!_showDebugInfo) return;

        // デバッグ情報構築
        _sb.Clear();
        _sb.AppendLine("=== Performance Monitor ===");
        _sb.AppendLine();

        // FPS情報
        _sb.AppendLine($"<b>[FPS]</b>");
        _sb.AppendLine($"Current: {_fps:F1} FPS");
        _sb.AppendLine($"Min: {_minFps:F1} FPS");
        _sb.AppendLine($"Max: {_maxFps:F1} FPS");
        _sb.AppendLine($"Avg: {_avgFps:F1} FPS");
        _sb.AppendLine($"Frame Time: {_deltaTime * 1000f:F2} ms");
        _sb.AppendLine();

        // GPU/CPU情報
        _sb.AppendLine($"<b>[Processing Time]</b>");
        _sb.AppendLine($"CPU Time: {_cpuTime:F2} ms");

        if (_gpuTime > 0)
        {
            string gpuLabel = Application.isMobilePlatform ? "GPU Wait Time" : "GPU Time";
            _sb.AppendLine($"{gpuLabel}: {_gpuTime:F2} ms");
        }
        else
        {
            _sb.AppendLine($"GPU Time: Not Available");
        }

        _sb.AppendLine();

        // メモリ情報
        _sb.AppendLine($"<b>[Memory]</b>");
        _sb.AppendLine($"Used: {_usedMemory} MB");
        _sb.AppendLine($"Reserved: {_totalMemory} MB");
        _sb.AppendLine();

        // シェーダー関連情報
        if (_waterRenderer != null)
        {
            _sb.AppendLine($"<b>[Shader Info]</b>");
            _sb.AppendLine($"WaterRenderer: Active");
            _sb.AppendLine($"Particle Count: {_waterRenderer.GetSphereCount()}");
        }

        // デバイス情報
        _sb.AppendLine();
        _sb.AppendLine($"<b>[Device]</b>");
        _sb.AppendLine($"Platform: {Application.platform}");
        _sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");
        _sb.AppendLine($"Resolution: {Screen.width}x{Screen.height}");
        _sb.AppendLine($"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");

        _text.text = _sb.ToString();
    }

    // 統計リセット用（外部から呼び出し可能）
    public void ResetStatistics()
    {
        _minFps = float.MaxValue;
        _maxFps = 0f;
        _avgFps = 0f;
        _frameCount = 0;
        _fpsSum = 0f;
    }

    // 表示切り替え（画面タップで切り替え可能にする場合）
    public void ToggleDisplay()
    {
        _showDebugInfo = !_showDebugInfo;
    }

    private void OnDestroy()
    {
        // ProfilerRecorderの解放
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        _mainThreadRecorder.Dispose();
        _renderThreadRecorder.Dispose();
        _gpuRecorder.Dispose();
#endif
    }
}
