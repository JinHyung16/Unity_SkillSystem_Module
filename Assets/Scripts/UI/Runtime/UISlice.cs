using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_UI
{
    public enum EUIFillDirection
    {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom,
    }

    [ExecuteAlways]
    public class UISlice : BaseBehaviour
    {
        [Header("Target")]
        [Tooltip("채워질 RectTransform. 비우면 이 컴포넌트가 부착된 GameObject의 RectTransform을 사용.")]
        [SerializeField] private RectTransform _fillTarget;

        [Header("Fill")]
        [SerializeField] private EUIFillDirection _direction = EUIFillDirection.LeftToRight;

        [Range(0f, 1f)]
        [SerializeField] private float _value = 1f;

        [Tooltip("0보다 크면 SmoothDamp로 부드럽게 보간. unscaledTime 사용 → 게임 정지(timeScale=0) 중에도 동작.")]
        [SerializeField, Min(0f)] private float _smoothTime = 0f;

        private float _displayed;
        private float _velocity;

        public float Value
        {
            get { return _value; }
            set { SetRatio(value); }
        }

        public RectTransform FillTarget
        {
            get { return _fillTarget; }
            set { _fillTarget = value; ApplyImmediate(); }
        }

        public EUIFillDirection Direction
        {
            get { return _direction; }
            set { _direction = value; ApplyImmediate(); }
        }

        public void SetRatio(float ratio)
        {
            _value = Mathf.Clamp01(ratio);
            if (Application.isPlaying == false || _smoothTime <= 0f)
                ApplyImmediate();
        }

        public void SetFromCurrentMax(float current, float max)
        {
            SetRatio(max > 0.0001f ? current / max : 0f);
        }

        public void SnapToValue()
        {
            ApplyImmediate();
        }

        protected override void OnEnabled()
        {
            ApplyImmediate();
        }

        private void Update()
        {
            RectTransform target = ResolveTarget();
            if (target == null)
                return;

            if (Application.isPlaying && _smoothTime > 0f)
            {
                if (Mathf.Abs(_displayed - _value) < 1e-5f)
                    return;
                _displayed = Mathf.SmoothDamp(_displayed, _value, ref _velocity, _smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(_displayed - _value) < 1e-4f)
                    _displayed = _value;
                WriteToTarget(target, _displayed);
                return;
            }

            if (Mathf.Abs(_displayed - _value) > 1e-5f)
            {
                WriteToTarget(target, _value);
            }
        }

        private void ApplyImmediate()
        {
            _displayed = _value;
            _velocity = 0f;
            RectTransform target = ResolveTarget();
            if (target != null)
                WriteToTarget(target, _value);
        }

        private RectTransform ResolveTarget()
        {
            if (_fillTarget != null)
                return _fillTarget;
            return transform as RectTransform;
        }

        private void WriteToTarget(RectTransform target, float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            _displayed = ratio;

            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.one;

            switch (_direction)
            {
                case EUIFillDirection.LeftToRight: max.x = ratio; break;
                case EUIFillDirection.RightToLeft: min.x = 1f - ratio; break;
                case EUIFillDirection.BottomToTop: max.y = ratio; break;
                case EUIFillDirection.TopToBottom: min.y = 1f - ratio; break;
            }

            target.anchorMin = min;
            target.anchorMax = max;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _value = Mathf.Clamp01(_value);
            if (_smoothTime < 0f)
                _smoothTime = 0f;

            UnityEditor.EditorApplication.delayCall -= DelayedApply;
            UnityEditor.EditorApplication.delayCall += DelayedApply;
        }

        private void DelayedApply()
        {
            UnityEditor.EditorApplication.delayCall -= DelayedApply;
            if (this == null)
                return;
            ApplyImmediate();
        }

        private void Reset()
        {

            if (_fillTarget != null)
                return;
            Transform candidate = transform.Find("Fill");
            if (candidate == null && transform.childCount > 0)
            {
                candidate = transform.GetChild(0);
            }
            if (candidate != null)
                _fillTarget = candidate as RectTransform;
        }
#endif
    }
}
