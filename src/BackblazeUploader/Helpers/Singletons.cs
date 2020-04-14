using System;
using System.Collections.Generic;
using System.Text;

namespace BackblazeUploader
{
    /// <summary>
    /// Contains Singletons that need to be accessed throughout the code.
    /// </summary>
    class Singletons
    {
        /// <summary>
        /// Private variable for our authenticationDetails singleton
        /// </summary>
        private static AuthenticationDetails mauthenticationDetails;
        /// <summary>
        /// <see cref="AuthenticationDetails"/> object to be used throughout the application.
        /// </summary>
        public static AuthenticationDetails authenticationDetails
        {
            get
            {
                //If its not set yet
                if (mauthenticationDetails == null)
                {
                    //Create it
                    mauthenticationDetails = new AuthenticationDetails();
                }
                //Either way return it
                return mauthenticationDetails;
            }
            set { mauthenticationDetails = value; }
        }

        /// <summary>
        /// Private variable for options property.
        /// </summary>
        private static Options moptions;
        /// <summary>
        /// Holds <see cref="Options"/> results of parsing the command line args supplied by the user.
        /// </summary>
        public static Options options
        {
            get
            {
                //If its not set yet
                if (moptions == null)
                {
                    //Create it
                    moptions = new Options();
                }
                //Either way return it
                return moptions;
            }
            set { moptions = value; }
        }

        /// <summary>
        /// Private variable holding logFileLock
        /// </summary>
        private static object mlogFileLock;
        /// <summary>
        /// Used to lock access to the log file for thread safety.
        /// </summary>
        public static object logFileLock
        {
            get
            {
                //If its not set yet
                if (mlogFileLock == null)
                {
                    //Create it
                    mlogFileLock = new object();
                }
                //Either way return it
                return mlogFileLock;
            }
            set { mlogFileLock = value; }
        }

        /// <summary>
        /// The last summary message outputted to the console
        /// </summary>
        public static string LastSummaryMessage { get; set; } = "Progress: 0%";
    }
}
