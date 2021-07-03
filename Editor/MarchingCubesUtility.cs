using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Reactics.Main.Battle.Editor.MarchingCubes
{
    public static class MarchingCubesUtility
    {
        public static List<Mesh> BuildCubeRepresentations(byte[] equivalenceClasses)
        {
            var meshes = new List<Mesh>();
            for (int i = 0; i < equivalenceClasses.Length; i++)
            {
                meshes.Add(new Mesh()
                {
                    name = $"MarchingCubesCase {i:X2}"
                });

            }
            BuildCubeRepresentations(equivalenceClasses, meshes);
            foreach (var mesh in meshes)
            {
                mesh.RecalculateBounds();
            }
            return meshes;
        }
        public static void BuildCubeRepresentations(byte[] equivalenceClasses, List<Mesh> meshes)
        {
            var nativeEquivalenceClasses = new NativeArray<byte>(equivalenceClasses, Allocator.TempJob);
            var md = Mesh.AllocateWritableMeshData(equivalenceClasses.Length);
            new BuildMCRepresentations
            {
                equivalenceClasses = nativeEquivalenceClasses,
                meshes = md,
                vertexAttributeDescriptor = VertexDescription.GetVertexAttributeDescriptors(),
                cornerSizeMultiplier = 0.1f,
                activeColor = Color.green,
                inactiveColor = Color.white
            }.Schedule(equivalenceClasses.Length, 1).Complete();
            Mesh.ApplyAndDisposeWritableMeshData(md, meshes);
        }
        [BurstCompile]
        internal struct BuildMCRepresentations : IJobParallelFor
        {
            [DeallocateOnJobCompletion]
            public NativeArray<byte> equivalenceClasses;
            public Mesh.MeshDataArray meshes;
            [DeallocateOnJobCompletion]
            public NativeArray<VertexAttributeDescriptor> vertexAttributeDescriptor;
            public float cornerSizeMultiplier;
            public Color activeColor;
            public Color inactiveColor;

            public void Execute(int index)
            {
                var mesh = meshes[index];
                mesh.SetVertexBufferParams(64, vertexAttributeDescriptor);
                mesh.SetIndexBufferParams(288, IndexFormat.UInt16);
                mesh.subMeshCount = 1;

                var vertices = mesh.GetVertexData<VertexDescription>();
                var indices = mesh.GetIndexData<ushort>();
                var equivalenceClass = equivalenceClasses[index];
                for (int i = 0; i < 8; i++)
                {
                    var offsetA = MarchingCubesUtility.MarchingCube.DefaultCube[i];
                    var color = (equivalenceClass & (1 << i)) != 0 ? activeColor : inactiveColor;
                    for (int j = 0; j < 8; j++)
                    {
                        var offsetB = MarchingCubesUtility.MarchingCube.DefaultCube[j];
                        vertices[i * 8 + j] = new VertexDescription
                        {
                            position = offsetA + (offsetB * cornerSizeMultiplier),
                            normal = math.normalize(offsetB),
                            color = color
                        };
                        if (index == 0)
                        {
                            Debug.Log($"{i},{j}: {math.normalize(offsetB)}");
                        }
                    }
                    FillQuadIndices(indices, i * 36, i * 8, 0, 0, 1, 4, 5);
                    FillQuadIndices(indices, i * 36, i * 8, 1, 2, 0, 6, 4);
                    FillQuadIndices(indices, i * 36, i * 8, 5, 3, 2, 7, 6);
                    FillQuadIndices(indices, i * 36, i * 8, 2, 1, 3, 5, 7);
                    FillQuadIndices(indices, i * 36, i * 8, 3, 4, 5, 6, 7);
                    FillQuadIndices(indices, i * 36, i * 8, 4, 2, 3, 0, 1);
                }
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, 288, MeshTopology.Triangles)
                {
                    bounds = new Bounds(Vector3.zero, new Vector3(0.5f + (0.5f * cornerSizeMultiplier), 0.5f + (0.5f * cornerSizeMultiplier), 0.5f + (0.5f * cornerSizeMultiplier)))
                });

            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void FillQuadIndices(NativeArray<ushort> indices, int indexOffset, int vertexOffset, int quadPosition, ushort vertexBL, ushort vertexBR, ushort vertexTL, ushort vertexTR)
            {
                /*                 indices[indexOffset + (quadPosition * 6)] = (ushort)(vertexOffset + vertexBL);
                                indices[indexOffset + 1 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexTL);
                                indices[indexOffset + 2 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexBR);
                                indices[indexOffset + 3 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexBR);
                                indices[indexOffset + 4 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexTL);
                                indices[indexOffset + 5 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexTR); */
                indices[indexOffset + (quadPosition * 6)] = (ushort)(vertexOffset + vertexBL);
                indices[indexOffset + 1 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexBR);
                indices[indexOffset + 2 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexTL);
                indices[indexOffset + 3 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexTL);
                indices[indexOffset + 4 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexBR);
                indices[indexOffset + 5 + (quadPosition * 6)] = (ushort)(vertexOffset + vertexTR);

            }
        }
        internal struct VertexDescription
        {

            public static NativeArray<VertexAttributeDescriptor> GetVertexAttributeDescriptors(Allocator allocator = Allocator.TempJob)
            {
                var r = new NativeArray<VertexAttributeDescriptor>(3, allocator, NativeArrayOptions.UninitializedMemory);
                r[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
                r[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);
                r[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4);
                return r;
            }
            public float3 position;
            public float3 normal;
            public Color color;

        }
        public static void CountCases(TransformationFlags flags,out Result cases)
        {
            cases = new Result
            {
                equivalenceClasses = new List<byte>(),
                cases = new EquivalenceClassReference[256]
            };
            
            
            for (ushort i = 0; i < 256; i++)
            {
                if (!Transform((byte)i, flags, cases.equivalenceClasses, out var reference))
                {
                    reference = new EquivalenceClassReference
                    {
                        index = cases.equivalenceClasses.Count
                    };
                    cases.equivalenceClasses.Add((byte)i);
                }
                cases.cases[i] = reference;
            }
            
        }
        private static bool Transform(byte activeVertices, TransformationFlags flags, List<byte> equivalenceClasses, out EquivalenceClassReference equivalenceClassReference)
        {
            var data = new TransformData();
            var index = -1;
            TransformInvert(activeVertices, flags, ref data, equivalenceClasses, ref index);
            if (index >= 0)
            {
                equivalenceClassReference = new EquivalenceClassReference
                {
                    index = index,
                    transforms = data
                };
                return true;
            }
            else
            {
                equivalenceClassReference = default;
                return false;
            }
        }
        private static void TransformInvert(byte activeVertices, TransformationFlags flags, ref TransformData transformData, List<byte> equivalenceClasses, ref int index)
        {
            transformData.invert = false;
            TransformXRotation(activeVertices, flags, ref transformData, equivalenceClasses, ref index);
            if ((flags & TransformationFlags.Invertable) != 0 && index < 0)
            {
                transformData.invert = true;
                TransformXRotation(activeVertices, flags, ref transformData, equivalenceClasses, ref index);
            }
        }
        private static void TransformXRotation(byte activeVertices, TransformationFlags flags, ref TransformData transformData, List<byte> equivalenceClasses, ref int index)
        {
            int rotations = (flags & TransformationFlags.XRotatable) != 0 ? 4 : 1;
            for (int i = 0; i < rotations && index < 0; i++)
            {
                transformData.xRotations = i;
                TransformYRotation(activeVertices, flags, ref transformData, equivalenceClasses, ref index);
            }
        }
        private static void TransformYRotation(byte activeVertices, TransformationFlags flags, ref TransformData transformData, List<byte> equivalenceClasses, ref int index)
        {
            int rotations = (flags & TransformationFlags.YRotatable) != 0 ? 4 : 1;
            for (int i = 0; i < rotations && index < 0; i++)
            {
                transformData.yRotations = i;
                TransformZRotation(activeVertices, flags, ref transformData, equivalenceClasses, ref index);
            }
        }
        private static void TransformZRotation(byte activeVertices, TransformationFlags flags, ref TransformData transformData, List<byte> equivalenceClasses, ref int index)
        {
            int rotations = (flags & TransformationFlags.ZRotatable) != 0 ? 4 : 1;
            for (int i = 0; i < rotations && index < 0; i++)
            {
                transformData.zRotations = i;
                CompareCubes(activeVertices, ref transformData, equivalenceClasses, ref index);
            }
        }
        private static void CompareCubes(byte activeVertices, ref TransformData transformData, List<byte> equivalenceClasses, ref int index)
        {
            var marchingCube = new MarchingCube(activeVertices, transformData);
            index = equivalenceClasses.IndexOf(marchingCube.activeVertices);
        }
        [Serializable]
        [Flags]
        public enum TransformationFlags
        {
            None = 0,
            XRotatable = 1,
            YRotatable = 2,
            ZRotatable = 4,
            Invertable = 8,
            [HideInInspector]
            HorizontalRotationOnly = XRotatable | ZRotatable,
            [HideInInspector]
            RotationOnly = XRotatable | YRotatable | ZRotatable,
            [HideInInspector]
            All = XRotatable | YRotatable | ZRotatable | Invertable
        }
        [Serializable]
        public struct TransformRotation : IEquatable<TransformRotation> {
            [SerializeField]
            internal int count;
            public int Count { get => count; set => count = value % 4; }
            public TransformRotation(int count)
            {
                this.count = count % 4;
            }

            public bool Equals(TransformRotation other)
            {
                return count == other.count;
            }

            public override int GetHashCode()
            {
                return 1110609940 + count.GetHashCode();
            }
            public static implicit operator int(TransformRotation rotation) => rotation.count;
            public static implicit operator TransformRotation(int count) => new TransformRotation {
                count = count % 4
            };
        }
        [Serializable]
        public struct TransformData
        {
            public TransformRotation xRotations, yRotations, zRotations;
            public bool invert;
            public float4x4 BuildTransformMatrix() {
                return float4x4.TRS(float3.zero, quaternion.EulerXYZ(math.float3(math.radians(90 * xRotations), math.radians(90 * yRotations), math.radians(90 * zRotations))), 1);
            }
        }
        [Serializable]
        public struct EquivalenceClassReference
        {
            public int index;
            public TransformData transforms;
        }
        [Serializable]
        public struct Result {
            public List<byte> equivalenceClasses;
            public EquivalenceClassReference[] cases;
        }
        public unsafe struct Cube
        {
            private static readonly float3x3 RotationMatrixX = math.float3x3(1, 0, 0, 0, 0, -1, 0, 1, 0);
            private static readonly float3x3 RotationMatrixY = math.float3x3(0, 0, 1, 0, 1, 0, -1, 0, 0);
            private static readonly float3x3 RotationMatrixZ = math.float3x3(0, -1, 0, 1, 0, 0, 0, 0, 1);
            public fixed float vertices[24];
            public static Cube Create(float size = 0.5f)
            {
                var c = new Cube();
                c.vertices[0] = -size;
                c.vertices[1] = -size;
                c.vertices[2] = -size;

                c.vertices[3] = size;
                c.vertices[4] = -size;
                c.vertices[5] = -size;

                c.vertices[6] = -size;
                c.vertices[7] = size;
                c.vertices[8] = -size;

                c.vertices[9] = size;
                c.vertices[10] = size;
                c.vertices[11] = -size;

                c.vertices[12] = -size;
                c.vertices[13] = -size;
                c.vertices[14] = size;

                c.vertices[15] = size;
                c.vertices[16] = -size;
                c.vertices[17] = size;

                c.vertices[18] = -size;
                c.vertices[19] = size;
                c.vertices[20] = size;

                c.vertices[21] = size;
                c.vertices[22] = size;
                c.vertices[23] = size;
                return c;
            }
            public float3 this[int index]
            {
                get => math.float3(vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);
                set
                {
                    vertices[index * 3] = value.x;
                    vertices[index * 3 + 1] = value.y;
                    vertices[index * 3 + 2] = value.z;
                }
            }
            public void RotateX(int count)
            {
                for (int i = 0; i < 8; i++)
                {
                    var m = this[i];
                    for (int j = 0; j < count % 4; j++)
                    {
                        m = math.mul(RotationMatrixX, m);
                    }
                    this[i] = m;
                }
            }
            public void RotateY(int count)
            {
                for (int i = 0; i < 8; i++)
                {
                    var m = this[i];
                    for (int j = 0; j < count % 4; j++)
                    {
                        m = math.mul(RotationMatrixY, m);
                    }
                    this[i] = m;
                }
            }
            public void RotateZ(int count)
            {
                for (int i = 0; i < 8; i++)
                {
                    var m = this[i];
                    for (int j = 0; j < count % 4; j++)
                    {
                        m = math.mul(RotationMatrixZ, m);
                    }
                    this[i] = m;
                }
            }
        }
        internal unsafe struct MarchingCube
        {
            internal static readonly Cube DefaultCube = Cube.Create();
            public Cube vertices;
            public byte activeVertices;
            public MarchingCube(byte activeVertices, TransformData transformData)
            {
                this.activeVertices = activeVertices;
                this.vertices = DefaultCube;
                ApplyTransform(transformData);
                UpdateActiveVertices();
            }
            private void ApplyTransform(TransformData transformData)
            {
                vertices.RotateX(transformData.xRotations);
                vertices.RotateY(transformData.yRotations);
                vertices.RotateZ(transformData.zRotations);
                if (transformData.invert)
                {
                    activeVertices = (byte)~activeVertices;
                }
            }
            private void UpdateActiveVertices()
            {
                byte newActiveVertices = 0;
                for (int i = 0; i < 8; i++)
                {
                    if ((activeVertices & (1 << i)) != 0)
                    {
                        float3 vertex = vertices[i];
                        for (int j = 0; j < 8; j++)
                        {
                            if (DefaultCube[j].Equals(vertex))
                            {
                                newActiveVertices |= (byte)(1 << j);
                            }
                        }
                    }
                }
                this.activeVertices = newActiveVertices;
            }
        }
    }
}