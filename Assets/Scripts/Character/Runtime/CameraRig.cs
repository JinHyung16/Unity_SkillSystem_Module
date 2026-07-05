using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Character
{

    public class CameraRig : BaseBehaviour
    {
        [Header("Bound Cameras")]
        [SerializeField] private Camera _gameCamera;
        [SerializeField] private Camera _uiCamera;

        public Camera GameCamera { get { return _gameCamera; } }
        public Camera UICamera { get { return _uiCamera; } }
    }
}
