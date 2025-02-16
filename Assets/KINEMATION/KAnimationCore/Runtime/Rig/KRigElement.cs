// Designed by KINEMATION, 2024.

using System;
using UnityEngine;

namespace KINEMATION.KAnimationCore.Runtime.Rig
{
    [Serializable]
    public struct KRigElement
    {
        public string name;
        [HideInInspector] public int index;
        public bool isVirtual;

        public KRigElement(int index = -1, string name = "None", bool isVirtual = false)
        {
            this.index = index;
            this.name = name;
            this.isVirtual = isVirtual;
        }
    }
}