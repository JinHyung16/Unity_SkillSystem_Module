using UnityEngine;

namespace Jinhyeong_Input
{

    public interface IInputProvider
    {
        Vector2 MoveAxis { get; }

        int FacingSign { get; }

        bool GetAttackDown();
        bool GetSkillSlotDown(KeyCode slotKey);
    }
}
