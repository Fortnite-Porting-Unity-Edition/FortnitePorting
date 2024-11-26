
using System.Collections.Generic;
using System.Linq;

public class UEAnim
{
    
}

public class Curve
{
    
}

public class Track
{
    
}

public class AnimKey
{
    public int Frame { get; protected set; }

    public static AnimKey FromArchive(FArchiveReader ar)
    {
        return new AnimKey
        {
            Frame = ar.ReadInt()
        };
    }
}

public class VectorKey : AnimKey
{
    public float[] Value { get; private set; } = new float[3];

    public static VectorKey FromArchive(FArchiveReader ar, float multiplier = 1)
    {
        return new VectorKey
        {
            Frame = ar.ReadInt(),
            Value = ar.ReadFloatVector(3).Select(v => v * multiplier).ToArray()
        };
    }
}

public class QuatKey : AnimKey
{
    public float[] Value { get; private set; } = new float[4];

    public static QuatKey FromArchive(FArchiveReader ar)
    {
        return new QuatKey
        {
            Frame = ar.ReadInt(),
            Value = ar.ReadFloatVector(4).ToArray()
        };
    }
}

public class FloatKey : AnimKey
{
    public float Value { get; private set; }

    public static FloatKey FromArchive(FArchiveReader ar)
    {
        return new FloatKey
        {
            Frame = ar.ReadInt(),
            Value = ar.ReadFloat()
        };
    }
}