using System;
using System.Collections.Generic;
using UnityEngine;
using static Core.Utilities.GameLogger;

namespace Core.Pool
{
    /// <summary>
    /// 풀 내 인스턴스 저장소 관리 (Queue, 크기 제한)
    /// </summary>
    /// <typeparam name="T">Component 타입</typeparam>
    public class PoolStorage<T> where T : Component
    {
        /// <summary>
        /// 풀에 저장되는 항목
        /// </summary>
        private class PoolItem
        {
            public T Component;
            public string Address;

            public PoolItem(T component, string address)
            {
                Component = component;
                Address = address;
            }
        }

        /// <summary>
        /// 제네릭 타입 캐싱 (typeof() 호출 비용 절감)
        /// </summary>
        private static class TypeCache<TComponent> where TComponent : T
        {
            public static readonly Type Type = typeof(TComponent);
        }

        // 타입별 풀 관리 (타입 → Queue)
        private readonly Dictionary<Type, Queue<PoolItem>> pools = new Dictionary<Type, Queue<PoolItem>>();

        // 타입별 풀 크기 제한
        private readonly Dictionary<Type, int> maxSizes = new Dictionary<Type, int>();

        // 기본 최대 풀 크기
        private readonly int defaultMaxSize;

        // 풀 이름 (로깅용)
        private readonly string poolName;

        /// <summary>
        /// PoolStorage 생성자
        /// </summary>
        /// <param name="defaultMaxSize">기본 최대 풀 크기</param>
        /// <param name="poolName">풀 이름 (로깅용)</param>
        public PoolStorage(int defaultMaxSize, string poolName)
        {
            this.defaultMaxSize = defaultMaxSize;
            this.poolName = poolName;
        }

        /// <summary>
        /// 풀에서 인스턴스를 꺼냅니다.
        /// </summary>
        /// <typeparam name="TComponent">꺼낼 Component 타입</typeparam>
        /// <param name="component">꺼낸 Component</param>
        /// <param name="address">꺼낸 인스턴스의 Address</param>
        /// <returns>풀에서 꺼내는 데 성공하면 true</returns>
        public bool TryTake<TComponent>(out T component, out string address) where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;

            if (pools.TryGetValue(type, out var pool) && pool.Count > 0)
            {
                var item = pool.Dequeue();
                component = item.Component;
                address = item.Address;

                Log($"[{poolName}] 재사용: {type.Name} (풀 남음: {pool.Count})");
                return true;
            }

            component = null;
            address = null;
            return false;
        }

        /// <summary>
        /// 풀에 인스턴스를 저장합니다.
        /// </summary>
        /// <typeparam name="TComponent">저장할 Component 타입</typeparam>
        /// <param name="component">저장할 Component</param>
        /// <param name="address">인스턴스의 Address</param>
        public void Store<TComponent>(T component, string address) where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;

            // 풀 가져오기 또는 생성
            if (!pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<PoolItem>();
                pools[type] = pool;
            }

            pool.Enqueue(new PoolItem(component, address));

            int maxSize = GetMaxSize(type);
            Log($"[{poolName}] 반환: {type.Name} (풀: {pool.Count}/{maxSize})");
        }

        /// <summary>
        /// 특정 타입의 풀이 꽉 찼는지 확인합니다.
        /// </summary>
        /// <typeparam name="TComponent">확인할 Component 타입</typeparam>
        /// <returns>풀이 꽉 찼으면 true</returns>
        public bool IsFull<TComponent>() where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;

            if (!pools.TryGetValue(type, out var pool))
            {
                return false;
            }

            int maxSize = GetMaxSize(type);
            return pool.Count >= maxSize;
        }

        /// <summary>
        /// 특정 타입의 최대 풀 크기를 설정합니다.
        /// </summary>
        /// <typeparam name="TComponent">설정할 Component 타입</typeparam>
        /// <param name="maxSize">최대 풀 크기</param>
        public void SetMaxSize<TComponent>(int maxSize) where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;
            maxSizes[type] = maxSize;

            Log($"[{poolName}] {type.Name} 풀 크기 설정: {maxSize}");
        }

        /// <summary>
        /// 특정 타입의 최대 풀 크기를 가져옵니다.
        /// </summary>
        public int GetMaxSize<TComponent>() where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;
            return GetMaxSize(type);
        }

        /// <summary>
        /// 특정 타입의 최대 풀 크기를 가져옵니다 (내부용).
        /// </summary>
        private int GetMaxSize(Type type)
        {
            return maxSizes.TryGetValue(type, out int size) ? size : defaultMaxSize;
        }

        /// <summary>
        /// 모든 PoolItem을 반환합니다 (Clear용).
        /// </summary>
        public List<(T component, string address)> GetAll()
        {
            var result = new List<(T, string)>();

            foreach (var pool in pools.Values)
            {
                while (pool.Count > 0)
                {
                    var item = pool.Dequeue();
                    result.Add((item.Component, item.Address));
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 타입의 풀을 비웁니다.
        /// </summary>
        /// <typeparam name="TComponent">비울 Component 타입</typeparam>
        /// <returns>비워진 PoolItem 리스트</returns>
        public List<(T component, string address)> ClearType<TComponent>() where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;
            var result = new List<(T, string)>();

            if (pools.TryGetValue(type, out var pool))
            {
                int count = pool.Count;

                while (pool.Count > 0)
                {
                    var item = pool.Dequeue();
                    result.Add((item.Component, item.Address));
                }

                pools.Remove(type);

                Log($"[{poolName}] 타입 풀 비움: {type.Name} ({count}개)");
            }

            return result;
        }

        /// <summary>
        /// 모든 풀을 비웁니다.
        /// </summary>
        public void Clear()
        {
            pools.Clear();
        }

        /// <summary>
        /// 특정 타입의 현재 풀 크기를 반환합니다.
        /// </summary>
        /// <typeparam name="TComponent">조회할 Component 타입</typeparam>
        /// <returns>현재 풀 크기</returns>
        public int GetCount<TComponent>() where TComponent : T
        {
            Type type = TypeCache<TComponent>.Type;
            return pools.TryGetValue(type, out var pool) ? pool.Count : 0;
        }

        /// <summary>
        /// 디버그 정보를 반환합니다.
        /// </summary>
        public Dictionary<Type, (int current, int max)> GetDebugInfo()
        {
            var info = new Dictionary<Type, (int, int)>();

            foreach (var kvp in pools)
            {
                Type type = kvp.Key;
                int poolCount = kvp.Value.Count;
                int maxSize = GetMaxSize(type);

                info[type] = (poolCount, maxSize);
            }

            return info;
        }
    }
}
