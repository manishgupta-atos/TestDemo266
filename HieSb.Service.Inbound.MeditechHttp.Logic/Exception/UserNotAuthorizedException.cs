using System;
using System.Collections.Generic;

namespace HieSb.Service.Inbound.MeditechHttp.Logic.Exceptions
{
    /// <summary>
    /// Exception thrown when the user calling the service is not authorized
    /// </summary>
    public class UserNotAuthorizedException : BaseMeditechHttpException
    {
        #region Constructor
        /// <summary>
        /// Initializes a new UserNotAuthorizedException
        /// </summary>
        /// <param name="username"></param>
        /// <param name="allowedGroups"></param>
        /// <param name="innerException"></param>
        public UserNotAuthorizedException(string username, IEnumerable<string> allowedGroups, Exception innerException)
            : base($"The user '{username}' was not a member of any of the accepted groups: {string.Join(", ", allowedGroups)}", innerException)
        {
            DisplayMessage = "The user was not authorized.";
        }
        #endregion
    }
}
