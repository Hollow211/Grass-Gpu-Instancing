using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public struct DataLayout
{
    public Matrix4x4 matrix;
    public Vector2 worldUV;
    public Vector3 Normal;
    public float colorIndex;
}

public class SpawnGrass : MonoBehaviour
{
    private TerrainData terrainData;
    private TerrainMisc terrainHelper;
    private Vector3 terrainSize;
    #region Terrain Props
    TerrainData mTerrainData;
    int alphamapWidth;
    int alphamapHeight;

    float[,,] mSplatmapData;
    int mNumTextures;
    #endregion

    public Material material;
    public Mesh mesh;
    private RenderParams rp;

    GraphicsBuffer commandBuf;
    GraphicsBuffer instanceDataBuffer;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 1;

    #region Distribution Variables
    [SerializeField] private float X_AxisDistribution = 0.03f;
    [SerializeField] private float Z_AxisDistribution = 0.03f;
    [SerializeField] private float X_RandomSeed = 0.05f;
    [SerializeField] private float Z_RandomSeed = 0.05f;
    #endregion

    #region Scale Variables
    [SerializeField] private Vector3 Scale = new Vector3(1, 1, 1);
    [SerializeField] private Vector2 XRandomScale = new Vector3(3, 7);
    [SerializeField] private Vector2 YRandomScale = new Vector3(2, 4);
    #endregion

    [SerializeField] private float ProbabilityOfGrass = 1f;


    private List<Vector3> DistributionPositions;
    private List<DataLayout> instData;

    private void Start()
    {


        GetTerrainProps();

        Vector2 SizeOfTerrainXZ = new Vector2(mTerrainData.size.x, mTerrainData.size.z);
        Shader.SetGlobalVector("_TerrainSize", SizeOfTerrainXZ);

        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments,
            commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
        
        terrainData = GetComponent<Terrain>().terrainData;
        terrainHelper = GetComponent<TerrainMisc>();
        terrainSize = terrainData.size;

        // Variable Initializations
        DistributionPositions = new List<Vector3>();
        instData = new List<DataLayout>();
        rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, mTerrainData.size); // use tighter bounds for better FOV culling

        // Get positions
        fillInstData();
    }

    void OnDestroy()
    {
        commandBuf?.Release();
        commandBuf = null;

        instanceDataBuffer?.Release();
        instanceDataBuffer = null;
    }

    void Update()
    {
        Graphics.RenderMeshIndirect(rp, mesh, commandBuf, commandCount);
    }

    void fillInstData()
    {
        int InstanceCount = 0;
        Vector3 terrainPosition = transform.position;
        for (float x = terrainPosition.x; x < terrainSize.x / 2; x += X_AxisDistribution)
        {
            for (float z = terrainPosition.z; z < terrainSize.z / 2; z += Z_AxisDistribution)
            {
                float FillorNot = Random.Range((float)0, (float)1);
                if (FillorNot > ProbabilityOfGrass)
                {
                    continue;
                }

                #region position
                //DistributionPositions[counter] = new Vector3(i, 0, j);
                float X_Axis = x + Random.Range(-X_RandomSeed, X_RandomSeed);
                float Z_Axis = z + Random.Range(-Z_RandomSeed, Z_RandomSeed);

                // Clamp the values to ensure they stay within terrain bounds
                X_Axis = Mathf.Clamp(X_Axis, terrainPosition.x, terrainSize.x / 2 - 0.1f);
                Z_Axis = Mathf.Clamp(Z_Axis, terrainPosition.z, terrainSize.z / 2 - 0.1f);

                Vector3 HeightMapScale = terrainData.heightmapScale;
                Vector3 new_position = new Vector3(X_Axis, 
                    Terrain.activeTerrain.SampleHeight(new Vector3(X_Axis,0,Z_Axis)),
                    Z_Axis);

                Ray ray = new Ray(new Vector3(X_Axis, 10, Z_Axis), Vector3.down);

                bool TerrainCheck = Physics.Raycast(ray, Mathf.Infinity, (1 << 6));

                if (TerrainCheck)
                {
                    continue;
                }

                int TerrainTexture = GetTerrainAtPosition(new_position);

                if (TerrainTexture == 2 || TerrainTexture == 3)
                    continue;

                #endregion

                #region Scale
                float RandomXScale = Random.Range(XRandomScale.x, XRandomScale.y);
                float RandomYScale = Random.Range(YRandomScale.x, YRandomScale.y);
                Vector3 new_Scale = new Vector3(RandomXScale,RandomYScale, Scale.z);
                #endregion

                #region Normals
                Vector3 Normal = new Vector3(0, 1, 0);
                #endregion
                
                DataLayout new_entry = new DataLayout();

                new_entry.matrix = Matrix4x4.TRS(new_position,
                Quaternion.Euler(0, Random.Range(0, 0), 0),
                new_Scale);

                new_entry.worldUV = new Vector2(new_position.x / terrainData.size.x, new_position.z / terrainData.size.z);

                new_entry.Normal = terrainData.GetInterpolatedNormal(new_position.x, new_position.z);

                new_entry.colorIndex = TerrainTexture;

                instData.Add(new_entry);
                InstanceCount++;

            }
        }
        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)InstanceCount;
        commandBuf.SetData(commandData);


        instanceDataBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            instData.Count,
            Marshal.SizeOf(typeof(DataLayout))
        );
        instanceDataBuffer.SetData(instData.ToArray());
        material.SetBuffer("_InstanceDataBuffer", instanceDataBuffer);

    }

    #region Terrain Functions

    private void GetTerrainProps()
    {
        mTerrainData = Terrain.activeTerrain.terrainData;
        alphamapWidth = mTerrainData.alphamapWidth;
        alphamapHeight = mTerrainData.alphamapHeight;

        mSplatmapData = mTerrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
        mNumTextures = mSplatmapData.Length / (alphamapWidth * alphamapHeight);
    }

    private Vector3 ConvertToSplatMapCoordinate(Vector3 playerPos)
    {
        Vector3 vecRet = new Vector3();
        Terrain ter = Terrain.activeTerrain;
        Vector3 terPosition = ter.transform.position;
        vecRet.x = ((playerPos.x - terPosition.x) / ter.terrainData.size.x) * ter.terrainData.alphamapWidth;
        vecRet.z = ((playerPos.z - terPosition.z) / ter.terrainData.size.z) * ter.terrainData.alphamapHeight;
        return vecRet;
    }

    private int GetActiveTerrainTextureIdx(Vector3 pos)
    {
        Vector3 TerrainCord = ConvertToSplatMapCoordinate(pos);
        int ret = 0;
        float comp = 0f;
        for (int i = 0; i < mNumTextures; i++)
        {
            if (comp < mSplatmapData[(int)TerrainCord.z, (int)TerrainCord.x, i])
                ret = i;
        }
        return ret;
    }

    private int GetTerrainAtPosition(Vector3 pos)
    {
        int terrainIdx = GetActiveTerrainTextureIdx(pos);
        return terrainIdx;
    }

    #endregion


}
