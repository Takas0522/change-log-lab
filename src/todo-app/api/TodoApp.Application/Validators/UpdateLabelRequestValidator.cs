using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

/// <summary>
/// ラベル更新リクエストのバリデーター
/// REQ-FUNC-011対応
/// </summary>
public class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ラベル名は必須です")
            .MaximumLength(50).WithMessage("ラベル名は50文字以内です");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("カラーコードは必須です")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("カラーコードはHEX形式(#RRGGBB)で指定してください");
    }
}
