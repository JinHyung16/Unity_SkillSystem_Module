using UnityEngine;

namespace Jinhyeong_Character
{
    /// <summary>Player 하위에 붙여 게임/UI 카메라 참조를 들고 있는 홀더. 카메라 연출의 진입점.</summary>
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour
    {
        [Header("Bound Cameras")]
        [SerializeField] private Camera _gameCamera;
        [SerializeField] private Camera _uiCamera;

        public Camera GameCamera { get { return _gameCamera; } }
        public Camera UICamera { get { return _uiCamera; } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_gameCamera != null && _uiCamera != null) return;

            Camera[] cams = GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cams.Length; i++)
            {
                Camera c = cams[i];
                bool looksLikeUI = c.name.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (looksLikeUI)
                {
                    if (_uiCamera == null) _uiCamera = c;
                }
                else
                {
                    if (_gameCamera == null) _gameCamera = c;
                }
            }
        }
#endif
    }
}
