using UnityEngine;
using UnityEngine.Pool;

namespace Map.OutlineEffect
{
    public abstract class TileEffectPool<T> : MonoBehaviour where T : MonoBehaviour, ITileEffect
    {
        public static TileEffectPool<T> Instance { get; private set; }

        [Header("Pool Configuration")] [SerializeField]
        private T prefab;

        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 100;

        private ObjectPool<T> pool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            pool = new ObjectPool<T>(
                createFunc: CreateEffect,
                actionOnGet: OnGetEffect,
                actionOnRelease: OnReleaseEffect,
                actionOnDestroy: OnDestroyEffect,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );
        }

        private T CreateEffect()
        {
            var instance = Instantiate(prefab, transform);
            return instance;
        }

        private void OnGetEffect(T outliner)
        {
            outliner.gameObject.SetActive(true);
        }

        private void OnReleaseEffect(T effect)
        {
            effect.ClearEffect();
            effect.gameObject.SetActive(false);
        }

        private void OnDestroyEffect(T effect)
        {
            if (effect != null && effect.gameObject != null)
            {
                Destroy(effect.gameObject);
            }
        }

        public T Get()
        {
            return pool.Get();
        }

        public void Release(T outliner)
        {
            if (outliner == null) return;
            pool.Release(outliner);
        }
    }
}