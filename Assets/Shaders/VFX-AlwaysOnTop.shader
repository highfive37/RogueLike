// Same technique as UI/AlwaysOnTop (ZTest Always instead of the default LEqual), adapted for a
// world-space particle system instead of a UGUI graphic. The boss's pre-charge telegraph VFX
// spawns right at head height, close enough to the boss's own model that it can end up partly
// behind the head/body mesh depending on camera angle - a warning cue the player is meant to
// react to shouldn't be able to lose to the very boss it's warning about (2026-09-18 user
// request). Additive-style blend + boosted tint mirrors the source pack's own "Standard Unlit"
// particle look (Blend SrcAlpha One reproduces that glow without needing the exact numeric
// blend-factor enum the pack's shader uses internally).
Shader "RogueLike/VFX/AlwaysOnTop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (4,4,4,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _TintColor;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                return col;
            }
            ENDCG
        }
    }
}
