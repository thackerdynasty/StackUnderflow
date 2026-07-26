using StackUnderflow.Services.ProfileImages;

namespace StackUnderflow.Tests;

/// <summary>
/// Direct tests for the validator, which has no dependency on storage or Azure.
/// </summary>
public class ProfileImageValidatorTests
{
    [Theory]
    [InlineData("png", "image/png", ".png")]
    [InlineData("jpeg", "image/jpeg", ".jpg")]
    [InlineData("webp", "image/webp", ".webp")]
    public async Task ValidateAsync_AcceptsAllowedFormats(string format, string expectedContentType, string expectedExtension)
    {
        var file = format switch
        {
            "png" => TestImages.Png(),
            "jpeg" => TestImages.Jpeg(),
            _ => TestImages.WebP(),
        };

        await using var stream = file.OpenReadStream();
        var result = await ProfileImageValidator.ValidateAsync(stream, file.Length);

        Assert.True(result.IsValid);
        Assert.Equal(expectedContentType, result.ContentType);
        Assert.Equal(expectedExtension, result.Extension);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ValidateAsync_RejectsDisallowedFormat()
    {
        var file = TestImages.Gif();

        await using var stream = file.OpenReadStream();
        var result = await ProfileImageValidator.ValidateAsync(stream, file.Length);

        Assert.False(result.IsValid);
        Assert.Contains("JPEG, PNG, and WebP", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_RejectsFileOverTwoMegabytes()
    {
        var file = TestImages.Png(ProfileImageValidator.MaxSizeBytes + 1);

        await using var stream = file.OpenReadStream();
        var result = await ProfileImageValidator.ValidateAsync(stream, file.Length);

        Assert.False(result.IsValid);
        Assert.Contains("2 MB", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_AcceptsFileExactlyAtTheLimit()
    {
        var file = TestImages.Png(ProfileImageValidator.MaxSizeBytes);

        await using var stream = file.OpenReadStream();
        var result = await ProfileImageValidator.ValidateAsync(stream, file.Length);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_RejectsEmptyFile()
    {
        using var stream = new MemoryStream();

        var result = await ProfileImageValidator.ValidateAsync(stream, 0);

        Assert.False(result.IsValid);
        Assert.Contains("empty", result.Error);
    }

    [Fact]
    public async Task ValidateAsync_RejectsFileTooShortToIdentify()
    {
        using var stream = new MemoryStream([0x89, 0x50]);

        var result = await ProfileImageValidator.ValidateAsync(stream, 2);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_RewindsStreamForTheCaller()
    {
        var file = TestImages.Png();

        await using var stream = file.OpenReadStream();
        await ProfileImageValidator.ValidateAsync(stream, file.Length);

        Assert.Equal(0, stream.Position);
    }
}