using TodoApp.Application.DTOs;

namespace TodoApp.Application.Services.Interfaces;

/// <summary>
/// ラベルサービスインターフェース
/// REQ-FUNC-008～012対応
/// </summary>
public interface ILabelService
{
    /// <summary>
    /// 全てのラベルを取得
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ラベルリスト</returns>
    Task<List<LabelDto>> GetAllLabelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// IDでラベルを取得
    /// </summary>
    /// <param name="id">LabelId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>LabelDto</returns>
    Task<LabelDto> GetLabelByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// ラベルを作成
    /// </summary>
    /// <param name="request">作成リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>作成されたLabelDto</returns>
    Task<LabelDto> CreateLabelAsync(
        CreateLabelRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ラベルを更新
    /// </summary>
    /// <param name="id">LabelId</param>
    /// <param name="request">更新リクエスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>更新されたLabelDto</returns>
    Task<LabelDto> UpdateLabelAsync(
        int id, 
        UpdateLabelRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// ラベルを削除
    /// </summary>
    /// <param name="id">LabelId</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task DeleteLabelAsync(int id, CancellationToken cancellationToken = default);
}
