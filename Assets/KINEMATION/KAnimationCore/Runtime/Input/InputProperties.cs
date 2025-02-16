using System;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Runtime.Input
{
    [Serializable]
    public struct BoolProperty
    {
        public string name;
        public bool defaultValue;
    }
    
    [Serializable]
    public struct IntProperty
    {
        public string name;
        public int defaultValue;
    }
    
    [Serializable]
    public struct FloatProperty
    {
        public string name;
        public float defaultValue;
        public float interpolationSpeed;
    }
    
    [Serializable]
    public struct VectorProperty
    {
        public string name;
        public Vector4 defaultValue;
    }
}