namespace StackUnderflow.Services.ProfileImages;

/// <summary>
/// Thrown when the storage backend rejects an upload — most often because the
/// container does not exist and could not be created, or the credentials lack
/// permission. Lets callers report a clear failure without referencing Azure
/// types, so the endpoint stays testable with a fake implementation.
/// </summary>
public sealed class ProfileImageStorageException(string message, Exception? innerException = null)
    : Exception(message, innerException);