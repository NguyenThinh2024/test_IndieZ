Shader "UI/BG Logo Hole (Scaled Mask)"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _LogoTex("Logo Mask (alpha)", 2D) = "white" {}
        _LogoCenter("Logo Center (UV)", Vector) = (0.5,0.5,0,0)
        _LogoScale("Logo Scale", Float) = 1
        _LogoScaleXY("Logo Scale XY", Vector) = (1,1,0,0)
        _HoleSoftness("Hole Softness", Range(0,0.2)) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color;

            sampler2D _LogoTex;
            float4 _LogoCenter;     // xy center in UV (0..1)
            float _LogoScale;       // >1 bigger hole, <1 smaller hole
            float4 _LogoScaleXY;    // xy independent hole scale in UV space
            float _HoleSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // BG full-screen UV
                float2 suv = i.uv;

                // Scale mask UV around center:
                // When _LogoScale is large -> logoUV spans smaller region? (need inverse mapping)
                // We want: bigger scale => sample logo over larger screen area (bigger hole)
                // So map: logoUV = (suv - center) / _LogoScale + 0.5
                float2 center = _LogoCenter.xy;
                float2 scaleXY = max(_LogoScaleXY.xy, float2(1e-5, 1e-5));

                if (_LogoScaleXY.x <= 0.0 || _LogoScaleXY.y <= 0.0)
                {
                    float s = max(_LogoScale, 1e-5);
                    scaleXY = float2(s, s);
                }

                float2 logoUV = (suv - center) / scaleXY + float2(0.5, 0.5);

                // outside logo texture => alpha = 0 (no hole)
                if (logoUV.x < 0 || logoUV.x > 1 || logoUV.y < 0 || logoUV.y > 1)
                    return col;

                float logoA = tex2D(_LogoTex, logoUV).a;

                // soften edge
                float hole = smoothstep(0.0, _HoleSoftness, logoA);

                // Invert (subtract): remove BG where logo is opaque
                col.a *= (1.0 - hole);

                return col;
            }
            ENDCG
        }
    }
}
