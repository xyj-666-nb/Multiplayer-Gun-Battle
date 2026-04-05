Shader "Custom/GlitchImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlitchIntensity ("Glitch Intensity", Range(0, 1)) = 0.3
        _ScanLineSpeed ("Scan Line Speed", Float) = 5.0
        _BlockSize ("Block Size", Float) = 0.05
        _ColorShift ("RGB Split Amount", Float) = 0.01
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _GlitchIntensity;
            float _ScanLineSpeed;
            float _BlockSize;
            float _ColorShift;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y;

                // Ëæ»ú¿éÆ«ÒÆ£¨Block Glitch£©
                float block = floor(uv.y / _BlockSize) * _BlockSize;
                float noise = frac(sin(block * 123.456 + time * 10) * 43758.5453);
                
                // Ë®Æ½Æ«ÒÆ
                if (noise < _GlitchIntensity)
                {
                    uv.x += (noise - 0.5) * 0.15 * _GlitchIntensity;
                }

                // É¨ÃèÏß¶¶¶¯
                float scan = sin(uv.y * 200 + time * _ScanLineSpeed) * 0.002 * _GlitchIntensity;

                // RGB ·ÖÀë£¨É«É¢£©
                fixed4 colR = tex2D(_MainTex, uv + float2(_ColorShift, scan));
                fixed4 colG = tex2D(_MainTex, uv);
                fixed4 colB = tex2D(_MainTex, uv - float2(_ColorShift, scan * 1.2));

                fixed4 col = fixed4(colR.r, colG.g, colB.b, colG.a);

                // Ëæ»úÉÁË¸£¨ÕûÌåÁÁ¶ÈÉÁË¸£©
                float flicker = 1.0 + (frac(sin(time * 50) * 43758.5453) - 0.5) * 0.15 * _GlitchIntensity;
                col.rgb *= flicker;

                // ÇáÎ¢Ôëµã
                float noise2 = frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
                col.rgb += (noise2 - 0.5) * 0.08 * _GlitchIntensity;

                return col;
            }
            ENDCG
        }
    }
}