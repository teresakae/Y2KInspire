// Drifting, wispy fog for FogGrid's sprite. Visual only: gameplay reads FogGrid's fog[] array.
// Two scrolling noise layers (world space, so texture size doesn't matter) unevens thin fog,
// so hole edges and regrowing fog thin out gradually and patchily while thick fog stays solid.
Shader "Cowncrow/FogSwirl"
{
    Properties
    {
        [HideInInspector] _MainTex ("Fog texture (set by FogGrid)", 2D) = "white" {}
        [HideInInspector] _Color ("Tint (SpriteRenderer colour)", Color) = (1,1,1,1)
        _NoiseScale ("Wisp size (noise per world unit; lower = bigger wisps)", Float) = 0.45
        _ScrollA ("Drift A (units per second, added to the wind)", Vector) = (0.12, 0.03, 0, 0)
        _ScrollB ("Drift B (units per second, added to the wind; crosses A so it churns)", Vector) = (-0.07, 0.05, 0, 0)
        _FullAlpha ("Full fog opacity (match Fog Color alpha; used to tell thin from thick)", Range(0.1, 1)) = 0.85
        _EdgeBreak ("Edge breakup (how patchy the thinning zone is, 0 = smooth gradient)", Range(0, 2)) = 0.8
        _BodyShade ("Body roll (how much the fog colour darkens in slow patches)", Range(0, 0.5)) = 0.12
        _Wobble ("Edge wobble (bends edges off the grid, in UV units)", Range(0, 0.05)) = 0.01
        _BreatheAmount ("Breathe amount (how much settled fog thins in places, 0 = steady)", Range(0, 0.5)) = 0.25
        _BreatheSpeed ("Breathe speed", Range(0, 0.5)) = 0.08
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "FogSwirl"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _NoiseScale;
                float4 _ScrollA;
                float4 _ScrollB;
                float _FullAlpha;
                float _EdgeBreak;
                float _BodyShade;
                float _Wobble;
                float _BreatheAmount;
                float _BreatheSpeed;
            CBUFFER_END

            // Set every frame by FogGrid (globals, so not in the material buffer). Radius 0 = off.
            float4 _CowHazeOval;   // xy = oval centre (world), zw = half width / height
            float4 _CowHazeLook;   // x = softness (fraction of radius), y = fog left at the centre
            // Set every frame by FogGrid: xy = wind direction * strength. Added to both drift layers, so fog and grass move together.
            float4 _FogWind;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 worldXY : TEXCOORD1;
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.worldXY = world.xy;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.color = i.color * _Color;
                return o;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 3 octaves, roughly 0.1..0.9
            float fbm(float2 p)
            {
                float v = 0.5 * vnoise(p);
                v += 0.25 * vnoise(p * 2.03 + 17.0);
                v += 0.125 * vnoise(p * 4.01 + 41.0);
                return v / 0.875;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 p = i.worldXY * _NoiseScale;

                // drifting UV offset bends the texture's pixel grid so hole edges are not stair-stepped
                float2 bend = float2(fbm(p * 2.0 + t * 0.05), fbm(p * 2.0 + 71.0 - t * 0.05)) - 0.45;
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + bend * 2.0 * _Wobble) * i.color;

                float nA = fbm(p + (_ScrollA.xy + _FogWind.xy) * t * _NoiseScale);
                float nB = fbm(p * 1.7 + 9.0 + (_ScrollB.xy + _FogWind.xy) * t * _NoiseScale);
                // stretch contrast: fbm clusters near 0.5, wisps need real gaps
                float n = saturate((nA * 0.6 + nB * 0.4 - 0.5) * 1.8 + 0.5);

                // soft thinning: opacity follows the fog amount; noise only unevens the middle of the ramp,
                // so full fog stays full and clear stays clear (no crisp contour sweeping through)
                float amount = saturate(tex.a / (_FullAlpha * max(i.color.a, 0.001)));
                float mid = amount * (1.0 - amount) * 4.0;   // 0 at clear and at full fog, 1 halfway
                float keep = saturate(amount + (n - 0.5) * _EdgeBreak * mid);

                // slow body roll: a different slice of the same drifting noise
                float n2 = fbm(p * 0.5 + 33.0 + (_ScrollA.xy + _ScrollB.xy + 2.0 * _FogWind.xy) * 0.5 * t * _NoiseScale);
                half3 rgb = tex.rgb * lerp(1.0 - _BodyShade, 1.0, n2);

                // breathe: slow, large-scale noise drifting a different way thins settled fog in places
                float nb = fbm(p * 0.35 + 5.0 + float2(-0.6, 0.4) * _BreatheSpeed * t);
                float breathe = lerp(1.0 - _BreatheAmount, 1.0, saturate((nb - 0.25) * 2.0));

                // cow haze: thin the fog in a soft oval so the cow's head shows through
                float haze = 1.0;
                if (_CowHazeOval.z > 0.0 && _CowHazeOval.w > 0.0)
                {
                    float d = length((i.worldXY - _CowHazeOval.xy) / _CowHazeOval.zw);
                    haze = lerp(_CowHazeLook.y, 1.0, smoothstep(1.0 - _CowHazeLook.x, 1.0, d));
                }

                return half4(rgb, tex.a * keep * breathe * haze);
            }
            ENDHLSL
        }
    }
}
