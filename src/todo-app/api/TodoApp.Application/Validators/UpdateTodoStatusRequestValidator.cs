using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

/// <summary>
/// ステータス更新リクエストのバリデーター
/// REQ-FUNC-007対応
/// </summary>
public class UpdateTodoStatusRequestValidator : AbstractValidator<UpdateTodoStatusRequest>
{
    public UpdateTodoStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("ステータスは必須です")
            .Must(BeValidStatus).WithMessage("無効なステータスです");
    }

    private static bool BeValidStatus(string status)
    {
        var validStatuses = new[] { "NotStarted", "InProgress", "Completed", "Abandoned" };
        return validStatuses.Contains(status);
    }
}
