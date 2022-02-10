#define SMOOTH
#define COLLISION

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Linq;
using Unity.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
#if COLLISION
    [RequireComponent(typeof(MeshCollider))]
#endif
public class Chunk : MonoBehaviour
{
    #region constants
    public const int noiseMapWidth = 16;//same contant is in MarchingCubes.compute
    static readonly Vector3Int noiseMapDims = Vector3Int.one * noiseMapWidth;
    const uint nullIndex = 0xFFFFFFFF;//same contant is in MarchingCubes.compute

    static readonly int noiseMapSize = Utility.Cube(noiseMapWidth);
    static readonly int vertexMapSize = Utility.Cube(2 * noiseMapWidth - 1) / 2;
    static readonly int indexMapSize = 3 * 5 * Utility.Cube(noiseMapWidth - 1);
    static readonly int groundTypeMapSize = noiseMapSize;

    static readonly int groupCountXGen2DNoise = Utility.DivIntCeil(noiseMapWidth, 8);
    static readonly int groupCountXGen3DNoise = Utility.DivIntCeil(noiseMapWidth, 4);
    static readonly int groupCountPlaceVertices = Utility.DivIntCeil(vertexMapSize, 64);
    static readonly int groupCountXTriangulate = Utility.DivIntCeil(noiseMapWidth - 1, 4);
    static readonly int groupCountXGenGroundTypes = Utility.DivIntCeil(noiseMapWidth, 4);
    #endregion

    //inspector
    [Tooltip("MarchingCubes.compute")]
    [SerializeField] private ComputeShader CompShader;

    #region shader stuff
    static RenderTexture noiseMap2D;//IO buffers
    static ComputeBuffer noiseMap3D;
    static ComputeBuffer vertexMap;
    static ComputeBuffer indexMap;
    static ComputeBuffer groundTypeMap;
    static int kernelGen2DNoise;//kernel id
    static int kernelGen3DNoise;
    static int kernelPlaceVertices;
    static int kernelTriangulate;
    static int kernelGenGroundTypes;
    //static int kernelAddSphere;
    static bool shaderSetup = false;
    static int u_positionId = Shader.PropertyToID("u_position");//name id of u_position uniform
    //static int u_terraform_spherePosId = Shader.PropertyToID("u_terraform_spherePos");
    //static int u_terraform_sphereRadiusId = Shader.PropertyToID("u_terraform_sphereRadius");
    //static int u_terraform_deltaId = Shader.PropertyToID("u_terraform_delta");
    #endregion

    public static Biome.Biome globalBiome;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    float cellWidth;
    float cellWidthInverse;
    ChunkSave chunkSave;

    public Vector3 position { get => transform.position; }
    public void SetWidth(float width) {
        cellWidth = width / (noiseMapWidth - 1 - 2/* -2: connects chunks when boudaries are cut out*/);
        cellWidthInverse = 1f / cellWidth;
        CompShader.SetFloat("u_cellWidth", cellWidth);//applies for every chunk
    }


        
    void Awake()
    {
        //don't setup if not active
        if (!gameObject.activeInHierarchy)
            return;

        SetupShader(CompShader);

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshCollider = GetComponent<MeshCollider>();
    }
    
    static void SetupShader(in ComputeShader compShader)
    {
        //only setup shader once
        if (shaderSetup)
            return;
        shaderSetup = true;

        //upload biome
        globalBiome.Upload(compShader);
        
        //assertions and other errors
        if (!SystemInfo.supportsComputeShaders)
            Debug.LogError("Compute shaders not supported.");
        if (!SystemInfo.supportsAsyncGPUReadback)
            Debug.LogError("AsyncGPUReadback not supported.");
        unsafe {
            Assert.AreEqual(sizeof(Vector4), 4 * sizeof(float));
        }

        //find kernels from source code
        kernelGen2DNoise = compShader.FindKernel("Gen2DNoise");
        kernelGen3DNoise = compShader.FindKernel("Gen3DNoise");
        kernelPlaceVertices = compShader.FindKernel("PlaceVertices");
        kernelTriangulate = compShader.FindKernel("Triangulate");
        kernelGenGroundTypes = compShader.FindKernel("GenGroundTypes");
        //kernelAddSphere = compShader.FindKernel("AddSphere");

        //allocate noiseMap2D on gpu
        var desc = new RenderTextureDescriptor(noiseMapWidth, noiseMapWidth, RenderTextureFormat.RFloat, 0, 1);
        desc.enableRandomWrite = true;
        noiseMap2D = new RenderTexture(desc);

        //allocate other IO buffers
        noiseMap3D = new ComputeBuffer(noiseMapSize, sizeof(float));
        vertexMap = new ComputeBuffer(vertexMapSize, 4 * sizeof(float));
        indexMap = new ComputeBuffer(indexMapSize, sizeof(uint));
        groundTypeMap = new ComputeBuffer(groundTypeMapSize, sizeof(uint));

        #region bind IO buffers
        compShader.SetTexture(kernelGen2DNoise, "NoiseMap2D", noiseMap2D);
        compShader.SetTexture(kernelGen3DNoise, "NoiseMap2D", noiseMap2D);

        compShader.SetBuffer(kernelGen3DNoise, "NoiseMap3D", noiseMap3D);
        compShader.SetBuffer(kernelPlaceVertices, "NoiseMap3D", noiseMap3D);
        compShader.SetBuffer(kernelTriangulate, "NoiseMap3D", noiseMap3D);
        compShader.SetBuffer(kernelGenGroundTypes, "NoiseMap3D", noiseMap3D);
        //compShader.SetBuffer(kernelAddSphere, "NoiseMap3D", noiseMap3D);

        compShader.SetBuffer(kernelPlaceVertices, "VertexMap", vertexMap);
        compShader.SetBuffer(kernelTriangulate, "VertexMap", vertexMap);

        compShader.SetBuffer(kernelTriangulate, "IndexMap", indexMap);

        compShader.SetBuffer(kernelGenGroundTypes, "GroundTypeMap", groundTypeMap);
        #endregion
    }

    void OnDestroy()
    {
        if (noiseMap2D != null) noiseMap2D.Release();
        if (noiseMap3D != null) noiseMap3D.Release();
        if (vertexMap != null) vertexMap.Release();
        if (indexMap != null) indexMap.Release();
        if (groundTypeMap != null) groundTypeMap.Release();
    }

    public void Generate(Vector3 position, ChunkSave chunkSave = null)
    {
        if (chunkSave == null)
            chunkSave = new ChunkSave();
        this.chunkSave = chunkSave;

        transform.position = position;

        //GPU code
        NativeArray<Vector4> hostVertexMap;
        NativeArray<uint> hostIndexMap;
        {
            //set uniforms
            CompShader.SetFloats(u_positionId, new float[] { position.x, position.y, position.z });

            //noise and ground types
            if (chunkSave.initialized)
            {
                //upload noise and ground types from chunkData
                noiseMap3D.SetData(chunkSave.noiseMap);
                groundTypeMap.SetData(chunkSave.groundTypeMap);
            }
            else//compute noise and ground types
            {
                //compute noise
                CompShader.Dispatch(kernelGen2DNoise, groupCountXGen2DNoise, groupCountXGen2DNoise, 1);
                CompShader.Dispatch(kernelGen3DNoise, groupCountXGen3DNoise, groupCountXGen3DNoise, groupCountXGen3DNoise);

                //compute ground types
                CompShader.Dispatch(kernelGenGroundTypes, groupCountXGenGroundTypes, groupCountXGenGroundTypes, groupCountXGenGroundTypes);

                //download noise and ground types to chunkSave
                var noiseRequest = AsyncGPUReadback.RequestIntoNativeArray(ref chunkSave.noiseMap, noiseMap3D);
                var groundTypeRequest = AsyncGPUReadback.RequestIntoNativeArray(ref chunkSave.groundTypeMap, groundTypeMap);
                noiseRequest.WaitForCompletion();
                groundTypeRequest.WaitForCompletion();
                chunkSave.initialized = true;
            }

            //compute vertices and indeces
            CompShader.Dispatch(kernelPlaceVertices, groupCountPlaceVertices, 1, 1);
            CompShader.Dispatch(kernelTriangulate, groupCountXTriangulate, groupCountXTriangulate, groupCountXTriangulate);

            //request data
            var vertexMapRequest = AsyncGPUReadback.Request(vertexMap);
            var indexMapRequest = AsyncGPUReadback.Request(indexMap);

            //wait for data to be ready
            indexMapRequest.WaitForCompletion();
            vertexMapRequest.WaitForCompletion();

            //dowload data to cpu
            hostVertexMap = vertexMapRequest.GetData<Vector4>();
            hostIndexMap = indexMapRequest.GetData<uint>();
        }

        //get vertices and indeces
        #if SMOOTH
        #region smooth

        //count vertices and indeces. see shader code for how unused vertices and indeces are marked
        int vertexCount = hostVertexMap.Count(v => v.w > .5);
        if (vertexCount < 1)//if no vertices, execution can stop here
        {
            meshRenderer.enabled = false;
            #if COLLISION
                meshCollider.sharedMesh = null;
            #endif
            return;
        }
        int indexCount = hostIndexMap.Count(i => i != nullIndex);

        //final, compacted arrays
        var vertices = new Vector3[vertexCount];
        var indeces = new int[indexCount];

        //newIndeces[<index to hostVertexMap>] == <index to vertices>
        var newIndeces = new int[indexMapSize];

        //compute vertices and newIndeces
        int currVertCount = 0;
        for (int i = 0; i < vertexMapSize; ++i)
        {
            Vector4 vertex = hostVertexMap[i];
            if (vertex.w > .5)//if vertex is used
            {
                vertices[currVertCount] = (Vector3)vertex;
                newIndeces[i] = currVertCount;
                ++currVertCount;
            }
        }

        //compute indeces
        int currIndxCount = 0;
        for (int i = 0; i < indexMapSize; ++i)
        {
            uint index = hostIndexMap[i];
            if (index != nullIndex)
            {
                indeces[currIndxCount] = newIndeces[index];
                ++currIndxCount;
            }
        }

        //vertex colors
        var vertexColors = new Color[vertexCount];
        var itemTypeHandler = Item.ItemTypeHandler.instance;
        for (uint i = 0; i < vertexCount; ++i)
        {
            //get mapPos
            Vector3Int boundaryOfset = Vector3Int.one;
            Vector3 vertexPos = vertices[i] * cellWidthInverse;
            Vector3Int vertexPosRounded = Vector3Int.RoundToInt(vertexPos);
            Vector3Int mapPos = vertexPosRounded + boundaryOfset;
            if (chunkSave.noiseMap[To1DIndex(mapPos, noiseMapDims)] < 0f)//if this point isn't solid
            {
                mapPos += Vector3Int.RoundToInt((vertexPos - (Vector3)vertexPosRounded).normalized);
            }

            //get item type
            uint groundTypeIndex = chunkSave.groundTypeMap[To1DIndex(mapPos, noiseMapDims)];
            Item.ItemType itemType = itemTypeHandler.itemTypes[groundTypeIndex];

            //apply vertex color
            vertexColors[i] = itemType.color;
        }
        #endregion
#else
        #region sharp

        //count indeces
        int indexCount = hostIndexMap.Count(i => i != nullIndex);
        int vertexCount = indexCount;

        if (indexCount < 1)//if no triangels, execution can stop here
        {
            meshRenderer.enabled = false;
#if COLLISION
                meshCollider.sharedMesh = null;
#endif
            return;
        }

        //final, compacted arrays
        var vertices = new Vector3[vertexCount];
        var indeces = new int[indexCount];

        //compute vertices
        int currVertCount = 0;
        for (int i = 0; i < indexMapSize; i++)
        {
            uint index = hostIndexMap[i];
            if (index != nullIndex)
            {
                vertices[currVertCount] = (Vector3)hostVertexMap[(int)index];
                ++currVertCount;
            }
        }

        //compute indeces (0, 1, 2, 3...)
        for (int i = 0; i < indexCount; ++i) {
            indeces[i] = i;
        }
        
        //vertex colors
        var vertexColors = new Color[vertexCount];
        uint triangleCount = (uint)vertexCount / 3;
        float cellWidthInverse = 1f / cellWidth * .9999f;
        var itemTypeHandler = Item.ItemTypeHandler.instance;
        for (uint i = 0; i < triangleCount; ++i)
        {
            uint firstVertI = 3 * i;

            //center of the triangle
            Vector3 centerPos = (
                vertices[firstVertI + 0] +
                vertices[firstVertI + 1] +
                vertices[firstVertI + 2]) / 3f;

            //get item type
            Vector3Int cellIndex = Vector3Int.FloorToInt(centerPos * cellWidthInverse);
            uint groundTypeIndex = chunkSave.groundTypeMap[To1DIndex(cellIndex, noiseMapDims)];
            Item.ItemType itemType = itemTypeHandler.itemTypes[groundTypeIndex];

            Color color = itemType.color;

            //apply vertex color
            vertexColors[firstVertI + 0] = color;
            vertexColors[firstVertI + 1] = color;
            vertexColors[firstVertI + 2] = color;
        }
        #endregion
#endif

        //release native arrays
        hostVertexMap.Dispose();
        hostIndexMap.Dispose();

        //create and attach Mesh object
        {
            //create and attach mesh
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = indeces;
            mesh.colors = vertexColors;
            mesh.RecalculateNormals();
            meshFilter.sharedMesh = mesh;

            //update collider
            #if COLLISION
                meshCollider.sharedMesh = mesh;
            #endif
        }

        //finally make the mesh visible again
        meshRenderer.enabled = true;
    }

    //places or mines a sphere (desides based on sign of delta)
    public void AddSphere(Vector3 sphereWorldPos, float sphereRadius, float delta, uint itemTypeToPlace, out float amountPlaced, out MiningLoot loot)
    {
        #region old shader based code
        ////set uniforms
        //CompShader.SetFloats(u_positionId, new float[] { position.x, position.y, position.z });
        //Vector3 sphereLocalPos = sphereWorldPos - position;
        //CompShader.SetFloats(u_terraform_spherePosId, new float[] { sphereLocalPos.x, sphereLocalPos.y, sphereLocalPos.z });
        //CompShader.SetFloat(u_terraform_sphereRadiusId, sphereRadius);
        //CompShader.SetFloat(u_terraform_deltaId, delta);

        ////upload chunkSave
        //noiseMap3D.SetData(chunkSave.noiseMap);
        //groundTypeMap.SetData(chunkSave.groundTypeMap);

        ////add sphere
        //CompShader.Dispatch(kernelAddSphere, groupCountXAddSphere, groupCountXAddSphere, groupCountXAddSphere);

        ////download noiseMap and groundTypeMap
        //var noiseMapRequest = AsyncGPUReadback.RequestIntoNativeArray(ref chunkSave.noiseMap, noiseMap3D);
        //var groundTypeRequest = AsyncGPUReadback.RequestIntoNativeArray(ref chunkSave.groundTypeMap, groundTypeMap);
        //noiseMapRequest.WaitForCompletion();
        //groundTypeRequest.WaitForCompletion();
        //chunkSave.initialized = true;
        #endregion

        ref NativeArray<float> _noiseMap = ref chunkSave.noiseMap;
        ref NativeArray<uint> _groundTypeMap = ref chunkSave.groundTypeMap;
        Vector3Int noiseMapDimensions = new Vector3Int(noiseMapWidth, noiseMapWidth, noiseMapWidth);

        //calculate box (minPos and maxPos)
        Vector3 spherePos = sphereWorldPos - position;
        Vector3 radiusVec = Vector3.one * sphereRadius;
        Vector3Int minPos = Vector3Int.FloorToInt((spherePos - radiusVec) * cellWidthInverse);
        Vector3Int maxPos = Vector3Int.CeilToInt((spherePos + radiusVec) * cellWidthInverse);
        minPos = Utility.Clamp(minPos, Vector3Int.zero, noiseMapDimensions - Vector3Int.one * 3);// *3: also subtract boundaries
        maxPos = Utility.Clamp(maxPos, Vector3Int.zero, noiseMapDimensions - Vector3Int.one * 3);

        amountPlaced = 0f;
        loot = new MiningLoot();

        for (int x = minPos.x; x <= maxPos.x; ++x)
            for (int y = minPos.y; y <= maxPos.y; ++y)
                for (int z = minPos.z; z <= maxPos.z; ++z)
                {
                    Vector3Int intPos = new Vector3Int(x, y, z);

                    //distance to center of the sphere
                    float sqrDist = (spherePos - (Vector3)intPos * cellWidth).sqrMagnitude;

                    //local noise delta
                    float localDelta = 1f - (sqrDist / (sphereRadius * sphereRadius));
                    localDelta = Mathf.Max(0f, localDelta) * delta;

                    Vector3Int boundaryOfset = Vector3Int.one;//the boundary is included in the noiseMap, but not in intPos
                    int mapIndex = To1DIndex(intPos + boundaryOfset, noiseMapDimensions);
                    float originalNoise = _noiseMap[mapIndex];
                    float newNoise = originalNoise + localDelta;

                    if (delta > 0f)//if placing
                    {
                        if (originalNoise < 0f)
                            _groundTypeMap[mapIndex] = itemTypeToPlace;

                        uint itemTypeId = _groundTypeMap[mapIndex];

                        if (itemTypeId == itemTypeToPlace)//if types match
                        {
                            //increase noise
                            _noiseMap[mapIndex] = newNoise;

                            //increase amount placed
                            if (originalNoise > 0f)
                                amountPlaced += localDelta;
                        }
                    }
                    else//if mining
                    {
                        _noiseMap[mapIndex] = newNoise;

                        if (originalNoise > 0f)
                        {
                            uint itemTypeId = _groundTypeMap[mapIndex];
                            loot.AddTo(itemTypeId, -localDelta);
                        }
                    }
                }

        //recalculate mesh
        Generate(transform.position, chunkSave);
    }

    int To1DIndex(Vector3Int index3D, Vector3Int gridSize)//copied from MarchingCubes.compute
    {
        return (gridSize.x * gridSize.y) * index3D.z +
            gridSize.x * index3D.y +
            index3D.x;
    }
}