// Designed by KINEMATION, 2024.

using System.Collections.Generic;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Runtime.Input
{
    [CreateAssetMenu(fileName = "NewInputConfig", menuName = "KINEMATION/Input Config")]
    public class UserInputConfig : ScriptableObject
    {
        public List<IntProperty> intProperties = new List<IntProperty>();
        public List<FloatProperty> floatProperties = new List<FloatProperty>();
        public List<BoolProperty> boolProperties = new List<BoolProperty>();
        public List<VectorProperty> vectorProperties = new List<VectorProperty>();
    }
}