using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour {

	public enum DrawMode {NoiseMap, ColourMap, Mesh};
	public DrawMode drawMode;

	public Noise.NormalizeMode normalizeMode;

	public const int mapChunkSize = 241; //chunk size 240 (+ 1 is needed)
	
	[Range(0,6)] //LOD clamped to 0-6
	public int editorPreviewLOD; //LOD Setting

	public float noiseScale;

	public int octaves;

	[Range(0,1)] //persistance clamped to 0-1
	public float persistance;

	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public float meshHeightMultiplier;

	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;

	public TerrainType[] regions;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	public void DrawMapInEditor()
    {
		MapData mapData = GenerateMapData(Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay>();
		if (drawMode == DrawMode.NoiseMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
		}
		else if (drawMode == DrawMode.ColourMap)
		{
			display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colorMap, mapChunkSize, mapChunkSize));
		}
		else if (drawMode == DrawMode.Mesh)
		{
			display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colorMap, mapChunkSize, mapChunkSize));
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
		MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
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
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, center + offset, normalizeMode);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight >= regions [i].height) {
						colourMap [y * mapChunkSize + x] = regions [i].colour;
					}
                    else
                    {
						break;
                    }
				}
			}
		}
		return new MapData(noiseMap,colourMap);
	}

	void OnValidate() {

		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
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

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

public struct MapData
{
	public readonly float[,] heightMap;
	public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}