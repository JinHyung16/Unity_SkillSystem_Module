using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_Input
{

    public class KeyboardInputProvider : BaseBehaviour, IInputProvider
    {
        public InputBindings Bindings;

        public Vector2 MoveAxis { get; private set; }
        public int FacingSign { get; private set; }

        private void Update()
        {
            if (Bindings == null)
                return;

            float x = 0f;
            float y = 0f;
            if (Input.GetKey(Bindings.MoveLeft))
                x -= 1f;
            if (Input.GetKey(Bindings.MoveRight))
                x += 1f;
            if (Input.GetKey(Bindings.MoveDown))
                y -= 1f;
            if (Input.GetKey(Bindings.MoveUp))
                y += 1f;

            Vector2 axis = new Vector2(x, y);
            if (axis.sqrMagnitude > 1f)
                axis.Normalize();
            MoveAxis = axis;

            if (x > 0f)
                FacingSign = 1;
            else if (x < 0f)
                FacingSign = -1;
        }

        public bool GetAttackDown()
        {
            if (Bindings == null)
                return false;
            return Input.GetKeyDown(Bindings.Attack);
        }

        public bool GetSkillSlotDown(KeyCode slotKey)
        {
            return Input.GetKeyDown(slotKey);
        }
    }
}
