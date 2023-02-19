* Prerequisites for full build:
    * Visual Studio 2022
	* .NET Framework SDKs/Targeting Packs for .NET 6, .NET Framework 4.6.2 and .NET Framework 4.7.1.

* Build

    Run the following commands for to get a clean repo:

        tools\scorch.cmd
        tools\restore.cmd
    
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
