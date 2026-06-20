using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Map.OutlineEffect
{
    public class TileOutlinerPool : MonoBehaviour
    {
        public static TileOutlinerPool Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private TileOutliner prefab;
        [SerializeField] private int defaultCapacity = 10;
        [SerializeField] private int maxPoolSize = 100;

        private ObjectPool<TileOutliner> pool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            pool = new ObjectPool<TileOutliner>(
                createFunc: CreateOutliner,
                actionOnGet: OnGetOutliner,
                actionOnRelease: OnReleaseOutliner,
                actionOnDestroy: OnDestroyOutliner,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );
        }

        private TileOutliner CreateOutliner()
        {
            TileOutliner instance = Instantiate(prefab, transform);
            return instance;
        }

        private void OnGetOutliner(TileOutliner outliner)
        {
            outliner.gameObject.SetActive(true);
        }

        private void OnReleaseOutliner(TileOutliner outliner)
        {
            outliner.ClearOutline();
            outliner.gameObject.SetActive(false);
        }

        private void OnDestroyOutliner(TileOutliner outliner)
        {
            try
            {
                Destroy(outliner.gameObject);
            }
            catch (Exception) { }
        }

        public TileOutliner Get()
        {
            return pool.Get();
        }

        public void Release(TileOutliner outliner)
        {
            if (outliner == null) return;
            pool.Release(outliner);
        }
    }
}