using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// _Schema.csv 파일을 파싱하여 CSVSchema 생성
/// </summary>
public static class CSVSchemaParser
{
    /// <summary>
    /// 스키마 파일 파싱
    /// </summary>
    /// <param name="tableName">테이블 이름 (예: "ItemData")</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>파싱된 스키마</returns>
    public static async UniTask<CSVSchema> ParseSchemaAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        string schemaFileName = $"{tableName}_Schema";

        // CSVParser를 사용하여 스키마 파싱
        List<CSVSchemaColumn> columns = await CSVParser.ParseAsync<CSVSchemaColumn>(
            schemaFileName,
            cancellationToken);

        if (columns == null || columns.Count == 0)
        {
            Debug.LogError($"[CSVSchemaParser] 스키마 파싱 실패: {schemaFileName}");
            return new CSVSchema { TableName = tableName };
        }

        return new CSVSchema
        {
            TableName = tableName,
            Columns = columns
        };
    }
}
