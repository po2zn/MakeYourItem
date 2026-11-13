Shader "Forge/Frost Hover Outline" {
 Properties { _Color("Color",Color)=(1,1,0,1) _Width("Width",Float)=0.018 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Cull Front ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
 struct Varyings { float4 positionCS:SV_POSITION; };
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;float _Width;
 CBUFFER_END
 Varyings vert(Attributes input){Varyings output;float3 world=TransformObjectToWorld(input.positionOS.xyz)+TransformObjectToWorldNormal(input.normalOS)*_Width;output.positionCS=TransformWorldToHClip(world);return output;}
 half4 frag(Varyings input):SV_Target{return _Color;}
 ENDHLSL
 } }
}
