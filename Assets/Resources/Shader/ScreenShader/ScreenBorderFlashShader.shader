Shader "Custom/ScreenBorderWaveShader"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _BorderColor("Border Color", Color) = (0,1,0,0.8)
        _BorderWidth("Border Width", Range(0.01, 0.3)) = 0.08
        _Distort("Distort Strength", Range(0, 0.1)) = 0.03
        _Alpha("Alpha", Range(0,1)) = 0
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            ZTest Always
            ZWrite Off
            Cull Off

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
                float4 _BorderColor;
                float _BorderWidth;
                float _Distort;
                float _Alpha;

                // 简单噪声（让边框不规则）
                float noise(float2 p)
                {
                    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
                }

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv);
                    float2 uv = i.uv;

                    // 动态扰动（让边框动起来）
                    float n = noise(uv * 6 + _Time.x * 3) * _Distort;
                    float d = min(min(uv.x, 1 - uv.x), min(uv.y, 1 - uv.y)) + n;

                    // 软边发光边框
                    float border = 1 - smoothstep(_BorderWidth, _BorderWidth + 0.01, d);
                    border *= border;

                    // 只叠加边框
                    col.rgb = lerp(col.rgb, _BorderColor.rgb, border * _Alpha * _BorderColor.a);

                    return col;
                }
                ENDCG
            }
        }
            FallBack "Unlit/Texture"
}