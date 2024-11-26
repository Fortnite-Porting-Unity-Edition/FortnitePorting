using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class UEWorld
{
    public List<HashedMesh> Meshes { get; private set; }
    public List<Actor> Actors { get; private set; }

    public static UEWorld FromArchive(FArchiveReader ar, float scale)
    {
        var world = new UEWorld();
        while (!ar.EOF())
        {
            var headerName = ar.ReadFString();
            var arraySize = ar.ReadInt();
            var byteSize = ar.ReadInt();

            switch (headerName)
            {
                case "MESHES":
                    world.Meshes = ar.ReadArray(arraySize, HashedMesh.FromArchive);
                    break;
                case "ACTORS":
                    world.Actors = ar.ReadArray(arraySize, ar => Actor.FromArchive(ar, scale));
                    break;
                default:
                    Debug.Log("Unknown Section Data: " + headerName);
                    ar.Skip(byteSize);
                    break;
            }
        }

        return world;
    }
}

public class HashedMesh
{
    public int Hash { get; private set; }
    public int ModelSize { get; private set; }
    public FArchiveReader ModelReader { get; private set; }

    public static HashedMesh FromArchive(FArchiveReader ar)
    {
        var hash = ar.ReadInt();
        var modelSize = ar.ReadInt();

        return new HashedMesh
        {
            Hash = hash,
            ModelSize = modelSize,
            ModelReader = ar.Chunk(modelSize)
        };
    }
}

public class Actor
{
    public string Name { get; private set; }
    public string ModelHash { get; private set; }
    public float[] Location { get; private set; } = new float[3];
    public float[] Rotation { get; private set; } = new float[4];
    public float[] Scale { get; private set; } = new float[3];

    public static Actor FromArchive(FArchiveReader ar, float scale)
    {
        return new Actor
        {
            Name = ar.ReadFString(),
            ModelHash = ar.ReadFString(),
            Location = ar.ReadFloatVector(3).Select(v => v * scale).ToArray(),
            Rotation = ar.ReadFloatVector(4),
            Scale = ar.ReadFloatVector(3)
        };
    }
}