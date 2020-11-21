using UnityEngine;
using System.Collections;

public static class MeshGenerator {

	public const int numSupportedLODs = 5;
	public const int numSupportedChunkSizes = 9; //must match length of below array
	public const int numSupportedFlatShadedChunkSizes = 3; //must match length of below array
	public static readonly int[] supportedChunkSizes = {48,72,96,120,144,168,192,216,240};
	public static readonly int[] supportedFlatShadedChunkSizes = {48, 72, 96};

	//heightMultiplier is for making the map not flat, heightCurve is for making it so the water doesn't also increase in height. heightCurve should match the level of the water near exactly.
	public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool useFlatShading) {
		AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2; //if LOD = 0 set meshSimplIncr = 1, otherwise set it to LOD * 2

		int borderedSize = heightMap.GetLength (0);
		int meshSize = borderedSize - 2 * meshSimplificationIncrement;
		int meshSizeUnsimplified = borderedSize - 2; //use this for vertexPosition, topLeftX and topLeftZ so that its not dependent on LOD of mesh

		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData (verticesPerLine, useFlatShading);

		int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;

		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
		{
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
			{
				bool isBorderVertex = y==0 || y==borderedSize -1 || x==0 || x==borderedSize-1;
                if (isBorderVertex)
                {
					vertexIndicesMap[x, y] = borderVertexIndex;
					borderVertexIndex--;
                }
                else
                {
					vertexIndicesMap[x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				int vertexIndex = vertexIndicesMap[x, y];
				Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
				float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
				Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

				meshData.AddVertex(vertexPosition, percent, vertexIndex);

				if (x < borderedSize - 1 && y < borderedSize - 1) { //create 2 triangles out of 4 points ABCD essentially a square. 1:ADC 2:DAB
					int a = vertexIndicesMap[x, y];
					int b = vertexIndicesMap[x+meshSimplificationIncrement, y];
					int c = vertexIndicesMap[x, y+meshSimplificationIncrement];
					int d = vertexIndicesMap[x+meshSimplificationIncrement, y+meshSimplificationIncrement];
					meshData.AddTriangle (a,d,c); //1
					meshData.AddTriangle (d,a,b); //2
				}

				vertexIndex++;
			}
		}
		meshData.FinalizeProcessMesh();

		return meshData;

	}
}

public class MeshData {
	Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;
	Vector3[] bakedNormals;

	Vector3[] borderVertices;
	int[] borderTriangles;

	int triangleIndex;
	int borderTriangleIndex;

	bool useFlatShading; //use flat shading or not

	public MeshData(int verticesPerLine, bool useFlatShading) {
		this.useFlatShading = useFlatShading;
		vertices = new Vector3[verticesPerLine * verticesPerLine];
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine-1)*(verticesPerLine-1)*6];

		borderVertices = new Vector3[verticesPerLine*4+4];
		borderTriangles = new int[24*verticesPerLine]; //6*4*verticesPerLine
	}

	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {

        if (vertexIndex < 0)
        {
			borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
			vertices[vertexIndex] = vertexPosition;
			uvs[vertexIndex] = uv;
        }

    }

	public void AddTriangle(int a, int b, int c)
	{
		if (a < 0 || b < 0 || c < 0)
		{
			borderTriangles[borderTriangleIndex] = a;
			borderTriangles[borderTriangleIndex + 1] = b;
			borderTriangles[borderTriangleIndex + 2] = c;
			borderTriangleIndex += 3;
		}
		else {
			triangles[triangleIndex] = a;
			triangles[triangleIndex + 1] = b;
			triangles[triangleIndex + 2] = c;
			triangleIndex += 3;
		}
	}

	Vector3[] CalculateNormals()
    {
		Vector3[] vertexNormals = new Vector3[vertices.Length]; //for storing the normals
		int trianglesCount = triangles.Length / 3;
        for (int i = 0; i < trianglesCount; i++)
        {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles[normalTriangleIndex]; //3 vertices in each triangles array, thus A B C by addition
			int vertexIndexB = triangles[normalTriangleIndex+1];
			int vertexIndexC = triangles[normalTriangleIndex+2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA,vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		int borderTrianglesCount = borderTriangles.Length / 3;
		for (int i = 0; i < borderTrianglesCount; i++)
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = borderTriangles[normalTriangleIndex]; //3 vertices in each triangles array, thus A B C by addition
			int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
			int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

			if (vertexIndexA >= 0)
			{
				vertexNormals[vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0) {
				vertexNormals[vertexIndexB] += triangleNormal;
			}
            if (vertexIndexC >= 0) {
				vertexNormals[vertexIndexC] += triangleNormal;
			}
		}

		for (int i = 0; i < vertexNormals.Length; i++)
        {
			vertexNormals[i].Normalize();
        }

		return vertexNormals;
    }

	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
		Vector3 pointA = (indexA<0)?borderVertices[-indexA - 1]:vertices[indexA];
		Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
		Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

		//use cross-product method to calculate surface normal
		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;

		return Vector3.Cross(sideAB,sideAC).normalized; //cross product


	}

	private void BakeNormals() //using this to separate CalculateNormals off the main thread
    {
		bakedNormals = CalculateNormals();
    }

	public void FinalizeProcessMesh()
    {
        if (useFlatShading)
        {
			FlatShading();
        }
        else
        {
			BakeNormals(); //don't need to bake normals for flat shading, since no edges of chunks are needed to be be blended
        }
    }

	void FlatShading() //flat shading option, no blended shading
    {
		Vector3[] flatShadedVertices = new Vector3[triangles.Length];
		Vector2[] flatShadedUvs = new Vector2[triangles.Length];

        for (int i=0; i < triangles.Length; i++)
        {
			flatShadedVertices[i] = vertices[triangles[i]];
			flatShadedUvs[i] = uvs[triangles[i]];
			triangles[i] = i;
        }
		vertices = flatShadedVertices;
		uvs = flatShadedUvs;
    }

	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;

        if (useFlatShading)
        {
			mesh.RecalculateNormals();
        }
        else
        {
			mesh.normals = bakedNormals;
		}
		return mesh;
	}

}