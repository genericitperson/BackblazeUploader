using System;
using System.Collections;
using System.Text.Json.Serialization;

namespace BackblazeUploader
{
    /// <summary>
    /// The data class that is serialized to provide the B2FinishLargeFileRequest json.
    /// </summary>
    public class B2FinishLargeFileRequest
    {
            /// <summary>
            /// Id of the file provided when the upload was started.
            /// </summary>
            [JsonPropertyName("fileId")]
            public String fileId {get; set;}
            /// <summary>
            /// List containing all the sha1 hashes for each part IN ORDER.
            /// </summary>
            [JsonPropertyName("partSha1Array")]
            public ArrayList partSha1Array {get; set;}
        
    }
}
