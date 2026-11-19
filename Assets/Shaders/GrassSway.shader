// Swaying grass tuft for SpriteRenderers. Lit like the other 2D sprites (URP 2D lights).
// The vertex shader bends the tuft along the fog wind (global _FogWind, set by FogGrid): the base stays
// planted, the tip moves. A gust wave travels along the wind so neighbours sway one after another.
// The tileset's tufts are painted on an opaque flat ground colour, so the fragment keys that colour out
// and draws the tuft shape in _Tint.
Shader "Cowncrow/GrassSway"
{
    Properties
    {
        [MainTexture] _MainTex ("Tuft sprite (set by SpriteRenderer)", 2D) = "white" {}
        _Tint ("Tuft colour", Color) = (0.435, 0.49, 0.208, 1)
        _GroundColor ("Ground colour painted behind the tufts (keyed out)", Color) = (0.608, 0.686, 0.369, 1)
        _KeyRange ("Key range (colour distance from the ground that counts as full tuft)", Range(0.01, 0.5)) = 0.1
        _TuftHeight ("Tuft height in units (where the bend reaches full strength)", Float) = 0.6
        _SwayAmount ("Sway amount (tip movement in units)", Float) = 0.08
        _WaveScale ("Wave scale (gusts per unit; higher = shorter waves)", Float) = 0.7
        _WaveSpeed ("Wave speed (radians per second)", Float) = 1.2
        _Flutter ("Flutter (per-tuft jitter, fraction of sway)", Range(0, 1)) = 0.25
        _FlutterSpeed ("Flutter speed (radians per second)", Float) = 2.5
        _WindLean ("Wind lean (steady push in units per unit of wind strength)", Float) = 0.5
    }
    SubShader
    {
        // DisableBatching: the sway reads the object's own position, so tufts must not be merged.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "GrassSway"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _GroundColor;
                float _KeyRange;
                float _TuftHeight;
                float _SwayAmount;
                float _WaveScale;
                float _WaveSpeed;
                float _Flutter;
                float _FlutterSpeed;
                float _WindLean;
            CBUFFER_END

            // Set every frame by FogGrid. xy = wind direction * strength. Zero = no lean, sway only.
            float4 _FogWind;

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);

                // wind direction; falls back to +x so the grass still sways when the wind is zero
                float strength = length(_FogWind.xy);
                float2 dir = strength > 0.0001 ? _FogWind.xy / strength : float2(1.0, 0.0);

                // base of the tuft in the world: wave and flutter use it, so the whole tuft moves as one
                float3 basePos = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, 0.0);
                float wave = sin(dot(basePos.xy, dir) * _WaveScale - _Time.y * _WaveSpeed);
                float phase = frac(sin(dot(basePos.xy, float2(12.9898, 78.233))) * 43758.5453) * 6.2831;
                float flutter = sin(_Time.y * _FlutterSpeed + phase);

                // weight squared: 0 at the base (planted), 1 at the tip
                float weight = saturate(input.positionOS.y / _TuftHeight);
                weight *= weight;

                float push = (strength * _WindLean + _SwayAmount * (wave + _Flutter * flutter)) * weight;
                float3 world = TransformObjectToWorld(input.positionOS);
                world.xy += dir * push;

                o.positionCS = TransformWorldToHClip(world);
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.color = input.color * unity_SpriteColor;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // shape = how far this pixel's colour is from the flat ground colour
                half key = saturate(length(tex.rgb - _GroundColor.rgb) / _KeyRange) * tex.a;
                half4 main = half4(_Tint.rgb * input.color.rgb, key * _Tint.a * input.color.a);

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, half4(1, 1, 1, 1), half3(0, 0, 1), surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }
    }
}
