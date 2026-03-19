Shader "Custom/ScreenPulse_UI"
{
    Properties
    {
        // 加一行这个，解决报错！
        _MainTex ("Texture", 2D) = "white" {}
        
        _PulseColor("脉冲颜色", Color) = (0,1,0,1)
        _PulseCenter("脉冲中心", Vector) = (0.5,0.5,0,0)
        _PulseRadius("脉冲半径", Float) = 0
        _PulseWidth("脉冲宽度", Float) = 0.1
        _PulseIntensity("脉冲强度", Float) = 1.0
    }

        SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
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

            // 声明变量
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float4 _PulseColor;
            float2 _PulseCenter;
            float _PulseRadius;
            float _PulseWidth;
            float _PulseIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dist = distance(i.uv, _PulseCenter);
                float pulse = smoothstep(_PulseRadius - _PulseWidth, _PulseRadius, dist);
                pulse *= 1.0 - smoothstep(_PulseRadius, _PulseRadius + _PulseWidth, dist);

                fixed4 col = _PulseColor;
                col.a = pulse * _PulseIntensity;
                return col;
            }
            ENDCG
        }
    }
        FallBack "Unlit/Transparent"
}