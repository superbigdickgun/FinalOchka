Shader "Custom/DarknessFog"
{
    Properties
    {
        _Color ("Darkness Color", Color) = (0.02, 0.02, 0.05, 1)
        _LightCount ("Light Count", Int) = 0

        _PlayerPos ("Player Pos", Vector) = (0,0,0,0)
        _PlayerDir ("Player Dir", Vector) = (1,0,0,0)
        _PlayerRange ("Player Range", Float) = 4
        _PlayerAngle ("Player Angle", Float) = 80
        _PlayerSoftness ("Player Softness", Float) = 1.5

        _LightPos0 ("Light0 Pos", Vector) = (0,0,0,0)
        _LightRadius0 ("Light0 Radius", Float) = 3
        _LightSoftness0 ("Light0 Softness", Float) = 0.5
        _LightIntensity0 ("Light0 Intensity", Float) = 0

        _LightPos1 ("Light1 Pos", Vector) = (0,0,0,0)
        _LightRadius1 ("Light1 Radius", Float) = 3
        _LightSoftness1 ("Light1 Softness", Float) = 0.5
        _LightIntensity1 ("Light1 Intensity", Float) = 0

        _LightPos2 ("Light2 Pos", Vector) = (0,0,0,0)
        _LightRadius2 ("Light2 Radius", Float) = 3
        _LightSoftness2 ("Light2 Softness", Float) = 0.5
        _LightIntensity2 ("Light2 Intensity", Float) = 0

        _LightPos3 ("Light3 Pos", Vector) = (0,0,0,0)
        _LightRadius3 ("Light3 Radius", Float) = 3
        _LightSoftness3 ("Light3 Softness", Float) = 0.5
        _LightIntensity3 ("Light3 Intensity", Float) = 0

        _LightPos4 ("Light4 Pos", Vector) = (0,0,0,0)
        _LightRadius4 ("Light4 Radius", Float) = 3
        _LightSoftness4 ("Light4 Softness", Float) = 0.5
        _LightIntensity4 ("Light4 Intensity", Float) = 0

        _LightPos5 ("Light5 Pos", Vector) = (0,0,0,0)
        _LightRadius5 ("Light5 Radius", Float) = 3
        _LightSoftness5 ("Light5 Softness", Float) = 0.5
        _LightIntensity5 ("Light5 Intensity", Float) = 0

        _LightPos6 ("Light6 Pos", Vector) = (0,0,0,0)
        _LightRadius6 ("Light6 Radius", Float) = 3
        _LightSoftness6 ("Light6 Softness", Float) = 0.5
        _LightIntensity6 ("Light6 Intensity", Float) = 0

        _LightPos7 ("Light7 Pos", Vector) = (0,0,0,0)
        _LightRadius7 ("Light7 Radius", Float) = 3
        _LightSoftness7 ("Light7 Softness", Float) = 0.5
        _LightIntensity7 ("Light7 Intensity", Float) = 0

        _LightPos8 ("Light8 Pos", Vector) = (0,0,0,0)
        _LightRadius8 ("Light8 Radius", Float) = 3
        _LightSoftness8 ("Light8 Softness", Float) = 0.5
        _LightIntensity8 ("Light8 Intensity", Float) = 0

        _LightPos9 ("Light9 Pos", Vector) = (0,0,0,0)
        _LightRadius9 ("Light9 Radius", Float) = 3
        _LightSoftness9 ("Light9 Softness", Float) = 0.5
        _LightIntensity9 ("Light9 Intensity", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD0;
            };

            float4 _Color;
            int _LightCount;

            float4 _LightPos0; float _LightRadius0; float _LightSoftness0; float _LightIntensity0;
            float4 _LightPos1; float _LightRadius1; float _LightSoftness1; float _LightIntensity1;
            float4 _LightPos2; float _LightRadius2; float _LightSoftness2; float _LightIntensity2;
            float4 _LightPos3; float _LightRadius3; float _LightSoftness3; float _LightIntensity3;
            float4 _LightPos4; float _LightRadius4; float _LightSoftness4; float _LightIntensity4;
            float4 _LightPos5; float _LightRadius5; float _LightSoftness5; float _LightIntensity5;
            float4 _LightPos6; float _LightRadius6; float _LightSoftness6; float _LightIntensity6;
            float4 _LightPos7; float _LightRadius7; float _LightSoftness7; float _LightIntensity7;
            float4 _LightPos8; float _LightRadius8; float _LightSoftness8; float _LightIntensity8;
            float4 _LightPos9; float _LightRadius9; float _LightSoftness9; float _LightIntensity9;

            float4 _PlayerPos;
            float4 _PlayerDir;
            float _PlayerRange;
            float _PlayerAngle;
            float _PlayerSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 worldPos = i.worldPos.xy;
                float darkness = _Color.a;

                // === СВЕТ (10 слотов) ===
                for (int l = 0; l < _LightCount; l++)
                {
                    float2 lightPos;
                    float lightRadius, lightSoftness, lightIntensity;

                    if (l == 0) { lightPos = _LightPos0.xy; lightRadius = _LightRadius0; lightSoftness = _LightSoftness0; lightIntensity = _LightIntensity0; }
                    else if (l == 1) { lightPos = _LightPos1.xy; lightRadius = _LightRadius1; lightSoftness = _LightSoftness1; lightIntensity = _LightIntensity1; }
                    else if (l == 2) { lightPos = _LightPos2.xy; lightRadius = _LightRadius2; lightSoftness = _LightSoftness2; lightIntensity = _LightIntensity2; }
                    else if (l == 3) { lightPos = _LightPos3.xy; lightRadius = _LightRadius3; lightSoftness = _LightSoftness3; lightIntensity = _LightIntensity3; }
                    else if (l == 4) { lightPos = _LightPos4.xy; lightRadius = _LightRadius4; lightSoftness = _LightSoftness4; lightIntensity = _LightIntensity4; }
                    else if (l == 5) { lightPos = _LightPos5.xy; lightRadius = _LightRadius5; lightSoftness = _LightSoftness5; lightIntensity = _LightIntensity5; }
                    else if (l == 6) { lightPos = _LightPos6.xy; lightRadius = _LightRadius6; lightSoftness = _LightSoftness6; lightIntensity = _LightIntensity6; }
                    else if (l == 7) { lightPos = _LightPos7.xy; lightRadius = _LightRadius7; lightSoftness = _LightSoftness7; lightIntensity = _LightIntensity7; }
                    else if (l == 8) { lightPos = _LightPos8.xy; lightRadius = _LightRadius8; lightSoftness = _LightSoftness8; lightIntensity = _LightIntensity8; }
                    else { lightPos = _LightPos9.xy; lightRadius = _LightRadius9; lightSoftness = _LightSoftness9; lightIntensity = _LightIntensity9; }

                    float dist = distance(worldPos, lightPos);
                    float lightAmt = 1 - smoothstep(lightRadius - lightSoftness, lightRadius, dist);
                    lightAmt *= lightIntensity;
                    darkness = min(darkness, 1 - lightAmt);
                }

                // === КОНУС ИГРОКА ===
                {
                    float2 toPlayer = worldPos - _PlayerPos.xy;
                    float distToPlayer = length(toPlayer);

                    if (distToPlayer < _PlayerRange)
                    {
                        float2 dir = normalize(toPlayer);
                        float dotResult = dot(dir, _PlayerDir.xy);
                        float angle = acos(dotResult) * 57.2958;

                        if (angle < _PlayerAngle * 0.5)
                        {
                            float coneAmt = 1 - smoothstep(_PlayerRange - _PlayerSoftness, _PlayerRange, distToPlayer);
                            darkness = min(darkness, 1 - coneAmt);
                        }
                    }
                }

                return float4(_Color.rgb, darkness);
            }
            ENDCG
        }
    }
}