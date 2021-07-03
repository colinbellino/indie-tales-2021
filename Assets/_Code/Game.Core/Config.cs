// using System.Collections.Generic;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using UnityEngine;
// using UnityEngine.ProBuilder;

// namespace Game.Core
// {
// 	public struct Config
// 	{
// 		public Material materialBlue;
// 		public Material materialRed;
// 		public Material materialTiles;

// 		public Dictionary<int, MeshData> meshes;
// 	}

// 	public struct MeshData
// 	{
// 		public int id;
// 		public NativeArray<float3> vertices;
// 		public NativeArray<float2> uvs;
// 		public NativeArray<float3> normals;
// 		public MeshFace[] faces;
// 		public Material[] materials;
// 	}

// 	public struct MeshFace
// 	{
// 		public NativeArray<int> triangles;
// 	}

// 	public class ConfigSystem : ComponentSystem
// 	{
// 		private Config config;

// 		protected override void OnCreate()
// 		{
// 			base.OnCreate();

// 			config = new Config
// 			{
// 				materialBlue = Resources.Load<Material>("Materials/Blue"),
// 				materialRed = Resources.Load<Material>("Materials/Red"),
// 				materialTiles = Resources.Load<Material>("Materials/Tiles"),
// 				meshes = new Dictionary<int, MeshData>(),
// 			};

// 			AddMesh(ExtractTileData(0, "Tiles/Dirt Cube"));
// 			AddMesh(ExtractTileData(1, "Tiles/Grass Cube"));

// 			AddMesh(ExtractTileData(99, "Path Point"));
// 		}

// 		protected override void OnUpdate() {
// 			// UnityEngine.Debug.Log("update");
// 		}

// 		protected override void OnDestroy()
// 		{
// 			foreach (var meshData in config.meshes)
// 			{
// 				meshData.Value.vertices.Dispose();
// 				meshData.Value.uvs.Dispose();
// 				meshData.Value.normals.Dispose();

// 				foreach (var face in meshData.Value.faces)
// 				{
// 					face.triangles.Dispose();
// 				}
// 			}
// 		}

// 		public Config GetConfig()
// 		{
// 			return config;
// 		}

// 		private void AddMesh(MeshData meshData)
// 		{
// 			config.meshes.Add(meshData.id, meshData);
// 		}

// 		private static MeshData ExtractTileData(int id, string resourcePath)
// 		{
// 			var mesh = Resources.Load<ProBuilderMesh>(resourcePath);
// 			var renderer = mesh.GetComponent<MeshRenderer>();

// 			var data = new MeshData
// 			{
// 				id = id,
// 				vertices = new NativeArray<float3>(mesh.vertexCount, Allocator.Persistent),
// 				uvs = new NativeArray<float2>(mesh.vertexCount, Allocator.Persistent),
// 				normals = new NativeArray<float3>(mesh.vertexCount, Allocator.Persistent),
// 				faces = new MeshFace[6],
// 				materials = renderer.sharedMaterials,
// 			};
// 			for (int faceIndex = 0; faceIndex < mesh.faces.Count; faceIndex++)
// 			{
// 				var trianglesIndex = 0;
// 				data.faces[faceIndex] = new MeshFace
// 				{
// 					triangles = new NativeArray<int>(mesh.faces[faceIndex].indexes.Count, Allocator.Persistent)
// 				};

// 				foreach (var tri in mesh.faces[faceIndex].indexes)
// 				{
// 					data.faces[faceIndex].triangles[trianglesIndex] = tri;
// 					trianglesIndex += 1;
// 				}
// 			}

// 			var meshVertices = mesh.GetVertices();
// 			for (int verticesIndex = 0; verticesIndex < data.vertices.Length; verticesIndex++)
// 			{
// 				data.vertices[verticesIndex] = meshVertices[verticesIndex].position;
// 				data.uvs[verticesIndex] = meshVertices[verticesIndex].uv0;
// 				data.normals[verticesIndex] = meshVertices[verticesIndex].normal;
// 			}

// 			return data;
// 		}
// 	}
// }
