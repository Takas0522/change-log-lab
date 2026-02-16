using TodoApp.Application.Common;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Services.Interfaces;

/// <summary>
/// ToDoサービスインターフェース
/// REQ-FUNC-001～007, REQ-FUNC-013～017対応
/// </summary>
public interface ITodoService
{
    /// <summary>
    /// ToDo一覧を取得（フィルタ・ページング対応）
    /// </summary>
    /// <param name="parameters">検索パラメータ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ページング結果</returns>
    Task<PagedResult<TodoDto>> GetTodosAsync(
        TodoQueryParameters parameters, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// IDでToDoを取得
    /// </summary>
    /// <param name="id">TodoId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>TodoDto</returns>
    Task<TodoDto> GetTodoByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを作成
    /// </summary>
    /// <param name="request">作成リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成されたTodoDto</returns>
    Task<TodoDto> CreateTodoAsync(
        CreateTodoRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを更新
    /// </summary>
    /// <param name="id">TodoId</param>
    /// <param name="request">更新リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新されたTodoDto</returns>
    Task<TodoDto> UpdateTodoAsync(
        long id, 
        UpdateTodoRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを削除
    /// </summary>
    /// <param name="id">TodoId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task DeleteTodoAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// ステータスを更新
    /// </summary>
    /// <param name="id">TodoId</param>
    /// <param name="request">ステータス更新リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新されたTodoDto</returns>
    Task<TodoDto> UpdateStatusAsync(
        long id, 
        UpdateTodoStatusRequest request, 
        CancellationToken cancellationToken = default);
}
