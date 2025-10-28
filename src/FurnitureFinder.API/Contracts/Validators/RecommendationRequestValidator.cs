using FluentValidation;

namespace FurnitureFinder.API.Contracts.Validators;

public class RecommendationRequestValidator : AbstractValidator<RecommendationRequest>
{
    public RecommendationRequestValidator()
    {
        RuleFor(x => x)
            .Must(request => (request.Image != null && request.Image.Length > 0) || !string.IsNullOrEmpty(request.SearchText))
            .WithMessage("Either Image or SearchText must be provided.");

        RuleFor(x => x.SearchText)
            .MaximumLength(150)
            .When(x => !string.IsNullOrEmpty(x.SearchText))
            .WithMessage("Search text is too long. Maximum length is 150 characters.");
    }
}
