using System.Text.Json.Serialization;


namespace BackblazeUploader
{
    /// <summary>
    /// Deserialization data class to hold the response of get_upload_part_url
    /// </summary>
    public class UploadPartUrlResponse
    {
        /// <summary>
        /// fileId we requested an upload url for
        /// </summary>
        [JsonPropertyName("fileId")]
        public string fileId { get; set; }
        /// <summary>
        /// authorizationToken for use with this upload url
        /// </summary>
        [JsonPropertyName("authorizationToken")]
        public string authorizationToken { get; set; }
        /// <summary>
        /// uploadUrl to be used by this worker thread.
        /// </summary>
        [JsonPropertyName("uploadUrl")]
        public string uploadUrl { get; set; }
    }
}
