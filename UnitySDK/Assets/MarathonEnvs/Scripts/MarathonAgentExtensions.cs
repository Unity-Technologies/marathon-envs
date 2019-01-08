

using UnityEngine;

namespace MLAgents
{
    public static class MarathonAgentExtensions
    {
        public static Vector3 GetNormalizedVelocity(this Agent agent, Vector3 metersPerSecond) 
        {
            var maxMetersPerSecond = (agent.agentBoundsMaxOffset - agent.agentBoundsMinOffset) 
                / agent.agentParameters.maxStep
                / Time.fixedDeltaTime;
            var maxXZ = Mathf.Max(maxMetersPerSecond.x, maxMetersPerSecond.z);
            maxMetersPerSecond.x = maxXZ;
            maxMetersPerSecond.z = maxXZ;
            maxMetersPerSecond.y = 53; // override with
            float x = metersPerSecond.x / maxMetersPerSecond.x;
            float y = metersPerSecond.y / maxMetersPerSecond.y;
            float z = metersPerSecond.z / maxMetersPerSecond.z;
            // clamp result
            x = Mathf.Clamp(x, -1f, 1f);
            y = Mathf.Clamp(y, -1f, 1f);
            z = Mathf.Clamp(z, -1f, 1f);
            Vector3 normalizedVelocity = new Vector3(x,y,z);
            return normalizedVelocity;
        }
        public static Vector3 GetNormalizedPosition(this Agent agent, Vector3 pos)
        {
            var maxPos = (agent.agentBoundsMaxOffset - agent.agentBoundsMinOffset);
            float x = pos.x / maxPos.x;
            float y = pos.y / maxPos.y;
            float z = pos.z / maxPos.z;
            // clamp result
            x = Mathf.Clamp(x, -1f, 1f);
            y = Mathf.Clamp(y, -1f, 1f);
            z = Mathf.Clamp(z, -1f, 1f);
            Vector3 normalizedPos = new Vector3(x,y,z);
            return normalizedPos;
        }
    } 
}