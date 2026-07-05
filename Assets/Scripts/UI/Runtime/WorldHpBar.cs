using UnityEngine;
using Jinhyeong_Common;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_UI
{

    public class WorldHpBar : BaseBehaviour
    {
        [SerializeField] private Damageable _target;
        [SerializeField] private UISlice _hpFill;

        [Tooltip("(선택) Shield 채움 바. 0보다 클 때만 표시.")]
        [SerializeField] private UISlice _shieldFill;

        [Tooltip("HP가 가득 찼을 때 CanvasGroup.alpha=0으로 숨김.")]
        [SerializeField] private bool _hideWhenFull = false;

        [Tooltip("(선택) 비우면 숨김 처리 안 함.")]
        [SerializeField] private CanvasGroup _group;

        private Damageable _bound;
        private bool _bindingValid;

        private void Awake()
        {
            _bindingValid =
                RequireRef(_target, nameof(_target)) &&
                RequireRef(_hpFill, nameof(_hpFill));
        }

        protected override void OnEnabled()
        {
            if (_bindingValid == false)
                return;
            Bind();
            Refresh();
        }

        protected override void OnDisabled()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_bound == _target)
                return;
            Unbind();
            _bound = _target;
            _bound.OnHealthChanged += HandleHealthChanged;
            _bound.OnDied += HandleDied;
        }

        private void Unbind()
        {
            if (_bound == null)
                return;
            _bound.OnHealthChanged -= HandleHealthChanged;
            _bound.OnDied -= HandleDied;
            _bound = null;
        }

        private void HandleHealthChanged(Damageable d)
        {
            Refresh();
        }

        private void HandleDied(Damageable d, SkillObject src)
        {
            if (_group != null)
                _group.alpha = 0f;
        }

        private void Refresh()
        {
            if (_bound == null)
            {
                _hpFill.Value = 0f;
                if (_shieldFill != null)
                    _shieldFill.Value = 0f;
                return;
            }

            float max = Mathf.Max(_bound.MaxHp, 0.0001f);

            _hpFill.SetFromCurrentMax(_bound.Hp, max);

            if (_shieldFill != null)
            {
                bool show = _bound.Shield > 0f;
                if (_shieldFill.gameObject.activeSelf != show)
                    _shieldFill.gameObject.SetActive(show);
                if (show)
                    _shieldFill.SetFromCurrentMax(_bound.Shield, max);
            }

            if (_group != null)
            {
                float ratio = _bound.Hp / max;
                bool full = ratio >= 0.999f && _bound.Shield <= 0f;
                _group.alpha = (_hideWhenFull && full) ? 0f : 1f;
            }
        }
    }
}
