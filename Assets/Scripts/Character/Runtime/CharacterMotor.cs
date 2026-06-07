using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Character
{
    /// <summary>MoveAxis 입력과 중력을 합쳐 CharacterController로 이동시키는 컴포넌트. SpeedMultiplier로 외부 버프/디버프 반영.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        [HideInInspector] public float MoveSpeed;
        [HideInInspector] public float SpeedMultiplier = 1f;

        private CharacterController _cc;
        private float _verticalVelocity;

        public Vector2 MoveAxis { get; set; }
        public CollisionFlags LastCollisionFlags { get; private set; }

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
        }

        private void Update()
        {
            ApplyGravity();
            Move();
        }

        private void ApplyGravity()
        {
            if (_cc.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity += CommonConfig.Physics.Gravity * Time.deltaTime;
            }
        }

        private void Move()
        {
            Vector2 axis = MoveAxis;
            Vector3 planar = new Vector3(axis.x, 0f, axis.y) * MoveSpeed * SpeedMultiplier;
            Vector3 velocity = planar + Vector3.up * _verticalVelocity;
            LastCollisionFlags = _cc.Move(velocity * Time.deltaTime);
        }
    }
}
