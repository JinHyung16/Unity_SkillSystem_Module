using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Managers
{
    public partial class PoolManager
    {
        private readonly Dictionary<string, Queue<GameObject>> _enemyPool =
            new Dictionary<string, Queue<GameObject>>(8);

        public GameObject Pool_Enemy_Get(string key = KeyEmpty)
        {
            if (string.IsNullOrEmpty(key)) key = KeyEmpty;

            if (_enemyPool.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
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

        public void Pool_Enemy_Return(string key, GameObject obj)
        {
            if (obj == null) return;
            if (string.IsNullOrEmpty(key)) key = KeyEmpty;

            obj.SetActive(false);
            if (PoolRoot != null) obj.transform.SetParent(PoolRoot, false);

            if (_enemyPool.TryGetValue(key, out Queue<GameObject> queue) == false)
            {
                queue = new Queue<GameObject>();
                _enemyPool[key] = queue;
            }
            queue.Enqueue(obj);
        }

        private void Pool_Enemy_Clear()
        {
            foreach (Queue<GameObject> queue in _enemyPool.Values)
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            _enemyPool.Clear();
        }
    }
}
