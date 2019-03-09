using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;


public class TerrainGenerator : MonoBehaviour
{

    Terrain terrain;
	Agent _agent;
	public int posXInTerrain;
	public int posYInTerrain;
	float[,] _heights;
	float[,] _rowHeight;

	public int heightIndex;
	public float curHeight;
	public float actionReward;

	internal const float _minHeight = 0f;
	internal const float _maxHeight = 10f;
	internal const float _minSpawnHeight = 0f;//2f;
	internal const float _maxSpawnHeight = 10f;//8f;
	const float _midHeight = 5f;
	float _mapScaleY;
	float[,] _heightMap;
	public List<float> debugLastHeights;
	public List<float> debugLastNormHeights;
	public float debugLastFraction;

	PhysicsScene physicsScene;

    // Start is called before the first frame update
    void Start()
    {
        physicsScene = (GetComponentInParent<SpawnableEnv>().GetPhysicsScene());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Reset()
    {
		if (this.terrain == null)
		{
			var parent = gameObject.transform.parent;
			terrain = parent.GetComponentInChildren<Terrain>();
			var sharedTerrainData = terrain.terrainData;
			terrain.terrainData = new TerrainData();
			terrain.terrainData.heightmapResolution = sharedTerrainData.heightmapResolution;
			terrain.terrainData.baseMapResolution = sharedTerrainData.baseMapResolution;
			terrain.terrainData.SetDetailResolution(sharedTerrainData.detailResolution, sharedTerrainData.detailResolutionPerPatch);
			terrain.terrainData.size = sharedTerrainData.size;
			terrain.terrainData.thickness = sharedTerrainData.thickness;
			terrain.terrainData.splatPrototypes = sharedTerrainData.splatPrototypes;
			terrain.terrainData.terrainLayers = sharedTerrainData.terrainLayers;
			var collider = terrain.GetComponent<TerrainCollider>();
			collider.terrainData = terrain.terrainData;
			_rowHeight = new float[terrain.terrainData.heightmapResolution,1];
		}
		if (this._agent == null)
			_agent = GetComponent<Agent>();
        //print($"HeightMap {this.terrain.terrainData.heightmapWidth}, {this.terrain.terrainData.heightmapHeight}. 
		// Scale {this.terrain.terrainData.heightmapScale}. Resolution {this.terrain.terrainData.heightmapResolution}");
        _mapScaleY = this.terrain.terrainData.heightmapScale.y;
		// get the normalized position of this game object relative to the terrain
        Vector3 tempCoord = (transform.position - terrain.gameObject.transform.position);
        Vector3 coord;
		tempCoord.x = Mathf.Clamp(tempCoord.x,0f, terrain.terrainData.size.x-0.000001f);
		tempCoord.z = Mathf.Clamp(tempCoord.z,0f, terrain.terrainData.size.z-0.000001f);
        coord.x = (tempCoord.x-1) / terrain.terrainData.size.x;
        coord.y = tempCoord.y / terrain.terrainData.size.y;
        coord.z = tempCoord.z / terrain.terrainData.size.z;
        // get the position of the terrain heightmap where this game object is
        posXInTerrain = (int) (coord.x * terrain.terrainData.heightmapWidth); 
        posYInTerrain = (int) (coord.z * terrain.terrainData.heightmapHeight);
        // we set an offset so that all the raising terrain is under this game object
        int offset = 0 / 2;
        // get the heights of the terrain under this game object
        _heights = terrain.terrainData.GetHeights(posXInTerrain-offset,posYInTerrain-offset, 100,1);
		curHeight = _midHeight;
		heightIndex = posXInTerrain;
		actionReward = 0f;
        
		ResetHeights();        
    }
	void ResetHeights()
	{
		if (_heightMap == null){
			_heightMap = new float [terrain.terrainData.heightmapWidth, terrain.terrainData.heightmapHeight];
        }
		heightIndex = 0;
		while(heightIndex <posXInTerrain)
			SetNextHeight(0);

		SetNextHeight(0);
		SetNextHeight(0);
		SetNextHeight(0);
		SetNextHeight(0);
		SetNextHeight(0);
		SetNextHeight(0);
		while(heightIndex < terrain.terrainData.heightmapWidth)
		{
			int action = Random.Range(0,21);
			try
			{
				SetNextHeight(action);				
			}
			catch (System.Exception ex)
			{
				SetNextHeight(action);
				throw;
			}
		}
		this.terrain.terrainData.SetHeights(0, 0, _heightMap);

	}
	void SetNextHeight(int action)
	{
		float actionSize = 0f;
		bool actionPos = (action-1) % 2 == 0;
		if (action != 0)
		{
			actionSize = ((float)((action+1)/2)) * 0.1f;
			curHeight += actionPos ? actionSize : -actionSize;
			if (curHeight < _minSpawnHeight) {
				curHeight = _minSpawnHeight;
				actionSize = 0;
			}
			if (curHeight > _maxSpawnHeight)
			{
				curHeight = _maxSpawnHeight;
				actionSize = 0;
			}
		}

		float height = curHeight / _mapScaleY;
		// var unit = terrain.terrainData.heightmapWidth / (int)_mapScaleY;
		int unit = 1;
		int startH = heightIndex * unit;
        for (int h = startH; h < startH+unit; h++)
        {
            for (int w = 0; w < terrain.terrainData.heightmapWidth; w++){
                _heightMap[w,h] = height;
			}
			height += 1/300f/_mapScaleY;
        }
		heightIndex++;
	}

	public List<float> GetDistances2d(IEnumerable<Vector3> points)
	{
		List<float> distances = points
			.Select(x=> GetDistance2d(x))
			.ToList();
		return distances;
	}
	float GetDistance2d(Vector3 point)
	{
		int layerMask = ~(1 << 14);
		RaycastHit hit;
		if (!physicsScene.Raycast(point, Vector3.down, out hit,_maxHeight,layerMask))
			return 1f;
		float distance = hit.distance;
		distance = Mathf.Clamp(distance, -1f, 1f);
		return distance;
	}

	public bool IsPointOffEdge(Vector3 point)
	{
        Vector3 localPos = (point - terrain.gameObject.transform.position);
		bool isOffEdge = false;
		isOffEdge |= (localPos.z < 0f);
		isOffEdge |= (localPos.z >= terrain.terrainData.size.z);
		return isOffEdge;
	}

	public (List<float>, float) GetDistances2d(Vector3 pos, bool showDebug)
	{
		int layerMask = ~(1 << 14);
        var xpos = pos.x;
        xpos -= 2f;
        float fraction = (xpos - (Mathf.Floor(xpos*5)/5)) * 5;
        float ypos = pos.y;
        List<Ray> rays = Enumerable.Range(0, 5*7).Select(x => new Ray(new Vector3(xpos+(x*.2f), TerrainGenerator._maxHeight, pos.z), Vector3.down)).ToList();
		RaycastHit hit;
		List<float> distances = rays.Select
			( ray=> {
				if (!physicsScene.Raycast(ray.origin, ray.direction, out hit,_maxHeight,layerMask))
					return _maxHeight;
				return ypos - (_maxHeight - hit.distance);
			}).ToList();
        if (Application.isEditor && showDebug)
        {
            var view = distances.Skip(10).Take(20).Select(x=>x).ToList();
            Monitor.Log("distances", view.ToArray());
            var time = Time.deltaTime;
            time *= _agent.agentParameters.numberOfActionsBetweenDecisions;
            for (int i = 0; i < rays.Count; i++)
            {
                var distance = distances[i];
                var origin = new Vector3(rays[i].origin.x, ypos,0f);
                var direction = distance > 0 ? Vector3.down : Vector3.up;
                var color = distance > 0 ? Color.yellow : Color.red;
                Debug.DrawRay(origin, direction*Mathf.Abs(distance), color, time, false);
            }
        }
		List<float> normalizedDistances = distances
			.Select(x => Mathf.Clamp(x, -10f, 10f))
			.Select(x => x/10f)
			.ToList();
		;
		debugLastNormHeights = normalizedDistances;
		debugLastHeights = distances;
		debugLastFraction = fraction;

		return (normalizedDistances, fraction); 
	}    
}    
