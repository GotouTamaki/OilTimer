Shader "Custom/MetaBallShader"
{
    Properties 
    {
        _RayLoopCount("LoopCount",  Range(0, 50)) = 40    // 10以上は非推奨  
        _RayDistance("RayDistance", int) = 100000
        _SphereDistanceThreshold("SphereDistanceThreshold", float) = 0.01
        _MetaballCorrectionConstant("MetaballCorrectionConstant",  Range(0, 10)) = 3
        _LimPower("LimPower", float) = 2
        [HDR] _HightLightColor("HightLightColor", Color) = (1.5, 1.5, 1.5, 1)
        _HightLightThreshold("HightLightThreshold", Range(0, 1.00)) = 0.99
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" // 透過できるようにする
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite On // 深度を書き込む
            Blend SrcAlpha OneMinusSrcAlpha // 透過できるようにする
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)

                int _RayLoopCount;
                float _RayDistance;
                float _SphereDistanceThreshold;
                float _MetaballCorrectionConstant;
                float _LimPower;
                half4 _HightLightColor;
                half _HightLightThreshold;

            CBUFFER_END

            #define MAX_SPHERE_COUNT 128 // 最大の球の個数
            float4 _Spheres[MAX_SPHERE_COUNT]; // 球の座標・半径を格納した配列
            half4 _BaseColors[MAX_SPHERE_COUNT]; // 球の中心の色を格納した配列
            half4 _LimColors[MAX_SPHERE_COUNT]; // 球のリムライトの色を格納した配列
            int _SphereCount; // 処理する球の個数

            // 入力データ用の構造体
            struct input
            {
                float4 vertex : POSITION; // 頂点座標
            };

            // vertで計算してfragに渡す用の構造体
            struct v2f
            {
                float4 pos : POSITION1; // ピクセルワールド座標
                float4 vertex : SV_POSITION; // 頂点座標
            };

            // 出力データ用の構造体
            struct output
            {
                float4 col: SV_Target; // ピクセル色
                float depth : SV_Depth; // 深度
            };

            // 入力 -> v2f
            v2f vert(const input v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.pos = mul(unity_ObjectToWorld, v.vertex); // ローカル座標をワールド座標に変換
                return o;
            }

            // 球の距離関数
            float4 sphereDistanceFunction(float4 sphere, float3 pos)
            {
                return length(sphere.xyz - pos) - sphere.w;
            }

            // 深度計算
            inline float getDepth(float3 pos)
            {
                const float4 vpPos = mul(UNITY_MATRIX_VP, float4(pos, 1.0));

                float z = vpPos.z / vpPos.w;
                #if defined(SHADER_API_GLCORE) || \
                    defined(SHADER_API_OPENGL) || \
                    defined(SHADER_API_GLES) || \
                    defined(SHADER_API_GLES3)
                return z * 0.5 + 0.5;
                #else
                return z;
                #endif
            }

            // smooth min関数
            float smoothMin(float x1, float x2, float k)
            {
                return -log(exp(-k * x1) + exp(-k * x2)) / k;
            }

            // 多項式近似型smooth min関数
            float smoothMinPoly(float a, float b, float k)
            {
                float h = max(k - abs(a - b), 0.0) / k;
                return min(a, b) - h * h * k * 0.25;
            }

            // 全ての球との最短距離を返す
            float getDistance(float3 pos)
            {
                float dist = _RayDistance;

                [loop]
                for (int i = 0; i < _SphereCount; i++)
                {
                    dist = smoothMinPoly(dist, sphereDistanceFunction(_Spheres[i], pos), _MetaballCorrectionConstant);    // 滑らかに繋げる
                }

                return dist;
            }

            // 色の算出
            half4 getColor(const float3 pos, const float rimRate)
            {
                half4 color = half4(0, 0, 0, 0);
                float weight = 0.01;

                [loop]
                for (int i = 0; i < _SphereCount; i++)
                {
                    const float4 sphere = _Spheres[i];
                    const float distToSphere = length(sphere.xyz - pos);

                    const float distinctness = 0.7;
                    const float x = clamp((length(sphere.xyz - pos) - sphere.w) * distinctness, 0, 1);
                    const float t = 1.0 - x * x * (3.0 - 2.0 * x);  // 色を滑らかに補間するための値

                    color += clamp(lerp(t * _BaseColors[i], t * _LimColors[i], rimRate), 0, 1);
                    color.a += clamp((t * _BaseColors[i].a) * (t * _LimColors[i].a), 0, 1);
                    weight += t;
                }

                color /= weight;
                return color;
            }

            // 法線の算出
            float3 getNormal(const float3 pos)
            {
                //テトラヘドラル法
                const float2 e = float2(0.0001, -0.0001);

                return normalize(
                    e.xyy * getDistance(pos + e.xyy) +
                    e.yyx * getDistance(pos + e.yyx) +
                    e.yxy * getDistance(pos + e.yxy) +
                    e.xxx * getDistance(pos + e.xxx)
                );
            }

            // v2f -> 出力
            output frag(const v2f i)
            {
                output o;


                float3 pos = i.pos.xyz; // レイの座標（ピクセルのワールド座標で初期化）
                Light light = GetMainLight();
                const float3 rayDir = normalize(pos.xyz - _WorldSpaceCameraPos); // レイの進行方向
                const half3 halfDir = normalize(light.direction - rayDir); // ハーフベクトル

                [loop]
                for (int i = 0; i < _RayLoopCount; i++)
                {
                    // posと球との最短距離
                    float dist = getDistance(pos);

                    // 距離が0.01以下になったら、色と深度を書き込んで処理終了
                    if (dist < _SphereDistanceThreshold)
                    {
                        half3 norm = getNormal(pos); // 法線
                        const float rimRate = pow(1 - abs(dot(norm, rayDir)), _LimPower);
                        half4 baseColor = getColor(pos, rimRate);

                        // ビュー方向とライト方向の両方を考慮
                        const float viewDot = dot(norm, -rayDir);
                        const float lightDot = dot(norm, light.direction);

                        float highlight = dot(norm, halfDir) > _HightLightThreshold ? 1 : 0; // ハイライト(トゥーン調)
                        half3 color = baseColor + highlight * _HightLightColor.rgb; // 色

                        o.col = half4(color, baseColor.a); // 塗りつぶし
                        o.depth = getDepth(pos); // 深度書き込み

                        return o;
                    }

                    // レイの方向に行進
                    pos += dist * rayDir;
                }

                // 衝突判定がなかったら透明にする
                o.col = 0;
                o.depth = 0;
                return o;
            }
            
            ENDHLSL
        }
    }
}