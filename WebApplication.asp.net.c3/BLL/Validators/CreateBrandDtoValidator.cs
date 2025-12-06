using FluentValidation;
using WebApplication.asp.net.c3.BLL.DTOs;

namespace WebApplication.asp.net.c3.BLL.Validators;

/// <summary>
/// Validator for CreateBrandDto
/// </summary>
public class CreateBrandDtoValidator : AbstractValidator<CreateBrandDto>
{
    public CreateBrandDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва бренду обов'язкова")
            .MaximumLength(200).WithMessage("Назва бренду не може перевищувати 200 символів")
            .Matches(@"^[а-яА-ЯіїєґІЇЄҐa-zA-Z0-9\s\-\.]+$").WithMessage("Назва містить недопустимі символи");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Опис не може перевищувати 1000 символів");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("URL логотипу не може перевищувати 500 символів")
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.LogoUrl))
            .WithMessage("Невірний формат URL логотипу");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500).WithMessage("URL веб-сайту не може перевищувати 500 символів")
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.WebsiteUrl))
            .WithMessage("Невірний формат URL веб-сайту");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Назва країни не може перевищувати 100 символів")
            .Matches(@"^[а-яА-ЯіїєґІЇЄҐa-zA-Z\s\-]+$").When(x => !string.IsNullOrEmpty(x.Country))
            .WithMessage("Назва країни містить недопустимі символи");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
