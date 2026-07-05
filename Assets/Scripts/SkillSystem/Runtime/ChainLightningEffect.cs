using System.Collections.Generic;
using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{
    public class ChainLightningEffect : BaseBehaviour
    {
        private const int SubPerSegment = 4;
        private const float JagFactor = 0.15f;

        private static Material _sharedMat;
        private static readonly List<Vector3> _pathBuf = new List<Vector3>(64);

        private LineRenderer _lr;
        private float _life;
        private float _age;
        private Color _color;

        public static void Spawn(IList<Vector3> nodes, Color color, float life = 0.22f, float width = 0.22f)
        {
            if (nodes == null || nodes.Count < 2)
                return;

            GameObject go = new GameObject("ChainLightningFx");

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = SharedMat();
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.widthMultiplier = width;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            BuildJaggedPath(nodes);
            lr.positionCount = _pathBuf.Count;
            for (int i = 0; i < _pathBuf.Count; i++)
                lr.SetPosition(i, _pathBuf[i]);

            ChainLightningEffect fx = go.AddComponent<ChainLightningEffect>();
            fx._lr = lr;
            fx._life = life > 0f ? life : 0.18f;
            fx._color = color;
            fx.ApplyAlpha(1f);
        }

        private static void BuildJaggedPath(IList<Vector3> nodes)
        {
            _pathBuf.Clear();
            _pathBuf.Add(nodes[0]);

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Vector3 a = nodes[i];
                Vector3 b = nodes[i + 1];
                Vector3 dir = b - a;
                float len = dir.magnitude;
                if (len < 0.0001f)
                {
                    _pathBuf.Add(b);
                    continue;
                }

                Vector3 fwd = dir / len;
                Vector3 side = Vector3.Cross(fwd, Vector3.up);
                if (side.sqrMagnitude < 0.0001f)
                    side = Vector3.right;
                side.Normalize();
                Vector3 up = Vector3.Cross(side, fwd);
                float mag = len * JagFactor;

                for (int s = 1; s <= SubPerSegment; s++)
                {
                    float t = (float)s / (SubPerSegment + 1);
                    Vector3 p = Vector3.Lerp(a, b, t);
                    p += side * Random.Range(-mag, mag) + up * Random.Range(-mag, mag);
                    _pathBuf.Add(p);
                }
                _pathBuf.Add(b);
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            ApplyAlpha(1f - t);
        }

        private void ApplyAlpha(float alpha)
        {
            if (_lr == null)
                return;
            Color c = _color;
            c.a = alpha;
            _lr.startColor = c;
            _lr.endColor = c;
        }

        private static Material SharedMat()
        {
            if (_sharedMat == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                if (sh == null)
                    sh = Shader.Find("Unlit/Color");
                _sharedMat = new Material(sh);
            }
            return _sharedMat;
        }
    }
}
