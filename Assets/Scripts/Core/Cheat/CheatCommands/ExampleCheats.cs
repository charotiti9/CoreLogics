#if UNITY_EDITOR
using Core.Utilities;
using UnityEngine;

namespace Core.Cheat.Commands
{
    /// <summary>
    /// 골드 추가 치트
    /// 사용법: AddGold [amount]
    /// </summary>
    public class AddGold : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                GameLogger.Log("[Cheat] 사용법: AddGold [amount]");
                return;
            }

            if (int.TryParse(args[0], out int amount))
            {
                // TODO: 실제 골드 추가 로직 구현
                GameLogger.Log($"[Cheat] 골드 {amount} 추가됨");
            }
            else
            {
                GameLogger.LogWarning($"[Cheat] 잘못된 수량: {args[0]}");
            }
        }
    }

    /// <summary>
    /// 아이템 추가 치트
    /// 사용법: AddItem [itemId] [count]
    /// 예: AddItem "Legendary Sword" 100
    /// </summary>
    public class AddItem : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 2)
            {
                GameLogger.Log("[Cheat] 사용법: AddItem [itemId] [count]");
                return;
            }

            string itemId = args[0];

            if (int.TryParse(args[1], out int count))
            {
                // TODO: 실제 아이템 추가 로직 구현
                GameLogger.Log($"[Cheat] 아이템 '{itemId}' {count}개 추가됨");
            }
            else
            {
                GameLogger.LogWarning($"[Cheat] 잘못된 수량: {args[1]}");
            }
        }
    }

    /// <summary>
    /// 레벨 설정 치트
    /// 사용법: SetLevel [level]
    /// </summary>
    public class SetLevel : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                GameLogger.Log("[Cheat] 사용법: SetLevel [level]");
                return;
            }

            if (int.TryParse(args[0], out int level))
            {
                // TODO: 실제 레벨 설정 로직 구현
                GameLogger.Log($"[Cheat] 레벨이 {level}로 설정됨");
            }
            else
            {
                GameLogger.LogWarning($"[Cheat] 잘못된 레벨: {args[0]}");
            }
        }
    }

    /// <summary>
    /// 무적 모드 토글 치트
    /// 사용법: GodMode
    /// </summary>
    public class GodMode : ICheat
    {
        private static bool isEnabled = false;

        public void Execute(string[] args)
        {
            isEnabled = !isEnabled;

            // TODO: 실제 무적 모드 로직 구현
            GameLogger.Log($"[Cheat] 무적 모드: {(isEnabled ? "활성화" : "비활성화")}");
        }
    }

    /// <summary>
    /// 스테이지 클리어 치트
    /// 사용법: ClearStage
    /// </summary>
    public class ClearStage : ICheat
    {
        public void Execute(string[] args)
        {
            // TODO: 실제 스테이지 클리어 로직 구현
            GameLogger.Log("[Cheat] 현재 스테이지 클리어됨");
        }
    }

    /// <summary>
    /// 텔레포트 치트
    /// 사용법: Teleport [x] [y] [z]
    /// </summary>
    public class Teleport : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                GameLogger.Log("[Cheat] 사용법: Teleport [x] [y] [z]");
                return;
            }

            bool parseSuccess = true;
            parseSuccess &= float.TryParse(args[0], out float x);
            parseSuccess &= float.TryParse(args[1], out float y);
            parseSuccess &= float.TryParse(args[2], out float z);

            if (parseSuccess)
            {
                Vector3 position = new Vector3(x, y, z);

                // TODO: 실제 텔레포트 로직 구현
                GameLogger.Log($"[Cheat] 위치 ({x}, {y}, {z})로 텔레포트됨");
            }
            else
            {
                GameLogger.LogWarning("[Cheat] 잘못된 좌표 형식입니다.");
            }
        }
    }

    /// <summary>
    /// 적 소환 치트
    /// 사용법: SpawnEnemy [enemyId] [count] [level]
    /// </summary>
    public class SpawnEnemy : ICheat
    {
        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                GameLogger.Log("[Cheat] 사용법: SpawnEnemy [enemyId] [count] [level]");
                return;
            }

            string enemyId = args[0];

            bool parseSuccess = true;
            parseSuccess &= int.TryParse(args[1], out int count);
            parseSuccess &= int.TryParse(args[2], out int level);

            if (parseSuccess)
            {
                // TODO: 실제 적 소환 로직 구현
                GameLogger.Log($"[Cheat] 적 '{enemyId}' Lv.{level} {count}마리 소환됨");
            }
            else
            {
                GameLogger.LogWarning("[Cheat] 잘못된 매개변수 형식입니다.");
            }
        }
    }
}
#endif
