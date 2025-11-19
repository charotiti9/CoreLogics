using System;
using System.Collections.Generic;

/// <summary>
/// CSV 스키마의 단일 컬럼 정의
/// </summary>
[Serializable]
public class CSVSchemaColumn
{
    public string ColumnName;
    public string Type;
    public string Description;
    public string Reference;

    /// <summary>
    /// 참조 여부
    /// </summary>
    public bool HasReference
    {
        get { return !string.IsNullOrEmpty(Reference); }
    }

    /// <summary>
    /// 참조 테이블명 (예: "CategoryData.ID" → "CategoryData")
    /// </summary>
    public string ReferenceTableName
    {
        get
        {
            if (!HasReference) return null;

            int dotIndex = Reference.IndexOf('.');
            if (dotIndex > 0)
                return Reference.Substring(0, dotIndex);

            return null;
        }
    }

    /// <summary>
    /// 참조 컬럼명 (예: "CategoryData.ID" → "ID")
    /// </summary>
    public string ReferenceColumnName
    {
        get
        {
            if (!HasReference) return null;

            int dotIndex = Reference.IndexOf('.');
            if (dotIndex > 0 && dotIndex < Reference.Length - 1)
                return Reference.Substring(dotIndex + 1);

            return null;
        }
    }
}

/// <summary>
/// CSV 테이블의 전체 스키마
/// </summary>
public class CSVSchema
{
    public string TableName;
    public List<CSVSchemaColumn> Columns;

    public CSVSchema()
    {
        Columns = new List<CSVSchemaColumn>();
    }
}
