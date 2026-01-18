/// <summary>
/// 모든 치트 명령이 구현해야 하는 인터페이스
/// 치트 클래스명은 Cheat.csv의 ID와 동일해야 합니다.
/// </summary>
public interface ICheat
{
    /// <summary>
    /// 치트를 실행합니다.
    /// </summary>
    /// <param name="args">명령어 뒤에 입력된 인자들 (공백으로 구분, 큰따옴표 내부는 하나의 인자)</param>
    void Execute(string[] args);
}
