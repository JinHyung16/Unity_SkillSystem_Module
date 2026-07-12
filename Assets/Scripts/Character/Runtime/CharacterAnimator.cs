using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Character
{

    /// <summary>
    /// 이동/공격 상태를 스틱맨 Animator 파라미터로 전달한다.
    /// - Speed(float): 이동 입력 크기 → Idle/Run 전환
    /// - Attack(trigger): 근접 공격 스윙 시 재생
    /// Animator는 비주얼(중첩 프리팹) 하위에 있으므로 GetComponentInChildren으로 찾는다.
    /// </summary>
    public class CharacterAnimator : BaseBehaviour
    {
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CharacterAttack _attack;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        private Animator _animator;
        private bool _bound;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null)
            {
                Debug.LogError($"[CharacterAnimator] '{name}': 하위에서 Animator를 찾지 못함 — 비주얼 모델 확인 필요", this);
                enabled = false;
                return;
            }

            _bound =
                RequireRef(_motor, nameof(_motor)) &&
                RequireRef(_attack, nameof(_attack));
        }

        protected override void OnEnabled()
        {
            if (_bound == false)
                return;
            _attack.OnFired += HandleAttackFired;
        }

        protected override void OnDisabled()
        {
            if (_bound == false)
                return;
            _attack.OnFired -= HandleAttackFired;
        }

        private void Update()
        {
            if (_bound == false)
                return;

            float move01 = Mathf.Clamp01(_motor.MoveAxis.magnitude);
            _animator.SetFloat(SpeedHash, move01);
        }

        private void HandleAttackFired()
        {
            if (_animator == null)
                return;
            _animator.SetTrigger(AttackHash);
        }
    }
}
