Shader "Custom/ScreenPulse_UI"
{
    Properties
    {
        _PulseColor("Âö³åÑÕÉ«", Color) = (0,1,0,1)
        _PulseCenter("Âö³åÖÐÐÄ", Vector) = (0.5,0.5,0,0)
        _PulseRadius("Âö³å°ë¾¶", Float) = 0
        _PulseWidth("Âö³å¿í¶È", Float) = 0.1
        _PulseIntensity("Âö³åÇ¿¶È", Float) = 1.0
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

            float4 _PulseColor;
            float2 _PulseCenter;
            float _PulseRadius;
            float _PulseWidth;
            float _PulseIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
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