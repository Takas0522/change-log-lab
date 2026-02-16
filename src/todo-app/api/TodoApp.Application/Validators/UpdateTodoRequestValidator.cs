using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

/// <summary>
/// ToDo更新リクエストのバリデーター
/// REQ-FUNC-003対応
/// </summary>
public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("タイトルは必須です")
            .MaximumLength(200).WithMessage("タイトルは200文字以内です");

        RuleFor(x => x.Content)
            .MaximumLength(4000).WithMessage("内容は4000文字以内です");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("ステータスは必須です")
            .Must(BeValidStatus).WithMessage("無効なステータスです");

        RuleFor(x => x.LabelIds)
            .Must(x => x == null || x.Count <= 10)
            .WithMessage("ラベルは最大10個までです");
    }

    private static bool BeValidStatus(string status)
    {
        var validStatuses = new[] { "NotStarted", "InProgress", "Completed", "Abandoned" };
        return validStatuses.Contains(status);
    }
}
