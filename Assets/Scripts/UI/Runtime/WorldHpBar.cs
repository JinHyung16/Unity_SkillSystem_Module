using UnityEngine;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_UI
{
    /// <summary>Damageable의 상태 변화를 자식 UiFillBar(HP/Shield)에 전달하는 단순 어댑터. UI 구조는 prefab이 책임지고, 빌보드는 같은 GameObject의 Billboard 컴포넌트가 처리. 이 컴포넌트는 카메라/렌더링과 무관.</summary>
    [DisallowMultipleComponent]
    public class WorldHpBar : MonoBehaviour
    {
        [Tooltip("비우면 GetComponentInParent<Damageable>()로 자동 바인딩.")]
        [SerializeField] private Damageable _target;

        [Tooltip("HP 채움 바. 비우면 자식에서 첫 UiFillBar를 자동 사용.")]
        [SerializeField] private UiFillBar _hpFill;

        [Tooltip("(선택) Shield 채움 바. 0보다 클 때만 표시.")]
        [SerializeField] private UiFillBar _shieldFill;

        [Tooltip("HP가 가득 찼을 때 CanvasGroup.alpha=0으로 숨김.")]
        [SerializeField] private bool _hideWhenFull = false;

        [Tooltip("비우면 같은 GameObject의 CanvasGroup을 자동 사용.")]
        [SerializeField] private CanvasGroup _group;

        private Damageable _bound;

        private void OnEnable()
        {
            ResolveRefs();
            Bind();
            Refresh();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void ResolveRefs()
        {
            if (_target == null) _target = GetComponentInParent<Damageable>();
            if (_hpFill == null) _hpFill = GetComponentInChildren<UiFillBar>(true);
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }

        private void Bind()
        {
            if (_target == null) return;
            if (_bound == _target) return;

            Unbind();
            _bound = _target;
            _bound.OnHealthChanged += HandleHealthChanged;
            _bound.OnDied += HandleDied;
        }

        private void Unbind()
        {
            if (_bound == null) return;
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
            if (_group != null) _group.alpha = 0f;
        }

        private void Refresh()
        {
            if (_bound == null)
            {
                if (_hpFill != null) _hpFill.Value = 0f;
                if (_shieldFill != null) _shieldFill.Value = 0f;
                return;
            }

            float max = Mathf.Max(_bound.MaxHp, 0.0001f);

            if (_hpFill != null) _hpFill.SetFromCurrentMax(_bound.Hp, max);

            if (_shieldFill != null)
            {
                bool show = _bound.Shield > 0f;
                if (_shieldFill.gameObject.activeSelf != show) _shieldFill.gameObject.SetActive(show);
                if (show) _shieldFill.SetFromCurrentMax(_bound.Shield, max);
            }

            if (_group != null)
            {
                float ratio = _bound.Hp / max;
                bool full = ratio >= 0.999f && _bound.Shield <= 0f;
                _group.alpha = (_hideWhenFull && full) ? 0f : 1f;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_target == null) _target = GetComponentInParent<Damageable>();
            if (_hpFill == null) _hpFill = GetComponentInChildren<UiFillBar>(true);
            if (_group == null) _group = GetComponent<CanvasGroup>();
        }
#endif
    }
}
