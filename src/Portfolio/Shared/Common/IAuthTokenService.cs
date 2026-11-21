using Portfolio.Application.Common;

namespace Portfolio.Application.Services;

/// <summary>
/// Issues the authorisation token for the shared key. The interface lives here so the
/// endpoint depends on an application concept rather than on a JWT library; the
/// implementation stays in the presentation project, which already owns the crypto
/// package. That also makes token construction testable without booting HTTP.
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Returns a signed token for <paramref name="providedKey"/>, or a typed failure:
    /// <see cref="ErrorKind.Unauthorized"/> when the key does not match, and
    /// <see cref="ErrorKind.Unexpected"/> when the server's own key is unusable.
    /// </summary>
    Result<string> IssueToken(string providedKey);
}
