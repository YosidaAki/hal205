Shader "Hidden/ProceduralStarfield"
{
    Properties
    {
        _StarDensity("Star Density", Float) = 0.6
        _StarSize("Star Size", Float) = 0.8
        _TwinkleAmount("Twinkle Amount", Float) = 0.5
        _TwinkleSpeed("Twinkle Speed", Float) = 1.2
        _MeteorIntensity("Meteor Intensity", Float) = 0.6
        _MeteorWidth("Meteor Width", Float) = 0.008
        _MeteorSpeed("Meteor Speed", Float) = 0.4
        _Tint("Tint", Color) = (1,1,1,1)
        _Seed("Seed", Float) = 123.45
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        Pass
        {
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex: POSITION; float2 uv: TEXCOORD0; };
            struct v2f { float4 pos: SV_POSITION; float2 uv: TEXCOORD0; };

            float _StarDensity;
            float _StarSize;
            float _TwinkleAmount;
            float _TwinkleSpeed;
            float _MeteorIntensity;
            float _MeteorWidth;
            float _MeteorSpeed;
            float4 _Tint;
            float _Seed;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21) + _Seed);
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float starShape(float2 p, float size)
            {
                float r = length(p);
                return smoothstep(size, size * 0.4, r);
            }

            float generateStars(float2 uv, out float phase)
            {
                float col = 0;
                phase = 0;

                float scales[3] = {40, 80, 160};
                float sizes[3] = {0.007, 0.004, 0.002};

                for (int i = 0; i < 3; i++)
                {
                    float s = scales[i];
                    float2 st = uv * s;
                    float2 id = floor(st);
                    float2 f = frac(st) - 0.5;

                    float rnd = hash21(id);
                    float th = saturate(_StarDensity * (1 - i * 0.2));

                    if (rnd < th)
                    {
                        float2 pos = f + (rnd - 0.5) * 0.5;
                        float star = (1 - starShape(pos, sizes[i] * _StarSize)) * (0.5 + rnd);
                        col += star;
                        phase += rnd * 6.28;
                    }
                }
                return saturate(col);
            }

            float meteor(float2 uv, float time)
            {
                float2 origin = float2(0.2, 0.9);
                float2 dir = normalize(float2(0.8, -0.7));

                float travel = frac(time * _MeteorSpeed);
                float2 center = origin + dir * travel * 1.6;

                float2 rel = uv - center;
                float along = dot(rel, dir);
                float across = dot(rel, float2(-dir.y, dir.x));

                float streak = smoothstep(_MeteorWidth, 0, abs(across)) *
                               smoothstep(0.02, -0.2, along);

                return streak * _MeteorIntensity * exp(-abs(along) * 10);
            }

            fixed4 frag(v2f i): SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                float phase;
                float stars = generateStars(uv, phase);

                float tw = sin(t * _TwinkleSpeed + phase) * 0.5 + 0.5;
                tw = lerp(1 - _TwinkleAmount, 1 + _TwinkleAmount, tw);

                float3 col = stars * tw;

                float met = meteor(uv, t);
                col += met * float3(1.2, 0.9, 0.7);

                col *= _Tint.rgb;
                return float4(saturate(col), 1);
            }
            ENDCG
        }
    }
    FallBack Off
}

