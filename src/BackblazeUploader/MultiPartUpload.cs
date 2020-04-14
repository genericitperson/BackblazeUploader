using System;
using System.Net;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using HeyRed.Mime;
using System.Linq;

namespace BackblazeUploader
{
    /// <summary>
    /// Does all the work for multi part uploads
    /// </summary>
    public class MultiPartUpload
    {
        #region Properties and variables
        /// <summary>
        /// Pivate string containing the path to the file being uploaded.
        /// </summary>
        private string mpathToFile;
        /// <summary>
        /// The path to the file being uploaded.
        /// </summary>
        public string pathToFile { get {
                return mpathToFile;
            }
            set
            {
                mpathToFile = value;
                fileMime = MimeTypesMap.GetMimeType(value);
            }
        }
        /// <summary>
        /// mime type fype for the file
        /// </summary>
        string fileMime;
        /// <summary>
        /// <see cref="FileInfo"/> object for the file being uploaded.
        /// </summary>
        FileInfo fileInfo;
        /// <summary>
        /// Instance of <see cref="UploadDetails"/> holding the info about upload progress. Should only be updated once a lock on <see cref="uploadDetailsLock"/> has been obtained.
        /// </summary>
        UploadDetails uploadDetails;
        /// <summary>
        /// Size of local file in bytes
        /// </summary>
        long localFileSize;
        /// <summary>
        /// Lock object for <see cref="uploadDetails"/>
        /// </summary>
        object uploadDetailsLock = new object();
        /// <summary>
        /// Name of the file being uploaded
        /// </summary>
        String fileName;
        /// <summary>
        /// Contains the fileDetails about the file being uploaded.
        /// </summary>
        public FileDetails fileDetails;
        /// <summary>
        /// Flag to let the thread manager know there are no remaining parts.
        /// </summary>
        private bool noMoreThreads = false;
        /// <summary>
        /// Max number of threads specified by the cli arguments, default is 20.
        /// </summary>
        int maxThreads;
        /// <summary>
        /// Used to monitor bandwidth performance
        /// </summary>
        BandwidthMonitor bandwidthMonitor = new BandwidthMonitor();
        /// <summary>
        /// List of all the threads we have created, access required by multiple parts of the page.
        /// </summary>
        List<Thread> AllThreads = new List<Thread>();
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor sets various items based on the passed in data.
        /// </summary>
        /// <param name="pathToFile">Path of the file to be uploaded</param>
        public MultiPartUpload(string pathToFile)
        {
            //Set filePath from the incoming because it generates the mime type
            this.pathToFile = pathToFile;
            //Create fileInfo object
            fileInfo = new FileInfo(pathToFile);
            //Set local file size
            localFileSize = fileInfo.Length;
            //Get maximum number of threads from parsed options
            maxThreads = Singletons.options.Threads;
            //Sets filename from fileInfo
            fileName = fileInfo.Name;
        }
        #endregion
        #region Methods
        #region Controllers, Starters & Finishers
        /// <summary>
        /// Manages every aspect of the upload from start to finish.
        /// </summary>
        public void UploadFile()
        {
            //Create a timestamp for logging time spent
            DateTime Start = DateTime.Now;

            //Create a new upload details to hold the details of the upload
            uploadDetails = new UploadDetails();
            //Start large file upload
            StartLargeFile();
            //Upload the parts
            RunUploadWorkers();
            //Finish Large File upload
            FinishLargeFile();

            #region Output final status message.
            //Get end datetime
            DateTime End = DateTime.Now;
            //Get the difference between the two as a string
            string diffInSeconds = Math.Round((End - Start).TotalSeconds, 1).ToString();
            //Get Mbps
            //First get MB
            var MBs = fileInfo.Length * 0.00000095367432;
            //Then mb
            var Mbs = MBs * 8;
            //Then calculate Mbps
            var Mbps = Mbs / (End - Start).TotalSeconds;
            StaticHelpers.DebugLogger($"Operation Finished. Operation Took: {diffInSeconds} seconds and transferred {Math.Round(MBs, 2)}MBs at a speed of {Math.Round(Mbps, 2)}Mbps", DebugLevel.Info);
            #endregion
        }

        /// <summary>
        /// Calls b2_start_large_file and gets the required idents to begin upload.
        /// </summary>
        public async void StartLargeFile()
        {
            // Setup JSON to post.
            String startLargeFileJsonStr = "{\"bucketId\":\"" + Singletons.authenticationDetails.bucketId + "\",\"fileName\":\"" + fileName + "\",\"contentType\":\"" + fileMime + "\"}";
            byte[] jsonData = Encoding.UTF8.GetBytes(startLargeFileJsonStr);

            // Send over the wire
            HttpWebRequest startLargeFileRequest = (HttpWebRequest)WebRequest.Create(Singletons.authenticationDetails.apiUrl + "/b2api/v2/b2_start_large_file");
            startLargeFileRequest.Method = "POST";
            startLargeFileRequest.Headers.Add("Authorization", Singletons.authenticationDetails.authorizationToken);
            startLargeFileRequest.ContentType = "application/json; charset=utf-8";
            startLargeFileRequest.ContentLength = jsonData.Length;
            using (Stream stream = startLargeFileRequest.GetRequestStream())
            {
                stream.Write(jsonData, 0, jsonData.Length);
                stream.Close();
            }

            // Handle the response and print the json
            try
            {
                HttpWebResponse startLargeFileResponse = (HttpWebResponse)startLargeFileRequest.GetResponse();
                //Trying swapping this so we get a stream for deserialization
                //using (StringReader responseReader = new StringReader(new StreamReader(startLargeFileResponse.GetResponseStream()).ReadToEnd()))
                using (Stream responseStream = startLargeFileResponse.GetResponseStream())
                {
                    fileDetails = await JsonSerializer.DeserializeAsync<FileDetails>(responseStream);
                }
                startLargeFileResponse.Close();
            }
            catch (WebException e)
            {
                using (HttpWebResponse errorResponse = (HttpWebResponse)e.Response)
                {
                    Console.WriteLine("Error code: {0}", errorResponse.StatusCode);
                    using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        String text = reader.ReadToEnd();
                        Console.WriteLine(text);
                    }
                }
            }
        }

        /// <summary>
        /// Finishes the file upload by calling b2_finish_large_file to combine all parts.
        /// </summary>
        public void FinishLargeFile()
        {
            // Create a request object and copy it to the memory stream.
            B2FinishLargeFileRequest finishLargeFileData = new B2FinishLargeFileRequest
            {
                fileId = fileDetails.fileId,
                partSha1Array = uploadDetails.partSha1Array
            };
            //So instead of using the old json serialized things going to use the new one...
            string FinishLargeFileInfoJsonString = JsonSerializer.Serialize<B2FinishLargeFileRequest>(finishLargeFileData);
            //Convert the string to a memory stream
            byte[] byteArray = Encoding.UTF8.GetBytes(FinishLargeFileInfoJsonString);
            MemoryStream finishLargeFileMemStream = new MemoryStream(byteArray);

            HttpWebRequest finishLargeFileRequest = (HttpWebRequest)WebRequest.Create(Singletons.authenticationDetails.apiUrl + "/b2api/v2/b2_finish_large_file");
            finishLargeFileRequest.Method = "POST";
            finishLargeFileRequest.Headers.Add("Authorization", Singletons.authenticationDetails.authorizationToken);
            finishLargeFileRequest.ContentType = "application/json; charset=utf-8";
            finishLargeFileRequest.ContentLength = finishLargeFileMemStream.Length;
            finishLargeFileMemStream.WriteTo(finishLargeFileRequest.GetRequestStream());
            HttpWebResponse finishLargeFileResponse;
            try
            {
                finishLargeFileResponse = (HttpWebResponse)finishLargeFileRequest.GetResponse();
            }
            catch (WebException e)
            {
                using (WebResponse r = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)r;
                    Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                    using (Stream dataE = r.GetResponseStream())
                    using (var reader = new StreamReader(dataE))
                    {
                        string text = reader.ReadToEnd();
                        Console.WriteLine(text);
                    }
                }
            }

        }

        #endregion

        #region Thread Management
        /// <summary>
        /// Runs & Manages the upload workers.
        /// </summary>
        public void RunUploadWorkers()
        {
 
 
            //Start the bandwidth monitor
            bandwidthMonitor.startMonitoring();
            //Set WorkFinished to false
            bool WorkFinished = false;
            //Start a continious loop until we have finished the work
            while (WorkFinished == false)
            {
                //If we are under max threads and bandwidth monitor doesn't forbid it and we haven't already started all parts
                if (maxThreads > AllThreads.Count() && bandwidthMonitor.CanIncrease == true && noMoreThreads == false)
                {
                    //Create thread to run StartUploadWorker
                    Thread thread = new Thread(StartUploadWorker);
                    //Start the thread
                    thread.Start();
                    //Put the thread in our list of threads
                    AllThreads.Add(thread);
                    
                }
                //Recalculate number of active threads by removing inactive
                AllThreads.RemoveAll(thread => thread.ThreadState == System.Threading.ThreadState.Stopped);
                //Output a debug message
                StaticHelpers.DebugLogger($"Current number of threads = {AllThreads.Count}", DebugLevel.FullDebug);

                //If whole file has been uploaded and all threads are stopped
                if (uploadDetails.totalBytesSent >= localFileSize && AllThreads.Count() == 0)
                {
                    //We have reached the end of parts to upload so tell the system not to upload anymore
                    WorkFinished = true;
                }
                //Add a wait so we aren't too aggressive in adding threads
                Thread.Sleep(3000);

            }


            bandwidthMonitor.StopMonitoring = true;
            StaticHelpers.DebugLogger("Upload has finished.", DebugLevel.Info);


        }
        #endregion
        #region Worker
        /// <summary>
        /// Starts and is the upload worker that does the actual uploads.
        /// </summary>
        public async void StartUploadWorker()
        {
            StaticHelpers.DebugLogger("Starting an internal upload worker", DebugLevel.Verbose);
            //Get our object containing our authorisation URLs
            UploadPartsUrlDetails uploadPartsUrlDetails = await GetUploadPartUrl();


            //While there are bytes still be sent
            while (uploadDetails.totalBytesSent < localFileSize)
            {
                //If the bandwidth monitor requires a reduction in usage
                if (bandwidthMonitor.reduceUsage || bandwidthMonitor.urgentReduceUsage)
                {
                    //Check thread count is greater than 1
                    if (AllThreads.Count(thread => thread.ThreadState != System.Threading.ThreadState.Stopped) > 1)
                    {
                        //Log to debug
                        StaticHelpers.DebugLogger("Received Kill Request from Bandwidth Monitor. Killing self....", DebugLevel.Verbose);
                        //Set reduceUsage to false
                        bandwidthMonitor.reduceUsage = false;
                        //Kill this thread
                        break;
                    } else
                    {
                        StaticHelpers.DebugLogger("Received Kill Request from Bandwidth Monitor HOWEVER as the only remaining thread I am ignoring.", DebugLevel.Verbose);
                    }
                }
                //Create variables outside of the lock so we can set it inside but still access it outside
                //For a snapshot of uploadDetails
                UploadDetails uploadDetailsSnapshot = new UploadDetails();
                //Create the byte array for the data we are going to use
                byte[] data = new byte[Singletons.options.PartSize * (1000 * 1000)];
                
                //Lock the uploadDetails
                lock (uploadDetailsLock)
                {
                    #region Check if we are on the last part or even if there are no parts left
                    //If there is nothing left to upload
                    if ((localFileSize - uploadDetails.totalBytesSent) == 0)
                    {
                        //Break out of the loop as there is no more work to do
                        break;
                    }
                    //If the remaining bytes are less the minimum part size
                    if ((localFileSize - uploadDetails.totalBytesSent) <= uploadDetails.minimumPartSize)
                    {
                        //Changes the bytes sent for part to the remaining number of bytes
                        uploadDetails.bytesSentForPart = (localFileSize - uploadDetails.totalBytesSent);

                    }

                    // Generate SHA1 Chunk
                    // Open stream of the file
                    FileStream f = File.OpenRead(pathToFile);
                    //Seek to the location in the file we are currently up to
                    f.Seek(uploadDetails.totalBytesSent, SeekOrigin.Begin);
                    //Read the data from the file that we are going to use this time
                    f.Read(data, 0, (int)uploadDetails.bytesSentForPart);
                    //Create a blank SHA1 hash
                    SHA1 sha1 = SHA1.Create();
                    //Hash the bytes in our current data and keep the hash in hashData
                    byte[] hashData = sha1.ComputeHash(data, 0, (int)uploadDetails.bytesSentForPart);
                    //Dispose of the hash
                    sha1.Dispose();
                    //Create a string builder to manipulate the hash
                    StringBuilder sb = new StringBuilder();
                    //Add data to every byte in the range
                    foreach (byte b in hashData)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    //Close the file read because we now have the data
                    f.Close();
                    //Add the hash to the hash array
                    uploadDetails.partSha1Array.Add(sb.ToString());

                    //Get all the values we might need to use internally. OR just make a snapshot of Upload Details? (Yes this should work!)
                    uploadDetailsSnapshot = uploadDetails.CloneMe();

                    //Update the actual uploadDetails with what we intend to do.
                    //Increment the partNo
                    uploadDetails.partNo++;
                    //Increment the totalBytesSent
                    uploadDetails.totalBytesSent = uploadDetails.totalBytesSent + uploadDetails.bytesSentForPart;

                }
                //To count number of failed attempts
                int WebRequestAttempt = 1;
                //To allow retry of the failed request
                RetryPartUpload:
                //Output urls for debugging
                StaticHelpers.DebugLogger("UploadPartsURL is: " + uploadPartsUrlDetails.uploadUrl + ". Key is: " + uploadPartsUrlDetails.authorizationToken, DebugLevel.FullDebug);
                //Start a new web request
                HttpWebRequest uploadPartRequest = (HttpWebRequest)WebRequest.Create(uploadPartsUrlDetails.uploadUrl);
                //Set to post
                uploadPartRequest.Method = "POST";
                //Set the request timeout to 5 minutes
                uploadPartRequest.Timeout = 5*60*1000;
                //Set authorization token (using the one for the current uploadPartUrl)
                //Intentionally generating error:
                //uploadPartRequest.Headers.Add("Authorization", uploadPartsUrlDetails.authorizationToken + "r");
                uploadPartRequest.Headers.Add("Authorization", uploadPartsUrlDetails.authorizationToken);
                //Set the part number
                uploadPartRequest.Headers.Add("X-Bz-Part-Number", uploadDetailsSnapshot.partNo.ToString());
                //Set the sha1 hash from the array (minus one on the part number because 0-index array
                uploadPartRequest.Headers.Add("X-Bz-Content-Sha1", (String)uploadDetailsSnapshot.partSha1Array[(uploadDetailsSnapshot.partNo - 1)]);
                //Set content type to json
                uploadPartRequest.ContentType = "application/json; charset=utf-8";
                //Set the content length to the bytes sent for the part
                uploadPartRequest.ContentLength = uploadDetailsSnapshot.bytesSentForPart;
                //Create a stream to use for the uploadPartRequest (this may be the one to change to a filestream)
                using (Stream stream = uploadPartRequest.GetRequestStream())
                {
                    //Write the data (through the stream?) to the uploadPartRequest 
                    stream.Write(data, 0, (int)uploadDetailsSnapshot.bytesSentForPart);
                    //Close the stream
                    stream.Close();
                }
                //Set upload response to null
                HttpWebResponse uploadPartResponse = null;
                //Verbose message
                StaticHelpers.DebugLogger("Starting upload of part " + uploadDetailsSnapshot.partNo, DebugLevel.Verbose);
                //Try the upload
                try
                {
                    //Try the upload and set the upload part response to the response
                    uploadPartResponse = (HttpWebResponse)uploadPartRequest.GetResponse();
                }
                //If theres an exception catch and output it
                catch (WebException e)
                {
                    if (e.Response == null)
                    {
                        StaticHelpers.DebugLogger("Upload has failed with error: " + e.Message, DebugLevel.Warn);
                    }
                    else
                    {
                        using (WebResponse r = e.Response)
                        {
                            HttpWebResponse httpResponse = (HttpWebResponse)r;
                            Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                            using (Stream dataE = r.GetResponseStream())
                            using (var reader = new StreamReader(dataE))
                            {
                                string text = reader.ReadToEnd();
                                Console.WriteLine(text);
                            }
                        }
                    }
                    //If we have failed less than 5 times
                    if (WebRequestAttempt < 5)
                    {
                        //Log a message
                        StaticHelpers.DebugLogger("Upload has failed, getting fresh uploadparturl and retrying....", DebugLevel.Verbose);
                        //Get our object containing our authorisation URLs
                        uploadPartsUrlDetails = await GetUploadPartUrl();
                        //Output fresh url for debugging
                        StaticHelpers.DebugLogger("Fresh UploadPartsURL is: " + uploadPartsUrlDetails.uploadUrl + ". Key is: " + uploadPartsUrlDetails.authorizationToken, DebugLevel.FullDebug);

                        //Wait a while
                        int secToWait = WebRequestAttempt * 2;
                        Thread.Sleep(secToWait * 1000);
                        //Increment counter
                        WebRequestAttempt++;
                        //Retry
                        goto RetryPartUpload;
                    }
                }
               
                //Close the upload part response
                uploadPartResponse.Close();
                //Log to the debugger what part we've just done
                StaticHelpers.DebugLogger("Uploaded Part " + uploadDetailsSnapshot.partNo, DebugLevel.Verbose);
                
            }
            //Check whether we have finished or if just this thread being killed:
            if (uploadDetails.totalBytesSent >= localFileSize)
            {
                //We have reached the end of parts to upload so tell the system not to upload anymore
                noMoreThreads = true;
            }
            

            StaticHelpers.DebugLogger("Internal upload worker is dead.", DebugLevel.Verbose);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Gets the uploadPartUrl for each worker function.
        /// </summary>
        /// <returns></returns>
        public async Task<UploadPartsUrlDetails> GetUploadPartUrl()
        {
            //Taken from https://www.backblaze.com/b2/docs/b2_get_upload_part_url.html with edits

            //Create an UploadPartsUrlDetails data object to hold the data
            UploadPartsUrlDetails uploadPartsUrlDetails = new UploadPartsUrlDetails();


            // Get Upload URL
            String getUploadUrlJsonStr = "{\"fileId\":\"" + fileDetails.fileId + "\"}";
            byte[] getUloadUrlJsonData = Encoding.UTF8.GetBytes(getUploadUrlJsonStr);
            //To allow us to count the number of failed attempts
            int WebRequestAttempt = 1;
        //To allow us to retry the webrequest if it fails (there is probably a better option here but I don't know it
        RetryRequest:

            HttpWebRequest getUploadUrlRequest = (HttpWebRequest)WebRequest.Create(Singletons.authenticationDetails.apiUrl + "/b2api/v2/b2_get_upload_part_url");
            //Intentionally generating an error for testing purposes
            //HttpWebRequest getUploadUrlRequest = (HttpWebRequest)WebRequest.Create(Singletons.authenticationDetails.apiUrl + "/b2api/v2/b2_get_upload_part_ur");
            getUploadUrlRequest.Method = "POST";
            getUploadUrlRequest.Headers.Add("Authorization", Singletons.authenticationDetails.authorizationToken);
            getUploadUrlRequest.ContentType = "application/json; charset=utf-8";
            getUploadUrlRequest.ContentLength = getUloadUrlJsonData.Length;
            using (Stream stream = getUploadUrlRequest.GetRequestStream())
            {
                stream.Write(getUloadUrlJsonData, 0, getUloadUrlJsonData.Length);
                stream.Close();
            }


            // Handle the response and print the json
            try
            {
                HttpWebResponse getUploadUrlResponse = (HttpWebResponse)getUploadUrlRequest.GetResponse();
                //I have made heavy changes here so errors are almost certainly due to that!
                using (Stream responseStream = getUploadUrlResponse.GetResponseStream())
                {

                    UploadPartUrlResponse uploadPartUrlResponse = await JsonSerializer.DeserializeAsync<UploadPartUrlResponse>(responseStream);

                    uploadPartsUrlDetails.authorizationToken = uploadPartUrlResponse.authorizationToken;
                    uploadPartsUrlDetails.uploadUrl = uploadPartUrlResponse.uploadUrl;
                }
                getUploadUrlResponse.Close();
            }
            catch (WebException e)
            {
                //Print error to console before retrying
                using (HttpWebResponse errorResponse = (HttpWebResponse)e.Response)
                {
                    Console.WriteLine("Error code: {0}", errorResponse.StatusCode);
                    using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        String text = reader.ReadToEnd();
                        Console.WriteLine(text);
                    }
                }
                //If we have failed less than 5 times
                if (WebRequestAttempt < 5)
                {
                    //Log a message
                    StaticHelpers.DebugLogger("We have failed to get a part upload URL, retrying....", DebugLevel.Verbose);
                    //Wait a while
                    int secToWait = WebRequestAttempt * 2;
                    Thread.Sleep(secToWait * 1000);
                    //Increment counter
                    WebRequestAttempt++;
                    //Go back to retry the request
                    goto RetryRequest;
                }
            }

            return uploadPartsUrlDetails;
        }
        #endregion
        #endregion
    }

}
