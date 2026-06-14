using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_SkillSystem
{
    /// <summary>캐릭터 GO에서 카메라 배치 기준이 되는 CameraRoot를 보유하는 홀더 컴포넌트. 프리팹에서 SerializeField로 바인딩하며 WorldSpawner가 카메라 스폰 위치로 사용.</summary>
    public class Playable : BaseBehaviour
    {
        [SerializeField] private GameObject _cameraRoot;

        /// <summary>카메라가 스폰될 때 배치 기준이 되는 루트.</summary>
        public Transform CameraRoot { get { return _cameraRoot != null ? _cameraRoot.transform : null; } }
    }
}
