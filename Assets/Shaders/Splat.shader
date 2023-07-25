Shader "Custom/Splat" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _AspectRatio ("AspectRatio", Float) = 1.0
        _Color ("Color", Float) = (1,1,1,1)
        _Point ("Point", Vector) = (0,0,0,0)
        _Radius ("Radius", Float) = 1.0
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _AspectRatio;
            float4 _Color;
            float2 _Point;
            float _Radius;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 p = i.uv - _Point.xy;
                p.x *= _AspectRatio;
                float3 splat = exp(-dot(p, p) / _Radius) * _Color.xyz;
                float3 base = tex2D(_MainTex, i.uv).xyz;
                return fixed4(base + splat, 1.0);
            }
            ENDCG
        }
    } 
}
