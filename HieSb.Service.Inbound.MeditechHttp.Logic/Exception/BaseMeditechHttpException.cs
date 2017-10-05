using System;

using HCA.Exceptions;

namespace HieSb.Service.Inbound.MeditechHttp.Logic.Exceptions
{
    /// <summary>
    /// Base exception class for all MeditechHttp exceptions
    /// </summary>
    public abstract class BaseMeditechHttpException : BaseException
    {
        #region Constructor
        /// <summary>
        /// Initializes a new BaseMeditechHttpException
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public BaseMeditechHttpException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
        #endregion
    }
}
