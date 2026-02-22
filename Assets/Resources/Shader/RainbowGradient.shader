Shader "TextMeshPro/RainbowGradient"
{
    Properties
    {
        // ── 彩虹渐变参数 ────────────────────────────────
        _RainbowAngle ("Rainbow Angle (0=↑ 90=→)", Range(0, 360)) = 0
        _RainbowSpeed ("Flow Speed (0=静止)", Range(0, 5)) = 0
        _RainbowSaturation ("Saturation", Range(0, 1)) = 0.9
        _RainbowBrightness ("Brightness", Range(0, 1)) = 1.0
        _RainbowTiling ("Tiling (彩虹重复次数)", Range(0.1, 5)) = 1.0
        _RainbowOffset ("Hue Offset", Range(0, 1)) = 0

        [Toggle] _UseRainbow ("Enable Rainbow", Float) = 0
        [HDR] _SolidColor ("Solid Color", Color) = (1,1,1,1)
        // ── TMP 标准属性（保持兼容）────────────────────
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0

        _WeightNormal ("Weight Normal", Float) = 0
        _WeightBold ("Weight Bold", Float) = 0.5
        _Sharpness ("Sharpness", Range(-1,1)) = 0

        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceTex ("Face Texture", 2D) = "white" {}
        _TextureWidth ("Texture Width", Float) = 512
        _TextureHeight ("Texture Height", Float) = 512
        _GradientScale ("Gradient Scale", Float) = 5
        _ScaleX ("Scale X", Float) = 1
        _ScaleY ("Scale Y", Float) = 1
        _ScaleRatioA ("Scale RatioA", Float) = 1
        _PerspectiveFilter ("Perspective", Range(0,1)) = 0.875
        _ShaderFlags ("Flags", Float) = 0

        _VertexOffsetX ("Vertex OffsetX", Float) = 0
        _VertexOffsetY ("Vertex OffsetY", Float) = 0
        _MaskCoord ("Mask Coordinates", Vector) = (0,0,32767,32767)
        _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        _MaskSoftnessX ("Mask SoftnessX", Float) = 0
        _MaskSoftnessY ("Mask SoftnessY", Float) = 0

        // ── Stencil 参数 ────────────────────────────────
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Lighting Off
        Cull [_CullMode]
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   VertShader
            #pragma fragment FragShader
            #pragma target 3.0
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // ── TMP 核心属性 ───────────────────────────
            sampler2D _MainTex;
            float _TextureWidth, _TextureHeight;
            float _GradientScale, _ScaleRatioA;
            float _Sharpness;
            float _FaceDilate;
            float _WeightNormal, _WeightBold;
            float _OutlineWidth, _OutlineSoftness;
            float4 _OutlineColor;
            float4 _FaceColor;
            float _Stencil;
            float _VertexOffsetX, _VertexOffsetY;
            float4 _MaskCoord, _ClipRect;
            float _MaskSoftnessX, _MaskSoftnessY;

            // ── 彩虹参数 ───────────────────────────────
            float _RainbowAngle;
            float _RainbowSpeed;
            float _RainbowSaturation;
            float _RainbowBrightness;
            float _RainbowTiling;
            float _RainbowOffset;

            float _UseRainbow;
            float4 _SolidColor;

            // ── 顶点输入 / 输出 ────────────────────────
            struct vertex_t
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 position : POSITION;
                float4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct pixel_t
            {
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex : SV_POSITION;
                float4 faceColor : COLOR;
                float4 outlineColor : COLOR1;
                float4 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 objectPos : TEXCOORD2; // 归一化字符局部坐标 0-1
                float4 param : TEXCOORD3;
                float4 mask : TEXCOORD4;
            };

            // ── HSV → RGB ──────────────────────────────
            float3 HSVtoRGB(float h, float s, float v)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(h + K.xyz) * 6.0 - K.www);
                return v * lerp(K.xxx, saturate(p - K.xxx), s);
            }

            // ── 顶点着色器 ─────────────────────────────
            pixel_t VertShader(vertex_t input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                pixel_t output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float bold = step(input.texcoord1.y, 0);
                float weight = lerp(_WeightNormal, _WeightBold, bold);
                float sd = _GradientScale * (1.0 + _Sharpness * 0.1) * _ScaleRatioA;

                output.param = float4(sd, weight / sd, 0, _OutlineSoftness * 0.5 / sd);
                output.faceColor = _FaceColor * input.color;
                output.outlineColor = _OutlineColor;
                output.texcoord0 = float4(input.texcoord0.xy, input.texcoord1.xy);
                output.texcoord1 = input.texcoord1;

                // 把 TMP uv1（字符局部 UV，范围约 0-1）直接传出
                output.objectPos = input.position.xy;

                float4 vpos = float4(input.position.xy
                      + float2(_VertexOffsetX, _VertexOffsetY),
                      input.position.z, input.position.w);
                output.vertex = UnityObjectToClipPos(vpos);

                // Mask
                float2 pxSize = float2(1.0 / _TextureWidth, 1.0 / _TextureHeight);
                float4 cr = clamp(_MaskCoord, -2e10, 2e10);
                output.mask = float4(vpos.xy * 2 - cr.xy - cr.zw,
                                                           0.25 / (0.25 * float2(
                                                               _MaskSoftnessX, _MaskSoftnessY) + pxSize));
                return output;
            }

            // ── 片元着色器 ─────────────────────────────
            fixed4 FragShader(pixel_t input) : SV_Target
            {
                // ① SDF 采样
                half d = tex2D(_MainTex, input.texcoord0.xy).a;

                // ② SDF → 面 / 轮廓 mask
                half pixelDist = length(float2(ddx(d), ddy(d)));
                half softW = max(pixelDist, 0.001);

                half faceMask = smoothstep(0.5 - softW, 0.5 + softW, d);
                half outlineEdge = 0.5 - _OutlineWidth * 0.5;
                half outlineMask = smoothstep(outlineEdge - softW, outlineEdge + softW, d)
                    * (1.0 - faceMask);

                // ③ 计算颜色（彩虹 or 纯色）
                float4 faceColor;
                if (_UseRainbow > 0.5)
                {
                    // 彩虹模式
                    float  rad = _RainbowAngle * 0.01745329;
                    // 直接用对象空间坐标投影，_RainbowScale 控制渐变跨度（单位：Unity 单位）
                    float  dir = dot(input.objectPos, float2(sin(rad), cos(rad)));
                    float  t   = frac(dir * 0.1 * _RainbowTiling
                                       + _RainbowOffset
                                       + _Time.y * _RainbowSpeed * 0.1);
                    float3 rainbow = HSVtoRGB(t, _RainbowSaturation, _RainbowBrightness);
                    faceColor = float4(rainbow, 1.0) * input.faceColor;
                }
                else
                {
                    // 纯色模式
                    faceColor = _SolidColor * input.faceColor;
                }

                // ⑤ 面 + 轮廓合成
                float4 color = faceColor * faceMask
                    + input.outlineColor * outlineMask;
                color.a = (faceMask + outlineMask) * faceColor.a;

                // ⑥ UI 裁剪
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.vertex.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                color.rgb *= color.a; // 预乘 Alpha
                return color;
            }
            ENDCG
        }
    }

    CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}