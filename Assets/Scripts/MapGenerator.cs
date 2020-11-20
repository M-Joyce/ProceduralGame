using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, Mesh, FalloffMap};
	public DrawMode drawMode;


	public TerrainData terrainData;
	public NoiseData noiseData;
	public TextureData textureData;

	public Material terrainMaterial;

	[Range(0,6)] //LOD clamped to 0-6
	public int editorPreviewLOD; //LOD Setting

	public bool autoUpdate;

	public float[,] falloffMap;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
			DrawMapInEditor();
        }
    }

	void OnTextureValuesUpdated()
    {
		textureData.ApplyToMaterial(terrainMaterial);
    }

	public int mapChunkSize{
        get{
            if (terrainData.useFlatShading){
				return 95;
            }
            else{
				return 239; //chunk size 240 (+ 1 is needed) -2 is for the borders, so 240 - 2 + 1 = 239 ... This formula is important if you change this value
			}
        }
    }

    public void DrawMapInEditor()
    {
		MapData mapData = GenerateMapData(Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		}
		else if (drawMode == DrawMode.Mesh)
		{
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorPreviewLOD, terrainData.useFlatShading));
		}
		else if (drawMode == DrawMode.FalloffMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
		}
	}

	public void RequestMapData(Vector2 center, Action<MapData> callback) //multi threading
    {
		ThreadStart threadStart = delegate //represents the MapDataThread
		{
			MapDataThread(center,callback);
		};

		new Thread(threadStart).Start();
    }

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) //multi threading
	{
		ThreadStart threadStart = delegate //represents the MeshDataThread
		{
			MeshDataThread(mapData, lod, callback);
		};

		new Thread(threadStart).Start();
	}


	void MapDataThread(Vector2 center, Action<MapData> callback) //multi threading
    {
		MapData mapData = GenerateMapData(center);
		lock (mapDataThreadInfoQueue){ //lock this, all threads one at a time here
			mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
		}
    }

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) //multi threading
	{
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, terrainData.useFlatShading);
		lock (meshDataThreadInfoQueue)
		{ //lock this, all threads one at a time here
			meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
		}
	}

	void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
        }

		if (meshDataThreadInfoQueue.Count > 0)
		{
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
			{
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
	}

    MapData GenerateMapData(Vector2 center) {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode); //+2 compensates for border

		if (terrainData.useFalloff)
		{

            if (falloffMap == null)
            {
				falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
            }

			for (int y = 0; y < mapChunkSize+2; y++)
			{
				for (int x = 0; x < mapChunkSize+2; x++)
				{
					if (terrainData.useFalloff) //if using fallOffMap
					{
						noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
					}
				}
			}
		}
		return new MapData(noiseMap);
	}

	void OnValidate() {

        if (terrainData != null)
        {
			terrainData.OnValuesUpdated -= OnValuesUpdated; //always keeps subscribed count at 1 by unsubscribing. This does nothing if that count is already 0
			terrainData.OnValuesUpdated += OnValuesUpdated;
        }

		if (noiseData != null)
		{
			noiseData.OnValuesUpdated -= OnValuesUpdated; //always keeps subscribed count at 1 by unsubscribing. This does nothing if that count is already 0
			noiseData.OnValuesUpdated += OnValuesUpdated;
		}
        if (textureData != null)
        {
			textureData.OnValuesUpdated -= OnValuesUpdated; //always keeps subscribed count at 1 by unsubscribing. This does nothing if that count is already 0
			textureData.OnValuesUpdated += OnValuesUpdated;
		}

	}

	struct MapThreadInfo<T> //T for generic, handle MapData and MeshData
    {
		public readonly Action<T> callback;
		public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

}


public struct MapData
{
	public readonly float[,] heightMap;

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}