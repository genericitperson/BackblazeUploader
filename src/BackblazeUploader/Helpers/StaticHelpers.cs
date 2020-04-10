using System;
using System.IO;
using System.Threading;

namespace BackblazeUploader
{
    /// <summary>
    /// Holds static functions used as helpers.
    /// </summary>
    static class StaticHelpers
    {
        /// <summary>
        /// Logs to various output streams.
        /// </summary>
        /// <remarks>
        /// Currently outputs to logFile called fullDebug.log in exe location and to console (depending upon set DebugLevel).
        /// </remarks>
        /// <param name="Message">Message to log</param>
        /// <param name="messageDebugLevel"><see cref="DebugLevel"/> for this message</param>
        public static void DebugLogger(string Message, DebugLevel messageDebugLevel = DebugLevel.Info)
        {

            #region Generate the message
            //Create the formatted message
            //Generate timestamp
            string Timestamp = DateTime.Now.ToLongTimeString();
            //Get current thread number
            string CurrentThread = Thread.CurrentThread.ManagedThreadId.ToString("D2");
            //Take the message and log it to the console
            string formattedMessage = "[" + CurrentThread + "][" + Timestamp + "]" + Message;
            #endregion
            #region Ouput to the logfile
            //Lock on the file so everyone trying to write to it doesn't cause issues
            lock (Singletons.logFileLock)
            {
                //Log file path. This could have significantly better error handling but needs must!
                string logFile = "fullDebug.log";
                //If the log file doesn't exist create it
                if (File.Exists(logFile) == false)
                {
                    //Create the logfile
                    var logFileCreation = File.Create(logFile);
                    //Now close the link so it doesn't block writing to it below, this could definitely be improved but as it will only be on the first write can't see it mattering too much!
                    logFileCreation.Close();

                }
                //Write line to the log file (this could be cached etc but right now for this want it all the time on each operation.
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logFile, true))
                {
                    //Write the given message
                    file.WriteLine(formattedMessage);
                    //Close so its accessible next time its needed
                    file.Close();
                }
            }
            #endregion
            #region Handling Errors (logs and closes program with error message)
            //If the messageDebugLevel is an error
            if (messageDebugLevel == DebugLevel.Error)
            {
                //Take the message and log it to the console
                Console.WriteLine(formattedMessage);
                //Kill the program with a general error exit code:
                Environment.Exit(1);
            }
            #endregion
            #region Display to console (or not)
            //Get the debug level
            DebugLevel debugLevel = Singletons.options.DebugLevel;
            //If debug level is not met return
            if (debugLevel < messageDebugLevel)
            {
                return;
            }

            //Take the message and log it to the console
            Console.WriteLine(formattedMessage);
            #endregion
        }
    }
}
