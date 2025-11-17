Shader "Custom/TextMeshPro Mobile/Distance Field Overlay (Port)"
{
Properties {
	_FaceColor		    ("Face Color", Color) = (1,1,1,1)
	_FaceDilate			("Face Dilate", Range(-1,1)) = 0

	_OutlineColor	    ("Outline Color", Color) = (0,0,0,1)
	_OutlineWidth		("Outline Thickness", Range(0,1)) = 0
	_OutlineSoftness	("Outline Softness", Range(0,1)) = 0

	_UnderlayColor	    ("Border Color", Color) = (0,0,0,.5)
	_UnderlayOffsetX 	("Border OffsetX", Range(-1,1)) = 0
	_UnderlayOffsetY 	("Border OffsetY", Range(-1,1)) = 0
	_UnderlayDilate		("Border Dilate", Range(-1,1)) = 0
	_UnderlaySoftness 	("Border Softness", Range(0,1)) = 0

	_WeightNormal		("Weight Normal", float) = 0
	_WeightBold			("Weight Bold", float) = .5

	_ShaderFlags		("Flags", float) = 0
	_ScaleRatioA		("Scale RatioA", float) = 1
	_ScaleRatioB		("Scale RatioB", float) = 1
	_ScaleRatioC		("Scale RatioC", float) = 1

	_MainTex			("Font Atlas", 2D) = "white" {}
	_TextureWidth		("Texture Width", float) = 512
	_TextureHeight		("Texture Height", float) = 512
	_GradientScale		("Gradient Scale", float) = 5
	_ScaleX				("Scale X", float) = 1
	_ScaleY				("Scale Y", float) = 1
	_PerspectiveFilter	("Perspective Correction", Range(0, 1)) = 0.875
	_Sharpness			("Sharpness", Range(-1,1)) = 0

	_VertexOffsetX		("Vertex OffsetX", float) = 0
	_VertexOffsetY		("Vertex OffsetY", float) = 0

	_ClipRect			("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	_MaskSoftnessX		("Mask SoftnessX", float) = 0
	_MaskSoftnessY		("Mask SoftnessY", float) = 0

	_StencilComp		("Stencil Comparison", Float) = 8
	_Stencil			("Stencil ID", Float) = 0
	_StencilOp			("Stencil Operation", Float) = 0
	_StencilWriteMask	("Stencil Write Mask", Float) = 255
	_StencilReadMask	("Stencil Read Mask", Float) = 255

	_CullMode			("Cull Mode", Float) = 0
	_ColorMask			("Color Mask", Float) = 15
}

SubShader {
	Tags {
		"Queue"="Overlay"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
	}

	Stencil {
		Ref [_Stencil]
		Comp [_StencilComp]
		Pass [_StencilOp]
		ReadMask [_StencilReadMask]
		WriteMask [_StencilWriteMask]
	}

	Cull [_CullMode]
	ZWrite Off
	Lighting Off
	Fog { Mode Off }
	ZTest Always
	Blend One OneMinusSrcAlpha
	ColorMask [_ColorMask]

	Pass {
		CGPROGRAM
		#pragma target 3.0
		#pragma vertex VertShader
		#pragma fragment PixShader
		#pragma shader_feature __ OUTLINE_ON
		#pragma shader_feature __ UNDERLAY_ON UNDERLAY_INNER

		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP

		#include "UnityCG.cginc"
		#include "UnityUI.cginc"

		sampler2D _MainTex;
		float4 _FaceColor;
		float _FaceDilate;

		float4 _OutlineColor;
		float _OutlineWidth;
		float _OutlineSoftness;

		float4 _UnderlayColor;
		float _UnderlayOffsetX;
		float _UnderlayOffsetY;
		float _UnderlayDilate;
		float _UnderlaySoftness;

		float _WeightNormal;
		float _WeightBold;

		float _ShaderFlags;
		float _ScaleRatioA;
		float _ScaleRatioB;
		float _ScaleRatioC;

		float _TextureWidth;
		float _TextureHeight;
		float _GradientScale;
		float _ScaleX;
		float _ScaleY;
		float _PerspectiveFilter;
		float _Sharpness;

		float _VertexOffsetX;
		float _VertexOffsetY;

		float4 _ClipRect;
		float _MaskSoftnessX;
		float _MaskSoftnessY;

		float _UIMaskSoftnessX;
		float _UIMaskSoftnessY;
		int _UIVertexColorAlwaysGammaSpace;

		struct appv {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			fixed4 color : COLOR;
			float4 texcoord0 : TEXCOORD0;
			float2 texcoord1 : TEXCOORD1;
		};

		struct v2p {
			float4 vertex : SV_POSITION;
			fixed4 faceColor : COLOR;
			fixed4 outlineColor : COLOR1;
			float4 texcoord0 : TEXCOORD0;
			half4 param : TEXCOORD1;
			half4 mask : TEXCOORD2;
			#if (defined(UNDERLAY_ON) || defined(UNDERLAY_INNER))
			float4 texcoord1 : TEXCOORD3;
			half2 underlayParam : TEXCOORD4;
			#endif
		};

		v2p VertShader(appv IN)
		{
			v2p OUT;
			OUT.vertex = UnityObjectToClipPos(IN.vertex);

			float bold = step(IN.texcoord0.w, 0);

			float4 vert = IN.vertex;
			vert.x += _VertexOffsetX;
			vert.y += _VertexOffsetY;
			float4 vPosition = UnityObjectToClipPos(vert);
			OUT.vertex = vPosition;

			float2 pixelSize = vPosition.w;
			pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

			float scale = rsqrt(dot(pixelSize, pixelSize));
			scale *= abs(IN.texcoord0.w) * _GradientScale * (_Sharpness + 1);
			if (UNITY_MATRIX_P[3][3] == 0)
				scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(IN.normal), normalize(WorldSpaceViewDir(vert)))));

			float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
			weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

			float layerScale = scale;

			scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
			float bias = (0.5 - weight) * scale - 0.5;
			float outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;

			if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
			{
				IN.color.rgb = UIGammaToLinear(IN.color.rgb);
			}
			float opacity = IN.color.a;
			#if (defined(UNDERLAY_ON) || defined(UNDERLAY_INNER))
				opacity = 1.0;
			#endif

			fixed4 faceColor = fixed4(IN.color.rgb, opacity) * _FaceColor;
			faceColor.rgb *= faceColor.a;

			fixed4 outlineColor = _OutlineColor;
			outlineColor.a *= opacity;
			outlineColor.rgb *= outlineColor.a;
			outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));

			#if (defined(UNDERLAY_ON) || defined(UNDERLAY_INNER))
			layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
			float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

			float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
			float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
			float2 layerOffset = float2(x, y);
			#endif

			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

			OUT.faceColor = faceColor;
			OUT.outlineColor = outlineColor;
			OUT.texcoord0 = float4(IN.texcoord0.x, IN.texcoord0.y, maskUV.x, maskUV.y);
			OUT.param = half4(scale, bias - outline, bias + outline, bias);
			const half2 maskSoftness = half2(max(_UIMaskSoftnessX, _MaskSoftnessX), max(_UIMaskSoftnessY, _MaskSoftnessY));
			OUT.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * maskSoftness + pixelSize.xy));

			#if (defined(UNDERLAY_ON) || defined(UNDERLAY_INNER))
			OUT.texcoord1 = float4(IN.texcoord0.xy + layerOffset, IN.color.a, 0);
			OUT.underlayParam = half2(layerScale, layerBias);
			#endif

			return OUT;
		}

		fixed4 PixShader(v2p IN) : SV_Target
		{
			half d = tex2D(_MainTex, IN.texcoord0.xy).a * IN.param.x;
			half4 c = IN.faceColor * saturate(d - IN.param.w);

			#ifdef OUTLINE_ON
				c = lerp(IN.outlineColor, IN.faceColor, saturate(d - IN.param.z));
				c *= saturate(d - IN.param.y);
			#endif

			#if defined(UNDERLAY_ON)
				d = tex2D(_MainTex, IN.texcoord1.xy).a * IN.underlayParam.x;
				c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * saturate(d - IN.underlayParam.y) * (1 - c.a);
			#endif

			#if defined(UNDERLAY_INNER)
				half sd = saturate(d - IN.param.z);
				d = tex2D(_MainTex, IN.texcoord1.xy).a * IN.underlayParam.x;
				c += float4(_UnderlayColor.rgb * _UnderlayColor.a, _UnderlayColor.a) * (1 - saturate(d - IN.underlayParam.y)) * sd * (1 - c.a);
			#endif

			#if UNITY_UI_CLIP_RECT
				half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
				c *= m.x * m.y;
			#endif

			#if (defined(UNDERLAY_ON) || defined(UNDERLAY_INNER))
				c *= IN.texcoord1.z;
			#endif

			#if UNITY_UI_ALPHACLIP
				clip(c.a - 0.001);
			#endif

			return c;
		}
		ENDCG
	}
}

CustomEditor "TMPro.EditorUtilities.TMP_SDFShaderGUI"
}
