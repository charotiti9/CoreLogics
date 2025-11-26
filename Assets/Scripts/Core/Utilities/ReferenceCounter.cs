using System;
using System.Collections.Generic;
using static Core.Utilities.GameLogger;

namespace Core.Utilities
{
    /// <summary>
    /// 범용 참조 카운팅 클래스
    /// 키별로 값을 추적하고 참조 카운트를 관리합니다.
    /// 참조 카운트가 0이 되면 자동으로 해제 콜백을 호출합니다.
    /// </summary>
    /// <typeparam name="TKey">참조를 식별하는 키 타입</typeparam>
    /// <typeparam name="TValue">추적할 값 타입</typeparam>
    public class ReferenceCounter<TKey, TValue>
    {
        /// <summary>
        /// 참조 정보 (값과 카운트)
        /// </summary>
        private class ReferenceInfo
        {
            public TValue Value;
            public int Count;

            public ReferenceInfo(TValue value)
            {
                Value = value;
                Count = 1;
            }
        }

        // 참조 딕셔너리
        private readonly Dictionary<TKey, ReferenceInfo> references = new Dictionary<TKey, ReferenceInfo>();

        // 해제 콜백 (참조 카운트가 0이 될 때 호출)
        private readonly Action<TKey, TValue> onReleaseCallback;

        // 로그 이름 (디버깅용)
        private readonly string logName;

        /// <summary>
        /// ReferenceCounter 생성자
        /// </summary>
        /// <param name="onReleaseCallback">참조 카운트 0 도달 시 호출될 콜백</param>
        /// <param name="logName">로그에 표시될 이름 (옵션)</param>
        public ReferenceCounter(Action<TKey, TValue> onReleaseCallback = null, string logName = "ReferenceCounter")
        {
            this.onReleaseCallback = onReleaseCallback;
            this.logName = logName;
        }

        #region 참조 관리

        /// <summary>
        /// 새로운 참조를 추가합니다. (참조 카운트 1로 시작)
        /// </summary>
        /// <param name="key">키</param>
        /// <param name="value">값</param>
        public void Add(TKey key, TValue value)
        {
            if (references.ContainsKey(key))
            {
                LogWarning($"[{logName}] 이미 존재하는 키입니다: {key}");
                return;
            }

            references[key] = new ReferenceInfo(value);
            Log($"[{logName}] 참조 추가: {key} (Count: 1)");
        }

        /// <summary>
        /// 기존 참조의 카운트를 증가시킵니다.
        /// </summary>
        /// <param name="key">키</param>
        /// <returns>증가 성공 여부 (키가 존재하지 않으면 false)</returns>
        public bool Increase(TKey key)
        {
            if (!references.TryGetValue(key, out var info))
            {
                return false;
            }

            info.Count++;
            Log($"[{logName}] 참조 증가: {key} (Count: {info.Count})");
            return true;
        }

        /// <summary>
        /// 참조 카운트를 감소시킵니다.
        /// 카운트가 0이 되면 자동으로 해제 콜백을 호출하고 딕셔너리에서 제거합니다.
        /// </summary>
        /// <param name="key">키</param>
        public void Decrease(TKey key)
        {
            if (!references.TryGetValue(key, out var info))
            {
                LogWarning($"[{logName}] 존재하지 않는 키입니다: {key}");
                return;
            }

            info.Count--;
            Log($"[{logName}] 참조 감소: {key} (Count: {info.Count})");

            // 참조 카운트가 0이 되면 해제
            if (info.Count <= 0)
            {
                // 콜백 호출
                onReleaseCallback?.Invoke(key, info.Value);

                // 딕셔너리에서 제거
                references.Remove(key);

                Log($"[{logName}] 참조 해제: {key}");
            }
        }

        #endregion

        #region 조회

        /// <summary>
        /// 값을 조회합니다.
        /// </summary>
        /// <param name="key">키</param>
        /// <param name="value">조회된 값</param>
        /// <returns>키가 존재하면 true</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (references.TryGetValue(key, out var info))
            {
                value = info.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 특정 키의 참조 카운트를 조회합니다.
        /// </summary>
        /// <param name="key">키</param>
        /// <returns>참조 카운트 (키가 없으면 0)</returns>
        public int GetCount(TKey key)
        {
            if (references.TryGetValue(key, out var info))
            {
                return info.Count;
            }
            return 0;
        }

        /// <summary>
        /// 키가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="key">키</param>
        /// <returns>존재하면 true</returns>
        public bool Contains(TKey key)
        {
            return references.ContainsKey(key);
        }

        /// <summary>
        /// 현재 관리 중인 총 참조 개수를 반환합니다.
        /// </summary>
        /// <returns>참조 개수</returns>
        public int GetTotalCount()
        {
            return references.Count;
        }

        /// <summary>
        /// 모든 키와 참조 카운트를 반환합니다. (디버깅용)
        /// </summary>
        /// <returns>키와 카운트의 읽기 전용 딕셔너리</returns>
        public IReadOnlyDictionary<TKey, int> GetAllCounts()
        {
            var result = new Dictionary<TKey, int>();
            foreach (var kvp in references)
            {
                result[kvp.Key] = kvp.Value.Count;
            }
            return result;
        }

        #endregion

        #region 정리

        /// <summary>
        /// 특정 키를 강제로 제거합니다.
        /// 해제 콜백을 호출하고 딕셔너리에서 제거합니다.
        /// </summary>
        /// <param name="key">키</param>
        public void Remove(TKey key)
        {
            if (!references.TryGetValue(key, out var info))
            {
                return;
            }

            // 콜백 호출
            onReleaseCallback?.Invoke(key, info.Value);

            // 딕셔너리에서 제거
            references.Remove(key);

            Log($"[{logName}] 강제 제거: {key}");
        }

        /// <summary>
        /// 모든 참조를 강제로 제거합니다.
        /// 모든 항목에 대해 해제 콜백을 호출하고 딕셔너리를 비웁니다.
        /// </summary>
        public void Clear()
        {
            // 모든 참조에 대해 콜백 호출
            foreach (var kvp in references)
            {
                onReleaseCallback?.Invoke(kvp.Key, kvp.Value.Value);
            }

            int count = references.Count;
            references.Clear();

            Log($"[{logName}] 모든 참조 해제 완료 (개수: {count})");
        }

        #endregion
    }
}
