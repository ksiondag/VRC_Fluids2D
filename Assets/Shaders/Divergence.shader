Shader "Custom/Divergence" {
    Properties {
        _MainTex ("Velocity Texture", 2D) = "white" {}
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
                float L = tex2D(_MainTex, i.l).x;
                float R = tex2D(_MainTex, i.r).x;
                float T = tex2D(_MainTex, i.t).y;
                float B = tex2D(_MainTex, i.b).y;

                float2 C = tex2D(_MainTex, i.uv).xy;
                if (i.l.x < 0.0) { L = -C.x; }
                if (i.r.x > 1.0) { R = -C.x; }
                if (i.t.y > 1.0) { T = -C.y; }
                if (i.b.y < 0.0) { B = -C.y; }

                float div = 0.5 * (R - L + T - B);
                return float4(div, 0.0, 0.0, 1.0);
            }
            ENDCG
        }
    } 
}
