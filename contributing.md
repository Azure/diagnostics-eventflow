* Prerequisites
    * Visual Studio 2015 Update 3 or above

* Build

    Run the following commands for to get a clean repo:

        tools\scorch.cmd
        tools\restore.cmd
    
    Run the following command to build a release build:

        tools\buildRelease.cmd

* Test

    Run the following command:

        tools\runtest.cmd

* Before sending out pull request:

    Run the following command and verify there is no error:

        tools\localCheckinGate.cmd

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
