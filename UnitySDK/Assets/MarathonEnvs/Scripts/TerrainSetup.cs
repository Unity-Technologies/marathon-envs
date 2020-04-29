using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSetup : MonoBehaviour
{
    public Vector3 TerrainSize;
    // Start is called before the first frame update
    void Start()
    {
        var terrain = GetComponent<Terrain>();
        terrain.terrainData.size = TerrainSize;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
