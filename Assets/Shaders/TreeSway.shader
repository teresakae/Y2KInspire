// Swaying tree part (trunk, canopy or hanging fruit) for SpriteRenderers. Lit like Sprite-Lit-Default,
// draws the sprite's own colours. Same wind and bend idea as GrassSway: global _FogWind from FogGrid.
// Trunk and canopy use the same wave phase (taken from world X only, smooth) so they move as one.
Shader "Cowncrow/TreeSway"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite (set by SpriteRenderer)", 2D) = "white" {}
        _BottomY ("Sprite bottom, local Y in units (sprites are centre-pivot: minus half the height)", Float) = -1.5
        _SwayHeight ("Sprite height in units (where the bend reaches full strength)", Float) = 3
        _SwayAmount ("Sway amount (top movement in units)", Float) = 0.04
        _BaseSway ("Base sway (fraction of the bend already applied at the sprite bottom; 1 = whole sprite moves evenly)", Range(0, 1)) = 0
        _WaveScale ("Wave scale (gusts per unit; higher = shorter waves)", Float) = 0.6
        _WaveSpeed ("Wave speed (radians per second)", Float) = 1.0
        _Flutter ("Flutter (jitter, fraction of sway)", Range(0, 1)) = 0.15
        _FlutterSpeed ("Flutter speed (radians per second)", Float) = 2.0
        _WindLean ("Wind lean (steady push in units per unit of wind strength)", Float) = 0.2
    }
    SubShader
    {
        // DisableBatching: the sway reads the object's own position, so parts must not be merged.
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "TreeSway"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BottomY;
                float _SwayHeight;
                float _SwayAmount;
                float _BaseSway;
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
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);

                // wind direction; falls back to +x so trees still sway when the wind is zero
                float strength = length(_FogWind.xy);
                float2 dir = strength > 0.0001 ? _FogWind.xy / strength : float2(1.0, 0.0);

                // Phase from world X only: trunk and canopy are the same tree (same X, different Y),
                // so they get the same phase and stay joined. Gusts still roll from tree to tree along X.
                float x = unity_ObjectToWorld._m03;
                float wave = sin(x * _WaveScale - _Time.y * _WaveSpeed);
                float flutter = sin(_Time.y * _FlutterSpeed + x * 3.7);

                // squared height: 0 at the bottom, 1 at the top. _BaseSway lifts the bottom so a canopy
                // starts already moved by about as much as the trunk's top.
                float h = saturate((input.positionOS.y - _BottomY) / _SwayHeight);
                float weight = _BaseSway + (1.0 - _BaseSway) * h * h;

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
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, half4(1, 1, 1, 1), half3(0, 0, 1), surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);
                half4 lit = CombinedShapeLightShared(surfaceData, inputData);
                return lit;
            }
            ENDHLSL
        }
    }
}
