* Prerequisites
    * Visual Studio 2017
	* .NET Framework 4.7.1 SDK and Targeting Pack
	* .NET Core 1.0 SDK
	* .NET Core 2.0 SDK

* Build

    Run the following commands for to get a clean repo:

        tools\scorch.cmd
        tools\restore.cmd
    
    *You must run `tools\restore.cmd` at least once before attempting to build or run tests*, otherwise you will encouter errors such as "missing FileParser.peg.cs file".
    
    Run the following command to build a release build:

        tools\buildRelease.cmd

    You can also build from Visual Studio (after running `tools\restore.cmd`) by opening Warsaw.sln solution file in the root of the repo.
    
* Test

    Run the following command:

        tools\runtest.cmd

* Before sending out pull request:

    Run the following command and verify there is no error:

        tools\localCheckinGate.cmd

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
