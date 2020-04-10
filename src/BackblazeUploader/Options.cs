using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace BackblazeUploader
{
    /// <summary>
    /// Options class for deserializing command line options into.
    /// </summary>
    /// <remarks>
    /// Used by the CommandLineParser nuget. Github: https://github.com/commandlineparser/commandline 
    /// </remarks>
    public class Options
    {
        #region Required
        /// <summary>
        /// Id for the applicationKey from Backblaze.
        /// </summary>
        [Option(
            Required = true, HelpText = "The ApplicationKey ID for the Backblaze API")]
        public string applicationKeyId { get; set; }
        /// <summary>
        /// Application key from backblaze
        /// </summary>
        [Option(
            Required = true, HelpText = "The ApplicationKey for the Backblaze API")]
        public string applicationKey { get; set; }

        /// <summary>
        /// Name of the bucket to upload to
        /// </summary>
        [Value(0, MetaName = "BucketName", HelpText = "The name of the bucket to upload to, must already exist.")]
        public string bucketName { get; set; }

        /// <summary>
        /// Path of the file to be uploaded
        /// </summary>
        [Value(1, MetaName = "FilePath", HelpText = "The path of the file to upload")]
        public string filePath { get; set; }
        #endregion

        #region Optional arguments
        /// <summary>
        /// <see cref="DebugLevel"/> to be applied.
        /// </summary>
        [Option(
            Default = 3, HelpText = "Configures the level of messages to be output. \n" +
            "           1 - Errors only \n" +
            "           2 - Errors and Warnings \n" +
            "           3 - (Default) Info, Errors & Warnings \n" +
            "           4 - Verbose messages, good for understanding whats happening under the bonnet \n" +
            "           5 - All available debug messages, use of this option is only recommended when troubleshooting issues.")]
        public DebugLevel DebugLevel { get; set; }

        /// <summary>
        /// Number of threads to utilise.
        /// </summary>
        [Option(
            Default = 20, HelpText = "Specifies maximum number of threads. Default is 20.")]
        public int Threads { get; set; }

        /// <summary>
        /// Part sizes to be used.
        /// </summary>
        [Option(
            Default = 20, HelpText = "Specifies size of individual parts to transfer in MBs. Minimum is 6.")]
        public int PartSize { get; set; }
        #endregion

        #region Usage Examples

        [Usage(ApplicationAlias = "BackblazeUploader.exe")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                return new List<Example>() {
                                            new Example("Upload a file", new Options { applicationKeyId="00378d9e6385be60000000012", applicationKey="K2895Mzq3Gm66cqeg6JSKFIE3YDMgqF9", bucketName = "TargetBucket", filePath= "C:\\Folder\\File To Upload.exe" })
                                          };
            }
        }

        #endregion
    }
}
