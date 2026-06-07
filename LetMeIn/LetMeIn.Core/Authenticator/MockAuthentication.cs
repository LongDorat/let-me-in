using LetMeIn.Core.Interfaces;

namespace LetMeIn.Core.Authenticator
{
    public class MockAuthentication : IAuthenticator
    {
        public bool Run()
        {
            return true;
        }
    }
}
