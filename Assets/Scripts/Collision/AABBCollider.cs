using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Collision
{
    public class AABBCollider : BaseBehaviour
    {
        [SerializeField] private Vector3 _center = Vector3.zero;
        [SerializeField] private Vector3 _size = new Vector3(1f, 2f, 1f);

        private static readonly List<AABBCollider> _all = new List<AABBCollider>(64);
        public static IReadOnlyList<AABBCollider> All { get { return _all; } }

        public Vector3 Center { get { return _center; } set { _center = value; } }
        public Vector3 Size { get { return _size; } set { _size = value; } }
        public Vector3 WorldCenter { get { return transform.position + _center; } }

        protected override void OnEnabled()
        {
            if (_all.Contains(this) == false)
                _all.Add(this);
        }

        protected override void OnDisabled()
        {
            _all.Remove(this);
        }

        public void GetXZBounds(out float minX, out float maxX, out float minZ, out float maxZ)
        {
            Vector3 c = WorldCenter;
            float hx = Mathf.Abs(_size.x) * 0.5f;
            float hz = Mathf.Abs(_size.z) * 0.5f;
            minX = c.x - hx;
            maxX = c.x + hx;
            minZ = c.z - hz;
            maxZ = c.z + hz;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 c = WorldCenter;
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.12f);
            Gizmos.DrawCube(c, _size);
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(c, _size);
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0.3f, 1f);
            UnityEditor.Handles.Label(c + Vector3.up * (Mathf.Abs(_size.y) * 0.5f + 0.15f), name + " (AABB)");
        }
#endif
    }
}
