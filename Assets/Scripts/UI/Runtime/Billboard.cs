using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;

namespace Jinhyeong_UI
{
    public enum EBillboardMode
    {
        /// <summary>카메라 회전을 그대로 복사. UI 평면이 항상 화면과 평행 → 가장 단순.</summary>
        MatchCameraRotation,

        /// <summary>수직(Y) 축으로만 회전. 카메라가 위에서 내려봐도 UI는 똑바로 서있음 (머리 위 HP 바 등에 적합).</summary>
        YawOnly,
    }

    /// <summary>월드스페이스 오브젝트를 카메라 방향으로 정렬하는 단일 책임 컴포넌트. CameraFollow.GameCamera 정적 참조를 매 LateUpdate에서 조회하므로 인스턴스마다 카메라 세팅할 필요 없음.</summary>
    public class Billboard : BaseBehaviour
    {
        [SerializeField] private EBillboardMode _mode = EBillboardMode.MatchCameraRotation;

        public EBillboardMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        private void LateUpdate()
        {
            Camera cam = CameraFollow.GameCamera;
            if (cam == null)
                return;

            if (_mode == EBillboardMode.MatchCameraRotation)
            {
                transform.rotation = cam.transform.rotation;
                return;
            }

            // YawOnly
            Vector3 toCam = transform.position - cam.transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 1e-6f)
                return;
            transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
        }
    }
}
