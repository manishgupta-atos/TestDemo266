namespace HieSb.Service.Inbound.MeditechHttp.Logic.Exceptions
{
    /// <summary>
    /// Exception thrown when the request was not valid
    /// </summary>
    public class InvalidRequestException : BaseMeditechHttpException
    {
        #region Constructor
        /// <summary>
        /// Initializes a new InvalidRequestException with a message
        /// </summary>
        /// <param name="message"></param>
        public InvalidRequestException(string message)
            : base(message, null)
        {
            DisplayMessage = "The request was not formatted correctly.";
        }
        #endregion
    }
}
