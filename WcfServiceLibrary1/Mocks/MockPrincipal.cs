using System;
using System.Security.Principal;

namespace HieSb.Service.Inbound.MeditechHttp.Tests.Mocks
{
    public class MockPrincipal : IPrincipal
    {
        #region Public Properties
        public IIdentity Identity { get; set; }
        #endregion

        #region Constructor
        public MockPrincipal(string username)
        {
            Identity = new MockIdentity()
            {
                Name = username
            };
        }
        #endregion

        #region Public Methods
        public bool IsInRole(string role)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
