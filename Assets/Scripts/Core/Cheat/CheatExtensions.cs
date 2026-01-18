using System.Text;

/// <summary>
/// 자동 생성된 Cheat 클래스의 확장 메서드
/// </summary>
public static class CheatExtensions
{
    /// <summary>
    /// 사용법 문자열을 생성합니다.
    /// 예: "AddItem [itemId] [count]"
    /// </summary>
    /// <param name="cheat">치트 데이터</param>
    /// <returns>사용법 문자열</returns>
    public static string GetUsage(this CheatData cheat)
    {
        if (string.IsNullOrEmpty(cheat.Parameters))
        {
            return cheat.ID;
        }

        var paramParts = cheat.Parameters.Split('|');
        var usage = new StringBuilder(cheat.ID);

        for (int i = 0; i < paramParts.Length; i++)
        {
            var nameTypePair = paramParts[i].Split(':');
            if (nameTypePair.Length > 0)
            {
                usage.Append(" [");
                usage.Append(nameTypePair[0]);
                usage.Append("]");
            }
        }

        return usage.ToString();
    }

    /// <summary>
    /// 매개변수 개수를 반환합니다.
    /// </summary>
    /// <param name="cheat">치트 데이터</param>
    /// <returns>매개변수 개수</returns>
    public static int GetParameterCount(this CheatData cheat)
    {
        if (string.IsNullOrEmpty(cheat.Parameters))
        {
            return 0;
        }

        return cheat.Parameters.Split('|').Length;
    }

    /// <summary>
    /// 파라미터 정보 구조체
    /// </summary>
    public struct ParameterInfo
    {
        public string Name;
        public string Type;

        public ParameterInfo(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// 파라미터 정보 목록을 반환합니다.
    /// </summary>
    /// <param name="cheat">치트 데이터</param>
    /// <returns>파라미터 정보 목록</returns>
    public static System.Collections.Generic.List<ParameterInfo> GetParameterInfoList(this CheatData cheat)
    {
        var result = new System.Collections.Generic.List<ParameterInfo>();

        if (string.IsNullOrEmpty(cheat.Parameters))
        {
            return result;
        }

        var paramParts = cheat.Parameters.Split('|');

        for (int i = 0; i < paramParts.Length; i++)
        {
            var nameTypePair = paramParts[i].Split(':');
            string paramName = nameTypePair.Length > 0 ? nameTypePair[0] : "";
            string paramType = nameTypePair.Length > 1 ? nameTypePair[1] : "string";

            result.Add(new ParameterInfo(paramName, paramType));
        }

        return result;
    }
}
