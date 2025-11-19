using System;

/// <summary>
/// CSV 참조 필드임을 표시하는 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CSVReferenceAttribute : Attribute
{
    public string ReferenceTableName { get; }
    public string ReferenceColumnName { get; }

    public CSVReferenceAttribute(string referenceTableName, string referenceColumnName)
    {
        ReferenceTableName = referenceTableName;
        ReferenceColumnName = referenceColumnName;
    }
}
