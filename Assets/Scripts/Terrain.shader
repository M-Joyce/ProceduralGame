Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture",2D) = "white"{}
        testScale("Scale",Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8; //max Color count, used for array sizes
        const static float epsilon = 1E-4; //very small number

        int layerCount;
        float3 baseColors[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColorStrength[maxLayerCount];
        float baseTextureScales[maxLayerCount];

        float minHeight;
        float maxHeight;

        sampler2D testTexture;
        float testScale;

        UNITY_DECLARE_TEX2DARRAY(baseTextures); //2D Texture Array Declaration
        

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float InverseLerp(float a, float b, float value) {
            return saturate((value - a) / (b - a)); //saturate will clamp this within 0-1
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
            //Tri-planar mapping of texture
            float3 scaledWorldPos = worldPos / scale;

            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float heightPercent = InverseLerp(minHeight, maxHeight, IN.worldPos.y);
            
            float3 blendAxes = abs(IN.worldNormal); //for Tri-planar mapping
            blendAxes = (blendAxes / (blendAxes.x + blendAxes.y + blendAxes.z)); //ensure value doesnt exceed 1 for preserving brightness

            for (int i = 0; i < layerCount; i++) {
                float drawStrength = InverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);
                float3 baseColor = baseColors[i] * baseColorStrength[i];
                float3 textureColor = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColorStrength[i]);

                o.Albedo = o.Albedo * (1-drawStrength) + (baseColor + textureColor) * drawStrength;
            }


            

        }
        ENDCG
    }
    FallBack "Diffuse"
}
