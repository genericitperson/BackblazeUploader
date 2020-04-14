using System;
using System.Net;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Threading;

namespace BackblazeUploader
{
    /// <summary>
    /// Manages initial authentication and initiates all Api Actions
    /// </summary>
    public class BackblazeApi
    {
        /// <summary>
        /// <see cref="AuthenticationDetails"/> for this session.
        /// </summary>
        public AuthenticationDetails authenticationDetails;


        /// <summary>
        /// Authorizes with Backblaze using the API credentials supplised by the user. Results go into <see cref="Singletons.authenticationDetails"/>
        /// </summary>
        public async void AuthorizeWithB2()
        {
            //Craete a webrequest
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create("https://api.backblazeb2.com/b2api/v2/b2_authorize_account");
            //Encode given credentials
            String credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(Singletons.options.applicationKeyId + ":" + Singletons.options.applicationKey));
            //Add authentication details to header
            webRequest.Headers.Add("Authorization", "Basic " + credentials);
            //Set request ContentType
            webRequest.ContentType = "application/json; charset=utf-8";
            
            // Handle the response and push the json to authenticationDetails
            try
            {
                HttpWebResponse authenticationResponse = (HttpWebResponse)webRequest.GetResponse();
                //I have made heavy changes here so errors are almost certainly due to that!
                using (Stream responseStream = authenticationResponse.GetResponseStream())
                {
                    //String json = new StreamReader(authenticationResponse.GetResponseStream()).ReadToEnd();
                    //Deserialize and return us the authenticationDetails object
                    AuthenticationDetails newAuthenticationDetails = await JsonSerializer.DeserializeAsync<AuthenticationDetails>(responseStream);

                    //Set to the singleton instance of authenticationDetails
                    /*this.authenticationDetails.apiUrl = authenticationDetails.apiUrl;
                    this.authenticationDetails.authorizationToken = authenticationDetails.authorizationToken;*/
                    //Set the singleton insstance to the one we just got
                    Singletons.authenticationDetails = newAuthenticationDetails;
                    //Set our instance to the singleton instance
                    authenticationDetails = Singletons.authenticationDetails;
                }
                authenticationResponse.Close();
                StaticHelpers.DebugLogger("Successfully authenticated with Backblaze.", DebugLevel.Verbose);
            }
            catch (WebException e)
            {
                using (HttpWebResponse errorResponse = (HttpWebResponse)e.Response)
                {
                    StaticHelpers.DebugLogger($"Internal Worker Error with API.Error code: {errorResponse.StatusCode}. Retrying....", DebugLevel.Warn );
                    using (StreamReader reader = new StreamReader(errorResponse.GetResponseStream()))
                    {
                        String text = reader.ReadToEnd();
                        StaticHelpers.DebugLogger($"Internal Worker Error with API.Error code: {text}. Retrying....", DebugLevel.Warn );
                    }
                }
            }

        }

        /// <summary>
        /// Fetches the bucketId from Backblaze based on the name provided by the user.
        /// </summary>
        /// <param name="BucketName">The name of the bucket to get the id for</param>
        public async void GetBucketId(string BucketName)
        {

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(authenticationDetails.apiUrl + "/b2api/v2/b2_list_buckets");
            string body = "{\"accountId\":\"" + authenticationDetails.accountId + "\", \"bucketName\":\"" + BucketName + "\"}";
            var data = Encoding.UTF8.GetBytes(body);
            webRequest.Method = "POST";
            webRequest.Headers.Add("Authorization", authenticationDetails.authorizationToken);
            webRequest.ContentType = "application/json; charset=utf-8";
            webRequest.ContentLength = data.Length;
            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            //Get the response and then a stream of that response
            WebResponse response = (HttpWebResponse)webRequest.GetResponse();
            Stream responseStream = response.GetResponseStream();
            //Deserialize onto ListBuckets object as BucketsList
            ListBuckets BucketsList = await JsonSerializer.DeserializeAsync<ListBuckets>(responseStream);
            //Check only 1 has been returned
            if (BucketsList.buckets.Count() != 1)
            {
                //Generate a fatal error and die
                StaticHelpers.DebugLogger("Bucket not found. Exiting.", DebugLevel.Error);
            }
            authenticationDetails.bucketId = BucketsList.buckets.First().bucketId;
            //var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            //response.Close();
            //Console.WriteLine(responseString);
        }

        /// <summary>
        /// Confirms a file exists after upload to confirm success.
        /// </summary>
        /// <param name="fileId">Id of the file to confirm exists.</param>
        /// <returns></returns>
        public bool ConfirmFileExists(string fileId)
        {
            //Set variable and goto allowing repeat of process
            int WebRequestAttempt = 1;
            RetryFileCheck:
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(authenticationDetails.apiUrl + "/b2api/v2/b2_get_file_info");
            string body = "{\"fileId\":\"" + fileId + "\"}";
            var data = Encoding.UTF8.GetBytes(body);
            webRequest.Method = "POST";
            webRequest.Headers.Add("Authorization", authenticationDetails.authorizationToken);
            webRequest.ContentType = "application/json; charset=utf-8";
            webRequest.ContentLength = data.Length;
            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            try
            {
                WebResponse response = (HttpWebResponse)webRequest.GetResponse();

                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                response.Close();
                //Console.WriteLine(responseString);
                //If we haven't had an exception by this point we have a code 200 response meaning file does exist so we can return true
                return true;
                //If theres an exception catch and output it
            } catch (WebException e)
            {
                if (e.Response == null)
                {
                    StaticHelpers.DebugLogger("Upload has failed with error: " + e.Message, DebugLevel.Error);
                }
                else
                {
                    using (WebResponse r = e.Response)
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)r;
                    StaticHelpers.DebugLogger($"Internal Worker Error with API.Error code: {httpResponse.StatusCode}. Retrying....", DebugLevel.Warn );
                        using (Stream dataE = r.GetResponseStream())
                        using (var reader = new StreamReader(dataE))
                        {
                            string text = reader.ReadToEnd();
                            StaticHelpers.DebugLogger($"Internal Worker Error with API.Error code: {text}. Retrying....", DebugLevel.Warn );
                        }
                    }
                }
                //If we have failed less than 5 times
                if (WebRequestAttempt < 5)
                {
                    //Log a message
                    StaticHelpers.DebugLogger("Cannot get upload success confirmation, retrying....", DebugLevel.Verbose);

                    //Wait a while
                    int secToWait = WebRequestAttempt * 2;
                    Thread.Sleep(secToWait * 1000);
                    //Increment counter
                    WebRequestAttempt++;
                    //Retry
                    goto RetryFileCheck;
                }
                //If we have got to this point everything has failed for this request and we need to die
                StaticHelpers.DebugLogger("Sorry, we can't confirm the file has been uploaded successfully, please check manually. fileId: " + fileId, DebugLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Starts a multipart upload.
        /// </summary>
        /// <param name="pathToFile">Full path to the file to upload.</param>
        public void MultiPartUpload(string pathToFile)
        {
            //Print info message
            StaticHelpers.DebugLogger($"Starting a multipart upload on: " + pathToFile, DebugLevel.Info);
            //Get a new PartUploader
            MultiPartUpload multiPartUpload = new MultiPartUpload(pathToFile);
            //Begin file upload
            multiPartUpload.UploadFile();
            //Check file upload has been successful:
            if (ConfirmFileExists(multiPartUpload.fileDetails.fileId))
            {
                StaticHelpers.DebugLogger("File upload has been confirmed.");
            }
        }
        
    }
}
