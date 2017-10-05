namespace HieSb.Service.Inbound.MeditechHttp.ServiceHost
{
    /// <summary>
    /// Extension methods for converting UploadResponses
    /// </summary>
    public static class UploadResponseExtensionMethods
    {
        /// <summary>
        /// Extension method for converting an UploadResponse to an HttpResponseMessage
        /// </summary>
        /// <param name="response"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string HttpResponseMessage()
        {
            return "httpResponse";
        }
    }
}
