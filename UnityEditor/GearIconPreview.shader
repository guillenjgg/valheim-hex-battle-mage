Shader "Hidden/HexBattleMage/GearIconPreview"
{
    Properties
    {
        _MainTex("Fabric", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _EmissionColor("Emission", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _EmissionColor;
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Output { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; };
            Output vert(Input input)
            {
                Output output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.normal = UnityObjectToWorldNormal(input.normal);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            fixed4 frag(Output input, fixed facing : VFACE) : SV_Target
            {
                fixed4 sampledColor = tex2D(_MainTex, input.uv);
                clip(sampledColor.a - 0.1);
                float shade = 0.60 + 0.40 * abs(dot(normalize(input.normal), normalize(float3(0.3, 0.6, 0.8))));
                if (facing < 0) { shade *= 0.12; }
                return fixed4(LinearToGammaSpace(sampledColor.rgb * _Color.rgb * shade + _EmissionColor.rgb), 1);
            }
            ENDCG
        }
    }
}

