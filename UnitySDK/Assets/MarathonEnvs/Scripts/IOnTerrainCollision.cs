using UnityEngine;

namespace MLAgents 
{
    public interface IOnTerrainCollision
    {
        void OnTerrainCollision(GameObject other, GameObject terrain);
    }
}