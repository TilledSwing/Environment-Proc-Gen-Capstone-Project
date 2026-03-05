using UnityEngine;

public class WaterLevelChecker : MonoBehaviour
{
    public int waterLevel;
    public ParticleSystem particleSystem;
    void Start()
    {
        waterLevel = ChunkGenNetwork.Instance.terrainDensityData.waterLevel;
    }

    void Update()
    {
        if (transform.position.y > waterLevel && ChunkGenNetwork.Instance.terrainDensityData.water && particleSystem.isPlaying)
        {
            transform.position.Set(transform.position.x, waterLevel, transform.position.z);
        }
    }
}
