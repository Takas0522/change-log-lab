namespace TodoApp.Application.Common;

/// <summary>
/// ページネーション結果
/// REQ-PERF-002対応
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

/// <summary>
/// API レスポンス
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<ApiError> Errors { get; set; } = new();
    public ResponseMeta? Meta { get; set; }
}

/// <summary>
/// API エラー
/// </summary>
public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Field { get; set; }
}

/// <summary>
/// レスポンスメタデータ
/// </summary>
public class ResponseMeta
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public int? Total { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}

/// <summary>
/// カスタム例外: リソースが見つからない
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    
    public NotFoundException(string resourceName, object key) 
        : base($"{resourceName} with key '{key}' was not found.")
    {
    }
}

/// <summary>
/// カスタム例外: バリデーションエラー
/// </summary>
public class ValidationException : Exception
{
    public List<ValidationError> Errors { get; set; } = new();
    
    public ValidationException(string message) : base(message) { }
    
    public ValidationException(List<ValidationError> errors) : base("Validation failed")
    {
        Errors = errors;
    }
}

/// <summary>
/// カスタム例外: 同時実行制御エラー
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}

/// <summary>
/// バリデーションエラー詳細
/// </summary>
public class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}
