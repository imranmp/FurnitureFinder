using FurnitureFinder.API.Contracts.Validators;
using FurnitureFinder.API.Contracts;

namespace FurnitureFinder.API.Tests.Validators;

public class RecommendationRequestValidatorTests
{
    private readonly RecommendationRequestValidator _validator = new();

    [Theory]
    [InlineData(null, null)]
    [InlineData(0, null)]
    [InlineData(0, "")]
    [InlineData(0, " ")]
    public void Validate_BothImageAndSearchTextIsMissing_ReturnsInvalid(int? length, string searchText)
    {
        // Arrange
        var imageMock = new Mock<IFormFile>();
        imageMock.Setup(f => f.Length).Returns(length ?? 0);

        var request = new RecommendationRequest(length == null ? null : imageMock.Object, searchText);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Either Image or SearchText must be provided.");
    }


    [Theory]
    [InlineData(151)]
    [InlineData(200)]
    public void Validate_SearchTextTooLong_ReturnsInvalid(int length)
    {
        // Arrange
        var longText = new string('a', length);
        var request = new RecommendationRequest(null, longText);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SearchText");
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(10, -1)]
    [InlineData(10, null)]
    [InlineData(null, 10)]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(150, 150)]
    public void Validate_ValidRequests_ReturnsValid(int? length, int? searchTextLength)
    {
        // Arrange
        var imageMock = new Mock<IFormFile>();
        imageMock.Setup(f => f.Length).Returns(length ?? 0);

        int l = 0;
        if (searchTextLength.HasValue)
        {
            l = searchTextLength.Value < 1 ? 0 : searchTextLength.Value;
        }

        var searchText = new string('a', l);

        var request = new RecommendationRequest(length == null ? null : imageMock.Object, searchText);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

}
