using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

public class ChunkSaver
{
    private Dictionary<Vector3Int, ChunkSave> chunks;

    public ChunkSaver()
    {
        chunks = new Dictionary<Vector3Int, ChunkSave>();
    }

    ~ChunkSaver() =>
        SaveAllAndUnload();

    public ChunkSave FindOrCreateChunk(Vector3Int chunkPos)
    {
        ChunkSave chunk;
        if (!chunks.TryGetValue(chunkPos, out chunk))
        {
            chunk = new ChunkSave();
            chunks.Add(chunkPos, chunk);
        }
        return chunk;
    }

    public ChunkSave TryFindChunk(Vector3Int chunkPos)
    {
        ChunkSave chunk;
        chunks.TryGetValue(chunkPos, out chunk);
        return chunk;
    }

    public bool TryFindChunk(Vector3Int chunkPos, out ChunkSave chunk)
    {
        return chunks.TryGetValue(chunkPos, out chunk);
    }

    public void SaveAllAndUnload()
    {
        foreach (var chunk in chunks.Values)
            chunk.Dispose();

        chunks.Clear();
    }
}

public class ChunkSave
{
    public NativeArray<float> noiseMap;
    public NativeArray<uint> groundTypeMap;
    public bool initialized = false;

    public ChunkSave()
    {
        noiseMap = new NativeArray<float>(Utility.Cube(Chunk.noiseMapWidth), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        groundTypeMap = new NativeArray<uint>(Utility.Cube(Chunk.noiseMapWidth), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }

    ~ChunkSave() =>
        Dispose();

    public void Dispose()
    {
        noiseMap.Dispose();
        groundTypeMap.Dispose();
    }
        
    public ChunkSave(ChunkSave previous)
    {
        Debug.LogError("ChunkSave copied. Not allowed");
    }
}