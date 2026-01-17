// Auto-Generated from CheatData_Schema.csv
// 수정하지 마세요!

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CSVTable("CheatData")]
public class CheatData : ICSVData
{
    /// <summary>
    /// 치트 명령어 ID (ICheat 구현 클래스명과 동일)
    /// </summary>
    public string ID;

    /// <summary>
    /// 치트 설명
    /// </summary>
    public string Description;

    /// <summary>
    /// 매개변수 정의 (name:type|name:type|...)
    /// </summary>
    public string Parameters;

}
