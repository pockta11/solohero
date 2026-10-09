// Additive unlit sprite (D-146): glow halos, light pillars and spark particles add their colour (times alpha) to what
// is behind them, so effects look lit on the dark chapters without a post-process bloom. Vertex colour
// (SpriteRenderer.color / particle colour) tints and scales it; alpha 0 adds nothing.
// D-149: built like URP's Sprite-Unlit-Default (Core2D include, sprite instancing data, _Color / _RendererColor), so
// glows from the effect atlas are drawn together the way the default sprites are instead of one draw call each.
Shader "SoloHero/SpriteAdditive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
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

        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

        struct Attributes
        {
            float3 positionOS : POSITION;
            float4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            half4 color : COLOR;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        half4 _MainTex_ST;
        float4 _Color;
        half4 _RendererColor;

        Varyings vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#ifdef UNITY_INSTANCING_ENABLED
            input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteFlip);
#endif
            output.positionCS = TransformObjectToHClip(input.positionOS);
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.color = input.color * _Color * _RendererColor;
#ifdef UNITY_INSTANCING_ENABLED
            output.color *= unity_SpriteColor;
#endif
            return output;
        }

        half4 frag(Varyings input) : SV_Target
        {
            return input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
