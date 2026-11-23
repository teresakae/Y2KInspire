// Gentle floating motion for the title-screen art (SpriteRenderers, unlit, alpha blended).
// Everything moves around the object's own origin (the sprite pivot): a vertical bob, a small tilt,
// a slow scale breathe, and an optional ripple that wobbles the picture's edges (for fog).
// Every renderer needs its OWN material: sprites sharing a material can be batched, and then
// the origin is lost. All amounts are small on purpose; 0 turns a motion off.
Shader "Cowncrow/TitleFloat"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite (set by SpriteRenderer)", 2D) = "white" {}
        _BobAmp ("Bob amount (world units, up and down)", Float) = 0
        _BobSpeed ("Bob speed (radians per second; period = 6.28 / speed)", Float) = 1
        _TiltDeg ("Tilt (degrees each way)", Float) = 0
        _TiltSpeed ("Tilt speed (radians per second)", Float) = 1
        _ScaleAmp ("Scale breathe (fraction; negative = shrinks while a bob rises)", Float) = 0
        _ScaleSpeed ("Scale speed (radians per second)", Float) = 1
        _Phase ("Phase (seconds of time offset; same on a shadow and its animal)", Float) = 0
        _WobbleAmp ("Ripple amount (UV units; 0 = off)", Float) = 0
        _WobbleScale ("Ripple scale (waves across the sprite)", Float) = 6
        _WobbleSpeed ("Ripple speed (radians per second)", Float) = 0.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "DisableBatching"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "TitleFloat"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _BobAmp;
                float _BobSpeed;
                float _TiltDeg;
                float _TiltSpeed;
                float _ScaleAmp;
                float _ScaleSpeed;
                float _Phase;
                float _WobbleAmp;
                float _WobbleScale;
                float _WobbleSpeed;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                float t = _Time.y + _Phase;

                // scale and tilt in object space, so they pivot on the object's origin
                float s = 1.0 + _ScaleAmp * sin(t * _ScaleSpeed);
                float a = radians(_TiltDeg) * sin(t * _TiltSpeed);
                float sn = sin(a);
                float cs = cos(a);
                float2 p = input.positionOS.xy * s;
                p = float2(p.x * cs - p.y * sn, p.x * sn + p.y * cs);

                // bob in world units, whatever the object's scale
                float3 world = TransformObjectToWorld(float3(p, input.positionOS.z));
                world.y += _BobAmp * sin(t * _BobSpeed);

                o.positionCS = TransformWorldToHClip(world);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                // two sines at odd ratios stand in for smooth noise; the edge drifts without a visible loop
                float t = _Time.y * _WobbleSpeed + _Phase;
                float2 w;
                w.x = sin(uv.y * _WobbleScale + t) + 0.5 * sin(uv.y * _WobbleScale * 2.3 - t * 1.7);
                w.y = sin(uv.x * _WobbleScale * 1.3 - t * 0.8) + 0.5 * sin(uv.x * _WobbleScale * 2.9 + t * 1.3);
                uv += _WobbleAmp * w;

                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return c * input.color;
            }
            ENDHLSL
        }
    }
}
