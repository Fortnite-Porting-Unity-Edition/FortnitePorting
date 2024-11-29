using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

using Editor.UEFormat.Source;

// Model Data
public class UEModel
{
    public List<UEModelLOD> LODs { get; set; } = new();
    public List<ConvexCollision> Collisions { get; set; } = new();
    public UEModelSkeleton Skeleton { get; set; } //annotate nullable

    public static UEModel FromArchive(FArchiveReader ar, float scale)
    {
        var model = new UEModel();

        while (!ar.EOF())
        {
            string sectionName = ar.ReadFString();
            int arraySize = ar.ReadInt();
            int byteSize = ar.ReadInt();

            switch (sectionName)
            {
                case "LODS":
                    model.LODs.AddRange(Enumerable.Range(0, arraySize)
                        .Select(_ => UEModelLOD.FromArchive(ar, scale))
                        .ToList());
                    break;
                case "SKELETON":
                    model.Skeleton = UEModelSkeleton.FromArchive(ar.Chunk(byteSize), scale);
                    break;
                case "COLLISION":
                    model.Collisions.AddRange(ar.ReadArray(arraySize, ar => ConvexCollision.FromArchive(ar, scale)).ToList());
                    break;
                default:
                    ar.Skip(byteSize);
                    break;
            }
        }

        return model;
    }
}

// LOD Data
public class UEModelLOD
{
    public string Name { get; private set; }
    public float[,] Vertices { get; private set; } = new float[0, 0];
    public int[,] Indices { get; private set; } = new int[0, 0];
    public float[,] Normals { get; private set; } = new float[0, 0];
    public List<float[]> Tangents { get; private set; } = new();
    public List<VertexColor> Colors { get; private set; } = new();
    public List<float[,]> UVs { get; private set; } = new();
    public List<Material> Materials { get; private set; } = new();
    public List<MorphTarget> Morphs { get; private set; } = new();
    public List<Weight> Weights { get; private set; } = new();

    public static UEModelLOD FromArchive(FArchiveReader ar, float scale)
    {
        var data = new UEModelLOD
        {
            Name = ar.ReadFString()
        };

        int lodSize = ar.ReadInt();
        ar = ar.Chunk(lodSize);

        while (!ar.EOF())
        {
            string headerName = ar.ReadFString();
            int arraySize = ar.ReadInt();
            int byteSize = ar.ReadInt();

            long currentPosition = ar.Position();

            switch (headerName)
            {
                case "VERTICES":
                    {
                        float[] flattened = ar.ReadFloatVector(arraySize * 3);
                        data.Vertices = Utils.FlattenedToFloatMatrix(flattened, arraySize, 3, scale);
                        break;
                    }
                case "INDICES":
                    {
                        int[] flatIndices = ar.ReadIntVector(arraySize);
                        data.Indices = Utils.FlattenedToIntMatrix(flatIndices, arraySize / 3, 3);
                        break;
                    }
                case "NORMALS":
                    {
                        float[] flattened = ar.ReadFloatVector(arraySize * 4);
                        float[,] reshaped = Utils.FlattenedToFloatMatrix(flattened, arraySize, 4);
                        data.Normals = Utils.ExtractSubMatrix(reshaped, 1, 3, 3); // Extract XYZ from WXYZ
                        // TODO: verify this is fine, all normals I tested had a W of 0 but maybe some don't?
                        break;
                    }
                case "TANGENTS":
                    {
                        ar.Skip(arraySize * 3 * 3); // Not implemented, skipping
                        break;
                    }
                case "VERTEXCOLORS":
                    {
                        data.Colors = new List<VertexColor>(arraySize);
                        for (int i = 0; i < arraySize; i++)
                        {
                            data.Colors.Add(VertexColor.FromArchive(ar));
                        }
                        break;
                    }
                case "TEXCOORDS":
                    {
                        data.UVs = new List<float[,]>(arraySize);
                        for (int i = 0; i < arraySize; i++)
                        {
                            int count = ar.ReadInt();
                            float[] flattened = ar.ReadFloatVector(count * 2);
                            data.UVs.Add(Utils.FlattenedToFloatMatrix(flattened, count, 2));
                        }
                        break;
                    }
                case "MATERIALS":
                    {
                        data.Materials = ar.ReadArray(arraySize, Material.FromArchive);
                        break;
                    }
                case "WEIGHTS":
                    {
                        //TODO: add back (got length errors when reading)
                        //data.Weights = ar.ReadArray(arraySize, Weight.FromArchive);
                        break;
                    }
                case "MORPHTARGETS":
                    {
                        //TODO: add back (got length errors when reading)
                        //data.Morphs = ar.ReadArray(arraySize, a => MorphTarget.FromArchive(a, scale));
                        break;
                    }
                default:
                    {
                        Debug.Log($"Unknown Mesh Data: {headerName}");
                        ar.Skip(byteSize);
                        break;
                    }
            }

            ar.Seek(currentPosition + byteSize);
        }

        return data;
    }
}

// Skeleton Data
public class UEModelSkeleton
{
    public List<Bone> Bones { get; set; } = new();

    public static UEModelSkeleton FromArchive(FArchiveReader ar, float scale)
    {
        var skeleton = new UEModelSkeleton();

        while (!ar.EOF())
        {
            string headerName = ar.ReadFString();
            int arraySize = ar.ReadInt();
            int byteSize = ar.ReadInt();

            switch (headerName)
            {
                case "BONES":
                    skeleton.Bones.AddRange(ar.ReadArray(arraySize, ar => Bone.FromArchive(ar, scale)).ToList());
                    break;
                default:
                    ar.Skip(byteSize);
                    break;
            }
        }

        return skeleton;
    }
}

// ConvexCollision Data
public class ConvexCollision
{
    public string Name { get; private set; }
    public float[,] Vertices { get; private set; } = new float[0, 0];
    public int[,] Indices { get; private set; } = new int[0, 0];

    public static ConvexCollision FromArchive(FArchiveReader ar, float scale)
    {
        var collision = new ConvexCollision
        {
            Name = ar.ReadFString()
        };

        // Read vertices
        int vertexCount = ar.ReadInt();
        float[] vertexData = ar.ReadFloatVector(vertexCount * 3);
        collision.Vertices = Utils.FlattenedToFloatMatrix(vertexData, vertexCount, 3, scale);

        // Read indices
        int indexCount = ar.ReadInt();
        int[] indexData = ar.ReadIntVector(indexCount);
        collision.Indices = Utils.FlattenedToIntMatrix(indexData, indexCount / 3, 3);

        return collision;
    }
}


public class VertexColor
{
    public string Name { get; private set; }
    public float[,] Data { get; private set; }

    public static VertexColor FromArchive(FArchiveReader ar)
    {
        string name = ar.ReadFString();
        int count = ar.ReadInt();
        byte[] flattened = ar.ReadByteVector(count * 4);
        float[,] colors = new float[count, 4];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                colors[i, j] = flattened[i * 4 + j] / 255.0f;
            }
        }
        return new VertexColor { Name = name, Data = colors };
    }
}

public class Material
{
    public string MaterialName { get; private set; }
    public int FirstIndex { get; private set; }
    public int NumFaces { get; private set; }

    public static Material FromArchive(FArchiveReader ar)
    {
        return new Material
        {
            MaterialName = ar.ReadFString(),
            FirstIndex = ar.ReadInt(),
            NumFaces = ar.ReadInt()
        };
    }
}

// Bone Data
public class Bone
{
    public string Name { get; private set; }
    public int ParentIndex { get; private set; }
    public float[] Position { get; private set; } = new float[3];
    public float[] Rotation { get; private set; } = new float[4];

    public static Bone FromArchive(FArchiveReader ar, float scale)
    {
        return new Bone
        {
            Name = ar.ReadFString(),
            ParentIndex = ar.ReadInt(),
            Position = ar.ReadFloatVector(3).Select(v => v * scale).ToArray(),
            Rotation = ar.ReadFloatVector(4)
        };
    }
}

public class Weight
{
    public int VertexIndex { get; private set; }
    public int BoneIndex { get; private set; }
    public float WeightValue { get; private set; }

    public static Weight FromArchive(FArchiveReader ar)
    {
        return new Weight
        {
            VertexIndex = ar.ReadInt(),
            BoneIndex = ar.ReadInt(),
            WeightValue = ar.ReadFloat()
        };
    }
}

public class MorphTarget
{
    public string Name { get; private set; }
    public MorphTargetData[] Deltas { get; private set; } = Array.Empty<MorphTargetData>();

    public static MorphTarget FromArchive(FArchiveReader ar, float scale)
    {
        var morph = new MorphTarget
        {
            Name = ar.ReadFString(),
            Deltas = ar.ReadBulkArray(ar => MorphTargetData.FromArchive(ar, scale)).ToArray()
        };

        return morph;
    }
}

public class MorphTargetData
{
    public float[] Position { get; private set; } = Array.Empty<float>();
    public float[] Normals { get; private set; } = new float[3];
    public int VertexIndex { get; private set; }

    public static MorphTargetData FromArchive(FArchiveReader ar, float scale)
    {
        var morphData = new MorphTargetData
        {
            Position = ar.ReadFloatVector(3).Select(v => v * scale).ToArray(),
            Normals = ar.ReadFloatVector(4).Select(v => v * scale).ToArray(),
            VertexIndex = ar.ReadInt()
        };
        
        return morphData;
    }
}

public class Socket
{
    public string Name { get; private set; }
    public string ParentName { get; private set; }
    public float[] Position { get; private set; } = new float[3];
    public float[] Rotation { get; private set; } = new float[4];
    public float[] Scale { get; private set; } = new float[3];

    public static Socket FromArchive(FArchiveReader ar, float scale)
    {
        return new Socket
        {
            Name = ar.ReadFString(),
            ParentName = ar.ReadFString(),
            Position = ar.ReadFloatVector(3).Select(v => v * scale).ToArray(),
            Rotation = ar.ReadFloatVector(4),
            Scale = ar.ReadFloatVector(3)
        };
    }
}

public class VirtualBone
{
    public string SourceName { get; private set; }
    public string TargetName { get; private set; }
    public string VirtualName { get; private set; }

    public static VirtualBone FromArchive(FArchiveReader ar)
    {
        return new VirtualBone
        {
            SourceName = ar.ReadFString(),
            TargetName = ar.ReadFString(),
            VirtualName = ar.ReadFString()
        };
    }
}

