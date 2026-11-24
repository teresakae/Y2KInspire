// Boiling hand-drawn line wobble for the key drawings. Works on SpriteRenderers and on uGUI Images in a world-space canvas.
// Unlit, alpha blended, and written the same way as URP's Sprite-Unlit-Default so the 2D SRP batcher stays on
// (no _MainTex_ST / _MainTex_TexelSize here, on purpose).
// Every _Fps the drawing is "redrawn": the fragment shifts its UVs by smooth noise that jumps to a new
// pattern each step, and the vertex stage nudges and tilts the whole quad a hair. Steps, not slides, so it reads as pencil.
// The drawing sits in a wide sprite sheet, so _Aspect stretches the noise cells to come out roughly square.
Shader "Cowncrow/Scribble"
{
    Properties
    {
        [MainTexture] _MainTex ("Drawing (set by SpriteRenderer or Image)", 2D) = "white" {}
        _Amount ("Wobble amount (fraction of texture)", Range(0, 0.02)) = 0.004
        _Scale ("Lines of wobble across the texture", Float) = 40
        _Fps ("Redraws per second", Range(1, 24)) = 8
        _Aspect ("Texture width / height (keeps noise cells square)", Float) = 1.74
        _Jitter ("Whole-drawing shake (object units)", Float) = 0.01
        _Tilt ("Whole-drawing tilt (degrees)", Range(0, 5)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Scribble"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex ScribbleVertex
            #pragma fragment ScribbleFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                float _Amount;
                float _Scale;
                float _Fps;
                float _Aspect;
                float _Jitter;
                float _Tilt;
            CBUFFER_END

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453) * 2.0 - 1.0;   // -1..1
            }

            // smooth value noise, -1..1 per axis
            float2 Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash22(i), Hash22(i + float2(1, 0)), f.x),
                            lerp(Hash22(i + float2(0, 1)), Hash22(i + float2(1, 1)), f.x), f.y);
            }

            Varyings ScribbleVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                // per-step shake and tilt of the whole quad; the object's position seeds it so two keys don't step alike
                float frame = floor(_Time.y * _Fps);
                float2 seed = float2(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13);
                float2 r = Hash22(float2(frame, frame * 1.7) + seed);
                float a = radians(r.x * _Tilt);
                float sn = sin(a);
                float cs = cos(a);
                float2 p = input.positionOS.xy;
                p = float2(p.x * cs - p.y * sn, p.x * sn + p.y * cs) + r * _Jitter;
                input.positionOS.xy = p;

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * unity_SpriteColor;
                return o;
            }

            half4 ScribbleFragment(Varyings input) : SV_Target
            {
                float frame = floor(_Time.y * _Fps);
                // a fresh noise pattern each step: a hashed offset, not a slide
                float2 stepOffset = Hash22(float2(frame, 7.31)) * 100.0;
                float2 p = input.uv * _Scale * float2(_Aspect, 1.0) + stepOffset;
                float2 n = Noise(p) + 0.5 * Noise(p * 2.3 + 17.0);   // two octaves
                float2 uv = input.uv + n * _Amount;

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return c * input.color;
            }
            ENDHLSL
        }
    }
}
