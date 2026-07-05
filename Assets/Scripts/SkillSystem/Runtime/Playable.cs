using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{

    public class Playable : BaseBehaviour
    {
        [SerializeField] private GameObject _cameraRoot;

        public Transform CameraRoot { get { return _cameraRoot != null ? _cameraRoot.transform : null; } }
    }
}
