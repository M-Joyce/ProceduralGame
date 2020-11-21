using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public static float maxViewDistance;


    public LODInfo[] detailLevels;

    public Transform viewer; //the player
    public Material mapMaterial; //map material

    const float viewerMoveThresholdForChunkUpdate = 25f; //for when to update chunks
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate; //squared
    const float colliderGenerationDistanceThreshold = 5f; //distance the player needs to be from a chunk border to trigger that chunk to generate a collision mesh/collider

    public int colliderLODIndex;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisible; //chunks visible in the view distance

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>(); //dictionary of TerrainChunks
    static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold; //Max view distance is the last LOD setting (this should be the furthest LOD visibleDstThreshold)
        chunkSize = mapGenerator.mapChunkSize - 1;
        chunksVisible = Mathf.RoundToInt(maxViewDistance/chunkSize);
        UpdateVisibleChunks(); //first update, otherwise Update() will have issues
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x,viewer.position.z) / mapGenerator.terrainData.uniformScale; //dividing by scale keeps this in sync with our map scale

        if (viewerPosition != viewerPositionOld) //If the player has moved since the last Update(), update the chunks with new collision meshes
        {
            foreach(TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) { //determine when to update chunks
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();

        for (int i = visibleTerrainChunks.Count - 1; i>=0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisible; yOffset <= chunksVisible; yOffset++)
        {
            for (int xOffset = -chunksVisible; xOffset <= chunksVisible; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord)) //dont update the chunk if already updated
                {
                    if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();

                    }
                    else
                    {
                        terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, colliderLODIndex, transform, mapMaterial));
                    }
                }
            }
        }
    }

    public class TerrainChunk //represents a chunk of terrain
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLODIndex;
        int previousLODIndex = -1;

        MapData mapData;
        bool mapDataReceived;
        bool hasSetCollider;

        public Vector2 coord;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material)
        {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);

            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("TerrainChunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();

            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale; //scale is for scaling the entire map size
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;
           
            meshObject.transform.parent = parent; //set the meshObject to be parented under the parent, keeps the editor clean from non-parented objects
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex)
                {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }

            }

            mapGenerator.RequestMapData(position,OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool wasVisible = isVisible();
                bool visible = viewerDistanceFromNearestEdge <= maxViewDistance; //is this terrain visible;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;                 
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                }

                if (wasVisible != visible) {
                    if (visible)
                    {
                        visibleTerrainChunks.Add(this);
                    }
                    else{
                        visibleTerrainChunks.Remove(this);
                    }
                    SetVisible(visible);
                }
            }
        }

        public void UpdateCollisionMesh()
        {
            if (!hasSetCollider) { //don't do anything here if the collider has already been set
                float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

                if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) //ensure the mesh is requested
                {
                    if (!lodMeshes[colliderLODIndex].hasRequestedMesh) //if hasnt yet requested a mesh
                    {
                        lodMeshes[colliderLODIndex].RequestMesh(mapData); //request mesh
                    }
                }

                if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) //if within distance threshold to generate collision mesh
                {
                    if (lodMeshes[colliderLODIndex].hasMesh) //if has mesh
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh; //generate collision mesh
                        hasSetCollider = true;
                    }
                }
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible); //set visible or not
        }

        public bool isVisible()
        {
            return meshObject.activeSelf;
        }

    }

    class LODMesh
    {

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo
    {
        [Range(0,MeshGenerator.numSupportedLODs -1)] //constrain number of LODs
        public int lod;
        public float visibleDstThreshold; //distance which within this LOD is active, once the view is outside of this threshold it switches to the next lower resolution LOD
        public bool useForCollider; //using the hight level of detail for this is most accurate, but could be slower

        public float sqrVisibleDstThreshold
        {
            get
            {
                return visibleDstThreshold * visibleDstThreshold;
            }
        }
    }

}
