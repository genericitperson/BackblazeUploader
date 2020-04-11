using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Linq;

namespace BackblazeUploader
{
    /// <summary>
    /// Monitors bandwidth usage and provides feedback for <see cref="MultiPartUpload.StartUploadWorker"/> to act upon.
    /// </summary>
    class BandwidthMonitor
    {
        /// <summary>
        /// Indicates the next thread in a position to do so should kill itself.
        /// </summary>
        public bool reduceUsage = false;
        /// <summary>
        /// Instructs any threads seeing this flag to kill itself.
        /// </summary>
        public bool urgentReduceUsage = false;
        /// <summary>
        /// Every ping response time from the last 5 seconds.
        /// </summary>
        public List<int> Last5Seconds = new List<int>();
        /// <summary>
        /// Indicates whether new workers can be created, set false for 30 seconds following any request for a reduction.
        /// </summary>
        public bool CanIncrease = true;
        /// <summary>
        /// Time of the lastFailedPing so CanIncrease can decide what to do.
        /// </summary>
        public DateTime LastFailedPing = DateTime.Now.AddSeconds(-60);
        /// <summary>
        /// Time of the last average above the threshold so CanIncrease can decide what to do.
        /// </summary>
        public DateTime LastHighAverage = DateTime.Now.AddSeconds(-60);
        /// <summary>
        /// Time between pings
        /// </summary>
        int pingInterval = 500;
        /// <summary>
        /// Flag so other threads can pass message to stop working.
        /// </summary>
        public bool StopMonitoring = false;

        /// <summary>
        /// Starts the bandwidth monitor
        /// </summary>
        public void startMonitoring()
        {
            //Log to debug
            StaticHelpers.DebugLogger("Starting bandwidth monitor, establishing baseline....", DebugLevel.Verbose);
            //Create thread to run StartUploadWorker
            Thread thread = new Thread(runMonitor);
            //Start the thread
            thread.Start();
            //Sleep for 5 seconds to form a baseline
            Thread.Sleep(5 * 1000);
        }

        /// <summary>
        /// Monitor function, runs in its own thread doing the monitoring.
        /// </summary>
        private void runMonitor()
        {
            //So... we will establish a baseline over 5 seconds. Then enter that as our baseline.
            // an increase from the baseline of 50% or more will be seen as needing us to back off
            // a failure of a ping will be a full retreat (killing 25% of runnign threads)
            
            //Spend 5 seconds looping to get a baseline (done separately to avoid a bunch of extra checks below)
            for (int i = 0; i < (5000 / pingInterval); i++)
            {
                //Add to the list
                PingReply pingReply = DoPing();
                //Add to our ping list
                Last5Seconds.Add((int)pingReply.RoundtripTime);

                //Sleep if ping time has been less than the interval time
                if (pingReply.RoundtripTime < pingInterval)
                {
                    //Sleep however long is required to have 250ms between pings
                    Thread.Sleep(pingInterval - (int)pingReply.RoundtripTime);
                }

            }

            //Calculate the lowbaseline
            int LowBaseLine = (int)Last5Seconds.Average() * 5;

            //Output
            StaticHelpers.DebugLogger($"Test ping average is: {Last5Seconds.Average()}. Baseline set at: {LowBaseLine}");

            

            while (StopMonitoring == false)
            {

                //Run the ping
                PingReply pingReply = DoPing();
                //Remove the oldest value and add the new one
                Last5Seconds.RemoveAt(0);
                Last5Seconds.Add((int)pingReply.RoundtripTime);

                //Output the returned ping amount (disabled even for full debug as it generates an insane amount of noise
                //StaticHelpers.DebugLogger($"Ping roundtrip: {pingReply.RoundtripTime}", DebugLevel.FullDebug);

                //Check if we need to make a change to our reduceUssage flag
                if (Last5Seconds.Average() > LowBaseLine)
                {
                    //Set flag to pull back
                    reduceUsage = true;
                    //Set lastHighAverage to now
                    LastHighAverage = DateTime.Now;
                    //Output thats what we want to log
                    StaticHelpers.DebugLogger($"Current Average Ping: {Last5Seconds.Average()}. Baseline is {LowBaseLine}. Request reduction in threads...", DebugLevel.Verbose);
                }
                //If we have a result of zero
                if (pingReply.RoundtripTime == 0)
                {
                    //Get the last2Results into a list
                    var Last3Results = Last5Seconds.TakeLast<int>(3);
                    //If the last 2 have been 0 meaning we've had it 3 in a row
                    if (Last3Results.Average() == 0)
                    {
                        //Set urgentReduceUsage to true
                        urgentReduceUsage = true;
                        //Output thats what we want
                        StaticHelpers.DebugLogger($"Average of last 3 pings: {Last3Results.Average()}. Request urgent reduction in threads...", DebugLevel.Verbose);
                    }
                } else
                {
                    //Make sure we aren't requesting an urgent reduction
                    urgentReduceUsage = false;
                }

                //Check if the last failed ping was within the last 30 second and prevent an increase in that time
                int LastFailedPingInSeconds = (int)(DateTime.Now - LastFailedPing).TotalSeconds;
                int LastHighAverageInSeconds = (int)(DateTime.Now - LastHighAverage).TotalSeconds;
                if (LastFailedPingInSeconds < 30 || LastHighAverageInSeconds < 30)
                {
                    CanIncrease = false;
                } else
                {
                    CanIncrease = true;
                }



                //Sleep if ping time has been less than the interval time
                if (pingReply.RoundtripTime < pingInterval)
                {
                    //Sleep however long is required to have 250ms between pings
                    Thread.Sleep(pingInterval - (int)pingReply.RoundtripTime);
                }

                
            }
            StaticHelpers.DebugLogger($"Bandwidth Monitor has shutdown.", DebugLevel.Verbose);

        }

        /// <summary>
        /// Helper to send pings.
        /// </summary>
        /// <returns></returns>
        private PingReply DoPing()
        {
            //Start a ping
            Ping ping = new Ping();
            //Send it
            PingReply pingReply = ping.Send("1.1.1.1", 250);
            //Return the value
            return pingReply;
        }
    }
}
