Shader "Custom/Fluids2D" {
    Properties {
        _Program ("Program", Int) = 0
        _MainTex ("Texture", 2D) = "white" {}
        _AspectRatio ("AspectRatio", Float) = 1.0

        _CalcTex ("Calc Texture", 2D) = "white" {}
        _TexelSize ("Texel Size", Vector) = (512, 512, 0, 0)
        // _DyeTexelSize ("Dye Texel Size", Vector) = (512, 512, 0, 0)
        _DeltaTime ("Delta Time", Float) = 0
        _Dissipation ("Dissipation", Float) = 0

        // Splat exclusive
        _Color ("Color", Float) = (1,1,1,1)
        _Point ("Point", Vector) = (0,0,0,0)
        _Radius ("Radius", Float) = 1.0

        // Clear exclusive
        _Value ("Value", Float) = 1.0

        // Curl exclusive
        _Curl ("Curl", Range(-10, 10)) = 0
    }
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            #define ADVECTION 0
            #define CLEAR 1
            #define CURL 2
            #define DIVERGENCE 3
            #define GRADIENT_SUBTRACT 4
            #define PRESSURE 5
            #define SPLAT 6
            #define VORTICITY 7

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float2 uvL : TEXCOORD1;
                float2 uvR : TEXCOORD2;
                float2 uvT : TEXCOORD3;
                float2 uvB : TEXCOORD4;
                float4 vertex : SV_POSITION;
            };

            int _Program;
            sampler2D _MainTex;
            float _AspectRatio;

            sampler2D _CalcTex;
            float4 _TexelSize;
            // float4 _DyeTexelSize;
            float _DeltaTime;
            float _Dissipation;

            float4 _Color;
            float2 _Point;
            float _Radius;

            float _Value;

            float _Curl;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvL = v.uv + float2(-_TexelSize.x, 0);
                o.uvR = v.uv + float2(_TexelSize.x, 0);
                o.uvT = v.uv + float2(0, _TexelSize.y);
                o.uvB = v.uv + float2(0, -_TexelSize.y);
                return o;
            }

            fixed4 splat (v2f i) : SV_Target {
                float2 p = i.uv - _Point.xy;
                p.x *= _AspectRatio;
                float3 splat = exp(-dot(p, p) / _Radius) * _Color.xyz;
                float3 base = tex2D(_MainTex, i.uv).xyz;
                return fixed4(base + splat, 1.0);
            }

            fixed4 clear (v2f i) : SV_Target {
                return _Value * tex2D(_MainTex, i.uv);
            }

            fixed4 advection (v2f i) : SV_Target {
                // Manual filtering code
                // float2 coord = i.uv - _DeltaTime * bilerp(_CalcTex, i.uv, _TexelSize.xy).xy * _TexelSize.xy;
                // fixed4 result = bilerp(_MainTex, coord, _DyeTexelSize.xy);
                float2 coord = i.uv - _DeltaTime * tex2D(_CalcTex, i.uv).xy * _TexelSize.xy;
                fixed4 result = tex2D(_MainTex, coord);
                float decay = 1.0 + _Dissipation * _DeltaTime;
                return result / decay;
            }

            fixed4 curl (v2f i) : SV_Target {
                float L = tex2D(_MainTex, i.uvL).g;
                float R = tex2D(_MainTex, i.uvR).g;
                float T = tex2D(_MainTex, i.uvT).r;
                float B = tex2D(_MainTex, i.uvB).r;
                float vorticity = R - L - T + B;
                return fixed4(0.5 * vorticity, 0.0, 0.0, 1.0);
            }

            fixed4 divergence (v2f i) : SV_Target {
                float L = tex2D(_MainTex, i.uvL).x;
                float R = tex2D(_MainTex, i.uvR).x;
                float T = tex2D(_MainTex, i.uvT).y;
                float B = tex2D(_MainTex, i.uvB).y;

                float2 C = tex2D(_MainTex, i.uv).xy;
                if (i.uvL.x < 0.0) { L = -C.x; }
                if (i.uvR.x > 1.0) { R = -C.x; }
                if (i.uvT.y > 1.0) { T = -C.y; }
                if (i.uvB.y < 0.0) { B = -C.y; }

                float div = 0.5 * (R - L + T - B);
                return float4(div, 0.0, 0.0, 1.0);
            }

            fixed4 gradientSubtract (v2f i) : SV_Target {
                float L = tex2D(_MainTex, i.uvL).x;
                float R = tex2D(_MainTex, i.uvR).x;
                float T = tex2D(_MainTex, i.uvT).x;
                float B = tex2D(_MainTex, i.uvB).x;
                float2 velocity = tex2D(_CalcTex, i.uv).xy;
                velocity -= float2(R - L, T - B);
                return fixed4(velocity, 0.0, 1.0);
            }

            fixed4 pressure (v2f i) : SV_Target {
                float L = tex2D(_CalcTex, i.uvL).x;
                float R = tex2D(_CalcTex, i.uvR).x;
                float T = tex2D(_CalcTex, i.uvT).x;
                float B = tex2D(_CalcTex, i.uvB).x;
                float divergence = tex2D(_MainTex, i.uv).x;
                float pressure = (L + R + B + T - divergence) * 0.25;
                return fixed4(pressure, 0.0, 0.0, 1.0);
            }

            fixed4 vorticity (v2f i) : SV_Target {
                float L = tex2D(_CalcTex, i.uvL).r;
                float R = tex2D(_CalcTex, i.uvR).r;
                float T = tex2D(_CalcTex, i.uvT).r;
                float B = tex2D(_CalcTex, i.uvB).r;
                float C = tex2D(_CalcTex, i.uv).r;

                float2 force = 0.5 * float2(abs(T) - abs(B), abs(R) - abs(L));
                force /= length(force) + 0.0001;
                force *= _Curl * C;
                force.y *= -1.0;

                float2 velocity = tex2D(_MainTex, i.uv).xy;
                velocity += force * _DeltaTime;
                velocity = clamp(velocity, -1000.0, 1000.0);
                return float4(velocity, 0.0, 1.0);
            }

            fixed4 frag (v2f i) : SV_Target {
                if (_Program == SPLAT) {
                    return splat(i);
                }
                if (_Program == CLEAR) {
                    return clear(i);
                }
                if (_Program == ADVECTION) {
                    return advection(i);
                }
                if (_Program == CURL) {
                    return curl(i);
                }
                if (_Program == DIVERGENCE) {
                    return divergence(i);
                }
                if (_Program == GRADIENT_SUBTRACT) {
                    return gradientSubtract(i);
                }
                if (_Program == PRESSURE) {
                    return pressure(i);
                }
                if (_Program == VORTICITY) {
                    return vorticity(i);
                }
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    } 
}
