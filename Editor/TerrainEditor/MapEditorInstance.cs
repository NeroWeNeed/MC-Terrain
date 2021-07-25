using System;
using System.IO;
using NeroWeNeed.Commons.Editor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Physics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeroWeNeed.Terrain.Editor
{
    public sealed class MapEditorInstance : IDisposable
    {
        private int4 span;
        private Vector3[] boundPoints;
        private bool showBounds = true;
        private NativeArray<MapAssetData.Chunk> chunks;
        private BlobAssetReference<Unity.Physics.MeshCollider> meshCollider;
        private UnityEngine.Plane basePlane = new UnityEngine.Plane(Vector3.up, Vector3.zero);
        public int4 Span
        {
            get => span; set
            {

                if (span.Equals(value))
                {
                    return;
                }
                var newChunks = new NativeArray<MapAssetData.Chunk>((value.z - value.x) * (value.w - value.y), Allocator.Persistent, NativeArrayOptions.ClearMemory);

                //TODO: Copy rows instead of cells.
                for (int y = value.y; y < value.w; y++)
                {
                    for (int x = value.x; x < value.z; x++)
                    {
                        if (x >= span.x && x < span.z && y >= span.y && y < span.w)
                        {
                            newChunks[((y - value.y) * (value.z - value.x)) + (x - value.x)] = chunks[((y - span.y) * (span.z - span.x)) + (x - span.x)];

                        }
                    }
                }
                chunks.Dispose();
                chunks = newChunks;
                boundPoints = new Vector3[] {
                    new Vector3(value.x,0,value.y)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(value.x,0,value.w)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(value.z,0,value.w)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(value.z,0,value.y)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(value.x,0,value.y)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                };
                span = value;
            }
        }

        public bool ShowBounds { get => showBounds; set => showBounds = value; }
        private int controlId;
        public unsafe MapEditorInstance(UnityEditor.Editor editor, string path)
        {
            using var fs = new StreamBinaryReader(path);
            controlId = editor.GetInstanceID();
            int4 span = new int4(
                fs.ReadInt(), fs.ReadInt(), fs.ReadInt(), fs.ReadInt()
            );
            var chunks = new NativeArray<MapAssetData.Chunk>((span.z - span.x) * (span.w - span.y), Allocator.Persistent, NativeArrayOptions.ClearMemory);
            var buffer = UnsafeUtility.Malloc(2048, 4, Allocator.Temp);
            for (int y = span.y; y < span.w; y++)
            {
                for (int x = span.x; x < span.z; x++)
                {
                    if (x >= span.x && x < span.z && y >= span.y && y < span.w)
                    {
                        fs.ReadBytes(buffer, sizeof(MapAssetData.Chunk));
                        MapAssetData.Chunk chunk = default;
                        UnsafeUtility.MemCpy(&chunk, buffer, sizeof(MapAssetData.Chunk));
                        //fs.ReadInto(ref chunk);
                        chunks[((y - span.y) * (span.z - span.x)) + (x - span.x)] = chunk;
                        //meshColliders[((y - span.y) * (span.z - span.x)) + (x - span.x)] = meshColliders[((y - this.span.y) * (this.span.z - this.span.x)) + (x - this.span.x)];
                    }
                }
            }
            this.chunks = chunks;
            this.span = span;
            boundPoints = new Vector3[] {
                    new Vector3(span.x,0,span.y)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(span.x,0,span.w)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(span.z,0,span.w)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(span.z,0,span.y)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                    new Vector3(span.x,0,span.y)*MarchingCubes.PaddedChunkHorizontalSpanInCells,
                };
        }

        public void OnSceneGUI(SceneView sceneView)
        {
            if (ShowBounds)
            {
                Handles.DrawPolyLine(boundPoints);
            }
            var current = Event.current;
            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(controlId, FocusType.Passive));
            if (RayCastToPoint(HandleUtility.GUIPointToWorldRay(current.mousePosition), out var output))
            {
                Handles.DrawWireCube(output.GetPosition(), Vector3.one*0.1f);
            }
        }
        private bool RayCastToPoint(UnityEngine.Ray ray, out RayOutput output)
        {
            if (meshCollider.IsCreated)
            {
                output = default;
                return false;
            }
            else
            {
                if (basePlane.Raycast(ray, out float dist))
                {
                    float3 rayPoint = ray.GetPoint(dist);
                    int3 point = (int3)math.round(rayPoint);

                    output = new RayOutput
                    {
                        chunk = new int2((point.x < 0 ? point.x - MarchingCubes.ChunkHorizontalSpanInCells : point.x) / MarchingCubes.ChunkHorizontalSpanInCells, (point.z < 0 ? point.z - MarchingCubes.ChunkHorizontalSpanInCells : point.z) / MarchingCubes.ChunkHorizontalSpanInCells),
                        point = new int3(point.x < 0 ? (point.x % MarchingCubes.ChunkHorizontalSpanInCells) + MarchingCubes.ChunkHorizontalSpanInCells : point.x % MarchingCubes.ChunkHorizontalSpanInCells, math.clamp(point.y, 0, MarchingCubes.ChunkVerticalSpanInCells), point.z < 0 ? (point.z % MarchingCubes.ChunkHorizontalSpanInCells) + MarchingCubes.ChunkHorizontalSpanInCells : (point.z % MarchingCubes.ChunkHorizontalSpanInCells))
                    };

                    return true;
                }
                else
                {
                    output = default;
                    return false;
                }
            }
        }
        public void Serialize(string path)
        {
            using (var writer = new StreamBinaryWriter(path))
            {
                Debug.Log(path);
                Debug.Log(span);
                writer.Write(span.x);
                writer.Write(span.y);
                writer.Write(span.z);
                writer.Write(span.w);
                //writer.WriteFrom(ref span);
                writer.WriteArray(chunks);

            }
            /*             AssetDatabase.SaveAssets();
                        AssetDatabase.ImportAsset(path); */

        }

        public void Dispose()
        {
            chunks.Dispose();
        }
    }
    public struct RayOutput
    {
        public int2 chunk;
        public int3 point;
        public float3 GetPosition() => new float3(
            (chunk.x * MarchingCubes.ChunkHorizontalSpanInCells) + point.x,
            point.y,
            (chunk.y * MarchingCubes.ChunkHorizontalSpanInCells) + point.z);
    }
}