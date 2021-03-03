Shader "Custom/vertColor"
{
    Properties
    {
        _Color ("Color", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 5.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 col : COLOR0;
            };
            struct FragInput
            {
                float4 vertex : SV_POSITION;
                float4 col : COLOR0;
            };
            float4 _Color;

            FragInput Vert(appdata _input)
            {
                FragInput _output;
                _output.vertex = UnityObjectToClipPos(_input.vertex);
                _output.col = _input.col;
                return _output;
            }
            float4 Frag(FragInput _input) : SV_Target
            {
                float4 c = _input.col;
                c.a = 1.f;
                c *= _Color;
                return c;
            }
            ENDHLSL
        }
    }
}