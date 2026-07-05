using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Collision
{
    public static class AABBPhysics
    {
        public static bool OverlapsXZ(Vector3 pos, float radius, AABBCollider ignore = null)
        {
            IReadOnlyList<AABBCollider> boxes = AABBCollider.All;
            for (int i = 0; i < boxes.Count; i++)
            {
                AABBCollider b = boxes[i];
                if (b == null || b == ignore)
                    continue;
                b.GetXZBounds(out float minX, out float maxX, out float minZ, out float maxZ);
                if (pos.x >= minX - radius && pos.x <= maxX + radius &&
                    pos.z >= minZ - radius && pos.z <= maxZ + radius)
                    return true;
            }
            return false;
        }

        public static Vector3 ResolvePlanarMove(Vector3 current, Vector3 planarDelta, float radius, AABBCollider ignore = null)
        {
            Vector3 result = Vector3.zero;

            Vector3 tryX = current + new Vector3(planarDelta.x, 0f, 0f);
            if (OverlapsXZ(tryX, radius, ignore) == false)
                result.x = planarDelta.x;

            Vector3 afterX = current + new Vector3(result.x, 0f, 0f);
            Vector3 tryZ = afterX + new Vector3(0f, 0f, planarDelta.z);
            if (OverlapsXZ(tryZ, radius, ignore) == false)
                result.z = planarDelta.z;

            return result;
        }

        public static bool SegmentBlockedXZ(Vector3 from, Vector3 to, float radius, AABBCollider ignore = null)
        {
            const int steps = 6;
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 p = Vector3.Lerp(from, to, t);
                if (OverlapsXZ(p, radius, ignore))
                    return true;
            }
            return false;
        }
    }
}
