// Orka showroom atmosferi: sıcak krem gradyan gökyüzü.
// Basit unlit skybox — URP ile uyumlu.
Shader "Orka/GradientSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.965, 0.937, 0.875, 1)
        _HorizonColor ("Horizon Color", Color) = (0.906, 0.855, 0.761, 1)
        _BottomColor ("Bottom Color", Color) = (0.851, 0.784, 0.659, 1)
        _Exponent ("Blend Exponent", Range(0.25, 8)) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _TopColor;
            half4 _HorizonColor;
            half4 _BottomColor;
            float _Exponent;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float y = normalize(i.dir).y;
                float tUp = pow(saturate(y), _Exponent);
                float tDown = pow(saturate(-y), _Exponent);
                half4 c = lerp(_HorizonColor, _TopColor, tUp);
                c = lerp(c, _BottomColor, tDown);
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
