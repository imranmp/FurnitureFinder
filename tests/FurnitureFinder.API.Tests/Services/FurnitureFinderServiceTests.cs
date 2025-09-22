using FurnitureFinder.API.Services;
using FurnitureFinder.API.Services.Interfaces;
using FurnitureFinder.API.Contracts;

namespace FurnitureFinder.API.Tests.Services;

public class FurnitureFinderServiceTests
{
    [Fact]
    public async Task AnalyzeAndRecommendAsync_ValidRequest_ReturnsRecommendationResponse()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var imageBytes = new byte[] { 1, 2, 3 };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream stream, CancellationToken ct) => stream.WriteAsync(imageBytes, 0, imageBytes.Length, ct));
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        var request = new RecommendationRequest(fileMock.Object, null);

        var azureVisionResult = new AzureVisionResult("desc")
        {
            Tags = ["tag1"],
            OtherDescriptions = ["other1"]
        };
        var imageUrl = new Uri("https://blob/test.jpg");
        var openAIDescription = "OpenAI description";
        var openAIConciseDescription = "Concise description";
        var recommendations = new List<ProductSearchResult>();
        var semanticQuery = "semantic-query";

        mock.Mock<IAzureVisionService>()
            .Setup(x => x.AnalyzeFurnitureAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(azureVisionResult);
        mock.Mock<IBlobStorageService>()
            .Setup(x => x.UploadImageAndGetSasUrlAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(imageUrl);
        mock.Mock<IAzureOpenAIService>()
            .Setup(x => x.GetImageDescription(azureVisionResult, imageUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(openAIDescription);
        mock.Mock<IAzureOpenAIService>()
            .Setup(x => x.GetConciseDescription(azureVisionResult, imageUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(openAIConciseDescription);
        mock.Mock<IAzureSearchService>()
            .Setup(x => x.FindComplementaryFurnitureAsync(azureVisionResult, openAIConciseDescription, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((semanticQuery, recommendations));

        // Act
        var service = mock.Create<FurnitureFinderService>();
        var result = await service.AnalyzeAndRecommendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(azureVisionResult, result.AzureVisionResult);
        Assert.Equal(openAIDescription, result.OpenAIDescription);
        Assert.Equal(openAIConciseDescription, result.OpenAIConciseDescription);
        Assert.Equal(semanticQuery, result.SemanticQuery);
        Assert.Equal(recommendations, result.Recommendations);
    }

    [Fact]
    public async Task AnalyzeAndRecommendAsync_NullImage_ThrowsException()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();
        var request = new RecommendationRequest(Image: null, null);

        // Act & Assert
        var service = mock.Create<FurnitureFinderService>();
        await Assert.ThrowsAsync<NullReferenceException>(() => service.AnalyzeAndRecommendAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeAndRecommendAsync_DependencyThrows_PropagatesException()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var imageBytes = new byte[] { 1, 2, 3 };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream stream, CancellationToken ct) => stream.WriteAsync(imageBytes, 0, imageBytes.Length, ct));
        fileMock.Setup(f => f.FileName).Returns("test.jpg");

        var request = new RecommendationRequest(fileMock.Object, null);

        mock.Mock<IAzureVisionService>()
            .Setup(x => x.AnalyzeFurnitureAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Vision error"));

        // Act & Assert
        var service = mock.Create<FurnitureFinderService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAndRecommendAsync(request, CancellationToken.None));
    }
}
