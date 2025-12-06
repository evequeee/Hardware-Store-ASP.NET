using FluentValidation;
using WebApplication.asp.net.c3.BLL.DTOs;

namespace WebApplication.asp.net.c3.BLL.Validators;

/// <summary>
/// Validator for UpdateCategoryDto
/// </summary>
public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID категорії має бути більше 0");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва категорії обов'язкова")
            .MaximumLength(200).WithMessage("Назва категорії не може перевищувати 200 символів")
            .Matches(@"^[а-яА-ЯіїєґІЇЄҐa-zA-Z0-9\s\-]+$").WithMessage("Назва містить недопустимі символи");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Опис не може перевищувати 1000 символів");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("URL зображення не може перевищувати 500 символів")
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.ImageUrl))
            .WithMessage("Невірний формат URL зображення");

        RuleFor(x => x.ParentCategoryId)
            .GreaterThan(0).When(x => x.ParentCategoryId.HasValue)
            .WithMessage("ID батьківської категорії має бути більше 0")
            .NotEqual(x => x.Id).When(x => x.ParentCategoryId.HasValue)
            .WithMessage("Категорія не може бути батьківською для самої себе");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Порядок сортування не може бути негативним");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
