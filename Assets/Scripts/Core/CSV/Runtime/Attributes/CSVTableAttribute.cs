using System;

/// <summary>
/// CSV 테이블 이름을 명시하는 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CSVTableAttribute : Attribute
{
    public string TableName { get; }

    public CSVTableAttribute(string tableName)
    {
        TableName = tableName;
    }
}
