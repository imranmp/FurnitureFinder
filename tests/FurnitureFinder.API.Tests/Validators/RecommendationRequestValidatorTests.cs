using FurnitureFinder.API.Contracts.Validators;
using FurnitureFinder.API.Contracts;

namespace FurnitureFinder.API.Tests.Validators;

public class RecommendationRequestValidatorTests
{
    private readonly RecommendationRequestValidator _validator = new();

    [Fact]
    public void Validate_ImageIsNull_ReturnsInvalid()
    {
        // Arrange
        var request = new RecommendationRequest(null, "Valid");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Image");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ImageIsEmptyOrZeroLength_ReturnsInvalid(long length)
    {
        // Arrange
        var imageMock = new Mock<IFormFile>();
        imageMock.Setup(f => f.Length).Returns(length);
        var request = new RecommendationRequest(imageMock.Object, "Valid");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Image");
    }

    [Fact]
    public void Validate_ImageIsNotEmpty_ReturnsValid()
    {
        // Arrange
        var imageMock = new Mock<IFormFile>();
        imageMock.Setup(f => f.Length).Returns(10);
        var request = new RecommendationRequest(imageMock.Object, "Valid");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(151)]
    [InlineData(200)]
    public void Validate_SearchTextTooLong_ReturnsInvalid(int length)
    {
        // Arrange
        var imageMock = new Mock<IFormFile>();
        imageMock.Setup(f => f.Length).Returns(10);
        var longText = new string('a', length);
        var request = new RecommendationRequest(imageMock.Object, longText);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SearchText");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(150)]
    public void Validate_SearchTextValidLength_ReturnsValid(int? length)
    {
        // Arrange
        var imageMock = new Mock<IFormFile>();
        imageMock.Setup(f => f.Length).Returns(10);

        string validText = null;
        if (length.HasValue)
        {
            validText = new string('a', length.Value);
        }
        var request = new RecommendationRequest(imageMock.Object, validText);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }
}
