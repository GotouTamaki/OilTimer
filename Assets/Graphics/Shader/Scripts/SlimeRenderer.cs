using UnityEngine;

[ExecuteAlways] // 再生していない間も座標と半径が変化するように
public class SlimeRenderer : MonoBehaviour
{
    private static readonly int SpheresID = Shader.PropertyToID("_Spheres");
    private static readonly int SphereCountID = Shader.PropertyToID("_SphereCount");
    private static readonly int WaterBaseColorsID = Shader.PropertyToID("_BaseColors");
    private static readonly int WaterLimColorsID = Shader.PropertyToID("_LimColors");

    [SerializeField] private Material material; // スライム用のマテリアル
    [SerializeField] private SphereCollider[] _colliders;
    [SerializeField] private Color _limColor = new Color(1, 1, 1, 1);

    private const int MaxSphereCount = 256; // 球の最大個数（シェーダー側と合わせる）
    private readonly Vector4[] _spheres = new Vector4[MaxSphereCount];
    private Vector4[] _baseColors = new Vector4[MaxSphereCount];
    private Vector4[] _limColors = new Vector4[MaxSphereCount];

    private void Start()
    {
        // 子のSphereColliderをすべて取得
        //_colliders = GetComponentsInChildren<SphereCollider>();

        // シェーダー側の _SphereCount を更新
        material.SetInt(SphereCountID, _colliders.Length);

        // ランダムな色を配列に格納
        for (var i = 0; i < _baseColors.Length; i++)
        {
            _baseColors[i] = (Vector4)Random.ColorHSV(0, 1, 1, 1, 1, 1);
            _limColors[i] = _limColor;
        }

        // シェーダー側の _Colors を更新
        material.SetVectorArray(WaterBaseColorsID, _baseColors);
        material.SetVectorArray(WaterLimColorsID, _limColors);
    }

    private void Update()
    {
        // 子のSphereColliderの分だけ、_spheres に中心座標と半径を入れていく
        for (var i = 0; i < _colliders.Length; i++)
        {
            var col = _colliders[i];
            var t = col.transform;
            var center = t.position;
            var radius = t.lossyScale.x * col.radius;
            // 中心座標と半径を格納
            _spheres[i] = new Vector4(center.x, center.y, center.z, radius);
        }

        // シェーダー側の _Spheres を更新
        material.SetVectorArray(SpheresID, _spheres);
    }
}
