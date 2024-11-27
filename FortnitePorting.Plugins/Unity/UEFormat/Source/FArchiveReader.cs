using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class FArchiveReader : IDisposable
{
    private readonly MemoryStream _data;
    private readonly BinaryReader _reader;
    private readonly long _size;

    public FArchiveReader(byte[] data)
    {
        _data = new MemoryStream(data);
        _reader = new BinaryReader(_data);
        _size = data.Length;
        _data.Seek(0, SeekOrigin.Begin);
    }

    public long Position()
    {
        return _data.Position;
    }

    public void Dispose()
    {
        _reader?.Close();
        _data?.Close();
    }

    public bool EOF()
    {
        return _data.Position >= _size;
    }

    public byte[] Read(int size)
    {
        return _reader.ReadBytes(size);
    }

    public byte[] ReadToEnd()
    {
        return _reader.ReadBytes((int)(_size - _data.Position));
    }

    public bool ReadBool()
    {
        return _reader.ReadBoolean();
    }

    public string ReadString(int size)
    {
        byte[] bytes = _reader.ReadBytes(size);
        return Encoding.UTF8.GetString(bytes);
    }

    public string ReadFString()
    {
        int size = ReadInt();
        byte[] bytes = _reader.ReadBytes(size);
        return Encoding.UTF8.GetString(bytes);
    }

    public int ReadInt()
    {
        return _reader.ReadInt32();
    }

    public int[] ReadIntVector(int size)
    {
        if (size <= 0) return Array.Empty<int>();
        int[] result = new int[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = ReadInt();
        }
        return result;
    }

    public short ReadShort()
    {
        return _reader.ReadInt16();
    }

    public byte ReadByte()
    {
        return _reader.ReadByte();
    }

    public float ReadFloat()
    {
        return _reader.ReadSingle();
    }

    public float[] ReadFloatVector(int size)
    {
        if (size <= 0) return Array.Empty<float>();
        float[] result = new float[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = ReadFloat();
        }
        return result;
    }

    public byte[] ReadByteVector(int size)
    {
        if (size <= 0) return Array.Empty<byte>();
        byte[] result = new byte[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = _reader.ReadByte();
        }
        return result;
    }

    public void Skip(int size)
    {
        _data.Seek(size, SeekOrigin.Current);
    }

    public void Seek(long pos)
    {
        _data.Seek(pos, SeekOrigin.Begin);
    }

    public List<T> ReadBulkArray<T>(Func<FArchiveReader, T> predicate)
    {
        int count = ReadInt();
        return ReadArray(count, predicate);
    }

    public List<T> ReadArray<T>(int count, Func<FArchiveReader, T> predicate)
    {
        List<T> result = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            result.Add(predicate(this));
        }
        return result;
    }

    public FArchiveReader Chunk(int size)
    {
        return new FArchiveReader(Read(size));
    }
}
