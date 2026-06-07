using UnityEngine;

namespace Jinhyeong_Input
{
    /// <summary>이동축, 좌우 facing 부호, 공격/스킬 버튼 입력을 제공하는 입력 소스 추상화. DFO식 2.5D 컨트롤을 가정해 마우스 조준은 두지 않음.</summary>
    public interface IInputProvider
    {
        Vector2 MoveAxis { get; }

        /// <summary>현재 향해야 할 좌우 부호. +1 = +X, -1 = -X, 0 = 변경 없음.</summary>
        int FacingSign { get; }

        bool GetAttackDown();
        bool GetSkillSlotDown(KeyCode slotKey);
    }
}
