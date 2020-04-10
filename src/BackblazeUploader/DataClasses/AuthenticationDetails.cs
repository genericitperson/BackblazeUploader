namespace BackblazeUploader
{
    /// <summary>
    /// Data class to hold authentication details from b2_authorize_account
    /// </summary>
    public class AuthenticationDetails
    {
        /// <summary>
        /// AccountId for the API credentials provided.
        /// </summary>
        public string accountId { get; set; }
        /// <summary>
        /// ApiUrl to use for all general calls (NOT for uploads)
        /// </summary>
        public string apiUrl { get; set; }
        /// <summary>
        /// Api authorizationToken for the apiUrl
        /// </summary>
        public string authorizationToken { get; set; }
        /// <summary>
        /// BucketId which has been retrieved based on the bucketName provided by user.
        /// </summary>
        public string bucketId { get; set; }

    }
}
