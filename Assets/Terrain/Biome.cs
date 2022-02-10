using UnityEngine;
using System;
using UnityEditor;

namespace Biome
{
    #region Param<T>
    //holds a value on host and reference to corresponding uniform
    [Serializable]
    public class Param<T>
    {
        public T value;
        public int uniformId { get; private set; }
        private string uniformName;

        public Param(string uniformName, string baseStructName = "", T value = default, bool addDot = true)
        {
            if (baseStructName != "" && addDot)
                baseStructName += ".";

            this.value = value;
            this.uniformName = baseStructName + uniformName;

            LocateUniform();
        }

        public void LocateUniform()
        {
            uniformId = Shader.PropertyToID(uniformName);
        }
    }

    public static class ParamUpload//overoaded Upload method of Param<T>
    {
        public static void Upload(this Param<int> param, ComputeShader shader) =>
            shader.SetInt(param.uniformId, param.value);

        public static void Upload(this Param<uint> param, ComputeShader shader) =>
            shader.SetInt(param.uniformId, (int)param.value);

        public static void Upload(this Param<float> param, ComputeShader shader) =>
            shader.SetFloat(param.uniformId, param.value);

        public static void Upload<T>(this Param<T> param, ComputeShader shader) =>
            Debug.LogError("Biome.Param<T>.Upload(ComputeShader shader): Can't upload type " + typeof(T).ToString());
    }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Param<>))]
    public class ParamDrawer : PropertyDrawer//makes Param<T> appear correctly in inspector
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            //EditorGUI.BeginProperty(rect, label, property);

            EditorGUI.PropertyField(rect, property.FindPropertyRelative("value"), label);

            //EditorGUI.EndProperty();
        }
    }
    #endif
#endregion

    [Serializable]
    public struct NoiseParam
    {
        [SerializeField] Param<float> totalAmplitude;
        [SerializeField] Param<float> majorWavelength;
        [SerializeField] Param<float> persistence;
        [SerializeField] Param<uint>  layerCount;
        [SerializeField] Param<int>   seed;

        public NoiseParam(string baseStructName)
        {
            totalAmplitude = new Param<float>(nameof(totalAmplitude), baseStructName, 30f, false);
            majorWavelength = new Param<float>(nameof(majorWavelength), baseStructName, 10f, false);
            persistence = new Param<float>(nameof(persistence), baseStructName, .5f, false);
            layerCount = new Param<uint>(nameof(layerCount), baseStructName, 3, false);
            seed = new Param<int>(nameof(seed), baseStructName, 31, false);
        }

        public void Upload(ComputeShader shader)
        {
            totalAmplitude.Upload(shader);
            majorWavelength.Upload(shader);
            persistence.Upload(shader);
            layerCount.Upload(shader);
            seed.Upload(shader);
        }

        public void LocateUniforms()
        {
            totalAmplitude.LocateUniform();
            majorWavelength.LocateUniform();
            persistence.LocateUniform();
            layerCount.LocateUniform();
            seed.LocateUniform();
        }
    }

    [Serializable]
    public class Biome
    {
        public NoiseParam noise2D;
        public NoiseParam noise3D;
        public Param<float> grassTreshold;

        public Biome()
        {
            noise2D = new NoiseParam("u_noise2D_");
            noise3D = new NoiseParam("u_noise3D_");
            grassTreshold = new Param<float>("u_grassTresh");
        }

        public void Upload(ComputeShader shader)
        {
            noise2D.Upload(shader);
            noise3D.Upload(shader);
            grassTreshold.Upload(shader);
        }

        public void LocateUniforms()
        {
            noise2D.LocateUniforms();
            noise3D.LocateUniforms();
            grassTreshold.LocateUniform();
        }
    }
}