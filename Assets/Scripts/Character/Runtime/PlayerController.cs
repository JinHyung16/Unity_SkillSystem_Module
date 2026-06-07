using UnityEngine;
using Jinhyeong_Input;
using Jinhyeong_SkillSystem;

namespace Jinhyeong_Character
{
    /// <summary>IInputProvider의 입력을 Motor/Facing/Attack/Skills에 분배하는 어댑터. 매 Update에서 이동축, 좌우 facing 부호, 공격, 스킬 슬롯 키를 처리.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterMotor))]
    [RequireComponent(typeof(CharacterFacing))]
    [RequireComponent(typeof(CharacterAttack))]
    [RequireComponent(typeof(SkillObject))]
    public class PlayerController : MonoBehaviour
    {
        public MonoBehaviour InputSource;
        public InputBindings Bindings;

        private IInputProvider _input;
        private CharacterMotor _motor;
        private CharacterFacing _facing;
        private CharacterAttack _attack;
        private SkillObject _skills;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _facing = GetComponent<CharacterFacing>();
            _attack = GetComponent<CharacterAttack>();
            _skills = GetComponent<SkillObject>();

            if (InputSource == null) InputSource = GetComponent<KeyboardInputProvider>();
            _input = InputSource as IInputProvider;
            if (_input == null)
            {
                Debug.LogError($"[PlayerController] '{name}'의 InputSource가 IInputProvider를 구현하지 않음");
            }
        }

        private void Update()
        {
            if (_input == null || Bindings == null) return;

            Vector2 axis = _input.MoveAxis;
            Vector3 worldMove = ComputeCameraRelativeMove(axis);
            _motor.MoveAxis = new Vector2(worldMove.x, worldMove.z);

            if (worldMove.sqrMagnitude > 0.0001f)
            {
                _facing.ForwardWorld = worldMove;
            }

            if (_input.GetAttackDown())
            {
                _attack.TryFire(_facing.ForwardPlanar);
            }

            for (int i = 0; i < Bindings.SkillSlots.Count; i++)
            {
                KeyCode slotKey = Bindings.SkillSlots[i].Key;
                if (slotKey == KeyCode.None) continue;
                if (_input.GetSkillSlotDown(slotKey))
                {
                    _skills.TryFireSlot(slotKey);
                }
            }
        }

        private Vector3 ComputeCameraRelativeMove(Vector2 axis)
        {
            if (axis.sqrMagnitude < 0.0001f) return Vector3.zero;

            float yaw = 0f;
            CameraFollow cam = CameraFollow.Active;
            if (cam != null) yaw = cam.Yaw;

            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 local = new Vector3(axis.x, 0f, axis.y);
            return yawRot * local;
        }
    }
}
