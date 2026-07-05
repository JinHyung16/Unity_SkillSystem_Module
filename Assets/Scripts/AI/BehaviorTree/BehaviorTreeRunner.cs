using UnityEngine;
using Jinhyeong_Common;

namespace Jinhyeong_AI.BehaviorTree
{

    public class BehaviorTreeRunner : BaseBehaviour
    {
        public bool Paused = false;

        private BTNode _root;

        public void SetRoot(BTNode root)
        {
            if (_root != null)
                _root.Reset();
            _root = root;
        }

        private void Update()
        {
            if (Paused || _root == null)
                return;
            _root.Tick(Time.deltaTime);
        }
    }
}
