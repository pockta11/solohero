// Unlit sprite with a flash overlay (D-096): _Flash 0..1 lerps the sprite colour toward _FlashColor (white) while
// keeping its alpha, so a hit reads as a clean white blink instead of a tint. Set per renderer through a
// MaterialPropertyBlock (SpriteFlash.Set). Vertex colour (SpriteRenderer.color) still tints first, for status colours.
// D-112: a 1-texel outline outside the silhouette in _OutlineColor (an empty pixel next to a filled one in any of
// the four directions), so every character reads as a bold sticker against busy backgrounds. The outline flashes
// with the body. It needs transparent room around each frame inside its sprite rect (character sheets keep 15+ px).
Shader "SoloHero/SpriteFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Flash ("Flash", Range(0, 1)) = 0
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _Outline ("Outline", Range(0, 1)) = 1
        _OutlineColor ("Outline Color", Color) = (0.1, 0.08, 0.19, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize;

        CBUFFER_START(UnityPerMaterial)
            half _Flash;
            half4 _FlashColor;
            half _Outline;
            half4 _OutlineColor;
        CBUFFER_END

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.color = input.color;
            output.uv = input.uv;
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            if (tex.a < 0.5h && _Outline > 0.5h)
            {
                float2 t = _MainTex_TexelSize.xy;
                half around = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(t.x, 0)).a
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(t.x, 0)).a
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, t.y)).a
                    + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0, t.y)).a;
                if (around > 0.5h)
                {
                    half3 rim = lerp(_OutlineColor.rgb, _FlashColor.rgb, _Flash);
                    return half4(rim, _OutlineColor.a * input.color.a);
                }
            }

            half4 c = tex * input.color;
            c.rgb = lerp(c.rgb, _FlashColor.rgb, _Flash);
            return c;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
