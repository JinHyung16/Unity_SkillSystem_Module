using System.Collections.Generic;
using UnityEngine;

namespace Jinhyeong_Managers
{
    public partial class PoolManager
    {
        private readonly Dictionary<string, Queue<GameObject>> _characterPool =
            new Dictionary<string, Queue<GameObject>>(8);

        public GameObject Pool_Character_Get(string key = KeyEmpty)
        {
            if (string.IsNullOrEmpty(key)) key = KeyEmpty;

            if (_characterPool.TryGetValue(key, out Queue<GameObject> queue) && queue.Count > 0)
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

        public void Pool_Character_Return(string key, GameObject obj)
        {
            if (obj == null) return;
            if (string.IsNullOrEmpty(key)) key = KeyEmpty;

            obj.SetActive(false);
            if (PoolRoot != null) obj.transform.SetParent(PoolRoot, false);

            if (_characterPool.TryGetValue(key, out Queue<GameObject> queue) == false)
            {
                queue = new Queue<GameObject>();
                _characterPool[key] = queue;
            }
            queue.Enqueue(obj);
        }

        private void Pool_Character_Clear()
        {
            foreach (Queue<GameObject> queue in _characterPool.Values)
            {
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            _characterPool.Clear();
        }
    }
}
