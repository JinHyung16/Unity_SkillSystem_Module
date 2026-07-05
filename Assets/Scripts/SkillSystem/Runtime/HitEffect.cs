using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{

    public class HitEffect : BaseBehaviour
    {
        private static Material _sharedMat;

        private float _life;
        private float _age;
        private Vector3 _baseScale;
        private Color _color;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;

        public static void Spawn(Vector3 pos, Color color, float life = 0.3f, float size = 1.5f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "HitFx";

            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = SharedMat();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            HitEffect fx = go.AddComponent<HitEffect>();
            fx._life = life > 0f ? life : 0.22f;
            fx._baseScale = go.transform.localScale;
            fx._color = color;
            fx._renderer = mr;
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

        private void Update()
        {
            _age += Time.deltaTime;
            float t = _age / _life;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.localScale = _baseScale * (1f + t * 1.4f);

            if (_renderer != null)
            {
                if (_mpb == null)
                    _mpb = new MaterialPropertyBlock();
                Color c = _color;
                c.a = 1f - t;
                _mpb.SetColor("_Color", c);
                _renderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
