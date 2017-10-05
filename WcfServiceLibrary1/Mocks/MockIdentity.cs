using System.Security.Principal;

namespace HieSb.Service.Inbound.MeditechHttp.Tests.Mocks
{
    /// <summary>
    /// Fake identity to use during unit tests
    /// </summary>
    public class MockIdentity : IIdentity
    {
        #region Public Properties
        public string AuthenticationType
        {
            get
            {
                return "Mock Security";
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                return true;
            }
        }

        public string Name { get; set; }
        #endregion
    }
}
