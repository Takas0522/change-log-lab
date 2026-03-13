using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Repositories.Interfaces;

/// <summary>
/// ToDoリポジトリインターフェース
/// REQ-COMP-002対応: データアクセス層
/// </summary>
public interface ITodoRepository
{
    /// <summary>
    /// ページングされたToDoリストを取得
    /// </summary>
    /// <param name="keyword">検索キーワード</param>
    /// <param name="statuses">ステータスフィルタ</param>
    /// <param name="labelIds">ラベルIDフィルタ</param>
    /// <param name="startDate">作成日開始</param>
    /// <param name="endDate">作成日終了</param>
    /// <param name="sortBy">ソート項目</param>
    /// <param name="sortOrder">ソート順序</param>
    /// <param name="page">ページ番号</param>
    /// <param name="pageSize">ページサイズ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ToDoリストと総件数</returns>
    Task<(List<Todo> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        List<string>? statuses,
        List<int>? labelIds,
        DateTime? startDate,
        DateTime? endDate,
        string sortBy,
        string sortOrder,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// IDでToDoを取得
    /// </summary>
    /// <param name="id">TodoId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>Todo、見つからない場合はnull</returns>
    Task<Todo?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを追加
    /// </summary>
    /// <param name="todo">追加するTodo</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>追加されたTodo</returns>
    Task<Todo> AddAsync(Todo todo, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを更新
    /// </summary>
    /// <param name="todo">更新するTodo</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新されたTodo</returns>
    Task<Todo> UpdateAsync(Todo todo, CancellationToken cancellationToken = default);

    /// <summary>
    /// ToDoを論理削除
    /// </summary>
    /// <param name="id">TodoId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
