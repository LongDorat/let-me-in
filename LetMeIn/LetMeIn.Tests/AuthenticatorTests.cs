using LetMeIn.Core.Authenticator;
using LetMeIn.Core.Interfaces;

namespace LetMeIn.Tests
{
  public class AuthenticatorTests
  {
    private readonly IAuthenticator _authenticator = new MockAuthentication();

    [Fact]
    public void TestAuthenticator()
    {
      // Arrange
      // Act
      var result = _authenticator.Run();

      // Assert
      Assert.True(result);
    }
  }
}
