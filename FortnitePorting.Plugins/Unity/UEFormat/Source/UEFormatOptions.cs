using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.UEFormat.Source.Enums
{
    public class UEFormatOptions
    {
        public bool Link { get; protected set; } = true;
        public float ScaleFactor { get; protected set; } = 0.01f;
    }

    public class UEModelOptions : UEFormatOptions
    {
        public float BoneLength { get; protected set; } = 4;
        public bool ReorientBones { get; private set; } = false;
        public bool ImportCollision { get; private set; } = false;
        public bool ImportSockets { get; private set; } = true;
        public bool ImportMorphTargets { get; private set; } = true;
        public bool ImportVirtualBones { get; private set; } = false;
        public int TargetLOD { get; private set; } = 0;
        public Dictionary<String, List<String>> AllowedReorientChildren { get; private set; }
    }

    public class UEAnimOptions : UEFormatOptions
    {
        public bool RotationOnly { get; protected set; } = false;
        public HumanDescription OverrideSkeleton { get; protected set; } // Confirm this is the right class
    }

    public class UEWorldOptions : UEFormatOptions
    {
        public bool InstanceMeshes { get; protected set; } = true;
    }
}