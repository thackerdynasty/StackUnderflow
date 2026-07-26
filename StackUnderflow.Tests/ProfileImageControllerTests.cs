using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackUnderflow.Areas.Api.Models;
using StackUnderflow.Services.ProfileImages;

namespace StackUnderflow.Tests;

/// <summary>
/// Covers the upload endpoint end to end through the controller, using a fake
/// storage implementation. No Azure credentials or Azurite instance required.
/// </summary>
public class ProfileImageControllerTests : IDisposable
{
    private const string UserId = "user-1";
    private const string OtherUserId = "user-2";

    private readonly ProfileImageTestContext _context = new();

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Upload_WithValidPng_StoresPathAndReturnsUrl()
    {
        await _context.AddUserAsync(UserId);
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, UserId);

        var result = await controller.Upload(UserId, TestImages.Png(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ProfileImageUploadedDto>(ok.Value);

        Assert.Equal($"{UserId}/image-1.png", dto.ProfileImagePath);
        Assert.Equal($"https://example.blob.core.windows.net/avatars/{UserId}/image-1.png", dto.ProfileImageUrl);
        Assert.Equal("image/png", storage.LastContentType);

        // The relative path is persisted, never the absolute URL.
        var stored = await _context.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == UserId);
        Assert.Equal($"{UserId}/image-1.png", stored.ProfileImagePath);
    }

    [Fact]
    public async Task Upload_WhenReplacingExistingImage_DeletesThePreviousBlob()
    {
        await _context.AddUserAsync(UserId, profileImagePath: $"{UserId}/old.jpg");
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, UserId);

        await controller.Upload(UserId, TestImages.Png(), CancellationToken.None);

        Assert.Equal([$"{UserId}/old.jpg"], storage.Deleted);
    }

    [Fact]
    public async Task Upload_WhenPreviousBlobIsAlreadyGone_StillSucceeds()
    {
        await _context.AddUserAsync(UserId, profileImagePath: $"{UserId}/missing.jpg");

        // A storage backend that treats a missing blob as a no-op, like the real one.
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, UserId);

        var result = await controller.Upload(UserId, TestImages.Png(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Upload_WhenFileExceedsMaxSize_ReturnsBadRequest()
    {
        await _context.AddUserAsync(UserId);
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, UserId);

        var oversized = TestImages.Png(ProfileImageValidator.MaxSizeBytes + 1);
        var result = await controller.Upload(UserId, oversized, CancellationToken.None);

        AssertProblem(result, StatusCodes.Status400BadRequest);

        // Validation must happen before any storage call.
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task Upload_WithDisallowedFileType_ReturnsBadRequest()
    {
        await _context.AddUserAsync(UserId);
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, UserId);

        // GIF bytes deliberately dressed up as a PNG, to prove the check reads the
        // file signature rather than trusting the name or Content-Type.
        var disguised = TestImages.Gif(fileName: "avatar.png", contentType: "image/png");
        var result = await controller.Upload(UserId, disguised, CancellationToken.None);

        AssertProblem(result, StatusCodes.Status400BadRequest);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task Upload_WhenUnauthenticated_ReturnsUnauthorized()
    {
        await _context.AddUserAsync(UserId);
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, signedInUserId: null);

        var result = await controller.Upload(UserId, TestImages.Png(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task Upload_ForAnotherUser_ReturnsForbidden()
    {
        await _context.AddUserAsync(UserId);
        await _context.AddUserAsync(OtherUserId);
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, UserId);

        // Signed in as UserId, but targeting someone else's profile.
        var result = await controller.Upload(OtherUserId, TestImages.Png(), CancellationToken.None);

        AssertProblem(result, StatusCodes.Status403Forbidden);
        Assert.Empty(storage.Uploaded);

        var untouched = await _context.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == OtherUserId);
        Assert.Null(untouched.ProfileImagePath);
    }

    [Fact]
    public async Task Upload_WhenStorageNotConfigured_ReturnsServiceUnavailable()
    {
        await _context.AddUserAsync(UserId);
        var storage = new FakeProfileImageStorage { IsConfigured = false };
        var controller = _context.CreateController(storage, UserId);

        var result = await controller.Upload(UserId, TestImages.Png(), CancellationToken.None);

        var problem = AssertProblem(result, StatusCodes.Status503ServiceUnavailable);
        Assert.Contains("README", problem.Detail);
        Assert.Empty(storage.Uploaded);

        var unchanged = await _context.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == UserId);
        Assert.Null(unchanged.ProfileImagePath);
    }

    [Fact]
    public async Task Upload_WithNoFile_ReturnsBadRequest()
    {
        await _context.AddUserAsync(UserId);
        var storage = new FakeProfileImageStorage();
        var controller = _context.CreateController(storage, UserId);

        var result = await controller.Upload(UserId, file: null, CancellationToken.None);

        AssertProblem(result, StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Upload_WhenStorageFails_ReturnsBadGatewayAndLeavesUserUnchanged()
    {
        await _context.AddUserAsync(UserId);
        var storage = new FakeProfileImageStorage
        {
            UploadException = new ProfileImageStorageException("container missing"),
        };
        var controller = _context.CreateController(storage, UserId);

        var result = await controller.Upload(UserId, TestImages.Png(), CancellationToken.None);

        AssertProblem(result, StatusCodes.Status502BadGateway);

        var unchanged = await _context.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == UserId);
        Assert.Null(unchanged.ProfileImagePath);
    }

    private static ProblemDetails AssertProblem(IActionResult result, int expectedStatusCode)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatusCode, problem.Status);
        return problem;
    }
}