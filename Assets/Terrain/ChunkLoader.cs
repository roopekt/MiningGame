using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Assertions;

public class ChunkLoader : MonoBehaviour
{
    [SerializeField] private GameObject ChunkPrefab;
    [SerializeField] private Transform Target;
    [SerializeField] private int RenderDistance = 4;
    [SerializeField] private float ChunkWidth = 8f;
    [SerializeField] private Biome.Biome Biome;
    [Header("Debug")]
    [Tooltip("Draw borders around closest chunk?")] 
    [SerializeField] private bool DrawChunkBorders = false;
    [SerializeField] private Transform BorderCube;//see above

    //readonly
    float chunkWidthInverse;
    Vector3Int centerOfset;//of set between chunkMap[0, 0, 0] and chunkMap's center (positive)
    Vector3Int[] spiral;//indeces to chunkMap sorted from nearest to farthest
    uint chunkCount;//length of spiral, chunkPool and sortedChunks

    Chunk[] chunkPool;//all Chunks
    uint[] sortedChunks;//indeces to chunkPool sorted from nearest to farthest
    uint[,,] chunkMap;//indeces to chunkPool. uint.MaxValue means no chunk
    Vector3Int targetPosInChunks;//target's position as chunks
    uint nextChunkToMove;//next chunk to move. index to sortedChunks
    uint nextHoleToFill;//next hole to fill. index to spiral
    bool chunksToMove = false;

    ChunkSaver saver;

    void Start()
    {
        //init simple constants
        chunkWidthInverse = 1 / ChunkWidth;
        centerOfset = new Vector3Int(RenderDistance, RenderDistance, RenderDistance);
        saver = new ChunkSaver();

        //simple init
        Biome.LocateUniforms();//relocate uniforms
        Chunk.globalBiome = Biome;

        //init chunkMap
        int chunkMapWidth = 2 * RenderDistance + 1;
        chunkMap = new uint[chunkMapWidth, chunkMapWidth, chunkMapWidth];
        for (uint x = 0; x < chunkMap.GetLength(0); x++)
            for (uint y = 0; y < chunkMap.GetLength(1); y++)
                for (uint z = 0; z < chunkMap.GetLength(2); z++)
                    chunkMap[x, y, z] = uint.MaxValue;

        //init spiral
        var spiralList = new List<Vector3Int>();
        var spiralKeys = new List<float>();//keys for sorting
        for (int x = 0; x < chunkMapWidth; x++)//get vectors for spiral
            for (int y = 0; y < chunkMapWidth; y++)
                for (int z = 0; z < chunkMapWidth; z++)
                {
                    Vector3 vec = new Vector3(x, y, z) - Vector3.one * (float)RenderDistance;
                    float key = vec.sqrMagnitude;
                    if (key < .1f + (float)(RenderDistance * RenderDistance))
                    {
                        spiralList.Add(new Vector3Int(x, y, z));
                        spiralKeys.Add(key);
                    }
                }
        spiral = spiralList.ToArray();
        Array.Sort(spiralKeys.ToArray(), spiral);

        chunkCount = (uint)spiral.Length;

        //init chunkPool
        Assert.IsNotNull(ChunkPrefab.GetComponent<Chunk>());
        chunkPool = new Chunk[chunkCount];
        Vector3 posOfset = Vector3.one * RenderDistance * ChunkWidth;//chunks wouldn't be moved if they were too close to Target
        for (uint i = 0; i < chunkCount; i++)
        {
            Chunk chunk = Instantiate(ChunkPrefab, transform.position + posOfset, Quaternion.identity, transform).GetComponent<Chunk>();
            chunk.SetWidth(ChunkWidth); 
            chunkPool[i] = chunk;
        }

        //init sortedChunks
        sortedChunks = new uint[chunkCount];
        for (uint i = 0; i < chunkCount; i++)
            sortedChunks[i] = i;

        OnCrossChunkBorder();

        #region debug tools
        if (DrawChunkBorders)
        {
            BorderCube = Instantiate(BorderCube, new GameObject(transform.name + "_DebugTools").transform);
            BorderCube.localScale = Vector3.one * ChunkWidth;
        }
        #endregion

        //some assertions
        Assert.IsTrue(chunkCount > 0);
        Assert.AreEqual(chunkPool.Length, chunkCount);
        Assert.AreEqual(chunkPool.Length, spiral.Length);
        Assert.AreEqual(chunkPool.Length, sortedChunks.Length);
    }

    void OnDestroy()
    {
        saver.SaveAllAndUnload();
    }

    void Update()
    {
        //update targetChunkPos and call OnCrossChunkBorder if necessary
        Vector3Int newPosInChunks = Vector3Int.FloorToInt(Target.position * chunkWidthInverse);
        if (newPosInChunks != targetPosInChunks)
        {
            targetPosInChunks = newPosInChunks;
            OnCrossChunkBorder();
        }

        //move a chunk, if any to move
        if (chunksToMove)
            MoveChunk();

        #region debug tools
        if (DrawChunkBorders)
            BorderCube.position = ((Vector3)targetPosInChunks + Vector3.one / 2) * ChunkWidth;
        #endregion
    }

    void MoveChunk()
    {
        //find a hole
        Vector3Int holeIndex;
        Vector3Int holeLocalPosInChunks;
        Vector3 holeLocalPos;
        while (true)
        {
            if (nextHoleToFill >= chunkCount)//no holes => no chunks to move
            {
                chunksToMove = false;
                return;
            }

            holeIndex = spiral[nextHoleToFill];
            ++nextHoleToFill;
            
            if (chunkMap[holeIndex.x, holeIndex.y, holeIndex.z] == uint.MaxValue)
            {
                holeLocalPosInChunks = holeIndex - centerOfset;
                holeLocalPos = (Vector3)holeLocalPosInChunks * ChunkWidth;
                break;
            }
            
        }

        Vector3 targetPos = (Vector3)targetPosInChunks * ChunkWidth;
        
        //happens when all chunks have been moved
        if (nextChunkToMove < 0) {
            chunksToMove = false;
            return;
        }

        //chunk to move
        uint movingChunkIndex = sortedChunks[nextChunkToMove];
        Chunk movingChunk = chunkPool[movingChunkIndex];
        Vector3 movingChunkLocalPos = movingChunk.position - targetPos;
        --nextChunkToMove;

        //new hole's chunkMap index
        Vector3Int newHoleLocalPosInChunks = Vector3Int.RoundToInt(movingChunkLocalPos * chunkWidthInverse);
        bool newHoleInChunkMap = newHoleLocalPosInChunks.sqrMagnitude <= RenderDistance * RenderDistance;//false, if chunk was taken from far, outside chunkMap
        Vector3Int newHoleIndex = newHoleLocalPosInChunks + centerOfset;

        //if the hole is farther than the chunk to move
        if (holeLocalPos.sqrMagnitude > movingChunkLocalPos.sqrMagnitude) {
            chunksToMove = false;
            return;
        }

        //move the chunk
        Vector3Int holePosInChunks = holeLocalPosInChunks + targetPosInChunks;
        movingChunk.Generate(holeLocalPos + targetPos, saver.FindOrCreateChunk(holePosInChunks));

        //update chunkMap
        chunkMap[holeIndex.x, holeIndex.y, holeIndex.z] = movingChunkIndex;
        if (newHoleInChunkMap)
            chunkMap[newHoleIndex.x, newHoleIndex.y, newHoleIndex.z] = uint.MaxValue;
    }

    void OnCrossChunkBorder()
    {
        chunksToMove = true;
        nextChunkToMove = chunkCount - 1;
        nextHoleToFill = 0;

        //clear chunkMap
        foreach (var index in spiral)
            chunkMap[index.x, index.y, index.z] = uint.MaxValue;

        //place chunks into chunkMap
        for (uint i = 0; i < chunkCount; ++i)
        {
            Vector3Int pos = Vector3Int.RoundToInt(chunkPool[i].position * chunkWidthInverse) - targetPosInChunks;
            if (pos.sqrMagnitude <= RenderDistance * RenderDistance)
            {
                pos += centerOfset;
                chunkMap[pos.x, pos.y, pos.z] = i;
            }
        }

        //sort sortedChunks
        var keys = new float[chunkCount];
        Vector3 targetPos = (Vector3)targetPosInChunks * ChunkWidth;
        for (int i = 0; i < chunkCount; ++i)
            keys[i] = (chunkPool[sortedChunks[i]].transform.position - targetPos).sqrMagnitude;
        Array.Sort(keys, sortedChunks);
    }

    void AddSphere(Vector3 sphereWorldPos, float sphereRadius, float delta, uint placingItemTypeId, out float amountPlaced, out MiningLoot loot)
    {
        Bounds AABB = new Bounds();
        AABB.SetMinMax(Vector3.zero, new Vector3(ChunkWidth, ChunkWidth, ChunkWidth));

        Vector3Int centerChunkIndex = Vector3Int.FloorToInt(sphereWorldPos * chunkWidthInverse) - targetPosInChunks + centerOfset;

        amountPlaced = 0f;
        loot = new MiningLoot();

        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    //get chunk
                    Vector3Int ofset = new Vector3Int(x, y, z);
                    Vector3Int chunkMapIndex = centerChunkIndex + ofset;
                    uint chunkIndex = chunkMap[chunkMapIndex.x, chunkMapIndex.y, chunkMapIndex.z];
                    if (chunkIndex == uint.MaxValue)//is there a loaded chunk here
                    {
                        Debug.LogWarning("Can't add a sphere to a chunk, because it's missing.");
                        continue;
                    }
                    Chunk chunk = chunkPool[chunkIndex];

                    if (AABB.SqrDistance(sphereWorldPos - chunk.position) < sphereRadius * sphereRadius)//if the sphere touches this chunk
                    {
                        //add sphere to the chunk
                        chunk.AddSphere(sphereWorldPos, sphereRadius, delta, placingItemTypeId, out amountPlaced, out loot);
                    }
                }
    }

    //mines a sphere
    public void MineSphere(Vector3 sphereWorldPos, float sphereRadius, float delta, out MiningLoot miningLoot)
    {
        #if DEBUG
        if (delta > 0f)
            Debug.LogWarning("ChunkLoader.MineSphere: positive delta");
        #endif

        AddSphere(sphereWorldPos, sphereRadius, delta, 0, out _, out miningLoot);
    }

    //mines a sphere. returns stuff mined as MiningLoot
    public MiningLoot MineSphere(Vector3 sphereWorldPos, float sphereRadius, float delta)
    {
        MiningLoot loot;
        MineSphere(sphereWorldPos, sphereRadius, delta, out loot);
        return loot;
    }

    //places a sphere. returns amount placed
    public float PlaceSphere(Vector3 sphereWorldPos, float sphereRadius, float delta, uint itemTypeId)
    {
        #if DEBUG
        if (delta < 0f)
            Debug.LogWarning("ChunkLoader.PlaceSphere: negative delta");
        #endif

        float amountPlaced;
        AddSphere(sphereWorldPos, sphereRadius, delta, itemTypeId, out amountPlaced, out _);
        return amountPlaced;
    }

    void Reset()
    {
        Target = Camera.main.transform;
    }
}