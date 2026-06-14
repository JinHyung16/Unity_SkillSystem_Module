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

    /// <summary>지정한 RectTransform(_fillTarget)을 0~1 비율로 채워주는 범용 UI 컴포넌트. Image.type=Filled의 픽셀 깨짐 없이 anchorMin/Max를 직접 조정해 매끄럽게 잘림. _fillTarget을 비우면 자기 자신의 RectTransform을 사용. ExecuteAlways로 에디터에서도 Value/Direction 변경이 즉시 반영됨. SmoothTime>0이면 unscaledTime 기준 SmoothDamp 보간(게임 정지 중에도 동작).</summary>
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

        /// <summary>보간 없이 표시값을 즉시 Value로 스냅 (스폰/리셋 시).</summary>
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

            // Play 중이고 보간 옵션이 있으면 SmoothDamp
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

            // 에디터 모드이거나 보간 비활성 — 즉시 동기화
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

            // OnValidate는 prefab 임포트 중에도 호출돼서 즉시 transform 변경이 위험할 수 있음.
            // delayCall로 안전한 타이밍에 적용 → 슬라이더/필드 변경이 인스펙터에 1프레임 내로 보임.
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
            // 컴포넌트 처음 부착 시 — _fillTarget을 자식 "Fill" 또는 첫 자식으로 자동 잡기 (편의)
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
