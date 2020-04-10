using System;

namespace BackblazeUploader
{
    /// <summary>
    /// Holds the upload authorization url and token for each upload worker
    /// </summary>
    public class UploadPartsUrlDetails
    {
        /// <summary>
        /// Authorization token for one thread to use.
        /// </summary>
        public String authorizationToken;
        /// <summary>
        /// uploadUrl this thread must use.
        /// </summary>
        public String uploadUrl;
    }
}
