#ifndef VOXELDATA
#define VOXELDATA

//this data is from MarchingCubes project (VoxelData.cs and TriangulationTableGenerator.cs)

//ofset coordinates of corner points
static const uint3 voxelCorners[8] = 
{
    //note binary counting (000 => 111)
    uint3(0, 0, 0),//0
    uint3(0, 0, 1),//1
    uint3(0, 1, 0),//2
    uint3(0, 1, 1),//3
    uint3(1, 0, 0),//4
    uint3(1, 0, 1),//5
    uint3(1, 1, 0),//6
    uint3(1, 1, 1) //7
};

//witch edge points are between witch corner points
static const uint2 voxelEdges[12] = 
{
    uint2(0, 1),//0
    uint2(2, 3),//1
    uint2(4, 5),//2
    uint2(6, 7),//3
    uint2(0, 4),//4
    uint2(1, 5),//5
    uint2(2, 6),//6
    uint2(3, 7),//7
    uint2(0, 2),//8
    uint2(1, 3),//9
    uint2(4, 6),//10
    uint2(5, 7) //11
};

//vector from corner 0 (0, 0, 0) to center of edge, doubled (to fit in ints)
static const uint3 voxelEdgeOfsets[12] =
{
    uint3(0, 0, 1),//0
    uint3(0, 2, 1),//1
    uint3(2, 0, 1),//2
    uint3(2, 2, 1),//3
    uint3(1, 0, 0),//4
    uint3(1, 0, 2),//5
    uint3(1, 2, 0),//6
    uint3(1, 2, 2),//7
    uint3(0, 1, 0),//8
    uint3(0, 1, 2),//9
    uint3(2, 1, 0),//10
    uint3(2, 1, 2) //11
};

#endif