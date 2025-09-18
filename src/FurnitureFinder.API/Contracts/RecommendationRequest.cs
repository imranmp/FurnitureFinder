namespace FurnitureFinder.API.Contracts;

public record RecommendationRequest(IFormFile Image, string? SearchText);
