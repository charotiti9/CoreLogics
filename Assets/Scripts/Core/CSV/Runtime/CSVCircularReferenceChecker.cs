using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CSV 테이블 간 순환 참조 검사
/// DFS 알고리즘을 사용하여 참조 그래프에서 순환을 탐지합니다.
/// </summary>
public static class CSVCircularReferenceChecker
{
    /// <summary>
    /// 스키마로부터 참조 그래프 구축
    /// </summary>
    /// <param name="schemas">테이블명 → 스키마 매핑</param>
    /// <returns>테이블명 → 참조하는 테이블명 리스트</returns>
    public static Dictionary<string, List<string>> BuildReferenceGraph(Dictionary<string, CSVSchema> schemas)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var schema in schemas.Values)
        {
            var references = new List<string>();

            // 각 컬럼의 참조 정보 수집
            for (int i = 0; i < schema.Columns.Count; i++)
            {
                CSVSchemaColumn column = schema.Columns[i];

                if (column.HasReference)
                {
                    string refTable = column.ReferenceTableName;

                    // 중복 제거
                    if (!references.Contains(refTable))
                    {
                        references.Add(refTable);
                    }
                }
            }

            graph[schema.TableName] = references;
        }

        return graph;
    }

    /// <summary>
    /// DFS 기반 순환 참조 검사
    /// </summary>
    /// <param name="graph">참조 그래프</param>
    /// <param name="cycle">순환이 발견된 경로 (출력)</param>
    /// <returns>순환 참조가 있으면 true</returns>
    public static bool HasCircularReference(Dictionary<string, List<string>> graph, out List<string> cycle)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var path = new List<string>();

        // 모든 노드에서 DFS 시작
        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
            {
                if (DFS(node, graph, visited, recursionStack, path, out cycle))
                {
                    return true;
                }
            }
        }

        cycle = null;
        return false;
    }

    /// <summary>
    /// DFS 재귀 탐색
    /// </summary>
    private static bool DFS(
        string node,
        Dictionary<string, List<string>> graph,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> path,
        out List<string> cycle)
    {
        visited.Add(node);
        recursionStack.Add(node);
        path.Add(node);

        // 이웃 노드 탐색
        if (graph.TryGetValue(node, out var neighbors))
        {
            for (int i = 0; i < neighbors.Count; i++)
            {
                string neighbor = neighbors[i];

                if (!visited.Contains(neighbor))
                {
                    // 방문하지 않은 노드 → 재귀 탐색
                    if (DFS(neighbor, graph, visited, recursionStack, path, out cycle))
                    {
                        return true;
                    }
                }
                else if (recursionStack.Contains(neighbor))
                {
                    // 재귀 스택에 있음 → 순환 발견!
                    int cycleStartIndex = path.IndexOf(neighbor);
                    cycle = path.GetRange(cycleStartIndex, path.Count - cycleStartIndex);
                    cycle.Add(neighbor); // 순환 완성
                    return true;
                }
            }
        }

        // 백트래킹
        recursionStack.Remove(node);
        path.RemoveAt(path.Count - 1);

        cycle = null;
        return false;
    }

    /// <summary>
    /// 순환 경로를 읽기 쉬운 문자열로 포맷
    /// </summary>
    /// <param name="cycle">순환 경로</param>
    /// <returns>포맷된 문자열 (예: "A → B → C → A")</returns>
    public static string FormatCyclePath(List<string> cycle)
    {
        if (cycle == null || cycle.Count == 0)
            return "";

        return string.Join(" → ", cycle);
    }
}
