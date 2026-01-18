#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;

namespace Core.Cheat
{
    /// <summary>
    /// 치트 입력 문자열을 파싱합니다.
    /// 큰따옴표로 감싼 문자열은 하나의 토큰으로 처리합니다.
    /// </summary>
    public static class CheatInputParser
    {
        /// <summary>
        /// 입력 문자열을 파싱하여 ID와 매개변수 배열로 분리합니다.
        /// </summary>
        /// <param name="input">입력 문자열</param>
        /// <param name="cheatId">치트 ID (출력)</param>
        /// <param name="args">매개변수 배열 (출력)</param>
        /// <returns>파싱 성공 여부</returns>
        public static bool TryParse(string input, out string cheatId, out string[] args)
        {
            cheatId = null;
            args = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var tokens = Tokenize(input);
            if (tokens.Count == 0)
            {
                return false;
            }

            cheatId = tokens[0];
            args = new string[tokens.Count - 1];

            for (int i = 1; i < tokens.Count; i++)
            {
                args[i - 1] = tokens[i];
            }

            return true;
        }

        /// <summary>
        /// 문자열을 토큰으로 분리합니다.
        /// 큰따옴표 내부는 하나의 토큰으로 처리합니다.
        /// </summary>
        /// <param name="input">입력 문자열</param>
        /// <returns>토큰 목록</returns>
        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    // 따옴표 토글
                    inQuotes = !inQuotes;
                }
                else if (c == ' ' && !inQuotes)
                {
                    // 따옴표 밖의 공백 = 토큰 구분
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
                else
                {
                    currentToken.Append(c);
                }
            }

            // 마지막 토큰 추가
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }
    }
}
#endif
