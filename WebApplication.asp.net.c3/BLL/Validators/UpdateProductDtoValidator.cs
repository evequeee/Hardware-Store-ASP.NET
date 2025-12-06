using FluentValidation;
using WebApplication.asp.net.c3.BLL.DTOs;

namespace WebApplication.asp.net.c3.BLL.Validators;

/// <summary>
/// Validator for UpdateProductDto
/// </summary>
public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID продукту має бути більше 0");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва продукту обов'язкова")
            .MaximumLength(300).WithMessage("Назва продукту не може перевищувати 300 символів")
            .Matches(@"^[а-яА-ЯіїєґІЇЄҐa-zA-Z0-9\s\-\.,\(\)]+$").WithMessage("Назва містить недопустимі символи");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU обов'язковий")
            .MaximumLength(100).WithMessage("SKU не може перевищувати 100 символів")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU має містити тільки великі літери, цифри та дефіси");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Опис не може перевищувати 2000 символів");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Ціна має бути більше 0")
            .LessThanOrEqualTo(1000000).WithMessage("Ціна занадто велика");

        RuleFor(x => x.DiscountPrice)
            .GreaterThan(0).When(x => x.DiscountPrice.HasValue)
            .WithMessage("Знижена ціна має бути більше 0")
            .LessThan(x => x.Price).When(x => x.DiscountPrice.HasValue)
            .WithMessage("Знижена ціна має бути менше звичайної ціни");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Кількість на складі не може бути негативною");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("ID категорії має бути більше 0");

        RuleFor(x => x.BrandId)
            .GreaterThan(0).WithMessage("ІD бренду має бути більше 0");
    }
}
