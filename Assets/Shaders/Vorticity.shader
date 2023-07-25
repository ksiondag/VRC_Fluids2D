Shader "Custom/Vorticity" {
    Properties {
        _MainTex ("Velocity Texture", 2D) = "white" {}
        _CurlTex ("Curl Texture", 2D) = "white" {}
        _Curl ("Curl", Range(-10, 10)) = 0
        _DeltaTime ("Delta Time", Range(0, 1)) = 0
        _TexelSize ("Texel Size", Vector) = (512, 512, 0, 0)
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
                float2 l : TEXCOORD1;
                float2 r : TEXCOORD2;
                float2 t : TEXCOORD3;
                float2 b : TEXCOORD4;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CurlTex;
            float _Curl;
            float _DeltaTime;
            float2 _TexelSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.l = v.uv + float2(-_TexelSize.x, 0);
                o.r = v.uv + float2(_TexelSize.x, 0);
                o.t = v.uv + float2(0, _TexelSize.y);
                o.b = v.uv + float2(0, -_TexelSize.y);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float L = tex2D(_CurlTex, i.l).r;
                float R = tex2D(_CurlTex, i.r).r;
                float T = tex2D(_CurlTex, i.t).r;
                float B = tex2D(_CurlTex, i.b).r;
                float C = tex2D(_CurlTex, i.uv).r;

                float2 force = 0.5 * float2(abs(T) - abs(B), abs(R) - abs(L));
                force /= length(force) + 0.0001;
                force *= _Curl * C;
                force.y *= -1.0;

                float2 velocity = tex2D(_MainTex, i.uv).xy;
                velocity += force * _DeltaTime;
                velocity = clamp(velocity, -1000.0, 1000.0);
                return float4(velocity, 0.0, 1.0);
            }
            ENDCG
        }
    } 
}
