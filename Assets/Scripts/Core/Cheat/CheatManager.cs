#if UNITY_EDITOR
using Core.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Core.Cheat
{
    /// <summary>
    /// 치트 시스템 관리자 (Editor 전용)
    /// ICheat 구현 클래스를 검색하고 실행합니다.
    /// </summary>
    public class CheatManager : LazySingleton<CheatManager>
    {
        // ID → ICheat 구현 타입 매핑
        private Dictionary<string, Type> cheatTypes = new Dictionary<string, Type>();

        // ID → Cheat 매핑
        private Dictionary<string, CheatData> cheatDataMap = new Dictionary<string, CheatData>();

        // 모든 Cheat 목록
        private List<CheatData> allCheatData = new List<CheatData>();

        // 초기화 여부
        private bool isInitialized = false;

        /// <summary>
        /// 초기화
        /// </summary>
        protected override void Initialize()
        {
            FindAllCheatTypes();
            LoadCheatData();
            isInitialized = true;
            GameLogger.Log("[CheatManager] 초기화 완료");
        }

        /// <summary>
        /// Assembly에서 ICheat 구현 클래스를 모두 찾아 캐싱합니다.
        /// </summary>
        private void FindAllCheatTypes()
        {
            cheatTypes.Clear();

            // 모든 로드된 Assembly에서 검색
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];

                // 시스템 어셈블리 스킵
                if (assembly.FullName.StartsWith("System") ||
                    assembly.FullName.StartsWith("Microsoft") ||
                    assembly.FullName.StartsWith("mscorlib"))
                {
                    continue;
                }

                try
                {
                    Type[] types = assembly.GetTypes();

                    for (int j = 0; j < types.Length; j++)
                    {
                        Type type = types[j];

                        // ICheat 구현 클래스인지 확인
                        if (typeof(ICheat).IsAssignableFrom(type) &&
                            !type.IsInterface &&
                            !type.IsAbstract)
                        {
                            string cheatId = type.Name;

                            if (!cheatTypes.ContainsKey(cheatId))
                            {
                                cheatTypes[cheatId] = type;
                                GameLogger.Log($"[CheatManager] 치트 발견: {cheatId}");
                            }
                            else
                            {
                                GameLogger.LogWarning($"[CheatManager] 중복된 치트 ID: {cheatId}");
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // 일부 어셈블리는 타입을 로드할 수 없음 (무시)
                }
            }
        }

        /// <summary>
        /// CSVManager에서 Cheat 데이터를 로드합니다.
        /// </summary>
        private void LoadCheatData()
        {
            cheatDataMap.Clear();
            allCheatData.Clear();

            if (!CSVManager.IsAlive())
            {
                GameLogger.LogWarning("[CheatManager] CSVManager가 초기화되지 않았습니다. 치트 데이터를 로드할 수 없습니다.");
                return;
            }

            var cheatList = CSVManager.Instance.GetTable<CheatData>();

            for (int i = 0; i < cheatList.Count; i++)
            {
                var data = cheatList[i];
                cheatDataMap[data.ID] = data;
                allCheatData.Add(data);
            }

            GameLogger.Log($"[CheatManager] 치트 데이터 로드 완료: {allCheatData.Count}개");
        }

        /// <summary>
        /// 치트 데이터를 다시 로드합니다.
        /// </summary>
        public void ReloadCheatData()
        {
            FindAllCheatTypes();
            LoadCheatData();
        }

        /// <summary>
        /// 치트를 실행합니다.
        /// </summary>
        /// <param name="input">입력 문자열 (예: "AddItem \"Legendary Sword\" 100")</param>
        /// <returns>실행 성공 여부</returns>
        public bool ExecuteCheat(string input)
        {
            if (!isInitialized)
            {
                GameLogger.LogError("[CheatManager] 초기화되지 않았습니다.");
                return false;
            }

            // 입력 파싱
            if (!CheatInputParser.TryParse(input, out string cheatId, out string[] args))
            {
                GameLogger.LogWarning("[CheatManager] 입력을 파싱할 수 없습니다.");
                return false;
            }

            // 치트 타입 찾기
            if (!cheatTypes.TryGetValue(cheatId, out Type cheatType))
            {
                GameLogger.LogWarning($"[CheatManager] 치트를 찾을 수 없습니다: {cheatId}");
                return false;
            }

            try
            {
                // 인스턴스 생성 및 실행
                ICheat cheat = (ICheat)Activator.CreateInstance(cheatType);
                cheat.Execute(args);
                GameLogger.Log($"[CheatManager] 치트 실행 완료: {input}");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[CheatManager] 치트 실행 중 오류 발생: {cheatId}\n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 등록된 모든 Cheat 목록을 반환합니다.
        /// </summary>
        /// <returns>Cheat 목록</returns>
        public List<CheatData> GetAllCheatData()
        {
            return allCheatData;
        }

        /// <summary>
        /// 특정 ID의 Cheat를 반환합니다.
        /// </summary>
        /// <param name="id">치트 ID</param>
        /// <returns>Cheat (없으면 null)</returns>
        public CheatData GetCheatData(string id)
        {
            if (cheatDataMap.TryGetValue(id, out CheatData data))
            {
                return data;
            }
            return null;
        }

        /// <summary>
        /// 입력 텍스트와 매칭되는 Cheat 목록을 반환합니다.
        /// </summary>
        /// <param name="inputText">입력 텍스트</param>
        /// <returns>매칭되는 Cheat 목록 (새 리스트 반환)</returns>
        public List<CheatData> GetMatchingCheats(string inputText)
        {
            var result = new List<CheatData>();

            // 빈 문자열이면 전체 목록 복사본 반환
            if (string.IsNullOrEmpty(inputText))
            {
                for (int i = 0; i < allCheatData.Count; i++)
                {
                    result.Add(allCheatData[i]);
                }
                return result;
            }

            string lowerInput = inputText.ToLower();

            for (int i = 0; i < allCheatData.Count; i++)
            {
                var data = allCheatData[i];
                if (data.ID.ToLower().Contains(lowerInput))
                {
                    result.Add(data);
                }
            }

            return result;
        }

        /// <summary>
        /// 정확히 일치하는 치트 ID를 찾아 반환합니다.
        /// </summary>
        /// <param name="id">치트 ID</param>
        /// <returns>일치하는 CheatData, 없으면 null</returns>
        public CheatData GetExactMatch(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            // 대소문자 무시하고 정확히 일치하는지 확인
            string lowerId = id.ToLower();

            for (int i = 0; i < allCheatData.Count; i++)
            {
                var data = allCheatData[i];
                if (data.ID.ToLower() == lowerId)
                {
                    return data;
                }
            }

            return null;
        }

        /// <summary>
        /// 치트 데이터가 로드되어 있는지 확인합니다.
        /// </summary>
        /// <returns>데이터가 있으면 true</returns>
        public bool HasCheatData()
        {
            return allCheatData.Count > 0;
        }

        /// <summary>
        /// 치트 타입이 존재하는지 확인합니다.
        /// </summary>
        /// <param name="id">치트 ID</param>
        /// <returns>존재 여부</returns>
        public bool HasCheatType(string id)
        {
            return cheatTypes.ContainsKey(id);
        }
    }
}
#endif
