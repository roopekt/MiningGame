using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;

public static class TriangulationTableGenerator
{
    [MenuItem("Custom Tools/Generate Triangulation Table")]
    public static void Activate()
    {
        byte[][] table = TriangulationTable();

        //parse string
        string text = "{";
        int row = 0;
        foreach (var triangulation in table)
        {
            if (row > 0) text += ",";
            text += "\n    { ";

            int i = 0;
            foreach (var vertex in triangulation)
            {
                if (i > 0) text += ", ";
                text += vertex.ToString();
                i++;
            }
            text += " }";
            row++;
        }
        text += "\n}";

        //copy to clipboard
        EditorGUIUtility.systemCopyBuffer = text;

        Debug.Log("Triangulation table copied to clipboard.");
        Debug.Log(text);
    }

    /*
     below code is from MarchingCubes Unity project

    modifications to original code:
    -reverse order by midifying BooleanMapToTriangulationIndex
    -every traingulation has 15 elements (5 triangles). unneeded data filled with 255
    -added FillArray function
     */

    //returns the triangulation table as array
    private static byte[][] TriangulationTable()
    {
        var baseTriangulations = new Dictionary<bool[], byte[]>
        {
            //clockwise winding order
            //          0 1 2 3 4 5 6 7
            [BoolArray("0 0 0 0 0 0 0 0")] = new byte[] { },
            [BoolArray("1 0 0 0 0 0 0 0")] = new byte[] { 4, 8, 0 },
            [BoolArray("1 0 0 0 1 0 0 0")] = new byte[] { 10, 8, 2, 2, 8, 0 },
            //[BoolArray("1 0 0 0 0 0 1 0")] = new byte[] { 6, 10, 3, 4, 8, 0 },//X
            [BoolArray("0 1 0 0 1 1 0 0")] = new byte[] { 11, 10, 9, 9, 10, 0, 10, 4, 0 },
            [BoolArray("1 1 0 0 1 1 0 0")] = new byte[] { 9, 11, 8, 11, 10, 8 },
            [BoolArray("0 1 1 0 1 1 0 0")] = new byte[] { 8, 6, 1, 11, 10, 9, 9, 10, 0, 10, 4, 0 },
            [BoolArray("1 0 0 1 0 1 1 0")] = new byte[] { 6, 10, 3, 7, 9, 1, 2, 5, 11, 4, 8, 0 },
            [BoolArray("1 1 0 1 0 1 0 0")] = new byte[] { 7, 8, 1, 8, 7, 4, 7, 11, 4, 4, 11, 2 },
            [BoolArray("0 1 0 1 1 1 0 0")] = new byte[] { 7, 11, 1, 4, 0, 1, 11, 4, 1, 11, 10, 4 },//{ 7, 11, 1, 4, 8, 1, 11, 4, 1, 11, 10, 4 } modifcation made to original table (original triangulation as commented on the left)
            [BoolArray("1 0 0 0 0 0 0 1")] = new byte[] { 11, 7, 3, 4, 8, 0 },
            //[BoolArray("1 0 0 0 1 0 0 1")] = new byte[] { 11, 7, 3, 10, 8, 2, 2, 8, 0 },//X
            //[BoolArray("0 0 1 0 1 0 0 1")] = new byte[] { 11, 7, 3, 8, 6, 1, 10, 4, 2 },//X
            [BoolArray("1 0 1 0 0 1 0 1")] = new byte[] { 7, 3, 2, 5, 7, 2, 4, 6, 1, 4, 1, 0 },
            [BoolArray("1 1 0 0 0 1 0 1")] = new byte[] { 9, 7, 3, 8, 9, 4, 4, 9, 3, 4, 3, 2 },

            //three lines commented out as reoriented (see .svg image in editor folder)
            //          0 1 2 3 4 5 6 7
            [BoolArray("0 0 0 1 0 1 1 0")] = new byte[] { 9, 1, 7, 10, 3, 6, 11, 2, 5, },
            [BoolArray("0 1 0 0 0 1 1 0")] = new byte[] { 3, 6, 10, 2, 0, 11, 11, 0, 9, },
            [BoolArray("0 1 0 0 0 0 0 1")] = new byte[] { 9, 5, 0, 11, 7, 3, },

            //extra cases. keys are inverses of above three, but triangles aren't (see same .svg)
            [BoolArray("1 1 1 0 1 0 0 1")] = new byte[] { 3, 11, 7, 1, 9, 5, 1, 5, 6, 2, 6, 5, 2, 10, 6 },
            [BoolArray("1 0 1 1 1 0 0 1")] = new byte[] { 6, 11, 9, 3, 11, 6, 0, 6, 9, 0, 2, 6, 2, 10, 6 },
            [BoolArray("1 0 1 1 1 1 1 0")] = new byte[] { 0, 7, 9, 0, 3, 7, 0, 11, 3, 0, 5, 11 }
        };

        //add mirror image to baseTriangulation (if key gets logically inverted (NOT-operation), value's order gets inverted (that flips facings of triangles))
        var newBaseTriangulations = new Dictionary<bool[], byte[]>();
        foreach (var triangulation in baseTriangulations)
        {
            var newKey = new bool[8];
            for (int i = 0; i < 8; i++) { newKey[i] = !triangulation.Key[i]; }

            var newValue = (byte[])triangulation.Value.Clone();
            Array.Reverse(newValue);

            newBaseTriangulations.Add(triangulation.Key, triangulation.Value);//add old triangulation to new dict   

            if (!baseTriangulations.Keys.Any(key => key.SequenceEqual(newKey)))//don't overwrite, if key already exists
                newBaseTriangulations.Add(newKey, newValue);//add new triangulation

        }
        baseTriangulations = newBaseTriangulations;

        //generate the triangulation table itself
        var symmetries = CubeSymmetries();
        var triangulationTable = new byte[256][];
        foreach (var baseTriangulation in baseTriangulations)
        {
            foreach (var symmetry in symmetries)
            {
                var triangulationIndex = BooleanMapToTriangulationIndex(ApplySymmetry(baseTriangulation.Key, symmetry));

                if (triangulationTable[triangulationIndex] == null)//if not already calculated
                {
                    var triangulation = new byte[3 * 5];//max 5 triangles
                    FillArray(triangulation, (byte)255);//255 means no vertex
                    int i = 0;
                    foreach (var edge in baseTriangulation.Value)
                    {
                        var newEdge = new Vector2Int(Array.IndexOf(symmetry, VoxelData.edges[edge].x),
                                                     Array.IndexOf(symmetry, VoxelData.edges[edge].y));
                        triangulation[i] = (byte)Array.IndexOf(VoxelData.edges, newEdge);

                        //add edge to triangulation
                        if (VoxelData.edges.Contains(newEdge))
                        {
                            triangulation[i] = (byte)Array.IndexOf(VoxelData.edges, newEdge);
                        }
                        else
                        {
                            newEdge = new Vector2Int(newEdge.y, newEdge.x);//mirror (still same edge)
                            triangulation[i] = (byte)Array.IndexOf(VoxelData.edges, newEdge);
                        }
                        i++;
                    }
                    triangulationTable[triangulationIndex] = triangulation;
                }
            }
        }

        return triangulationTable;
    }

    private static int[][] CubeSymmetries()//only rotational symmetries, not mirrorings
    {
        #region starting data
        var basicRotations = new Quaternion[]
        {
            Quaternion.Euler(0, 0, 0),
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(180, 0, 0),
            Quaternion.Euler(-90, 0, 0),
            Quaternion.Euler(0, 0, 90),
            Quaternion.Euler(0, 0, -90)
        };

        var basicAngels = new float[] { 0f, 90f, 180, -90f };
        #endregion

        //generate rotations
        Quaternion[] rotations = new Quaternion[24];
        int I = 0;
        foreach (Quaternion basicRotation in basicRotations)
        {
            foreach (float angel in basicAngels)
            {
                rotations[I] = Quaternion.AngleAxis(angel, basicRotation * Vector3.up) * basicRotation;
                I++;
            }
        }

        //generate symmetries
        int[][] symmetries = new int[24][];
        Vector3Int[] corners = new Vector3Int[8]; for (int i = 0; i < 8; i++) { corners[i] = VoxelData.corners[i] * 2 - Vector3Int.one; }//this cube is two units wide
        for (int rotation = 0; rotation < 24; rotation++)
        {
            symmetries[rotation] = new int[8];
            for (int corner = 0; corner < 8; corner++)
            {
                symmetries[rotation][corner] = Array.IndexOf(corners, Vector3Int.RoundToInt(rotations[rotation] * corners[corner]));
            }
        }

        return symmetries;
    }

    private static type[] ApplySymmetry<type>(type[] array, int[] symmetry)
    {
        var newArray = new type[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            newArray[i] = array[symmetry[i]];
        }
        return newArray;
    }

    private static byte BooleanMapToTriangulationIndex(bool[] corners)
    {
        //Array.Reverse(corners);
        byte index = 0;
        for (int i = 0; i < 8; i++)
        {
            if (corners[i] == true)
            {
                index += (byte)(1 << i);
            }
        }
        return index;
    }

    private static bool[] BoolArray(string str)// "010" or "0 1 0" to { false, true, false }
    {
        return str.Replace(" ", "").Select(chr => chr == '1').ToArray();
    }
    
    private static void FillArray<T>(T[] array, T value) {//fill array with value. this isn't from MarchinCubes project
        for (int i = 0; i < array.Length; ++i) {
            array[i] = value;
        }
    }
}