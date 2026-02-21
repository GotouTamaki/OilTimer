using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways] // 再生していない間も座標と半径が変化するように
public class WaterRenderer : MonoBehaviour
{
    private static readonly int SpheresID = Shader.PropertyToID("_Spheres");
    private static readonly int SphereCountID = Shader.PropertyToID("_SphereCount");
    private static readonly int WaterBaseColorsID = Shader.PropertyToID("_BaseColors");
    private static readonly int WaterLimColorsID = Shader.PropertyToID("_LimColors");

    [SerializeField] private Material _material; // 水用のマテリアル
    [SerializeField] private ParticleSettings[] _particleSettings; // 複数のパーティクルシステムと色のペア
    // [SerializeField] private Color[] _waterBaseColors;
    // [SerializeField] private Color[] _waterLimColors;
    [SerializeField] private float _cullingDistance = 20f; // カメラからの距離
    [SerializeField] private float _minRadiusThreshold = 0.1f; // 最小半径
    [SerializeField] private float _radiusAdjustment = 1.0f;

    private const int MaxSphereCount = 128; // 球の最大個数(シェーダー側と合わせる)
    private readonly Vector4[] _spheres = new Vector4[MaxSphereCount];
    // private SphereCollider[] _colliders;
    private Vector4[] _particleBaseColors = new Vector4[MaxSphereCount];
    private Vector4[] _particleLimColors = new Vector4[MaxSphereCount];
    // private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[MaxSphereCount];
    private ParticleSystem.Particle[][] _particleArray;
    // private int _count;
    private int _totalCount;

    public int GetSphereCount() => _totalCount;

    private void Start()
    {
        // 先に色を設定
        // material.SetVectorArray(WaterBaseColorsID, _particleBaseColors);
        // material.SetVectorArray(WaterLimColorsID, _particleLimColors);

        _particleArray = new ParticleSystem.Particle[_particleSettings.Length][];

        if (_particleSettings != null)
        {
            for (int i = 0; i < _particleSettings.Length; i++)
            {
                if (_particleSettings[i].particleSystem != null)
                {
                    _particleArray[i] = new ParticleSystem.Particle[_particleSettings[i].particleSystem.main.maxParticles];
                    _particleSettings[i].particleSystem.GetParticles(_particleArray[i]);
                }
            }
        }

        UpdateWater();
    }

    private void Update()
    {
        UpdateWater();
    }

    /// <summary>
    /// 水の色を変更しシェーダー側へ渡す処理
    /// </summary>
    public void UpdateWater()
    {
        if (_material == null || _particleSettings == null || _particleSettings.Length == 0)
            return;

        int sphereIndex = 0;
        Vector3 cameraPos = Camera.main.transform.position;

        // 各パーティクルシステムから情報を取得
        for (int particleSettingsIndex = 0; particleSettingsIndex < _particleSettings.Length; particleSettingsIndex++)
        {
            var settings = _particleSettings[particleSettingsIndex];

            if (settings.particleSystem == null || settings.particleSystem.gameObject == null) continue;

            // _particleArrayが初期化されていない場合は初期化
            if (_particleArray == null || _particleArray.Length != _particleSettings.Length)
            {
                _particleArray = new ParticleSystem.Particle[_particleSettings.Length][];

                for (int i = 0; i < _particleSettings.Length; i++)
                {
                    if (_particleSettings[i].particleSystem != null)
                    {
                        _particleArray[i] = new ParticleSystem.Particle[_particleSettings[i].particleSystem.main.maxParticles];
                    }
                }
            }

            // パーティクル配列の確保
            // if (particleSettingsIndex >= _particleArray[particleSettingsIndex].Length)
            // {
            //     _particleSettings[particleSettingsIndex].particleSystem.GetParticles(_particleArray[particleSettingsIndex]);
            // }

            var particles = _particleArray[particleSettingsIndex];
            int count = settings.particleSystem.GetParticles(particles);

            // 球の最大数を超えないようにする
            for (int i = 0; i < count && sphereIndex < MaxSphereCount; i++)
            {
                var center = particles[i].position;
                var radius = particles[i].GetCurrentSize(settings.particleSystem);

                // カリング処理
                float distanceToCamera = Vector3.Distance(cameraPos, center);
                if (distanceToCamera > _cullingDistance) continue; // 遠すぎる
                if (radius < _minRadiusThreshold) continue; // 小さすぎる

                // 座標と半径を格納
                _spheres[sphereIndex] = new Vector4(center.x, center.y, center.z, radius * _radiusAdjustment);

                // 色を格納
                _particleBaseColors[sphereIndex] = settings.baseColor;
                _particleLimColors[sphereIndex] = settings.limColor;

                sphereIndex++;
            }
        }

        _totalCount = sphereIndex;

        // シェーダーに送信
        _material.SetInt(SphereCountID, _totalCount);
        _material.SetVectorArray(SpheresID, _spheres);
        _material.SetVectorArray(WaterBaseColorsID, _particleBaseColors);
        _material.SetVectorArray(WaterLimColorsID, _particleLimColors);
    }

    private void OnDestroy()
    {
        // パーティクル配列を明示的にクリア(GCの対象にする)
        _particleArray = null;
    }
}