// This shader fills the mesh shape with a color predefined in the code.
Shader "NeroWeNeed/Terrain/Basic Terrain Material"
{
    // The properties block of the Unity shader. In this example this block is empty
    // because the output color is predefined in the fragment shader code.
    Properties
    { 

    }

    // The SubShader block containing the Shader code. 
    SubShader
    {
        // SubShader Tags define when and under which conditions a SubShader block or
        // a pass is executed.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" }

        Pass
        {
            // The HLSL code block. Unity SRP uses the HLSL language.
            HLSLPROGRAM
            // This line defines the name of the vertex shader. 
            #pragma vertex vert
            // This line defines the name of the fragment shader. 
            #pragma fragment frag
            #pragma editor_sync_compilation
            #pragma instancing_options assumeuniformscaling
            #pragma target 4.5

            // The Core.hlsl file contains definitions of frequently used HLSL
            // macros and functions, and also contains #include references to other
            // HLSL files (for example, Common.hlsl, SpaceTransforms.hlsl, etc.).
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"            
            StructuredBuffer<float3> Vertices;
            StructuredBuffer<float3> Normals;
            StructuredBuffer<uint> Indices;         

            // The structure definition defines which variables it contains.
            // This example uses the Attributes structure as an input structure in
            // the vertex shader.


            struct Varyings
            {
                // The positions in this struct must have the SV_POSITION semantic.
                float4 position  : SV_POSITION;
                float4 normal : NORMAL;
                float4 tangent : TANGENT;
            };            

            // The vertex shader definition with properties defined in the Varyings 
            // structure. The type of the vert function must match the type (struct)
            // that it returns.
            Varyings vert(uint vertexId : SV_VertexID)
            {
                // Declaring the output object (OUT) with the Varyings struct.
                Varyings OUT;
                // The TransformObjectToHClip function transforms vertex positions
                // from object space to homogenous space

                OUT.position = TransformObjectToHClip(Vertices[vertexId]);
                OUT.normal = float4(TransformObjectToWorldNormal(Normals[vertexId]),1);
                float3 v1 = cross(OUT.normal.xyz,float3(0,1,0));
                float3 v2 = cross(OUT.normal.xyz,float3(0,0,1));
                OUT.tangent = float4(TransformObjectToWorldDir(normalize(length(v1) > length(v2) ? v1 : v2)),1);
                //OUT.positionHCS = float4(Vertices[vertexId],1);
                // Returning the output.
                return OUT;
            }

            // The fragment shader definition.            
            half4 frag() : SV_Target
            {
                // Defining the color variable and returning it.
                half4 customColor;
                customColor = half4(1, 1, 1, 1);
                return customColor;
            }
            ENDHLSL
        }
    }
}