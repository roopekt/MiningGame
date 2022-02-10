using System;
using UnityEngine;

public static class Utility
{
    public static int PowInt(int x, uint exp)//x raised to exp (x^exp). inefficient
    {
        int result = 1;
        while (exp > 0)
        {
            result *= x;
            --exp;
        }
        return result;
    }

    public static uint Cube(uint x) =>
        x * x * x;
    public static int Cube(int x) =>
        x * x * x;
    public static float Cube(float x) =>
        x * x * x;

    public static int DivIntCeil(int a, int b) =>
        (a + b - 1) / b;// floor(a / b + (b - 1) / b)

    public static uint Clamp(uint x, uint min, uint max) =>
        Math.Max(Math.Min(x, max), min);
    public static int Clamp(int x, int min, int max) =>
        Math.Max(Math.Min(x, max), min);

    public static Vector3Int Clamp(Vector3Int vec, Vector3Int min, Vector3Int max) =>
        new Vector3Int(Clamp(vec.x, min.x, max.x),
                       Clamp(vec.y, min.y, max.y),
                       Clamp(vec.z, min.z, max.z));

    //modulo, that works correctly for negative x
    public static int Mod(int x, int m) =>
        ((x % m) + m) % m;
}
