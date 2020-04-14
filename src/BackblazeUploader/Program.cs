using CommandLine;
using System;
using System.IO;
using System.Linq;

namespace BackblazeUploader
{
    class Program
    {

        /*
         *  Test Notes
         *  With Single Thread and 20MB file: Test Took: 50.3603453 seconds
         *  With Single Thread and 20MB file: Test Took: 44.648904099999996 seconds
         *  With 1 thread with 100MB file: Test Took: 220.0786028 seconds
         *  With 3 threads with 100MB file: Test Took: 170.9150036 seconds
         *  With 10 threads with 100MB file: Test Took: 95.6517026 seconds
         *  With 13 threads (max for chunk size) with 100MB file: Test Took: 93.2367336 seconds
         * 
         * 
         * TODO: Confirm file is there before exiting
         * 
         */
        static void Main(string[] args)
        {
            //Get start datetime to calculate time taken
            DateTime Start = DateTime.Now;

            //Parse commandline options This is using this parser (may or may not be best but works for now): https://github.com/commandlineparser/commandline
            var ParserResult = Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(OptionsToOptions) ;
            //Exit if parsed options not sufficient
            if (ParserResult.Tag.ToString() == "NotParsed")
            {
                //Check whether help has been requested...
                foreach (string arg in args) {
                    //Check if it contains "help"
                    if (arg.Contains("help")) {
                        //If it has exit with a success code
                        Environment.Exit(0);
                    }
                }
                //If helps hasn't been requested exit with an error that paramaters are missing.
                StaticHelpers.DebugLogger("Incorrect or missing commandline paramaters, please check and try again.", DebugLevel.Error);
            }

            BackblazeApi Backblaze = new BackblazeApi();
            //Login to B2
            Backblaze.AuthorizeWithB2();
            //Get BucketId
            Backblaze.GetBucketId(Singletons.options.bucketName);
            //Start a multipart upload
            Backblaze.MultiPartUpload(Singletons.options.filePath);
            

            //Get end datetime
            DateTime End = DateTime.Now;
            //Get the difference between the two as a string
            string diffInSeconds = (End - Start).TotalSeconds.ToString();
            StaticHelpers.DebugLogger("Total program runtime: " + diffInSeconds + " seconds", DebugLevel.FullDebug);
            //Console.ReadLine();
            //Kill the program with a success exit code (needed because of threads left running):
            Environment.Exit(0);
        }

        static void OptionsToOptions(Options opts)
        {

            //Validate any options that need validating:
            if (File.Exists(opts.filePath) == false)
            {
                //Throw an error cause the file doesn't exist, for now just write to console.
                StaticHelpers.DebugLogger("The file specified does not exist! File specified was: " + Singletons.options.filePath, DebugLevel.Error);
            }
            
            //Set options to our singleton
            Singletons.options = opts;
            
        }

        
    }
}
