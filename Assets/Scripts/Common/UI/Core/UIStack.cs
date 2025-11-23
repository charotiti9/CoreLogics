using System.Collections.Generic;

namespace Common.UI
{
    /// <summary>
    /// UI 스택 관리 (뒤로가기 기능)
    /// PopUp 레이어 등에서 ESC 키 처리 시 사용됩니다.
    /// List 기반으로 구현하여 Remove 연산을 효율적으로 처리합니다.
    /// </summary>
    public class UIStack
    {
        // List를 Stack처럼 사용 (끝에 추가/제거)
        private readonly List<UIBase> stack = new List<UIBase>(16);

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

            stack.Add(ui);
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

            int lastIndex = stack.Count - 1;
            UIBase ui = stack[lastIndex];
            stack.RemoveAt(lastIndex);
            return ui;
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

            return stack[stack.Count - 1];
        }

        /// <summary>
        /// 특정 UI를 스택에서 제거합니다.
        /// List 기반이므로 임시 객체 생성 없이 직접 제거 가능합니다.
        /// </summary>
        /// <param name="ui">제거할 UI</param>
        public void Remove(UIBase ui)
        {
            if (ui == null)
            {
                return;
            }

            stack.Remove(ui);
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
