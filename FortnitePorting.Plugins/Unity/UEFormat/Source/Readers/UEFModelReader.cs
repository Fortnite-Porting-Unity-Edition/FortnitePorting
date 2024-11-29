using System;
using System.Collections.Generic;
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
                Debug.LogFormat("Legacy deserialize for version: {0}", header.FileVersion);
            }

            if (data == null)
            {
                Debug.LogError("Failed to import data");
                return;
            }
            var lodMesh = data.LODs.First();

            var mesh = new Mesh();
            mesh.name = lodMesh.Name;
            
            mesh.vertices = FloatMatrixToVectorArray(lodMesh.Vertices);
            mesh.normals = FloatMatrixToVectorArray(lodMesh.Normals);

            var triangles = IndicesToTriangles(lodMesh.Indices);
            UnityEngine.Material[] materials = Array.Empty<UnityEngine.Material>();
            if (lodMesh.Materials is { Count: > 0 })
            {
                materials = new UnityEngine.Material[lodMesh.Materials.Count];
                mesh.subMeshCount = lodMesh.Materials.Count;
                for (int i = 0; i < lodMesh.Materials.Count; i++)
                {
                    Material material = lodMesh.Materials[i];
                    
                    // TODO: create materials and assign to asset (as prefab?)
                    materials[i] = new UnityEngine.Material(Shader.Find("Fortnite Porting/FP_Material"))
                    {
                        name = material.MaterialName
                    };
                    

                    int[] subMeshTriangles = new int[material.NumFaces * 3];
                    Array.Copy(triangles, material.FirstIndex, subMeshTriangles, 0, subMeshTriangles.Length);
                    mesh.SetTriangles(subMeshTriangles, i);
                }
            }
            else
            {
                mesh.triangles = triangles;
            }

            
            BuildUVMaps(mesh, lodMesh.UVs);
            
            
            
            // Colors
            // Weights
            // Morph targets
            // Finish Materials
            // Skeleton
                        
                        
            // These were messing up the normals, verify they aren't needed before ignoring them
            //mesh.RecalculateNormals();
            //mesh.RecalculateTangents();


            mesh.RecalculateBounds();


            AssetDatabase.CreateAsset(mesh, "Assets/UEFormat/meshes/" + header.ObjectName + ".asset");
            
            if (materials.Length > 0)
            {
                CreateMeshPrefab(mesh, materials, "Assets/UEFormat/", header.ObjectName);
            }
            
            //Create folders if they don't exist
            Debug.LogFormat("Imported mesh: {0}", header.ObjectName);
        }

        private static Vector3[] FloatMatrixToVectorArray(float[,] floatArray)
        {
            Vector3[] vectors = new Vector3[floatArray.GetLength(0)];
            for (int i = 0; i < floatArray.GetLength(0); i++)
            {
                vectors[i] = new Vector3(floatArray[i, 0], floatArray[i, 2], floatArray[i, 1]);
            }

            return vectors;
        }

        private static int[] IndicesToTriangles(int[,] indices)
        {
            var triangles = new int[indices.GetLength(0) * 3];
            for (int i = 0; i < indices.GetLength(0); i++)
            {
                // Reverse the order the triangle data is stored to fix normals
                int position = i * 3;
                triangles[position] = indices[i, 2];
                triangles[position + 1] = indices[i, 1];
                triangles[position + 2] = indices[i, 0];
            }

            return triangles;
        }

        private static void BuildUVMaps(Mesh mesh, List<float[,]> uvArray)
        {
            if (uvArray is { Count: < 1 }) return;
            
            mesh.uv = BuildUVMap(uvArray[0]);

            if (uvArray.Count <= 1) return;
            
            // UVs 2 and 3 are used to store unity-baked lightmaps
            int uvIndex = 0;
            foreach (var uv in uvArray)
            {
                if (uvIndex > 5)
                {
                    Debug.LogWarningFormat("UV count exceeds number of available UV slots.  Total UVs: {0}", uvArray.Count);
                }
                if (uvIndex != 0)
                {
                    mesh.SetUVs(uvIndex + 2, BuildUVMap(uv));
                }
                uvIndex++;
            }
        }

        private static Vector2[] BuildUVMap(float[,] uvArray)
        {
            Vector2[] uv = new Vector2[uvArray.GetLength(0)];
            for (int i = 0; i < uvArray.GetLength(0); i++)
            {
                uv[i] = new Vector2(uvArray[i, 0], uvArray[i, 1]);
            }

            return uv;
        }
        
        //TODO: Is this the right way to go about this?
        private static void CreateMeshPrefab(Mesh mesh, UnityEngine.Material[] materials, string assetPath, string assetName)
        {
            // Create a temp GameObject for the mesh
            GameObject tempPrefab = new GameObject(mesh.name);
            MeshFilter filter = tempPrefab.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            
            // Create new materials
            // TODO: add check for existing materials
            foreach (var mat in materials)
            {
                AssetDatabase.CreateAsset(mat, assetPath + "/materials/" + mat.name + ".mat");
            }

            // Assign the materials to the renderer
            MeshRenderer renderer = tempPrefab.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;

            // Save the prefab
            string prefabPath = System.IO.Path.ChangeExtension(assetPath + assetName, ".prefab");
            PrefabUtility.SaveAsPrefabAsset(tempPrefab, prefabPath);

            // Clean up
            GameObject.DestroyImmediate(tempPrefab);
        }
    }
}