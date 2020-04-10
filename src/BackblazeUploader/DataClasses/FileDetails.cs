using System.Text.Json.Serialization;
using HeyRed.Mime;

namespace BackblazeUploader
{
    /// <summary>
    /// Contains the details of the file needed for upload.
    /// </summary>
    public class FileDetails
    {

        /// <summary>
        /// fileId provided by start_large_file_upload.
        /// </summary>
        [JsonPropertyName("fileId")]
        public string fileId { get; set; }
        /// <summary>
        /// BucketId got from B2 based on bucketName from user.
        /// </summary>
        [JsonPropertyName("bucketId")]
        public string bucketId { get; set; }
        /// <summary>
        /// Private variable holding the fileName
        /// </summary>
        private string mfileName;
        /// <summary>
        /// fileName property. Automatically calculates mime type when set.
        /// </summary>
        [JsonPropertyName("fileName")]
        public string fileName { get {
                return mfileName;
            } set
            {
                mfileName = value;
                fileMime = MimeTypesMap.GetMimeType(value);
            }
        }
        /// <summary>
        /// Mimetype of the file. Calculated by HeyRed.Mime.MimeTypesMap
        /// </summary>
        public string fileMime { get; set; }
   

    }
}
