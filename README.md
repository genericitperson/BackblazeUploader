# Backblaze Uploader

A CLI program to multi thread uploads to Backblaze's B2 cloud storage solution.

***Please note: The uploader is designed for large files, because it only supports multi-part uploads it will not upload files smaller than 5MB***

## Getting Started Using Backblaze Uploader

The software is released as a standalone prebuilt program that can simply be downloaded and ran. There are two versions:
- Self-contained/Standalone - this package has no dependencies and can run immediately by itself, the trade off is its larger size.
- Compact Version - this version requires .Net Core already be installed on the system, see prerequisites below for info on how to install these.

### Prerequisites

If you are installing the self contained version there are no prerequisites.

If you would prefer to use the smaller executable the .Net Core (Version 3.1 or greater) runtime must be installed. This can be downloaded direct from [Microsoft] (https://dotnet.microsoft.com/download). The download required is for the most up to date runtime (required to run .Net Core 

## Usage

The software needs to be run from a command prompt (Powershell, Windows Command Prompt or a Linux Shell) and all required arguments must be supplied for it to successfully work.

An example of a simple upload command would be:

```bash
BackblazeUploader.exe --applicationkey K2895Mzq3Gm66cqeg6JSKFIE3YDMgqF9 --applicationkeyid 00378d9e6385be60000000012 TargetBucket "C:\Folder\File To Upload.exe"
```



```
--applicationkeyid       Required. The ApplicationKey ID for the Backblaze API

  --applicationkey       Required. The ApplicationKey for the Backblaze API

  --debuglevel           (Default: 3) Configures the level of messages to be output.
                                    1 - Errors only
                                    2 - Errors and Warnings
                                    3 - (Default) Info, Errors & Warnings
                                    4 - Verbose messages, good for understanding whats happening under the bonnet
                                    5 - All available debug messages, use of this option is only recommended when troubleshooting issues.

  --threads              (Default: 20) Specifies maximum number of threads. Default is 20.

  --partsize             (Default: 20) Specifies size of individual parts to transfer in MBs. Minimum is 6.

  --help                 Display this help screen.

  --version              Display version information.

  BucketName (pos. 0)    The name of the bucket to upload to, must already exist.

  FilePath (pos. 1)      The path of the file to upload
```



### Step by step initial setup/usage

1. Download the release you want to use.
   - Ensure you have .Net Core if you don't choose the self contained package.
2. Move to a location of your choosing
3. Run it from the commandline using the arguments described above.



## Troubleshooting/Issues

If any issues occur a log file is available in the running folder called fullDebug.log which contains all debug error messages.

I will be happy to look into any bugs submitted but as this is not a commercial venture cannot make guarantees as to when this may be possible.

## Feature Requests

I would be happy to consider requests submitted.

## Contributing

I do not anticipate others contributing to this project however if you wish to do so it would be welcomed. If I receive contact from those wishing to do I will generate a contributions policy. If you wish to contribute please get in contact.

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## Authors

* *Sam Foley* - Initial work - [GenericITPerson](https://github.com/GenericITPerson)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* *Backblaze* - For the code samples available on their API page that assisted me in creating this program. - [Backblaze API Guide](https://www.backblaze.com/b2/docs/calling.html)
* * *Billie Thompson* - For the very helpful template used to generate this page - [PurpleBooth](https://github.com/PurpleBooth) 