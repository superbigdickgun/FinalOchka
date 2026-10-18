Shader "Custom/DarknessFog"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Dark Color", Color) = (0,0,0,1)

        _PlayerPos ("Player Pos", Vector) = (0,0,0,0)
        _PlayerDir ("Player Dir", Vector) = (1,0,0,0)
        _PlayerRange ("Range", Float) = 4
        _PlayerAngle ("Angle", Float) = 80
        _PlayerSoftness ("Softness", Float) = 1.5

        _LightCount ("Light Count", Int) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float4 _PlayerPos;
            float4 _PlayerDir;
            float _PlayerRange;
            float _PlayerAngle;
            float _PlayerSoftness;

            int _LightCount;

            float4 _LightPos0;
            float _LightRadius0;
            float _LightSoftness0;
            float _LightIntensity0;

            float4 _LightPos1;
            float _LightRadius1;
            float _LightSoftness1;
            float _LightIntensity1;

            float4 _LightPos2;
            float _LightRadius2;
            float _LightSoftness2;
            float _LightIntensity2;

            float4 _LightPos3;
            float _LightRadius3;
            float _LightSoftness3;
            float _LightIntensity3;

            float4 _LightPos4;
            float _LightRadius4;
            float _LightSoftness4;
            float _LightIntensity4;

            float4 _LightPos5;
            float _LightRadius5;
            float _LightSoftness5;
            float _LightIntensity5;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
                return OUT;
            }

            float ComputeLight(float2 worldPos, float4 lpos, float r, float s, float intens)
            {
                float d = distance(worldPos, lpos.xy);
                float fade = 1 - saturate((d - (r - s)) / s);
                return fade * intens;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 baseCol = tex2D(_MainTex, IN.texcoord) * IN.color;

                // === КОНУС ИГРОКА ===
                float2 toPixel = IN.worldPos.xy - _PlayerPos.xy;
                float distP = length(toPixel);
                float coneVisible = 0;

                if (distP < _PlayerRange)
                {
                    float2 dirP = normalize(toPixel);
                    float2 playerDirN = normalize(_PlayerDir.xy);
                    float dotProd = dot(dirP, playerDirN);
                    float ang = degrees(acos(clamp(dotProd, -1, 1)));

                    if (ang < _PlayerAngle * 0.5)
                    {
                        float radFade = 1 - saturate((distP - (_PlayerRange - _PlayerSoftness)) / _PlayerSoftness);
                        float angFade = 1 - saturate((ang - (_PlayerAngle * 0.5 - 5)) / 5.0);
                        coneVisible = radFade * angFade;
                    }
                }

                // === СВЕЧИ ===
                float lightVisible = 0;

                if (_LightCount > 0) lightVisible = max(lightVisible, ComputeLight(IN.worldPos.xy, _LightPos0, _LightRadius0, _LightSoftness0, _LightIntensity0));
                if (_LightCount > 1) lightVisible = max(lightVisible, ComputeLight(IN.worldPos.xy, _LightPos1, _LightRadius1, _LightSoftness1, _LightIntensity1));
                if (_LightCount > 2) lightVisible = max(lightVisible, ComputeLight(IN.worldPos.xy, _LightPos2, _LightRadius2, _LightSoftness2, _LightIntensity2));
                if (_LightCount > 3) lightVisible = max(lightVisible, ComputeLight(IN.worldPos.xy, _LightPos3, _LightRadius3, _LightSoftness3, _LightIntensity3));
                if (_LightCount > 4) lightVisible = max(lightVisible, ComputeLight(IN.worldPos.xy, _LightPos4, _LightRadius4, _LightSoftness4, _LightIntensity4));
                if (_LightCount > 5) lightVisible = max(lightVisible, ComputeLight(IN.worldPos.xy, _LightPos5, _LightRadius5, _LightSoftness5, _LightIntensity5));

                float totalLight = max(coneVisible, lightVisible);
                baseCol.a *= (1 - totalLight);

                return baseCol;
            }
            ENDCG
        }
    }
}