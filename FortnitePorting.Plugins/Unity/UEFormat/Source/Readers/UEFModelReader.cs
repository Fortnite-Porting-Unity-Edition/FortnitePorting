using System.Linq;
using Editor.UEFormat.Source.Enums;
using UnityEditor;
using UnityEngine;

namespace Editor.UEFormat.Source.Readers
{
    public class UEFModelReader
    {

        public static void ImportUEModelData(FArchiveReader ar, UEFormatHeader header, UEModelOptions importOptions)
        {
            UEModel data = null;
            if (header.FileVersion >= EUEFormatVersion.LevelOfDetailFormatRestructure)
            {
                data = UEModel.FromArchive(ar, importOptions.ScaleFactor);
            }
            else
            {
                Debug.Log("Legacy deserialize for version: " + header.FileVersion);
            }

            if (data == null)
            {
                Debug.LogError("Failed to import data");
                return;
            }
            var lodMesh = data.LODs.First();

            var mesh = new Mesh();
            mesh.name = lodMesh.Name;
            
            mesh.vertices = FloatArrayToVector3Array(lodMesh.Vertices);
            
            var triangles = new int[lodMesh.Indices.GetLength(0) * 3];
            for (int i = 0; i < lodMesh.Indices.GetLength(0); i++)
            {
                int position = i * 3;
                triangles[position] = lodMesh.Indices[i, 0];
                triangles[position + 1] = lodMesh.Indices[i, 1];
                triangles[position + 2] = lodMesh.Indices[i, 2];
            }

            mesh.triangles = triangles;
            
            // Flip normals?
            mesh.normals = FloatArrayToVector3Array(lodMesh.Normals);

            if (lodMesh.UVs.Any())
            {
                var uv0 = lodMesh.UVs.First();

                Vector2[] uv = new Vector2[uv0.GetLength(0)];
                for (int i = 0; i < uv0.GetLength(0); i++)
                {
                    uv[i] = new Vector2(uv0[i, 0], uv0[i, 1]);
                }

                mesh.uv = uv;
                // Additional UVs (UV1 and UV2 for other data in unity?)
            }
            
            // Colors
            // Weights
            // Morph targets
            // Materials
            // Skeleton
            
            //Create folders if they don't exist
            AssetDatabase.CreateAsset(mesh, "Assets/UEFormat/" + header.ObjectName + ".asset");
            Debug.Log("Imported mesh: " + header.ObjectName);
        }

        private static Vector3[] FloatArrayToVector3Array(float[,] floatArray)
        {
            Vector3[] vectors = new Vector3[floatArray.GetLength(0)];
            for (int i = 0; i < floatArray.GetLength(0); i++)
            {
                vectors[i] = new Vector3(floatArray[i, 0], floatArray[i, 2], floatArray[i, 1]);
            }

            return vectors;
        }
        
        
        // Mesh mesh = new Mesh();
        // mesh.vertices = vertices; // Populate from your file
        // mesh.triangles = indices; // Populate from your file
        // mesh.RecalculateNormals();
        //
        // AssetDatabase.CreateAsset(mesh, "Assets/YourMesh.asset");

    }
}