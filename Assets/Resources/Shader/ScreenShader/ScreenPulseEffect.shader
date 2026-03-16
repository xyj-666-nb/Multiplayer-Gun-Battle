Shader "Custom/ScreenPulseEffect"
{
    Properties
    {
        _MainTex("屏幕纹理", 2D) = "white" {}
        _PulseColor("脉冲颜色", Color) = (0,1,0,1) // 绿色
        _PulseCenter("脉冲中心", Vector) = (0.5,0.5,0,0) // 屏幕中心
        _PulseRadius("脉冲半径", Float) = 0
        _PulseWidth("脉冲宽度", Float) = 0.1
        _PulseIntensity("脉冲强度", Float) = 1.0
    }

        SubShader
        {
            Tags { "Queue" = "Overlay" "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                ZTest Always
                ZWrite Off
                Cull Off

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
                    // 1. 获取原屏幕颜色
                    fixed4 originalColor = tex2D(_MainTex, i.uv);

                // 2. 计算当前像素到脉冲中心的距离
                float dist = distance(i.uv, _PulseCenter);

                // 3. 计算脉冲环（环形渐变）
                float pulse = smoothstep(_PulseRadius - _PulseWidth, _PulseRadius, dist);
                pulse *= 1.0 - smoothstep(_PulseRadius, _PulseRadius + _PulseWidth, dist);

                // 4. 叠加绿色脉冲效果
                fixed3 finalColor = originalColor.rgb + _PulseColor.rgb * pulse * _PulseIntensity;

                return fixed4(finalColor, originalColor.a);
            }
            ENDCG
        }
        }
            FallBack "Unlit/Texture"
}