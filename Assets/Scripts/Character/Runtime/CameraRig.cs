using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Character
{
    /// <summary>게임/UI 카메라 참조를 들고 있는 홀더. SerializeField로 명시 바인딩(자동 탐색 폴백 없음).</summary>
    public class CameraRig : BaseBehaviour
    {
        [Header("Bound Cameras")]
        [SerializeField] private Camera _gameCamera;
        [SerializeField] private Camera _uiCamera;

        public Camera GameCamera { get { return _gameCamera; } }
        public Camera UICamera { get { return _uiCamera; } }
    }
}
