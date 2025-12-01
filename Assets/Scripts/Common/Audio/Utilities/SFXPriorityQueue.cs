using System.Collections.Generic;

namespace Common.Audio
{
    /// <summary>
    /// SFX 우선순위 기반 재생 관리
    /// 최대 동시 재생 수 제한 시 낮은 우선순위 사운드를 정지시킵니다.
    /// </summary>
    public class SFXPriorityQueue
    {
        private int maxConcurrentSounds;
        private List<PriorityItem> activeSounds;

        private struct PriorityItem
        {
            public SFXSound Sound;
            public int Priority;
        }

        public SFXPriorityQueue(int maxCount)
        {
            maxConcurrentSounds = maxCount;
            activeSounds = new List<PriorityItem>(maxCount);
        }

        /// <summary>
        /// 우선순위를 확인하고 재생 가능 여부 반환
        /// </summary>
        /// <param name="sound">재생할 SFX 사운드</param>
        /// <param name="priority">우선순위 (높을수록 우선)</param>
        /// <returns>재생 가능 여부</returns>
        public bool TryPlay(SFXSound sound, int priority)
        {
            // 1. 슬롯이 남아있으면 즉시 재생
            if (activeSounds.Count < maxConcurrentSounds)
            {
                activeSounds.Add(new PriorityItem { Sound = sound, Priority = priority });
                return true;
            }

            // 2. 가장 낮은 우선순위 찾기
            int lowestIndex = 0;
            int lowestPriority = activeSounds[0].Priority;

            for (int i = 1; i < activeSounds.Count; i++)
            {
                if (activeSounds[i].Priority < lowestPriority)
                {
                    lowestPriority = activeSounds[i].Priority;
                    lowestIndex = i;
                }
            }

            // 3. 현재 요청이 더 높은 우선순위면 교체
            if (priority > lowestPriority)
            {
                var lowestItem = activeSounds[lowestIndex];
                lowestItem.Sound.Stop();
                activeSounds.RemoveAt(lowestIndex);

                activeSounds.Add(new PriorityItem { Sound = sound, Priority = priority });
                return true;
            }

            return false;
        }

        /// <summary>
        /// 재생 완료된 사운드 제거
        /// </summary>
        public void Remove(SFXSound sound)
        {
            for (int i = activeSounds.Count - 1; i >= 0; i--)
            {
                if (activeSounds[i].Sound == sound)
                {
                    activeSounds.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// 모든 사운드 제거
        /// </summary>
        public void Clear()
        {
            activeSounds.Clear();
        }
    }
}
