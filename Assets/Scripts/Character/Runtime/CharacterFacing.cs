using UnityEngine;

namespace Jinhyeong_Character
{
    public enum EFacingMode
    {
        DfoSign,
        WorldYaw,
    }

    /// <summary>캐릭터의 평면 facing을 관리하는 컴포넌트. DfoSign 모드는 ±X 좌우 플립(던파식), WorldYaw 모드는 ForwardWorld로 들어온 임의 평면 방향으로 yaw 회전(3D MMORPG식).</summary>
    [DisallowMultipleComponent]
    public class CharacterFacing : MonoBehaviour
    {
        [HideInInspector] public float TurnSpeed;

        [SerializeField] private EFacingMode _mode = EFacingMode.WorldYaw;
        [SerializeField] private int _initialFacingSign = 1;

        private int _currentSign;
        private Vector3 _forwardWorld;

        public EFacingMode Mode { get { return _mode; } set { _mode = value; } }

        public int FacingSign
        {
            get { return _currentSign; }
            set
            {
                if (value > 0)
                {
                    _currentSign = 1;
                    _forwardWorld = Vector3.right;
                }
                else if (value < 0)
                {
                    _currentSign = -1;
                    _forwardWorld = Vector3.left;
                }
            }
        }

        public Vector3 ForwardWorld
        {
            get { return _forwardWorld; }
            set
            {
                Vector3 v = value;
                v.y = 0f;
                if (v.sqrMagnitude < 0.0001f) return;
                _forwardWorld = v.normalized;
                _currentSign = _forwardWorld.x >= 0f ? 1 : -1;
            }
        }

        public Vector3 ForwardPlanar
        {
            get
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                float sq = f.sqrMagnitude;
                if (sq < 0.0001f) return _forwardWorld;
                return f / Mathf.Sqrt(sq);
            }
        }

        private void Awake()
        {
            _currentSign = _initialFacingSign >= 0 ? 1 : -1;
            _forwardWorld = _currentSign >= 0 ? Vector3.right : Vector3.left;
        }

        private void Update()
        {
            Quaternion target;
            if (_mode == EFacingMode.DfoSign)
            {
                target = (_currentSign >= 0)
                    ? Quaternion.LookRotation(Vector3.right, Vector3.up)
                    : Quaternion.LookRotation(Vector3.left, Vector3.up);
            }
            else
            {
                if (_forwardWorld.sqrMagnitude < 0.0001f) return;
                target = Quaternion.LookRotation(_forwardWorld, Vector3.up);
            }

            if (TurnSpeed <= 0f)
            {
                transform.rotation = target;
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, TurnSpeed * Time.deltaTime);
            }
        }
    }
}
