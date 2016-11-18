# Microsoft.Diagnostic.EventFlow

## Introduction
The EventFlow library suite allows applications to define what diagnostics data to collect, and where they should be outputted to. Diagnostics data can be anything from performance counters to application traces.
It runs in the same process as the application, so communication overhead is minimized. It also has an extensibility mechanism so additional inputs and outputs can be created and plugged into the framework. It comes with the following inputs and outputs:

**Inputs**
- Trace (aka System.Diagnostics.Trace)
- EventSource 
 
**Outputs**
- StdOutput (console output)
- Application Insights
- Azure EventHub
- Elastic Search

The EventFlow suite supports .NET applications and .NET Core applications. It allows diagnostic data to be collected and transferred for applications running in these Azure environments:

- Azure Web Apps
- Service Fabric
- Azure Cloud Service
- Azure Virtual Machines


## Getting Started
1. To quickly get started, you can create a simple console application in VisualStudio and install the nuget package Microsoft.Diagnostics.EventFlow. The package is still in preview, if you're installing the package from Manage NuGet Packages window, please check **"Include prerelease"**.
2. After the nuget package is installed, there should be a eventFlowConfig.json file added to the project. This file contains default configuration for the EventFlow pipeline. A few sections with optional configuration elements are commented outs. These sections can be enabled as desired. Here is what the file looks like
```js
{
    "inputs": [
        // {
        //   "type": "EventSource",
        //   "sources": [
        //     { "providerName": "Microsoft-Windows-ASPNET" }
        //   ]
        // },
        {
            "type": "Trace",
            "traceLevel": "Warning"
        }
    ],
    "filters": [
        {
            "type": "drop",
            "include": "Level == Verbose"
        }
    ],
    "outputs": [
        // Please update the instrumentationKey.
        {
            "type": "ApplicationInsights",
            "instrumentationKey": "00000000-0000-0000-0000-000000000000"
        }
    ],
    "schemaVersion": "2016-08-11",
    // "healthReporter": {
    //   "type": "CsvHealthReporter",
    //   "logFileFolder": ".",
    //   "logFilePrefix": "HealthReport",
    //   "minReportLevel": "Warning",
    //   "throttlingPeriodMsec": "1000"
    // },
    // "settings": {
    //    "pipelineBufferSize": "1000",
    //    "maxEventBatchSize": "100",
    //    "maxBatchDelayMsec": "500",
    //    "maxConcurrency": "8",
    //    "pipelineCompletionTimeoutMsec": "30000"
    // },
    "extensions": []
}
```
3. If you wish to send diagnostics data to Application Insights, fill in the value for the instrumentationKey. If not, simply remove the Application Insights section.
4. To add a StdOutput output, install the Microsoft.Diagnostic.EventFlow.Outputs.StdOutput nuget package. Then add the following in the outputs array in eventFlowConfig.json:
```json
    {
        "type": "StdOutput"
    }
```
5. Create an EventFlow pipeline in your application code using the code below. Make sure there is at least one output defined in the configuration file. Run your application and see your traces in console output or Application Insights.
```csharp
    using (var pipeline = DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
    {
        System.Diagnostics.Trace.TraceWarning("EventFlow is working!");
        Console.ReadLine();
    }
```

## Configuration Details
The EventFlow pipeline is built around three core concepts: inputs, outputs, and filters. The number of inputs, outputs, and filters depend on the need of diagnostics. The configuration 
also has a healthReporter and settings section for configuring settings fundamental to the pipeline operation. At last, the extensions section allows declaration of custom developed
plugins. These extension declarations act like references. On pipeline initialization, EventFlow will search in the extensions first for input, output, or filter implementations.

### Inputs
These define what data will flow into the engine. At least one input is required. Each input type has its own set of parameters.

#### Trace
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Inputs.Trace**

This input listens to traces written with System.Diagnostics.Trace API. Here is an example showing all possible settings:
```json
{
    "type": "Trace",
    "traceLevel":  "Warning"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "Trace" | Yes | Specifies the input type. For this input, it must be "Trace". |
| `traceLevel` | Critical, Error, Warning, Information, Verbose, All | No | Specifies the collection trace level. Traces with equal or higher severity than specified are collected. For example, if Warning is specified, then Critial, Error, and Warning traces are collected. Default is All. |

#### EventSource
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Inputs.EventSource**

This input listens to EventSource traces. EventSource classes can be created in the application by deriving from the [System.Diagnostics.Tracing.EventSource](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx) class. Here is an example showing all possible settings:
```json
{
    "type": "EventSource",
    "sources": [
        {
            "providerName": "MyEventSource",
            "level": "Informational",
            "keywords": "0x7F"
        }
    ]
}
```

*Top Object*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "EventSource" | Yes | Specifies the input type. For this input, it must be "EventSource". |
| `sources` | JSON array | Yes | Specifies the EventSource objects to collect. |

*Sources Object*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `providerName` | provider name | Yes | This field specify what kind of input this is. For this input, it must be "EventSource" |
| `level` | Critial, Error, Warning, Informational, Verbose, LogAlways | No | Specifies the collection trace level. Traces with equal or higher severity than specified are collected. For example, if Warning is specified, then Critial, Error, and Warning traces are collected. Default is All. |
|`keywords` | An integer | No | A bitmask that specifies what events to collect. Only events with keyword matching the bitmask are collected, except if it's 0, which means everything is collected. Default is 0. |

#### PerformanceCounter
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Inputs.PerformanceCounter**

This input enables gathering data from Windows performance counters. Only *process-specific* counters are supported, that is, the counter must have an instance that is associated with the current process. For machine-wide counters use an external agent such as [Azure Diagnostics Agent](https://azure.microsoft.com/en-us/documentation/articles/azure-diagnostics/) or create a custom input.

*Finding the counter instance that corresponds to the current process*

In general there is no canonical way to find a performance counter instance that corresponds to current process. Two methods are commonly used in practice:

- A special performance counter that provides instance name to process ID mapping.
    This solution involves a set of counters that use the same instance name for a given process. Among them there is a special counter with a value that is the process ID of the corresponding process. Searching for the instance of the special counter with a value equal to the current process ID allows to discover what the instance name is used for the current process. Examples of this approach include the Windows Process category (special counter "ID Process") and all .NET counters (special counter "Process ID" in the ".NET CLR Memory" category).
    
- Process ID can be encoded directly into the instance name.
    .NET performance counters can use this approach when [ProcessNameFormat flag](https://msdn.microsoft.com/en-us/library/dd537616.aspx) is set in the in the registry. 

EventFlow PerformanceCounter input supports the first method of deterimining counter instance name for current process via configuration settings. It also supports the second method, but only for .NET performance counters.

*Configuration example*
```json
{
    "type": "PerformanceCounter",
    "sampleIntervalMsec": "5000",
    "counters": [
        {
            "counterCategory": "Process",
            "counterName": "Private Bytes"
        }, 
        {
            "counterCategory": ".NET CLR Exceptions",
            "counterName": "# of Exceps Thrown / sec"
        }
    ]
}
```

*Top-level configuration settings*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "PerformanceCounter" | Yes | Specifies the input type. For this input, it must be "PerformanceCounter". |
| `sampleIntervalMsec` | integer | No | Specifies the sampling rate for the whole input (in milliseconds).   This is the rate at which the collection loop for the whole input executes. Default is 10 seconds. |
| `counters` | JSON array of Counter objects | Yes | Specifies performance counters to collect data from. |

*Counter class*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `counterCategory` | string | Yes | Category of the performance counter to monitor |
| `counterName` | string | Yes | Name of the counter to monitor. |
| `collectionIntervalMsec` | integer | No | Sampling interval for the counter. Values for the counter are read not more often than at this rate. Default is 30 seconds. |
| `processIdCounterCategory` and `processIdCounterName` | string | No | The category and name of the performance counter that provides process ID to counter instance name mapping. It is not necessary to specify these for the "Process" counter category and for .NET performance counters. |
| `useDotNetInstanceNameConvention` | boolean | No | Indicates that the counter instance names include process ID as described in [ProcessNameFormat documentation](https://msdn.microsoft.com/en-us/library/dd537616.aspx). |

*Important usage note*

Some performance counters require the user to be a member of the Performance Monitor Users system group. 
This can manifest itself by health reporter reporting "category does not exist" errors from PerformanceCounter output, 
despite the fact that the category and counter are properly configured and clearly visible in Windows Performance Monitor. 
If you need to consume such counters, make sure the account your process runs under belogs to Performance Monitor Users group.

#### Serilog

*Nuget package:* **Microsoft.Diagnostics.EventFlow.Inputs.Serilog**

This input enables capturing diagnostic data created through [Serilog library](https://serilog.net/). It is designed to work with [Observable Serilog sink](https://github.com/serilog/serilog-sinks-observable). 

*Configuration example*
The Serilog input has no configuration, other than the "type" property that specifies the type of the input (must be "Serilog"):
```json
{
  "type": "Serilog"
}
```

*Example: instantiating a Serilog logger that uses EventFlow Serilog input*

```csharp
using System;
using System.Linq;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Serilog;
using Serilog.Events;

namespace SerilogEventFlow
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(".\\eventFlowConfig.json"))
            {
                IObserver<LogEvent> serilogInput = pipeline.Inputs.OfType<SerilogInput>().First();
                Log.Logger = new LoggerConfiguration().WriteTo.Observers(events => events.Subscribe(serilogInput)).CreateLogger();

                Log.Information("Hello from {friend} for {family}!", "SerilogInput", "EventFlow");
                Log.CloseAndFlush();
                Console.ReadKey();
            }
        }
    }
}
```

### Outputs
Outputs define where data will be published from the engine. It's an error if there are no outputs defined. Each output type has its own set of parameters.

#### StdOutput
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Outputs.StdOutput**

This output writes data to the console window. Here is an example showing all possible settings:
```json
{
    "type": "StdOutput"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "StdOutput" | Yes | Specifies the output type. For this output, it must be "StdOutput". |

#### Event Hub
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Outputs.EventHub**

This output writes data to the [Azure Event Hub](https://azure.microsoft.com/en-us/documentation/articles/event-hubs-overview/). Here is an example showing all possible settings:
```json
{
    "type": "EventHub",
    "eventHubName": "myEventHub",
    "connectionString": "Endpoint=sb://<myEventHubNamespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<MySharedAccessKey>"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "EventHub" | Yes | Specifies the output type. For this output, it must be "EventHub". |
| `eventHubName` | event hub name | No | Specifies the name of the event hub. |
| `connectionString` | connection string | Yes | Specifies the connection string for the event hub. The corresponding shared access policy must have send permission. If the event hub name does not appear in the connection string, then it must be specified in the eventHubName field. |

#### Application Insights
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Outputs.ApplicationInsights**

This output writes data to the [Azure Application Insights service](https://azure.microsoft.com/en-us/documentation/articles/app-insights-overview/). Here is an example showing all possible settings:
```json
{
    "type": "ApplicationInsights",
    "instrumentationKey": "00000000-0000-0000-0000-000000000000"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "ApplicationInsights" | Yes | Specifies the output type. For this output, it must be "ApplicationInsights". |
| `instrumentationKey` | GUID | Yes | Specifies the instrumentation key for the targeted Application Insights resource. The key is in the form of a GUID. The key can be found on the Application Insights blade in Azure Portal. |

*Standard metadata support*

Application Insights output supports `metric` and `request` metadata. Each event decorated with either of these metadata types will be reported as Application Insights *metric* or *request*, respectively, enabling rich support for visualization and alerting that Application Insights provides. All other events will be reported as Application Insights *traces*. 

#### Elasticsearch
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch**

This output writes data to the [Elasticsearch](https://www.elastic.co/products/elasticsearch). Here is an example showing all possible settings:
```json
{
    "type": "ElasticSearch",
    "indexNamePrefix": "app1-",
    "serviceUri": "https://myElasticSearchCluster:9200",
    "basicAuthenticationUserName": "esUser1",
    "basicAuthenticationPassword": "<MyPassword>",
    "eventDocumentTypeName": "diagData"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "ElasticSearch" | Yes | Specifies the output type. For this output, it must be "ElasticSearch". |
| `indexNamePrefix` | string | No | Specifies the prefix to be used when creating the Elasticsearch index. This prefix, together with the date of when the data was generated, will be used to form the name of the Elasticsearch index. If not specified, a prefix will not be used. |
| `serviceUri` | URL:port | Yes | Specifies where the Elasticsearch cluster is. This is needed for EventFlow to locate the cluster and send the data. |
| `basicAuthenticationUserName` | string | No | Specifies the user name used to authenticate with Elasticsearch. To protect the cluster, authentication is often setup on the cluster. |
| `basicAuthenticationPassword` | string | No | Specifies the password used to authenticate with Elasticsearch. This field should be used only if basicAuthenticationUserName is specified. |
| `eventDocumentTypeName` | string | Yes | Specifies the document type to be applied when data is written. Elasticsearch allows documents to be typed, so they can be distinguished from other types. This type name is user-defined. |

*Standard metadata support*

Elasticsearch output supports `metric` and `request` metadata. Events decorated with this metadata will get additional properties when sent to Elasticsearch.

Fields injected by `metric` metadata are:
| Field | Description |
| :---- | :-------------- |
| `MetricName` | The name of the metric, read directly from the metadata. |
| `Value` | The value of the metric, read from the event property specified by `metricValueProperty`. |

Fields injected byt the `request` metadata are:
| Field | Description |
| :---- | :-------------- |
| `RequestName` | The name of the request, read the event property specified by `requestNameProperty`. |
| `Duration` | Request duration, read from the event property specified by `durationProperty` (if available). |
| `IsSuccess` | Success indicator,  read from the event property specified by `isSuccessProperty` (if available). |
| `ResponseCode` | Response code for the request, read from the event property specified by `durationProperty` (if available). | 

#### OMS (Operations Management Suite)

*Nuget package*: **Microsoft.Diagnostics.EventFlow.Outputs.Oms**

The OMS output writes data to [Operations Management Suite](https://www.microsoft.com/en-us/cloud-platform/operations-management-suite) Log Analytics workspaces. You will need to create a Log Analytics workspace in Azure and know its ID and key before using OMS output. Here is a sample configuration fragment enabling the output:
```json
{
  "type": "OmsOutput",
  "workspaceId": "<workspace-GUID>",
  "workspaceKey": "<base-64-encoded workspace key>"
}
```

Supported configuration settings are:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "OmsOutput" | Yes | Specifies the output type. For this output, it must be "OmsOutput". |
| `workspaceId` | string (GUID) | Yes | Specifies the workspace identifier. |
| `workspaceKey` | string (base-64) | Yes | Specifies the workspace authentication key. |
| `logTypeName` | string | No | Specifies the log entry type created by the output. Default value for this setting is "Event", which results in "Event_CL" entries being created in OMS (the "_CL" suffix is appended automatically by OMS ingestion service). |

### Filters
As data comes through the EventFlow pipeline, the application can add extra processing or tagging to them. These optional operations are accomplished with filters. Filters can transform, drop, or tag data with extra metadata, with rules based on custom expressions.
With metadata tags, filters and outputs operating further down the pipeline can apply different processing for different data. For example, an output component can choose to send only data with a certain tag. Each filter type has its own set of parameters.

#### drop
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Core**

This filter discards all data that satisfies the include expression. Here is an example showing all possible settings:
```json
{
    "type": "drop",
    "include": "Level == Verbose || Level == Informational"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "drop" | Yes | Specifies the filter type. For this filter, it must be "drop". |
| `include` | logical expression | Yes | Specifies the logical expression that determines if the action should apply to the event data or not. For information about the logical expression, please see section [Logical Expressions](#logical-expressions). |

#### metadata
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Core**

This filter adds additional metadata to all data that satisfies the include expression. Here is an example showing all possible settings:
```json
{
    "type": "metadata",
    "metadata": "metric",
    "include": "ProviderName == MyEventProvider && EventId == 3",
    "customTag1": "tag1",
    "customTag2": "tag2"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "metadata" | Yes | Specifies the filter type. For this filter, it must be "metadata". |
| `metadata` | string | Yes | Specifies the metadata type. This field is used only if type is "metadata", so it shouldn't appear in other filter types. The metadata type is user-defined and is persisted along with metadata tag added to the event data. |
| `include` | logical expression | Yes | Specifies the logical expression that determines if the action should apply to the event data or not. For information about the logical expression, please see section [Logical Expressions](#logical-expressions). |
| *[others]* | string | No | Specifies custom properties that should be added along with this metadata object. When the event data is processed by other filters or outputs, these properties can be accessed. The names of these properties are custom-defined and the possible set is open-ended. For a particular filter, zero or more custom properties can be defined. In the example above, customTag1 and customTag2 are such properties. |

### Standard metadata types

EventFlow core library defines a couple of standard metadata types: `metric` and `request`. They have pre-defined set of fields and are recognized by [Application Insights](#application-insights) and [Elasticsearch](#elasticsearch) outputs (see documentation for each output, respectively, to learn how they handle standard metadata).

**Metric metadata type**

Metrics are named time series of floating-point values. Metric metadata defines how metrics are derived from ordinary events. Following fields are supported:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `metadata` | "metric" | Yes | Indicates metric metadata definition; must be "metric". |
| `metricName` | string | Yes | The name of the metric. |
| `metricValue` | double | (see Remarks) | The value of the metric. This is useful for "counter" type of metric when each occurrence of a particular event should result in an increment of the counter. |
| `metricValueProperty` | string | (see Remarks) | The name of the event property that holds the metric value. | 

Remarks: 
1. Either `metricValue` or `metricValueProperty` must be specified.

**Request metadata type**

Requests are special events that represent invocations of a network service by its clients. Request metadata defines how requests are derived from ordinary events. Following fields are supported:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `metadata` | "request" | Yes | Indicates request metadata definition; must be "request". |
| `requestNameProperty` | string | Yes | The name of the event property that contanis the name of the request (to distinguish between different kinds of requests). |
| `isSuccessProperty` | string | No | The name of the event property that specifies whether the request ended successfully. It is expected that the event property is, or can be converted to a boolean. |
| `durationProperty` | string | No | The name of the event property that specifies the request duration (execution time). |
| `durationUnit` | "TimeSpan", "milliseconds", "seconds", "minutes" or "hours" | No | Specifies the type of data used by request duration property. If not set, it is assumed that request duration is expressed as a double value, representing milliseconds. |
| `responseCodeProperty` | string | No | The name of the event property that specifies response code associated with the request. A response code describes in more detail the outcome of the request. It is expected that the event property is, or can be converted to a string. |

### Health Reporter
Every software component can generate errors or warnings the developer should be aware of. The EventFlow library is no exception. An EventFlow health reporter reports errors and warnings generated by any components in the EventFlow pipeline.
In what format the report is presented depends on the implementation of the health reporter. The EventFlow library suite includes two health reporters: CsvHealthReporter and ServiceFabricHealthReporter. 

The CsvHealthReporter is the default health reporter for EventFlow library and is used if the health reporter configuration section is omitted. Its configuration parameters are described below.

ServiceFabricHealthReporter is described in the [Service Fabric support paragraph](#service-fabric-support). It is designed to be used in the context of Service Fabric applications and does not need any configuration.

#### CsvHealthReporter
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Core**

This health reporter writes all errors, warnings, and informational traces generated from the pipeline into a CSV file. Here is an example showing all possible settings:
```json
"healthReporter": {
    "type": "CsvHealthReporter",
    "logFileFolder": ".",
    "logFilePrefix": "HealthReport",
    "minReportLevel": "Warning",
    "throttlingPeriodMsec": "1000"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "CsvHealthReporter" | Yes | Specifies the health reporter type. For this reporter, it must be "CsvHealthReporter". |
| `logFileFolder` | file path | No | Specifies a path for the CSV log file to be written. It can be an absolute path, or a relative path. If it's a relative path, then it's computed relative to the directory where the EventFlow core library is in. However, if it's an ASP.NET application, it's relative to the app_data folder. |
| `logFilePrefix` | file path | No | Specifies a prefix used for the CSV log file. CsvHealthReporter creates the log file name by combining this prefix with the date of when the file is generated. If the prefix is omitted, then a default prefix of "HealthReport" is used. |
| `minReportLevel` | Error, Warning, Message | No | Specifies the collection report level. Report traces with equal or higher severity than specified are collected. For example, if Warning is specified, then Error, and Warning traces are collected. Default is Error. |
| `throttlingPeriodMsec` | number of milliseconds | No | Specifies the throttling time period. This setting protects the health reporter from being overwhelmed, which can happen if a message is repeatedly generated due to an error in the pipeline. Default is 0, for no throttling. |

### Pipeline Settings
The EventFlow configuration has settings allowing the application to adjust certain behaviors of the pipeline. These range from how many events the pipeline buffer, to the timeout the pipeline should use when waiting for an operation. If this section is omitted, the pipeline will use default settings.
Here is an example of all the possible settings:
```json
"settings": {
    "pipelineBufferSize": "1000",
    "maxEventBatchSize": "100",
    "maxBatchDelayMsec": "500",
    "maxConcurrency": "8",
    "pipelineCompletionTimeoutMsec": "30000"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `pipelineBufferSize` | number | No | Specifies how many events the pipeline can buffer if the events cannot flow through the pipeline fast enough. This buffer protects loss of data in cases where there is a sudden burst of data. |
| `maxEventBatchSize` | number | No | Specifies the maximum number of events to be batched before the batch gets pushed through the pipeline to filters and outputs. The batch is pushed down when it reaches the maxEventBatchSize, or its oldest event has been in the batch for more than maxBatchDelayMsec milliseconds. |
| `maxBatchDelayMsec` | number of milliseconds | No | Specifies the maximum time that events are held in a batch before the batch gets pushed through the pipeline to filters and outputs. The batch is pushed down when it reaches the maxEventBatchSize, or its oldest event has been in the batch for more than maxBatchDelayMsec milliseconds. |
| `maxConcurrency` | number | No | Specifies the maximum number of threads that events can be processed. Each event will be processed by a single thread, by multiple threads can process different events simultaneously. |
| `pipelineCompletionTimeoutMsec` | number of milliseconds | No | Specifies the timeout to wait for the pipeline to shutdown and clean up. The shutdown process starts when the DiagnosePipeline object is disposed, which usually happens on application exit. |

## Service Fabric Support

*Nuget Package*: **Microsoft.Diagnostics.EventFlow.ServiceFabric**

This package contains two components that make it easier to include EventFlow in Service Fabric applications: the `ServiceFabricDiagnosticPipelineFactory` and `ServiceFabricHealthReporter`. `ServiceFabricHealthReporter` is used automatically by `ServiceFabricDiagnosticPipelineFactory`. It does not require any configuration and does not need to be listed in the pipeline configuration file.

The `ServiceFabricDiagnosticPipelineFactory` is a replacement for the standard `DiagnosticPipelineFactory`, one that uses Service Fabric configuration support to load pipeline configuration. The resulting pipeline reports any execution problems through the Service Fabric health subsystem. The factory exposes a static `Create()` method that takes two parameters:

| Parameter | Default Value | Description |
| :-------- | :-------------- | :---------- |
| `healthEntityName` | (none) | The name of the health entity that will be used to report EventFlow pipeline health to Service Fabric. Usually it is set to a value that helps you identify the service using the pipeline, for example "MyApplication-MyService-DiagnosticsPipeline". |
| `configurationFileName` | "eventFlowConfig.json" | The name of the configuration file that contains pipeline configuration. The file is expected to be part of the (Service Fabric) service configuration package.| 

The recommended place to create the diagnostic pipeline is in the service `Main()` method:

```csharp
public static void Main(string[] args)
{
    try
    {
        using (var diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("MyApplication-MyService-DiagnosticsPipeline"))
        {
            ServiceRuntime.RegisterServiceAsync("MyServiceType", ctx => new MyService(ctx)).Wait();

            ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(MyService).Name);

            Thread.Sleep(Timeout.Infinite);
        }
    }
    catch (Exception e)
    {
        ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
        throw;
    }
}
```

## Logical Expressions
The logical expression allows you to filter events based on the event properties. For example, you can have an expression like "ProviderName == MyEventProvider && EventId == 3", where you specify the event property name on the left side and the value to compare on the right side. If the value on the right side contains special characters, you can enclose it in double quotes.

Here are the supported operators:

Comparison: `==`, `!=`, `<`, `>`, `<=`, `>=`

Regular Expression: `~=` (provide a regular expression pattern on the right)

Logical: `&&`, `||`, `!` (the precedence is `!` > `&&` > `||`)

Grouping: `(expression)` (grouping can be used to change the evaluation order of expressions with logical operators)

## Store Secret Securely
If you don't want to put sensitive information in the EventFlow configuration file, you can store the information at a secured place and pass it to the configuration at run time. Here is the sample code:
```csharp
string configFilePath = @".\eventFlowConfig.json";
IConfiguration config = new ConfigurationBuilder().AddJsonFile(configFilePath).Build();
IConfiguration eventHubOutput = config.GetSection("outputs").GetChildren().FirstOrDefault(c => c["type"] == "EventHub");

if (eventHubOutput != null)
{
    string eventHubConnectionString = GetEventHubConnectionStringFromSecuredPlace();
    eventHubOutput["connectionString"] = eventHubConnectionString;
}

using (DiagnosticPipeline pipeline = DiagnosticPipelineFactory.CreatePipeline(config))
{
    // ...
}
```

## Contribution
* Prereq
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

 
