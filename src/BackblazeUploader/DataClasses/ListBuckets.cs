using System.Collections.Generic;

namespace BackblazeUploader
{
    /// <summary>
    /// Used for deserializing the results of list_buckets
    /// </summary>
    class ListBuckets
    {
        /// <summary>
        /// IEnumerable of <see cref="BucketDetails"/> to take the bucket details.
        /// </summary>
        public IEnumerable<BucketDetails> buckets { get; set; }
    }
}
