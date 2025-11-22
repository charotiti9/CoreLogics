using System.Collections.Generic;

namespace Common.UI
{
    /// <summary>
    /// UI 스택 관리 (뒤로가기 기능)
    /// PopUp 레이어 등에서 ESC 키 처리 시 사용됩니다.
    /// </summary>
    public class UIStack
    {
        private readonly Stack<UIBase> stack = new Stack<UIBase>();

        /// <summary>
        /// 스택에 있는 UI 개수
        /// </summary>
        public int Count => stack.Count;

        /// <summary>
        /// UI를 스택에 추가합니다.
        /// </summary>
        /// <param name="ui">추가할 UI</param>
        public void Push(UIBase ui)
        {
            if (ui == null)
            {
                return;
            }

            // 중복 방지
            if (stack.Contains(ui))
            {
                return;
            }

            stack.Push(ui);
        }

        /// <summary>
        /// 스택 최상단 UI를 제거하고 반환합니다.
        /// </summary>
        /// <returns>스택 최상단 UI (스택이 비어있으면 null)</returns>
        public UIBase Pop()
        {
            if (stack.Count == 0)
            {
                return null;
            }

            return stack.Pop();
        }

        /// <summary>
        /// 스택 최상단 UI를 반환합니다. (제거하지 않음)
        /// </summary>
        /// <returns>스택 최상단 UI (스택이 비어있으면 null)</returns>
        public UIBase Peek()
        {
            if (stack.Count == 0)
            {
                return null;
            }

            return stack.Peek();
        }

        /// <summary>
        /// 특정 UI를 스택에서 제거합니다.
        /// </summary>
        /// <param name="ui">제거할 UI</param>
        public void Remove(UIBase ui)
        {
            if (ui == null || stack.Count == 0)
            {
                return;
            }

            // Stack은 직접 제거를 지원하지 않으므로 임시 스택 사용
            var temp = new Stack<UIBase>();
            bool found = false;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == ui && !found)
                {
                    found = true;
                    continue; // 제거할 UI는 다시 넣지 않음
                }
                temp.Push(current);
            }

            // 다시 원래 스택에 복원 (역순으로)
            while (temp.Count > 0)
            {
                stack.Push(temp.Pop());
            }
        }

        /// <summary>
        /// 스택을 비웁니다.
        /// </summary>
        public void Clear()
        {
            stack.Clear();
        }
    }
}
