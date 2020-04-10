using System;
using System.Collections.Generic;
using System.Text;

namespace BackblazeUploader
{
    /// <summary>
    /// The debug level for use in the debug output
    /// </summary>
    public enum DebugLevel
    {
        /// <summary>
        /// Errors, always displayed.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Warnings, displayed by default.
        /// </summary>
        Warn = 2,
        /// <summary>
        /// Information, displayed by default.
        /// </summary>
        Info = 3,
        /// <summary>
        /// Verbose output, contains more details than information but still relatively limited.
        /// </summary>
        Verbose = 4,
        /// <summary>
        /// Full debug output, contains lots of status information, far more than usually required.
        /// </summary>
        FullDebug = 5 

    }
}
