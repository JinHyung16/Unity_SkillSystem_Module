using UnityEngine;

namespace Jinhyeong_Common
{

    public abstract class BaseBehaviour : MonoBehaviour
    {

        protected virtual void OnEnabled() { }

        protected virtual void OnDisabled() { }

        private void OnEnable()
        {
            OnEnabled();
        }

        private void OnDisable()
        {
            OnDisabled();
        }

        protected bool RequireRef(Object reference, string fieldName)
        {
            if (reference != null)
                return true;

            Debug.LogError($"[{GetType().Name}] '{name}': 필수 참조 '{fieldName}' 미바인딩 — 프리팹/인스펙터에서 연결해야 함", this);
            enabled = false;
            return false;
        }
    }
}
