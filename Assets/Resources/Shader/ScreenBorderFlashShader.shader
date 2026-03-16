Shader "Unlit/BorderEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BorderColor ("Border Color", Color) = (0,1,0,0.8)
        _BorderWidth ("Border Width", Range(0.01, 0.3)) = 0.1
        _Distort ("Distort", Range(0, 0.05)) = 0.02
        _Alpha ("Alpha", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _BorderColor;
            float _BorderWidth;
            float _Distort;
            float _Alpha;

            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float n = noise(uv * 8 + _Time.x * 4) * _Distort;
                float d = min(min(uv.x, 1-uv.x), min(uv.y, 1-uv.y)) + n;
                float border = 1.0 - smoothstep(_BorderWidth, _BorderWidth + 0.01, d);
                border = pow(border, 2);

                fixed4 col = fixed4(0,0,0,0);
                col.rgb = _BorderColor.rgb;
                col.a = border * _Alpha;
                return col;
            }
            ENDCG
        }
    }
}