using System;
using System.Text.Json.Nodes;
using Xunit;

namespace E2EApiTest.Helpers;

/// <summary>API 断言辅助，失败消息统一附带原始响应便于定位</summary>
public static class ApiAssert
{
    /// <summary>断言业务成功：HTTP 200 且 code=0</summary>
    /// <param name="result">API 响应</param>
    /// <param name="testId">用例编号，用于失败定位</param>
    /// <returns>响应数据节点</returns>
    public static JsonNode? Ok(ApiResult result, String testId)
    {
        Assert.True(result.StatusCode == 200, $"[{testId}] 期望 HTTP 200，实际 {result.StatusCode}。响应：{result.Content}");
        Assert.True(result.Code == 0, $"[{testId}] 期望 code=0，实际 {result.Code}（{result.Message}）。响应：{result.Content}");
        return result.Data;
    }

    /// <summary>断言业务失败码</summary>
    /// <param name="result">API 响应</param>
    /// <param name="code">期望业务码</param>
    /// <param name="testId">用例编号</param>
    public static void Fail(ApiResult result, Int32 code, String testId)
    {
        Assert.True(result.Code == code, $"[{testId}] 期望 code={code}，实际 {result.Code}（{result.Message}）。响应：{result.Content}");
    }

    /// <summary>断言 HTTP 状态码</summary>
    /// <param name="result">API 响应</param>
    /// <param name="status">期望 HTTP 状态码</param>
    /// <param name="testId">用例编号</param>
    public static void HttpStatus(ApiResult result, Int32 status, String testId)
    {
        Assert.True(result.StatusCode == status, $"[{testId}] 期望 HTTP {status}，实际 {result.StatusCode}。响应：{result.Content}");
    }

    /// <summary>断言响应数据为非空数组并返回</summary>
    /// <param name="result">API 响应</param>
    /// <param name="testId">用例编号</param>
    /// <returns>数据数组</returns>
    public static JsonArray Rows(ApiResult result, String testId)
    {
        var array = result.Rows;
        Assert.True(array != null, $"[{testId}] data 不是数组。响应：{result.Content}");
        return array!;
    }

    /// <summary>断言字段级校验错误包含指定字段</summary>
    /// <param name="result">API 响应</param>
    /// <param name="field">字段名（大小写不敏感）</param>
    /// <param name="testId">用例编号</param>
    public static void HasFieldError(ApiResult result, String field, String testId)
    {
        var errors = result.FieldErrors;
        Assert.True(errors != null, $"[{testId}] 响应缺少 fieldErrors。响应：{result.Content}");

        foreach (var item in errors!)
        {
            var name = ApiResult.GetString(item?["field"]);
            if (String.Equals(name, field, StringComparison.OrdinalIgnoreCase)) return;
        }

        Assert.Fail($"[{testId}] fieldErrors 未包含字段 {field}。响应：{result.Content}");
    }
}
