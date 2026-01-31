using FluentValidation;
using TodoApp.Application.DTOs;

namespace TodoApp.Application.Validators;

/// <summary>
/// ラベル作成リクエストのバリデーター
/// REQ-FUNC-010対応
/// </summary>
public class CreateLabelRequestValidator : AbstractValidator<CreateLabelRequest>
{
    public CreateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ラベル名は必須です")
            .MaximumLength(50).WithMessage("ラベル名は50文字以内です");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("カラーコードは必須です")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("カラーコードはHEX形式(#RRGGBB)で指定してください");
    }
}
