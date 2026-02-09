using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Repositories.Interfaces;

/// <summary>
/// ラベルリポジトリインターフェース
/// REQ-FUNC-008～012対応
/// </summary>
public interface ILabelRepository
{
    /// <summary>
    /// 全てのラベルを取得（論理削除されていないもの）
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ラベルリスト</returns>
    Task<List<Label>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// IDでラベルを取得
    /// </summary>
    /// <param name="id">LabelId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>Label、見つからない場合はnull</returns>
    Task<Label?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 複数IDでラベルを取得
    /// </summary>
    /// <param name="ids">LabelIdリスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ラベルリスト</returns>
    Task<List<Label>> GetByIdsAsync(List<int> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// ラベル名で取得（重複チェック用）
    /// </summary>
    /// <param name="name">ラベル名</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>Label、見つからない場合はnull</returns>
    Task<Label?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// ラベルを追加
    /// </summary>
    /// <param name="label">追加するLabel</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>追加されたLabel</returns>
    Task<Label> AddAsync(Label label, CancellationToken cancellationToken = default);

    /// <summary>
    /// ラベルを更新
    /// </summary>
    /// <param name="label">更新するLabel</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新されたLabel</returns>
    Task<Label> UpdateAsync(Label label, CancellationToken cancellationToken = default);

    /// <summary>
    /// ラベルを論理削除
    /// </summary>
    /// <param name="id">LabelId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
