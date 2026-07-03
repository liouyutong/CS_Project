Shader "CustomUI/ColorWheel_Final_Dynamic"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Hue("Hue", Float) = 0
        _GlobalS("Global Selection S", Range(0,1)) = 0.5
        _ColorsCount("Colors Segments", int) = 128
        _WheelsCount("Number of Wheels", int) = 1
        _StartingAngle("Starting Angle", Range(0,360)) = 0
        
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Stencil { Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest[unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex : POSITION; float2 texcoord : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 texcoord : TEXCOORD0; fixed4 color : COLOR; };

            sampler2D _MainTex;
            float _Hue, _GlobalS, _ColorsCount, _WheelsCount, _StartingAngle;

            float invL(float a, float b, float v) { return saturate((v - a) / (b - a)); }

            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            float3 HSV2RGB(float3 c) {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float d = distance(IN.texcoord, half2(0.5, 0.5)) / 0.5;
                if (d > 1.0) discard;

                float h_atan = atan2(IN.texcoord.y - 0.5, IN.texcoord.x - 0.5);
                float h = (h_atan + (UNITY_PI * 2 * (_StartingAngle / 360.0)));
                h = (h >= 0 ? h : (2 * UNITY_PI + h)) / (2 * UNITY_PI);
                float shiftedH = fmod((h + _Hue) / _WheelsCount, 1.0);
                float discretedH = floor(shiftedH * _ColorsCount) / (_ColorsCount - 1.0);

                // --- 恢復動態連動：根據 _GlobalS (鼠標距離) 決定整體色調 ---
                float dynV = 1.0;
                float dynS = 1.0;
                if (_GlobalS <= 0.7) {
                    dynV = lerp(0.0, 1.0, pow(invL(0.01, 0.7, _GlobalS), 0.4));
                } else {
                    dynS = lerp(1.0, 0.35, invL(0.7, 0.98, _GlobalS));
                }

                // --- 局部漸層：保持色盤本身的深-原-淺視覺 ---
                float localV = 1.0;
                float localS = 1.0;
                if (d <= 0.7) {
                    localV = lerp(0.0, 1.0, pow(invL(0.0, 0.7, d), 0.5));
                } else {
                    localS = lerp(1.0, 0.4, invL(0.7, 1.0, d));
                }

                // 結合動態與局部，並加上邊緣平滑
                float3 rgb = HSV2RGB(float3(discretedH, dynS * localS, dynV * localV));
                float alpha = smoothstep(1.0, 0.98, d);
                return fixed4(rgb, alpha) * IN.color;
            }
            ENDCG
        }
    }
}