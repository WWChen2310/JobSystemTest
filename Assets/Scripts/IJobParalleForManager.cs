using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
struct NoiseParalleForJob : IJobParallelFor
{
    public float ElapsedTime;
    public float DeltaTime;
    //[WriteOnly]
    public NativeArray<Vector3> Offsets;
    //[ReadOnly]
    public NativeArray<Vector3> OriginPositions;

    public void Execute(int index)
    {
        var p = OriginPositions[index];
        var sinx = Mathf.Sin(1f * ElapsedTime + Perlin.Noise(0.3f * p.x + ElapsedTime));
        var siny = Mathf.Cos(1f * ElapsedTime + Perlin.Noise(0.5f * p.y + ElapsedTime));
        var sinz = Mathf.Sin(1f * ElapsedTime + Perlin.Noise(0.7f * p.z - ElapsedTime));
        Offsets[index] = 2f * new Vector3(sinx, siny, sinz);
    }
}

public class IJobParalleForManager : MonoBehaviour
{
    public int WorldEdgeSize = 15;
    private Transform[] m_cubes;
    private JobHandle m_jobHandle;
    private NativeArray<Vector3> m_native_Offsets;
    private NativeArray<Vector3> m_native_Positions;

    void OnEnable()
    {
        m_cubes = new Transform[WorldEdgeSize * WorldEdgeSize * WorldEdgeSize];
        m_native_Offsets = new NativeArray<Vector3>(m_cubes.Length, Allocator.Persistent);
        m_native_Positions = new NativeArray<Vector3>(m_cubes.Length, Allocator.Persistent);

        var index = 0;
        for (int x = 0; x < WorldEdgeSize; x++)
        {
            for (int y = 0; y < WorldEdgeSize; y++)
            {
                for (int z = 0; z < WorldEdgeSize; z++)
                {
                    m_cubes[index] = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    m_cubes[index].position = new Vector3(x, y, z) * 5f - new Vector3(WorldEdgeSize * 5f * 0.5f, WorldEdgeSize * 5f * 0.5f, 0);
                    m_native_Positions[index] = m_cubes[index].position;
                    index++;
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var noisejob = new NoiseParalleForJob
        {
            ElapsedTime = Time.time,
            DeltaTime = Time.deltaTime,
            Offsets = m_native_Offsets,
            OriginPositions = m_native_Positions,
        };
        m_jobHandle = noisejob.Schedule(m_cubes.Length, 1, m_jobHandle);
    }

    void LateUpdate()
    {
        m_jobHandle.Complete();
        for (int i = 0; i < m_native_Offsets.Length; i++)
        {
            m_cubes[i].position = m_native_Positions[i] + m_native_Offsets[i];
        }        
    }

    private void OnDisable()
    {
        m_native_Offsets.Dispose();
        m_native_Positions.Dispose();
    }
}
