using UnityEngine;

namespace Jinhyeong_AI.BehaviorTree
{
    /// <summary>매 Update마다 루트 BTNode를 Tick하는 MonoBehaviour 구동기. Paused로 일시 정지 가능.</summary>
    public class BehaviorTreeRunner : MonoBehaviour
    {
        public bool Paused = false;

        private BTNode _root;

        public void SetRoot(BTNode root)
        {
            if (_root != null) _root.Reset();
            _root = root;
        }

        private void Update()
        {
            if (Paused || _root == null) return;
            _root.Tick(Time.deltaTime);
        }
    }
}
