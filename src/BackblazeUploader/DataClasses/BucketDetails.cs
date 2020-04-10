namespace BackblazeUploader
{
    /// <summary>
    /// Data class taking the details of bucket when deserialized from Get_bucket_list.
    /// </summary>
    class BucketDetails
    {
        /// <summary>
        /// id of the bucket
        /// </summary>
        public string bucketId { get; set; }
        /// <summary>
        /// Name of the bucket
        /// </summary>
        public string bucketName { get; set; }
    }
}
