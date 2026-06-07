using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Managers
{
    public partial class PoolManager
    {
        private readonly Dictionary<string, Queue<GameObject>> _skillPool =
            new Dictionary<string, Queue<GameObject>>(16);

        public GameObject Pool_Skill_Get(string key = KeyEmpty)
        {
            if (string.IsNullOrEmpty(key)) key = KeyEmpty;

            if (_skillPool.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
            {
                GameObject pooled = queue.Dequeue();
                if (pooled != null)
                {
                    pooled.SetActive(true);
                    return pooled;
                }
            }

            if (key == KeyEmpty) return null;
            return InstantiateFromAddressable(key);
        }

        public void Pool_Skill_Return(string key, GameObject obj)
        {
            if (obj == null) return;
            if (string.IsNullOrEmpty(key)) key = KeyEmpty;

            obj.SetActive(false);
            if (PoolRoot != null) obj.transform.SetParent(PoolRoot, false);

            if (_skillPool.TryGetValue(key, out Queue<GameObject> queue) == false)
            {
                queue = new Queue<GameObject>();
                _skillPool[key] = queue;
            }
            queue.Enqueue(obj);
        }

        private void Pool_Skill_Clear()
        {
            foreach (Queue<GameObject> queue in _skillPool.Values)
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            _skillPool.Clear();
        }
    }
}
