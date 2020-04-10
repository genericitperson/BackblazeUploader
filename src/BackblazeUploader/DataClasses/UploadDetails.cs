using System.Collections;

namespace BackblazeUploader
{
    /// <summary>
    /// Holds the details of the upload progress for each worker thread to access.
    /// </summary>
    /// <remarks>
    /// Value in this class should only be changed having issued a lock on <see cref="MultiPartUpload.uploadDetailsLock"/>. In most cases non-critical reading without a lock will be acceptable.
    /// PLEASE NOTE: Values in this class are updated BEFORE the worker actually attempts the upload, this ensures the next worker can start the next part.
    /// </remarks>
    public class UploadDetails
    {
        #region Properties
        /// <summary>
        /// Tracks the total number of bytes promised (see remarks on class) to be sent so far.
        /// </summary>
        public long totalBytesSent = 0;
        /// <summary>
        /// Number of bytes to be sent for each part. Calculated based off the options.PartSize from the user or default.
        /// </summary>
        public long bytesSentForPart = Singletons.options.PartSize * (1000 * 1000);
        /// <summary>
        /// part number to be transmitted next.
        /// </summary>
        public int partNo = 1;
        /// <summary>
        /// Minimum size of each part to send (same as bytesSentForPart)
        /// </summary>
        public long minimumPartSize;
        /// <summary>
        /// Array of sha1 hashes for each uploaded file IN ORDER
        /// </summary>
        public ArrayList partSha1Array = new ArrayList();
        #endregion


        #region Functions
        /// <summary>
        /// Constructor - sets minimumPartSize to bytesSentForPart.
        /// </summary>
        public UploadDetails()
        {
            minimumPartSize = bytesSentForPart;
        }

        public UploadDetails CloneMe()
        {
            return (UploadDetails)this.MemberwiseClone();
        }
    }
        #endregion
}
