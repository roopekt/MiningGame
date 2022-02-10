using UnityEngine;

//this file has been translated from VoxelData.cginc

public static class VoxelData
{
    //ofset coordinates of corner points of the cube
    public static readonly Vector3Int[] corners = 
    {
        //note binary counting (000 => 111)
        new Vector3Int(0, 0, 0),//0
        new Vector3Int(0, 0, 1),//1
        new Vector3Int(0, 1, 0),//2
        new Vector3Int(0, 1, 1),//3
        new Vector3Int(1, 0, 0),//4
        new Vector3Int(1, 0, 1),//5
        new Vector3Int(1, 1, 0),//6
        new Vector3Int(1, 1, 1) //7
    };

    //whitch edge points are between witch corner points
    public static readonly Vector2Int[] edges = 
    {
        new Vector2Int(0, 1),//0
        new Vector2Int(2, 3),//1
        new Vector2Int(4, 5),//2
        new Vector2Int(6, 7),//3
        new Vector2Int(0, 4),//4
        new Vector2Int(1, 5),//5
        new Vector2Int(2, 6),//6
        new Vector2Int(3, 7),//7
        new Vector2Int(0, 2),//8
        new Vector2Int(1, 3),//9
        new Vector2Int(4, 6),//10
        new Vector2Int(5, 7),//11
    };

    //see edgeOfsets below
    private static Vector3Int[] GetEdgeOfsets()
    {
        Vector3Int[] ofsets = new Vector3Int[12];
        for (int i = 0; i < ofsets.Length; i++) {
            ofsets[i] = corners[edges[i].x] + corners[edges[i].y];
        }
        return ofsets;
    }

    //vectors from corner 0 (0, 0, 0) to center of edge, doubled (to fit in ints)
    public static readonly Vector3Int[] edgeOfsets = GetEdgeOfsets();
}