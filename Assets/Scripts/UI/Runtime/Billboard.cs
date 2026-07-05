using UnityEngine;
using Jinhyeong_Character;
using Jinhyeong_Common;

namespace Jinhyeong_UI
{
    public enum EBillboardMode
    {

        MatchCameraRotation,

        YawOnly,
    }

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

            Vector3 toCam = transform.position - cam.transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 1e-6f)
                return;
            transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
        }
    }
}
