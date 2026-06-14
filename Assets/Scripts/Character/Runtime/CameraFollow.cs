using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_Managers;

namespace Jinhyeong_Character
{
    /// <summary>플레이어 주위를 우클릭 드래그로 yaw/pitch 회전하고 마우스 휠로 zoom in/out 하는 MMORPG식 3인칭 카메라. yaw/pitch/distance를 구면 좌표로 보유하고 LateUpdate에서 SmoothDamp로 위치를 따라가며 LookAt으로 타겟을 응시한다. 입력은 레거시 Input(우클릭, Mouse X/Y, Mouse ScrollWheel).</summary>
    public class CameraFollow : BaseBehaviour
    {
        [Header("Follow")]
        [Tooltip("타겟 위 회전 중심(카메라가 응시하고 그 주위를 도는 지점).")]
        [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 1.2f, 0f);

        [Tooltip("위치 SmoothDamp 시간상수. 작을수록 빠르게 따라감.")]
        [SerializeField] private float _smoothTime = 0.08f;

        [Tooltip("타겟이 비활성/null일 때 따라가지 않음.")]
        [SerializeField] private bool _onlyWhenAlive = true;

        [Header("Orbit")]
        [SerializeField] private float _initialYaw = 0f;
        [SerializeField] private float _initialPitch = 25f;
        [SerializeField] private float _initialDistance = 8f;

        [SerializeField] private float _pitchMin = 5f;
        [SerializeField] private float _pitchMax = 75f;
        [SerializeField] private float _distanceMin = 3f;
        [SerializeField] private float _distanceMax = 16f;

        [SerializeField] private float _yawSensitivity = 4f;
        [SerializeField] private float _pitchSensitivity = 3f;
        [SerializeField] private float _zoomSensitivity = 15f;

        [Tooltip("회전 입력을 받는 마우스 버튼. 0=좌, 1=우(권장), 2=가운데.")]
        [SerializeField] private int _orbitMouseButton = 1;

        [Tooltip("우클릭 드래그 동안 커서를 잠금/숨김 처리.")]
        [SerializeField] private bool _lockCursorWhileDragging = true;

        [Header("Camera")]
        [Tooltip("0보다 크면 자식 게임 카메라 FOV를 강제로 덮어씀.")]
        [SerializeField] private float _fovOverride = 55f;

        private Transform _target;
        private Vector3 _velocity;
        private Camera _gameCamera;
        private Camera _uiCamera;

        private float _yaw;
        private float _pitch;
        private float _distance;

        public float Yaw { get { return _yaw; } }
        public float Pitch { get { return _pitch; } }
        public float Distance { get { return _distance; } }

        public Camera GameCameraRef { get { return _gameCamera; } }
        public Camera UICameraRef { get { return _uiCamera; } }

        public static CameraFollow Active { get; private set; }

        /// <summary>현재 활성 게임 카메라. 월드 빌보드/스크린 좌표 계산 등에서 Camera.main 대신 사용.</summary>
        public static Camera GameCamera { get { return Active != null ? Active._gameCamera : null; } }

        /// <summary>현재 활성 UI 전용 카메라(없으면 null).</summary>
        public static Camera UICamera { get { return Active != null ? Active._uiCamera : null; } }

        private void Awake()
        {
            // Main.unity에서 OBJ_Camera가 Player의 자식으로 박혀있으면 부모 yaw 회전이
            // 카메라 worldPos에 매 프레임 영향을 주면서 SmoothDamp가 잡아내지 못하는 jitter를 만든다.
            // 런타임에 루트로 detach해서 카메라를 완전히 독립적으로 만든다 (worldPos 유지).
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }

            _yaw = _initialYaw;
            _pitch = Mathf.Clamp(_initialPitch, _pitchMin, _pitchMax);
            _distance = Mathf.Clamp(_initialDistance, _distanceMin, _distanceMax);

            ResolveChildCameras();
            ApplyFovOverride();
        }

        protected override void OnEnabled()
        {
            Active = this;
            GameEvents.OnPlayerSpawned   += HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned += HandlePlayerDespawned;

            Player p = GameEvents.CurrentPlayer;
            if (p != null)
                _target = p.transform;

            ApplyFovOverride();
            SnapToTarget();
        }

        protected override void OnDisabled()
        {
            if (Active == this)
                Active = null;
            GameEvents.OnPlayerSpawned   -= HandlePlayerSpawned;
            GameEvents.OnPlayerDespawned -= HandlePlayerDespawned;

            if (_lockCursorWhileDragging && Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void HandlePlayerSpawned(Player p)
        {
            if (p == null)
            {
                _target = null;
                return;
            }
            _target = p.transform;
            SnapToTarget();
        }

        private void HandlePlayerDespawned(Player p)
        {
            if (_target != null && p != null && _target == p.transform)
                _target = null;
        }

        private void Update()
        {
            ReadOrbitInput();
            ReadZoomInput();
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;
            if (_onlyWhenAlive && _target.gameObject.activeInHierarchy == false)
                return;

            Vector3 pivot = _target.position + _pivotOffset;
            Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredPos = pivot + orbitRot * (Vector3.back * _distance);

            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _velocity, _smoothTime);

            // 회전은 SmoothDamp 위치가 아니라 desiredPos 기준으로 잡는다.
            // 그러지 않으면 캐릭터 이동 중에 transform.position이 desiredPos보다 뒤쳐져서
            // lookDir의 yaw가 흔들리고 카메라가 좌/우로 살짝 도는 것처럼 보임.
            transform.rotation = orbitRot;
        }

        private void ReadOrbitInput()
        {
            bool down = Input.GetMouseButtonDown(_orbitMouseButton);
            bool up = Input.GetMouseButtonUp(_orbitMouseButton);
            bool held = Input.GetMouseButton(_orbitMouseButton);

            if (_lockCursorWhileDragging)
            {
                if (down)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else if (up)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (held == false)
                return;

            float dx = Input.GetAxisRaw("Mouse X");
            float dy = Input.GetAxisRaw("Mouse Y");

            _yaw += dx * _yawSensitivity;
            _pitch -= dy * _pitchSensitivity;
            _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);
        }

        private void ReadZoomInput()
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) < 0.0001f)
                return;
            _distance = Mathf.Clamp(_distance - scroll * _zoomSensitivity, _distanceMin, _distanceMax);
        }

        private void SnapToTarget()
        {
            if (_target == null)
                return;

            Vector3 pivot = _target.position + _pivotOffset;
            Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = pivot + orbitRot * (Vector3.back * _distance);
            transform.rotation = orbitRot;
            _velocity = Vector3.zero;
        }

        private void ApplyFovOverride()
        {
            if (_fovOverride <= 0f)
                return;
            if (_gameCamera == null)
                ResolveChildCameras();
            if (_gameCamera == null)
                return;
            _gameCamera.fieldOfView = _fovOverride;
        }

        private void ResolveChildCameras()
        {
            Camera[] cams = GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cams.Length; i++)
            {
                Camera c = cams[i];
                bool looksLikeUI = c.name.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (looksLikeUI)
                {
                    if (_uiCamera == null)
                        _uiCamera = c;
                }
                else
                {
                    if (_gameCamera == null)
                        _gameCamera = c;
                }
            }
            // 게임 카메라가 안 잡혔으면 첫 번째 카메라를 폴백
            if (_gameCamera == null && cams.Length > 0)
                _gameCamera = cams[0];
        }
    }
}
