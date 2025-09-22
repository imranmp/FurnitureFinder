using FluentValidation;

namespace FurnitureFinder.API.Contracts.Validators;

public class RecommendationRequestValidator : AbstractValidator<RecommendationRequest>
{
    public RecommendationRequestValidator()
    {
        RuleFor(x => x.Image)
            .NotNull()
            .WithMessage("Image is required")
            .Must(image => image != null && image.Length > 0)
            .WithMessage("Image is required");

        RuleFor(x => x.SearchText)
            .MaximumLength(150)
            .When(x => !string.IsNullOrEmpty(x.SearchText))
            .WithMessage("Search text is too long. Maximum length is 150 characters.");
    }
}
