using System;
using System.Collections;
using System.Collections.Generic;
using Editor.UEFormat.Source.Enums;
using Editor.UEFormat.Source.Readers;
using UnityEditor;
using UnityEngine;

public class UEFormat
{
    private static string MAGIC = "UEFORMAT";

    [MenuItem("Tools/Import UEFormat")]
    public static void ImportUEFormat()
    {
        string filePath = EditorUtility.OpenFilePanel("Import UEFormat", "", "uemodel,ueanim");
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.Log("No file selected.");
            return;
        }

        Debug.Log($"Importing asset: {filePath}");

        var success = ProcessAsset(filePath);

        Debug.Log($"Import successful: {success}");
    }

    private static bool ProcessAsset(string path)
    {
        var importOptions = new UEModelOptions();
        byte[] fileBytes = System.IO.File.ReadAllBytes(path);
        if (fileBytes.Length == 0) return false;
        
        var ar = new FArchiveReader(fileBytes);
        
        //Check magic
        var fileMagic = ar.ReadString(MAGIC.Length);
        if (fileMagic != MAGIC)
        {
            Debug.LogError($"Invalid Magic: {fileMagic}");
            return false;
        }

        var header = UEFormatHeader.FromArchive(ar);

        if (header == null)
        {
            Debug.LogError("Error reading header");
            return false;
        }

        var readArchive = ar;
        if (header.IsCompressed)
        {
            //handle decompression
            var compressionType = ar.ReadFString();
            var uncompressedSize = ar.ReadInt();
            var compressedSize = ar.ReadInt();

            switch (compressionType)
            {
                case "GZIP":
                    break;
                case "ZSTD":
                    break;
                default:
                    Debug.LogError($"Invalid Compression Type: {compressionType}");
                    return false;
            }
        }

        switch (header.Identifier)
        {
            case "UEMODEL":
                UEFModelReader.ImportUEModelData(ar, header, importOptions);
                break;
            case "UEANIM":
                break;
            default:
                Debug.LogError($"Unknown identifier: {header.Identifier}");
                return false;
        }


        return true;
    }
}

public class UEFormatHeader
{
    public string Identifier { get; set; }
    public EUEFormatVersion FileVersion { get; set; }
    public string ObjectName { get; set; }
    public bool IsCompressed { get; set; }

    public static UEFormatHeader FromArchive(FArchiveReader ar)
    {
        var identifier = ar.ReadFString();
        var fileVersion = GetVersionFromArchive(ar);
        if (fileVersion == EUEFormatVersion.InvalidVersion || fileVersion > EUEFormatVersion.LatestVersion)
        {
            Debug.LogError($"Invalid version: {fileVersion}");
            return null;
        }
        
        return new()
        {
            Identifier = identifier,
            FileVersion = fileVersion,
            ObjectName = ar.ReadFString(),
            IsCompressed = ar.ReadBool()
        };
    }

    private static EUEFormatVersion GetVersionFromArchive(FArchiveReader ar)
    {
        var versionBytes = ar.ReadByte();
        if (Enum.TryParse(((int)versionBytes).ToString(), out EUEFormatVersion version))
            return version;
        
        return EUEFormatVersion.InvalidVersion;

    }
}
