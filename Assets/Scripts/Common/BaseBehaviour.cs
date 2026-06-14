using UnityEngine;

namespace Jinhyeong_Common
{
    /// <summary>프로젝트 전 MonoBehaviour의 단일 베이스. Unity의 OnEnable/OnDisable 매직 메서드를 여기서 단독 소유하고,
    /// 자식은 protected virtual OnEnabled()/OnDisabled()를 override한다(자식이 OnEnable/OnDisable을 직접 선언하면 안 됨).
    /// 라이프사이클 진입점을 한 곳에 모아 유지보수/디버깅을 쉽게 한다.</summary>
    public abstract class BaseBehaviour : MonoBehaviour
    {
        /// <summary>Unity OnEnable 대체 훅. 활성화 시점 로직은 여기에.</summary>
        protected virtual void OnEnabled() { }

        /// <summary>Unity OnDisable 대체 훅. 비활성화 시점 로직(이벤트 해제 등)은 여기에.</summary>
        protected virtual void OnDisabled() { }

        private void OnEnable()
        {
            OnEnabled();
        }

        private void OnDisable()
        {
            OnDisabled();
        }

        /// <summary>필수 참조 검증. 미바인딩이면 시끄럽게 LogError 후 컴포넌트를 비활성화(fail-fast)한다.
        /// 폴백 GetComponent로 몰래 자동 연결하지 않는다 — 개발자가 인스펙터 바인딩을 빼먹으면 즉시 드러나게.
        /// 반환값으로 호출부에서 조기 종료를 판단한다.</summary>
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
