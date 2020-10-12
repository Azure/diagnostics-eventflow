# Microsoft.Diagnostics.EventFlow

## Introduction
The EventFlow library suite allows applications to define what diagnostics data to collect, and where they should be outputted to. Diagnostics data can be anything from performance counters to application traces.
It runs in the same process as the application, so communication overhead is minimized. It also has an extensibility mechanism so additional inputs and outputs can be created and plugged into the framework. It comes with the following inputs and outputs:

*Inputs*
- [Trace (a.k.a. System.Diagnostics.Trace)](#trace) 
- [EventSource](#eventsource)
- [PerformanceCounter](#performancecounter)
- [Serilog](#serilog)
- [Microsoft.Extensions.Logging](#microsoftextensionslogging)
- [ETW (Event Tracing for Windows)](#etw-event-tracing-for-windows)
- [Application Insights](#application-insights-input)
- [Log4net](#log4net)
- [NLog](#nlog)
- [DiagnosticSource](#diagnosticsource)

*Outputs*
- [StdOutput (console output)](#stdoutput)
- [HTTP (json via http)](#http)
- [Application Insights](#application-insights)
- [Azure EventHub](#event-hub)
- [Elasticsearch](#elasticsearch)
- [Azure Monitor Logs](#azure-monitor-logs)

There are several EventFlow extensions available from non-Microsoft authors and vendors, including:
- [Google Big Query output](https://github.com/tsu1980/diagnostics-eventflow-bigquery)
- [SQL Server output](https://github.com/ycamargo/diagnostics-eventflow-sqloutput)
- [ReflectInsight output](https://github.com/reflectsoftware/Microsoft.Diagnostics.EventFlow.Outputs.ReflectInsight)
- [Splunk output](https://github.com/hortha/diagnostics-eventflow-splunk)

The EventFlow suite supports .NET applications and .NET Core applications. It allows diagnostic data to be collected and transferred for applications running in these Azure environments:

- Azure Web Apps
- Service Fabric
- Azure Cloud Service
- Azure Virtual Machines

The core of the library, as well as inputs and outputs listed above [are available as NuGet packages](https://www.nuget.org/packages?q=Microsoft.Diagnostics.EventFlow).

## Getting Started
1. To quickly get started, you can create a simple console application in VisualStudio and install the following Nuget packages:
    * Microsoft.Diagnostics.EventFlow.Inputs.Trace
    * Microsoft.Diagnostics.EventFlow.Outputs.ApplicationInsights
    * Microsoft.Diagnostics.EventFlow.Outputs.StdOutput

2. Add a JSON file named "eventFlowConfig.json" to your project and set the Build Action property of the file to "Copy if Newer". Set the content of the file to the following:
```js
{
    "inputs": [
    {
      "type": "Trace",
      "traceLevel": "Warning"
    }
  ],
  "outputs": [
    // Please update the instrumentationKey.
    {
      "type": "ApplicationInsights",
      "instrumentationKey": "00000000-0000-0000-0000-000000000000"
    }
  ],
  "schemaVersion": "2016-08-11"
}
```

Note: if you are using VisualStudio for Mac, you might need to edit the project file directly. Make the following snippet for `eventFlowConfig.json` file is included in the project definition:
   ```xml
   <ItemGroup>
    <None Include="eventFlowConfig.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
   ```

3. If you wish to send diagnostics data to Application Insights, fill in the value for the instrumentationKey. If not, you can send traces to console output instead by replacing the Application Insights output with the standard output. The `outputs` property in the configuration should then look like this: 
```jsonc
    "outputs": [
    {
      "type": "StdOutput"
    }
  ],
```

4. Create an EventFlow pipeline in your application code using the code below. Make sure there is at least one output defined in the configuration file. Run your application and see your traces in console output or in Application Insights.
```csharp
    using (var pipeline = DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
    {
        System.Diagnostics.Trace.TraceWarning("EventFlow is working!");
        Console.WriteLine("Trace sent to Application Insights. Press any key to exit...");
        Console.ReadKey(intercept: true);
    }
```
It usually takes a couple of minutes for the traces to show in Application Insights Azure portal.

## Configuration Details
The EventFlow pipeline is built around three core concepts: [inputs](#inputs), [outputs](#outputs), and [filters](#filters). The number of inputs, outputs, and filters depend on the need of diagnostics. The configuration 
also has a healthReporter and settings section for configuring settings fundamental to the pipeline operation. Finally, the extensions section allows declaration of custom developed
plugins. These extension declarations act like references. On pipeline initialization, EventFlow will search extensions to instantiate custom  inputs, outputs, or filters.

### Inputs
These define what data will flow into the engine. At least one input is required. Each input type has its own set of parameters.

#### Trace
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Inputs.Trace**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.Trace/)

This input listens to traces written with System.Diagnostics.Trace API. Here is an example showing all possible settings:
```jsonc
{
    "type": "Trace",
    "traceLevel":  "Warning"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "Trace" | Yes | Specifies the input type. For this input, it must be "Trace". |
| `traceLevel` | Critical, Error, Warning, Information, Verbose, All | No | Specifies the collection trace level. Traces with equal or higher severity than specified are collected. For example, if Warning is specified, then Critial, Error, and Warning traces are collected. Default is Error. |

#### EventSource
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Inputs.EventSource**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.EventSource/)

This input listens to EventSource traces. EventSource classes can be created in the application by deriving from the [System.Diagnostics.Tracing.EventSource](https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx) class. Here is an example showing all possible settings:
```jsonc
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

*Top object*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "EventSource" | Yes | Specifies the input type. For this input, it must be "EventSource". |
| `sources` | JSON array | Yes | Specifies the EventSource objects to collect. |

*Source object (element of the sources array)*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `providerName` | EventSource name | Yes(*) | Specifies the name of the EventSource to track. |
| `providerNamePrefix` | EventSource name prefix | Yes(*) | Specifies the name prefix of EventSource(s) to track. For example, if the value is "Microsoft-ServiceFabric", all EventSources that have names starting with Microsoft-ServiceFabric (Microsoft-ServiceFabric-Services, Microsoft-ServiceFabric-Actors and so on) will be tracked. |
| `disabledProviderNamePrefix` | provider name | Yes(*) | Specifies the name prefix of the EventSource(s) that must be ignored. No events from these sources will be captured(***). |
| `level` | Critial, Error, Warning, Informational, Verbose, LogAlways | No(**) | Specifies the collection trace level. Traces with equal or higher severity than specified are collected. For example, if Warning is specified, then Critial, Error, and Warning traces are collected. Default is LogAlways, which means "provider decides what events are raised", which usually results in all events being raised. |
| `keywords` | An integer | No(**) | A bitmask that specifies what events to collect. Only events with keyword matching the bitmask are collected, except if it's 0, which means everything is collected. Default is 0. |
| `eventCountersSamplingInterval` | An integer | No | Specifies the sampling interval in seconds for collecting data from EventCounters. Default is 0, which means no EventCounter data is collected.|

*Remarks*

(*) Out of `providerName`, `providerNamePrefix` and `disabledProviderNamePrefix`, only one can be used for a single source. In other words, with a single source one can enable an EventSource by name, or enable a set of EventSources by prefix, or disable a set of EventSources by prefix.

(**) `level` and `keywords` can be used for enabling EventSources, but not for disabling them. Disabling events using level and keywords is not supported (but one can use level and/or keywords to *selectively enable* a subset of events from a given EventSource).

(***) There is an issue with .NET frameworks 4.6 and 4.7, and .NET Core framework 1.1 and 2.0 where dynamically created EventSource events are dispatched to all listeners, regardless whether listeners subscribe to events from these EventSources; for more information see https://github.com/dotnet/coreclr/issues/14434  `disabledProviderNamePrefix` property can be usesd to suppress these events.<br/>
Disabling EventSources is not recommended under normal circumstances, as it introduces a slight performance penalty. Instead, selectively enable necessary events through combination of EventSource names, event levels, and keywords.

#### DiagnosticSource
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource/)

This input listens to System.Diagnostics.DiagnosticSource sources. See the [DiagnosticSource User's Guide](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md) for details of usage. Here is an example showing all possible settings:
```jsonc
{
    "type": "DiagnosticSource",
    "sources": [
        { "providerName": "MyDiagnosticSource" }
    ]
}
```

*Top object*

| Field | Values/Types | Required | Description |
| :---- | :----------- | :------: | :---------- |
| `type` | "DiagnosticSource" | Yes | Specifies the input type. For this input, it must be "DiagnosticSource". |
| `sources` | JSON array | Yes | Specifies the DiagnosticSource objects to collect. |

*Source object (element of the sources array)*

| Field | Values/Types | Required | Description |
| :---- | :----------- | :------: | :---------- |
| `providerName` | DiagnosticSource name | Yes | Specifies the name of the DiagnosticSource to track. |

#### PerformanceCounter
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Inputs.PerformanceCounter**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.PerformanceCounter/)

This input enables gathering data from Windows performance counters. Only *process-specific* counters are supported, that is, the counter must have an instance that is associated with the current process. For machine-wide counters use an external agent such as [Azure Diagnostics Agent](https://azure.microsoft.com/en-us/documentation/articles/azure-diagnostics/) or create a custom input.

*Finding the counter instance that corresponds to the current process*

In general there is no canonical way to find a performance counter instance that corresponds to current process. Two methods are commonly used in practice:

- A special performance counter that provides instance name to process ID mapping.
    This solution involves a set of counters that use the same instance name for a given process. Among them there is a special counter with a value that is the process ID of the corresponding process. Searching for the instance of the special counter with a value equal to the current process ID allows to discover what the instance name is used for the current process. Examples of this approach include the Windows Process category (special counter "ID Process") and all .NET counters (special counter "Process ID" in the ".NET CLR Memory" category).
    
- Process ID can be encoded directly into the instance name.
    .NET performance counters can use this approach when [ProcessNameFormat flag](https://msdn.microsoft.com/en-us/library/dd537616.aspx) is set in the in the registry. 

EventFlow PerformanceCounter input supports the first method of deterimining counter instance name for current process via configuration settings. It also supports the second method, but only for .NET performance counters.

*Configuration example*
```jsonc
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
If you need to consume such counters, make sure the account your process runs under belongs to Performance Monitor Users group.

#### Serilog

*Nuget package:* [**Microsoft.Diagnostics.EventFlow.Inputs.Serilog**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.Serilog/)

This input enables capturing diagnostic data created through [Serilog library](https://serilog.net/).

*Configuration example*
```jsonc
{
  "type": "Serilog",
  "useSerilogDepthLevel": true
}
```

*Top-level configuration settings*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "Serilog" | Yes | Specifies the input type. For this input, it must be "Serilog". |
| `useSerilogDepthLevel` | bool | No | If true the input will try to preserve the structure of data passed to it, up to a maximum depth determined by Serilog maximum destructuring depth setting; otherwise all objects will be flattened. Defaults to false for backward compatibility. |

*Example: instantiating a Serilog logger that uses EventFlow Serilog input*

```csharp
using System;
using System.Linq;
using Microsoft.Diagnostics.EventFlow;
using Serilog;

namespace SerilogEventFlow
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(".\\eventFlowConfig.json"))
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.EventFlow(pipeline)
                    .CreateLogger();

                Log.Information("Hello from {friend} for {family}!", "SerilogInput", "EventFlow");
                
                Log.CloseAndFlush();
                Console.ReadKey();
            }
        }
    }
}
```

#### Microsoft.Extensions.Logging

*Nuget package:* [**Microsoft.Diagnostics.EventFlow.Inputs.MicrosoftLogging**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.MicrosoftLogging/)

This input enables capturing diagnostic data created through Microsoft.Extensions.Logging library and ILogger interface. 

*Configuration example*
The ILogger input has no configuration, other than the "type" property that specifies the type of the input (must be "Microsoft.Extensions.Logging"):
```jsonc
{
  "type": "Microsoft.Extensions.Logging"
}
```

*Example: instantiating a ILogger that uses EventFlow ILogger input*

```csharp
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Extensions.Logging;

namespace LoggerEventFlow
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(".\\eventFlowConfig.json"))
            {
                var factory = new LoggerFactory()
                    .AddEventFlow(pipeline);

                var logger = new Logger<Program>(factory);
                using (logger.BeginScope(myState))
                {
                    logger.LogInformation("Hello from {friend} for {family}!", "LoggerInput", "EventFlow");
                }
            }
        }
    }
}
```

*Example: using EventFlow ILogger input with ASP.NET Core*
The following example shows how to enable EventFlow ILogger inside a Service Fabric stateless service that uses ASP.NET Core.

1. Modify the service class so that its constructor takes a `DiagnosticPipeline` instance as a parameter:
```csharp
        private static void Main()
        {
            try
            {
                using (ManualResetEvent terminationEvent = new ManualResetEvent(initialState: false))
                using (var pipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("CoreBasedFabricPlusEventFlow-Diagnostics"))
                {
                    Console.CancelKeyPress += (sender, eventArgs) => Shutdown(diagnosticsPipeline, terminationEvent);

                    AppDomain.CurrentDomain.UnhandledException += (sender, unhandledExceptionArgs) =>
                    {
                        ServiceEventSource.Current.UnhandledException(unhandledExceptionArgs.ExceptionObject?.ToString() ?? "(no exception information)");
                        Shutdown(diagnosticsPipeline, terminationEvent);
                    };

                    ServiceRuntime.RegisterServiceAsync("Web1Type",
                        context => new Web1(context, pipeline)).GetAwaiter().GetResult();

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(Web1).Name);

                    terminationEvent.WaitOne();
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        private static void Shutdown(IDisposable disposable, ManualResetEvent terminationEvent)
        {
            try
            {
                disposable.Dispose();
            }
            finally
            {
                terminationEvent.Set();
            }
        }
```

2. In the CreateServiceInstanceListeners() method add the pipeline as a singleton service to ASP.NET dependency injection container

```csharp
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new WebListenerCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting WebListener on {url}");

                        return new WebHostBuilder().UseWebListener()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatelessServiceContext>(serviceContext)
                                            .AddSingleton<DiagnosticPipeline>(this.diagnosticPipeline))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseApplicationInsights()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
        }
```
3. In the Startup class configure the loggerFactory by calling AddEventFlow on it:

```csharp
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            var diagnosticPipeline = app.ApplicationServices.GetRequiredService<DiagnosticPipeline>();
            loggerFactory.AddEventFlow(diagnosticPipeline);

            app.UseMvc();
        }
```

4. Now you can assume the logger factory will be constructor-injected into your controllers:

```csharp
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ILogger<ValuesController> logger;

        public ValuesController(ILogger<ValuesController> logger)
        {
            this.logger = logger;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            this.logger.LogInformation("Hey, someone just called us!");
            return new string[] { "value1", "value2" };
        }

      // (rest of controller code is irrelevant)
```


#### ETW (Event Tracing for Windows)

*Nuget package:* [**Microsoft.Diagnostics.EventFlow.Inputs.Etw**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.Etw/)

This input captures data from Microsoft Event Tracing for Windows (ETW) providers. Both manifest-based providers as well as providers based on managed `EventSource` infrastructure are supported. The data is captured machine-wide and requires that the identity the process uses belongs to Performance Log Users built-in administrative group.

*Note* 

To capture data from EventSources running in the same process as EventFlow, the [EventSource input](#eventsource) is a better choice, with better performance and no additional security requirements.

*Configuration example*
```jsonc
{
    "type": "ETW",
    "sessionNamePrefix": "MyCompany-MyApplication",
    "cleanupOldSessions": true,
    "reuseExistingSession": true,
    "providers": [
        {
            "providerName": "Microsoft-ServiceFabric",
            "level": "Warning",
            "keywords": "0x7F"
        }
    ]
}
```

*Top Object*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "ETW" | Yes | Specifies the input type. For this input, it must be "ETW". |
| `sessionNamePrefix` | string | No | The ETW trace session will be created with this name prefix, which helps in differentiating  ETW input instances owned by multiple applications. If not set, a default prefix will be used. |
| `cleanupOldSessions` | boolean | No | If set, existing ETW trace sessions matching the `sessionNamePrefix` will be closed. This helps to collect leftover session instances, as there is a limit on their number. |
| `reuseExistingSession` | boolean | No | If turned on, then an existing trace session matching the `sessionNamePrefix` will be re-used. If `cleanupOldSessions` is also turned on, then it will leave one session open for re-use. |
| `providers` | JSON array | Yes | Specifies ETW providers to collect data from. |

*Providers object*

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `providerName` | provider name | Yes(*) | Specifies the name of the ETW provider to track. |
| `providerGuid` | provider GUID | Yes(*) | Specifies the GUID of the ETW provider to track. |
| `level` | Critial, Error, Warning, Informational, Verbose, LogAlways | No | Specifies the collection trace level. Traces with equal or higher severity than specified are collected. For example, if Warning is specified, then Critial, Error, and Warning traces are collected. Default is LogAlways, which means "provider decides what events are raised", which usually results in all events being raised. |
|`keywords` | An integer | No | A bitmask that specifies what events to collect. Only events with keyword matching the bitmask are collected, except if it's 0, which means everything is collected. Default is 0. |

(*) Either providerName, or providerGuid must be specified. When both are specified, provider GUID takes precedence.

#### Application Insights input

*Nuget package:* [**Microsoft.Diagnostics.EventFlow.Inputs.ApplicationInsights**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.ApplicationInsights/)

Application Insights input is designed for the following scenario:

1. You have an application that uses Application Insights for monitoring and diagnostics.
2. You want to send a portion of your Application Insights telemetry to some destination other than Application Insights (e.g. Azure EventHub or Elasticsearch; the assumption is there is an EventFlow output for where the data needs to go). 

For example, you might want to leverage [Application Insights sampling capabilities](https://docs.microsoft.com/en-us/azure/application-insights/app-insights-sampling) to reduce the amount of data analyzed by Application Insights without losing analysis fidelity, while sending full raw logs to Elasticsearch to do detailed log search during problem troubleshooting.

Application Insights input supports all standard Application Insights telemetry types: trace, request, event, dependency, metric, exception, page view and availability. 

*Usage&#8211;leveraging EventFlow configuration file*

To use Application Insights input in an application that creates EventFlow pipeline using a configuration file, do the following:

1. Add the `EventFlowTelemetryProcessor` to your Application Insights configuration file (it goes into `TelemetryProcessors` element):
   ```xml
   <ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings" >
     <!-- ... -->
     <TelemetryProcessors>
        <!-- ... -->
        <Add Type="Microsoft.Diagnostics.EventFlow.ApplicationInsights.EventFlowTelemetryProcessor, Microsoft.Diagnostics.EventFlow.Inputs.ApplicationInsights" />
        <!-- ... -->
     </TelemetryProcessors>
     <!-- ... -->
   </ApplicationInsights>
   ```
   *Note that the order of telemetry processors does matter.* In particular, if the `EventFlowTelemetryProcessor` is placed before the Application Insights sampling processor, EventFlow will capture all telemetry, but if the `EventFlowTelemetryProcessor` is placed after the sampling processor, it will only "see" telemetry that was sampled in. For more information on configuring Application Insights see [Application Insights configuration documentation](https://docs.microsoft.com/azure/application-insights/app-insights-configuration-with-applicationinsights-config).

2. In the EventFlow configuration make sure to include the Application Insights input. It does not take any parameters:
   ```jsonc
   { "type": "ApplicationInsights" }
   ```

3. In your application code, after the EventFlow pipeline is created, find the `EventFlowTelemetryProcessor` and set its `Pipeline` property to the instance of the EventFlow pipeline:
   ```csharp
   using (var pipeline = DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
   {
       // ...
       EventFlowTelemetryProcessor efTelemetryProcessor = TelemetryConfiguration.Active.TelemetryProcessors.OfType<EventFlowTelemetryProcessor>().First();
       efTelemetryProcessor.Pipeline = pipeline;
       // ...
   }
   ```

This is it&#8211;after the `EventFlowTelemetryProcessor.Pipeline` property is set, the `EventFlowTelemetryProcessor` will start sending AI telemetry into the EventFlow pipeline.

*Usage&#8211;ASP.NET Core*

ASP.NET Core application tend to use code to create various parts of their request processing pipeline, and both EventFlow and Application Insights follow that model. Application Insights input in EventFlow provides a class called `EventFlowTelemetryProcessorFactory` that helps connecting Application Insights with EventFlow in ASP.NET Core environment. Here is an example how one could set up Application Insights input to send telemetry to Elasticsearch:

```csharp
using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.EventFlow;
using Microsoft.Diagnostics.EventFlow.ApplicationInsights;
using Microsoft.Diagnostics.EventFlow.HealthReporters;
using Microsoft.Diagnostics.EventFlow.Inputs;
using Microsoft.Diagnostics.EventFlow.Outputs;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.AspNetCore;

namespace AspNetCoreEventFlow
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var eventFlow = CreateEventFlow(args))
            {
                BuildWebHost(args, eventFlow).Run();
            }
        }

        public static IWebHost BuildWebHost(string[] args, DiagnosticPipeline eventFlow) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddSingleton<ITelemetryProcessorFactory>(sp => new EventFlowTelemetryProcessorFactory(eventFlow)))
                .UseStartup<Startup>()
                .UseApplicationInsights()
                .Build();

        private static DiagnosticPipeline CreateEventFlow(string[] args)
        {
            // Create configuration instance to access configuration information for EventFlow pipeline
            // To learn about common configuration sources take a peek at https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs (CreateDefaultBuilder method). 
            // Here we assume all necessary information comes from command-line arguments and environment variables.
            var configBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            if (args != null)
            {
                configBuilder.AddCommandLine(args);
            }
            var config = configBuilder.Build();

            var healthReporter = new CsvHealthReporter(new CsvHealthReporterConfiguration());
            var aiInput = new ApplicationInsightsInputFactory().CreateItem(null, healthReporter);
            var inputs = new IObservable<EventData>[] { aiInput };
            var sinks = new EventSink[]
            {
                new EventSink(new ElasticSearchOutput(new ElasticSearchOutputConfiguration {
                    ServiceUri = config["ElasticsearchServiceUri"]
                    // Set other configuration settings, as necessary
                }, healthReporter), null)
            };

            return new DiagnosticPipeline(healthReporter, inputs, null, sinks, null, disposeDependencies: true);
        }
    }
}

```

#### Log4net

*Nuget package:* [**Microsoft.Diagnostics.EventFlow.Inputs.Log4net**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.Log4net/)

This input enables capturing diagnostic data sent to the [Log4net project](https://logging.apache.org/log4net/).

*Configuration example*
The Log4net input has one configuration, the Log4net Level:
```jsonc
{
  "type": "Log4net",
  "logLevel": "Debug"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "Log4net" | Yes | Specifies the output type. For this output, it must be "Log4net". |
| `logLevel` | "Debug", "Info", "Warn", "Error", or "Fatal" | Yes | Specifies minimum [Log4net Level](https://logging.apache.org/log4net/log4net-1.2.11/release/sdk/log4net.Core.Level.html) for captured events. For example, if the level is `Warn`, the input will capture events with levels equal to `Warn`, `Error`, or `Fatal`. |


*Example: instantiating a Log4net logger that uses EventFlow Log4net input*

```csharp
using System;
using Microsoft.Diagnostics.EventFlow;
using log4net;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(".\\eventFlowConfig.json"))
            {
                var logger = LogManager.GetLogger("EventFlowRepo", "MY_LOGGER_NAME");

                logger.Debug("Hey! Listen!", new Exception("uhoh"));
            }
        }
    }
}
```

#### NLog

*Nuget package:* [**Microsoft.Diagnostics.EventFlow.Inputs.NLog**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Inputs.NLog/)

This input enables capturing diagnostic data sent to the [NLog library](http://nlog-project.org/).

*Configuration example*
The NLog input has no configuration, other than the "type" property that specifies the type of the input (must be "NLog"):
```jsonc
{
  "type": "NLog"
}
```

*Example: instantiating a NLog logger that uses EventFlow NLog input*

```csharp
using System;
using Microsoft.Diagnostics.EventFlow;
using NLog;

namespace NLogEventFlow
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var pipeline = DiagnosticPipelineFactory.CreatePipeline(".\\eventFlowConfig.json"))
            {
                var nlogTarget = pipeline.ConfigureNLogInput(pipeline, NLog.LogLevel.Info);
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger.Info("Hello from {friend} for {family}!", "NLogInput", "EventFlow");
                NLog.LogManager.Shutdown();
                Console.ReadKey();
            }
        }
    }
}
```


### Outputs
Outputs define where data will be published from the engine. It's an error if there are no outputs defined. Each output type has its own set of parameters.

#### StdOutput
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Outputs.StdOutput**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Outputs.StdOutput/)

This output writes data to the console window. Here is an example showing all possible settings:
```jsonc
{
    "type": "StdOutput"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "StdOutput" | Yes | Specifies the output type. For this output, it must be "StdOutput". |

#### Http
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Outputs.HttpOutput**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Outputs.HttpOutput/)

This output writes data to a webserver using diffent encoding methods (Json or JsonLines, eg. for logstash). Here is an example showing all possible settings:
```jsonc
{
    "type": "Http",
    "serviceUri": "https://example.com/",
    "format": "Json",
    "httpContentType": "application/x-custom-type",
    "basicAuthenticationUserName": "httpUser1",
    "basicAuthenticationUserPassword": "<MyPassword>"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "Http" | Yes | Specifies the output type. For this output, it must be "Http". |
| `serviceUri` | string | Yes | Target service URL endpoint (can be HTTP and HTTPS) |
| `format` | "Json", "JsonLines" | No | Defines the message format (and the default HTTP Content-Type header). "Json" a json object with multiple array items and "JsonLines" one line per json object (multiple objects) |
| `basicAuthenticationUserName` | string | No | Specifies the user name used to authenticate with webserver. |
| `basicAuthenticationUserPassword` | string | No | Specifies the password used to authenticate with webserver. This field should be used only if basicAuthenticationUserName is specified. |
| `httpContentType` | string | No | Defines the HTTP Content-Type header |
| `headers` | object | No | Specifies custom headers that will be added to event upload request. Each property of the object becomes a separate header. |

#### Event Hub
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Outputs.EventHub**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Outputs.EventHub/)

This output writes data to the [Azure Event Hub](https://azure.microsoft.com/en-us/documentation/articles/event-hubs-overview/). Here is an example showing all possible settings:
```jsonc
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
| `partitionKeyProperty` | string | No | The name of the event property that will be used as the PartitionKey for the Event Hub events. |

#### Application Insights
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Outputs.ApplicationInsights**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Outputs.ApplicationInsights/)

This output writes data to the [Azure Application Insights service](https://azure.microsoft.com/en-us/documentation/articles/app-insights-overview/). Here is an example showing all possible settings:
```jsonc
{
    "type": "ApplicationInsights",
    "instrumentationKey": "00000000-0000-0000-0000-000000000000",
    "configurationFilePath": "path-to-ApplicationInsights.config"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "ApplicationInsights" | Yes | Specifies the output type. For this output, it must be "ApplicationInsights". |
| `instrumentationKey` | string | No (*) | Specifies the instrumentation key for the targeted Application Insights resource. The key is typically a GUID; it can be found on the Azure Monitor Application Insights blade in Azure portal. <br/> The value in this field overrides any value in the Application Insights configuration file (see the `configurationFilePath` parameter) and in the connection string (`connectionString` parameter). |
| `connectionString` | string | No (*) | Specifies the Application Insights connection string. <br/> The value in this field overrides any value in the Application Insights configuration file (see the `configurationFilePath` parameter). |
| `configurationFilePath` | string | No (*) | Specifies the path to Application Insights configuration file. This parameter is optional-if no value is specified, default configuration for the Application Insights output will be used. For more information see [Application Insights documentation](https://docs.microsoft.com/en-us/azure/application-insights/app-insights-configuration-with-applicationinsights-config). |

(*) At least one of the parameters: `instrumentationKey`, `connectionString`, or `configurationFilePath` must be provided for the Application Insights output configuration to be valid.

In Service Fabric environment the Application Insights configuration file can should be part of the default service configuration package (the 'Config' package). To resolve the path of the configuration file within the service configuration package set the value of `configurationFilePath` to `servicefabricfile:/ApplicationInsights.config`. For more information on this syntax see [Service Fabric support](#service-fabric-support)

*Standard metadata support*

Application Insights output supports all standard metadata (request, metric, dependency and exception). Each of these metadata types corresponds to a native Application Insights telemetry type, enabling rich support for visualization and alerting that Application Insights provides. The Application Insights output also supports *event* metadata (corresponding to AI event telemetry type). This metadata is meant to represent significant application events, like a new user registered with the system or a new version of the code being deployed. Event metadata supports following properties:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `metadata` | "ai_event" | Yes | Indicates Application Insights event metadata; must be "ai_event". |
| `eventNameProperty` | string | (see Remarks) | The name of the event property that will be used as the name of the AI event telemetry. |
| `eventName` | string | (see Remarks) | The name of the event (if the name is supposed to be taken verbatim from metadata). |

Remarks:
1. Either `eventNameProperty` or `eventName` must be given.

All other events will be reported as Application Insights *traces* (telemetry of type Trace). 

#### Elasticsearch
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Outputs.ElasticSearch/)

This output writes data to the [Elasticsearch](https://www.elastic.co/products/elasticsearch). Here is an example showing all possible settings:
```jsonc
{
    "type": "ElasticSearch",
    "indexNamePrefix": "app1",
    "serviceUri": "https://myElasticSearchCluster:9200;https://myElasticSearchCluster:9201;https://myElasticSearchCluster:9202",
    "connectionPoolType": "Sniffing",
    "basicAuthenticationUserName": "esUser1",
    "basicAuthenticationUserPassword": "<MyPassword>",
    "numberOfShards": 1,
    "numberOfReplicas": 1,
    "refreshInterval": "15s",
    "defaultPipeline": "my-pipeline",
    "mappings": {
        "properties": {
            "timestamp": {
                "type": "date_nanos"
            }
        }
    }

}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "ElasticSearch" | Yes | Specifies the output type. For this output, it must be "ElasticSearch". |
| `indexNamePrefix` | string | No | Specifies the prefix to be used when creating the Elasticsearch index. This prefix, together with the date of when the data was generated, will be used to form the name of the Elasticsearch index. If not specified, a prefix will not be used. |
| `serviceUri` | URL:port | Yes | Specifies where the Elasticsearch cluster is. This is needed for EventFlow to locate the cluster and send the data. Single URL or semicolon-separated URLs for connection pooling are accepted |
| `connectionPoolType` | "Static", "Sniffing", or "Sticky" | No | Specifies the Connection Pool that takes care of registering what nodes there are in the cluster. |
| `basicAuthenticationUserName` | string | No | Specifies the user name used to authenticate with Elasticsearch. To protect the cluster, authentication is often setup on the cluster. |
| `basicAuthenticationUserPassword` | string | No | Specifies the password used to authenticate with Elasticsearch. This field should be used only if basicAuthenticationUserName is specified. |
| `eventDocumentTypeName` | string | Yes (ver < 2.7.0) <br/> N/A (ver >= 2.7.0) | Specifies the document type to be applied when data is written. Elasticsearch allows documents to be typed, so they can be distinguished from other types. This type name is user-defined. <br/> <br/> Starting with Elasticsearch 7.x the [mapping types have been removed](https://www.elastic.co/guide/en/elasticsearch/reference/7.0/removal-of-types.html). Consequently this configuration setting has been removed from Elasticsearch output version 2.7.0 and newer. |
| `numberOfShards` | int | No | Specifies how many shards to create the index with. If not specified, it defaults to 1.|
| `numberOfReplicas` | int | No | Specifies how many replicas the index is created with. If not specified, it defaults to 5.|
| `refreshInterval` | string | No | Specifies what refresh interval the index is created with. If not specified, it defaults to 15s.|
| `defaultPipeline` | string | No | Specifies the default ingest node pipeline the index is created with. If not specified, a default pipeline will not be used.|
| `mappings`        | object | No | Specifies how documents created by the Elasticsearch output are stored and indexed (index mappings). For more information refer to [Elasticsearch documentation on index mappings](https://www.elastic.co/guide/en/elasticsearch/reference/current/mapping.html). |
| `mappings.properties` | object | No | Specifies property mappings for documents created by the Elasticsearch output. Currently only property *type* mappings can be specified. Supported types are: `text`, `keyword`, `date`, `date_nanos`, `boolean`, `long`, `integer`, `short`, `byte`, `double`, `float`, `half_float`, `scaled_float`, `ip`, `geo_point`, `geo_shape`, and `completion`. | 

*Standard metadata support*

Elasticsearch output supports all standard metadata types. Events decorated with metadata will get additional properties when sent to Elasticsearch.

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
| `ResponseCode` | Response code for the request, read from the event property specified by `responseCodeProperty` (if available). |

*Elasticsearch version support*

| Elasticsearch output package version | Supported Elasticsearch server version |
| :---- | :---- |
| 1.x | 2.x |
| 2.6.x | 6.x |
| 2.7.x | 7.x |

#### Azure Monitor Logs

*Nuget package*: [**Microsoft.Diagnostics.EventFlow.Outputs.AzureMonitorLogs**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Outputs.AzureMonitorLogs/)

The Azure Monitor Logs output writes data to [Azure Monitor Logs service (also knowns as Log Analytics service)](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/diagnostic-logs-overview) via [HTTP Data Collector API](https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api). You will need to create a Log Analytics workspace in Azure and know its ID and key before using Azure Monitor Logs output. Here is a sample configuration fragment enabling the output:
```jsonc
{
  "type": "AzureMonitorLogs",
  "workspaceId": "<workspace-GUID>",
  "workspaceKey": "<base-64-encoded workspace key>",
  "serviceDomain" : "<optional domain for Log Analytics workspace>"
}
```

Supported configuration settings are:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "AzureMonitorLogs" | Yes | Specifies the output type. For this output, it must be "AzureMonitorLogs". |
| `workspaceId` | string (GUID) | Yes | Specifies the workspace identifier. |
| `workspaceKey` | string (base-64) | Yes | Specifies the workspace authentication key. |
| `logTypeName` | string | No | Specifies the log entry type created by the output. Default value for this setting is "Event", which results in "Event_CL" entries being created in Log Analytics (the "_CL" suffix is appended automatically by Log Analytics Data Collector). |
| `serviceDomain` | string | No | Specifies the domain for your Log Analytics workspace. Default value is "ods.opinsights.azure.com", for Azure Commercial.

### Filters
As data comes through the EventFlow pipeline, the application can add extra processing or tagging to them. These optional operations are accomplished with filters. Filters can transform, drop, or tag data with extra metadata, with rules based on custom expressions.
With metadata tags, filters and outputs operating further down the pipeline can apply different processing for different data. For example, an output component can choose to send only data with a certain tag. Each filter type has its own set of parameters.

Filters can appear in two places in the EventFlow configuration: on the same level as inputs and outputs (_global filters_) and as part of output declaration (_output-specific filters_). Global filters are applied to all data coming from the inputs. Output-specific filters are applied to just one output, just before the data reaches the output. Here is an example with two global filters and one output-specific filter:

```jsonc
{
    "inputs": [...],

    "filters": [
        // The following are global filters
        {
            "type": "drop",
            "include": "..."
        },
        {
            "type": "drop",
            "include": "..."
        }
    ],

    "outputs": [
        {
            "type": "ApplicationInsights",
            "instrumentationKey": "00000000-0000-0000-0000-000000000000",
            "filters": [
                {
                    "type": "metadata",
                    "metadata": "metric",
                    // ... 
                }
            ]
        }
    ]
}
```

EventFlow comes with two standard filter types: `drop` and `metadata`.

#### drop
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Core**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Core/)

This filter discards all data that satisfies the include expression. Here is an example showing all possible settings:
```jsonc
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
*Nuget Package*: [**Microsoft.Diagnostics.EventFlow.Core**](https://www.nuget.org/packages/Microsoft.Diagnostics.EventFlow.Core/)

This filter adds additional metadata to all event data that satisfies the include expression. The filter recognizes a few standard properties (`type`, `metadata` and `include`); the rest are custom properties, specific for the given metadata type:
```jsonc
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
| `include` | logical expression | Yes | Specifies the logical expression that determines if the metadata is applied to the event data. For information about the logical expression, please see section [Logical Expressions](#logical-expressions). |
| *[others]* | string | No | Specifies custom properties that should be added along with this metadata object. When the event data is processed by other filters or outputs, these properties can be accessed. The names of these properties are custom-defined and the possible set is open-ended. For a particular filter, zero or more custom properties can be defined. In the example above, customTag1 and customTag2 are such properties. |

Here are a few examples of using the metadata filter:

1. Submit a metric with a value of 1 (a counter) whenever there is a Service Fabric stateful service run failure

    ```jsonc
    {
    "type": "metadata",
    "metadata": "metric",
    "include": "ProviderName==Microsoft-ServiceFabric-Services 
                && EventName==StatefulRunAsyncFailure",
    "metricName": "StatefulRunAsyncFailure",
    "metricValue": "1.0"
    }
    ```
    
2. Turn processor time performance counter into a metric

    ```jsonc
    {
      "type": "metadata",
      "metadata": "metric",
      "include": "ProviderName==EventFlow-PerformanceCounterInput && CounterCategory==Process 
                  && CounterName==\"% Processor Time\"",
      "metricName": "MyServiceProcessorTimePercent",
      "metricValueProperty": "Value"
    }
    ```
    
3. Turn a custom EventSource event into a request. The event has 3 interesting properties: requestTypeName indicates what kind of request it was; durationMsec has the total request processing duration and ‘isSuccess’ indicates whether the processing succeeded or failed

    ```jsonc
    {
      "type": "metadata",
      "metadata": "request",
      "include": "ProviderName==MyCompany-MyApplication-FrontEndService 
                  && EventName==ServiceRequestStop",
      "requestNameProperty": "requestTypeName",
      "durationProperty": "durationMsec",
      "isSuccessProperty": "isSuccess"
    }
    ```

### Standard metadata types

EventFlow core library defines several standard metadata types. They have pre-defined set of fields and are recognized by [Application Insights](#application-insights) and [Elasticsearch](#elasticsearch) outputs (see documentation for each output, respectively, to learn how they handle standard metadata).

**Metric metadata type**

Metrics are named time series of floating-point values. Metric metadata defines how metrics are derived from ordinary events. Following fields are supported:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `metadata` | "metric" | Yes | Indicates metric metadata definition; must be "metric". |
| `metricName` | string | (see Remarks) | The name of the metric. |
| `metricNameProperty` | string | (see Remarks) | The name of the event property that holds metric name. |
| `metricValue` | double | (see Remarks) | The value of the metric. This is useful for "counter" type of metric when each occurrence of a particular event should result in an increment of the counter. |
| `metricValueProperty` | string | (see Remarks) | The name of the event property that holds the metric value. |

Remarks: 
1. Either `metricName` or `metricNameProperty` must be specified.
2. Either `metricValue` or `metricValueProperty` must be specified.

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

**Dependency metadata type**

Dependency event represents the act of calling a service that your service depends on. It has the following properties:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `metadata` | "dependency" | Yes | Indicates dependency metadata definition; must be "dependency". |
| `isSuccessProperty` | string | No | The name of the event property that specifies whether the request ended successfully. It is expected that the event property is, or can be converted to a boolean. |
| `durationProperty` | string | No | The name of the event property that specifies the request duration (execution time). |
| `durationUnit` | "TimeSpan", "milliseconds", "seconds", "minutes" or "hours" | No | Specifies the type of data used by request duration property. If not set, it is assumed that request duration is expressed as a double value, representing milliseconds. |
| `responseCodeProperty` | string | No | The name of the event property that specifies response code associated with the request. A response code describes in more detail the outcome of the request. It is expected that the event property is, or can be converted to a string. |
| `targetProperty` | string | Yes | The name of the event property that specifies the target of the call, i.e. the identifier of the service that your service depends on. |
| `dependencyType` | string | No | An optional, user-defined designation of the dependency type. For example, it could be "SQL", "cache", "customer_data_service" or similar. |

**Exception metadata type**

Exception event corresponds to an occurrence of an unexpected exception. Usually a small amount of exceptions is continuously being thrown, caught and handled by a .NET process, this is normal and should not raise a concern. On the other hand, if an exception is unhandled, or unexpected, it needs to be logged and examined. This metadata is meant to cover the second case. It has the following properties:

| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `metadata` | "exception" | Yes | Indicates exception metadata definition; must be "exception". |
| `exceptionProperty` | string | Yes | The name of the event property that carries the (unexpected) exception object. Note that (for maximum information fidelity) the expected type of the event property is `System.Exception`. In other words, the actual exception is expected to be part of event data, and not just a stringified version of it. |

Also see [Application Insight Exception metadata type with EvenSource input issue](https://github.com/Azure/diagnostics-eventflow/issues/92)

### Health Reporter
Every software component can generate errors or warnings the developer should be aware of. The EventFlow library is no exception. An EventFlow health reporter reports errors and warnings generated by any components in the EventFlow pipeline.
In what format the report is presented depends on the implementation of the health reporter. The EventFlow library suite includes two health reporters: CsvHealthReporter and ServiceFabricHealthReporter. 

The CsvHealthReporter is the default health reporter for EventFlow library and is used if the health reporter configuration section is omitted. Its configuration parameters are described below.

ServiceFabricHealthReporter is described in the [Service Fabric support paragraph](#service-fabric-support). It is designed to be used in the context of Service Fabric applications and does not need any configuration.

#### CsvHealthReporter
*Nuget Package*: **Microsoft.Diagnostics.EventFlow.Core**

This health reporter writes all errors, warnings, and informational traces generated from the pipeline into a CSV file. Here is an example showing all possible settings:
```jsonc
"healthReporter": {
    "type": "CsvHealthReporter",
    "logFileFolder": ".",
    "logFilePrefix": "HealthReport",
    "minReportLevel": "Warning",
    "throttlingPeriodMsec": "1000",
    "singleLogFileMaximumSizeInMBytes": "8192",
    "logRetentionInDays": "30",
    "ensureOutputCanBeSaved": "false",
    "assumeSharedLogFolder": "false"
}
```
| Field | Values/Types | Required | Description |
| :---- | :-------------- | :------: | :---------- |
| `type` | "CsvHealthReporter" | Yes | Specifies the health reporter type. For this reporter, it must be "CsvHealthReporter". |
| `logFileFolder` | file path | No | Specifies a path for the CSV log file to be written. It can be an absolute path, or a relative path. If it's a relative path, then it's computed relative to the current working directory of the program. However, if it's an ASP.NET application, it's relative to the app_data folder. |
| `logFilePrefix` | file path | No | Specifies a prefix used for the CSV log file. CsvHealthReporter creates the log file name by combining this prefix with the date of when the file is generated. If the prefix is omitted, then a default prefix of "HealthReport" is used. |
| `minReportLevel` | Error, Warning, Message | No | Specifies the collection report level. Report traces with equal or higher severity than specified are collected. For example, if Warning is specified, then Error, and Warning traces are collected. Default is Error. |
| `throttlingPeriodMsec` | number of milliseconds | No | Specifies the throttling time period. This setting protects the health reporter from being overwhelmed, which can happen if a message is repeatedly generated due to an error in the pipeline. Default is 0, for no throttling. |
| `singleLogFileMaximumSizeInMBytes` | File size in MB/number | No | Specifies the size of the log file in MB before rotating happens. The default value is 8192 MB (8 GB). Once the size of log file exceeds the value, it will be renamed from fileName.csv to fileName_last.csv. Then logs will be written to a new fileName.csv. This setting prevents a single log file become too big. |
| `logRetentionInDays` | number of days for the logs files retain | No | Specifies how long log files will be retained. The default value is 30 days. Any log files created earlier than the specified number of days ago will be removed automatically. This prevents continuous generation of logs that might lead to storage exhaustion. |
| `ensureOutputCanBeSaved` | boolean | No | Specifies whether the health reporter is going to ensure the permission to write to the log folder. The default value is `false`. When set to `true`, it will prevent the pipeline creation when it can't write the log. Otherwise, it will ignore the error. |
| `assumeSharedLogFolder` | boolean | No | Specifies whether the health reporter will use a random string in attempt to make the health reporter file unique. This is useful if multiple processes using EventFlow use the same folder to store their health reporter files. Default value is `false`. |

CsvHealthReporter will try to open the log file for writing during initialization. If it can't, by default, a debug message will be output to the debugger viewer like Visual Studio Output window, etc. This can happen especially if a value for the log file path is not provided (default is used, which is application executables folder) and the application executables are residing on a read-only file system. Docker tools for Visual Studio use this configuration during debugging, so for containerized services the recommended practice is to specify the log file path explicitly.

### Pipeline Settings
The EventFlow configuration has settings allowing the application to adjust certain behaviors of the pipeline. These range from how many events the pipeline buffer, to the timeout the pipeline should use when waiting for an operation. If this section is omitted, the pipeline will use default settings.
Here is an example of all the possible settings:
```jsonc
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

The `ServiceFabricDiagnosticPipelineFactory` is a replacement for the standard `DiagnosticPipelineFactory`, one that uses Service Fabric configuration support to load pipeline configuration. The resulting pipeline reports any execution problems through the Service Fabric health subsystem. The factory exposes a static `Create()` method that takes the following parameters:

| Parameter | Default Value | Description |
| :-------- | :-------------- | :---------- |
| `healthEntityName` | (none) | The name of the health entity that will be used to report EventFlow pipeline health to Service Fabric. Usually it is set to a value that helps you identify the service using the pipeline, for example "MyApplication-MyService-DiagnosticsPipeline". |
| `configurationFileName` | "eventFlowConfig.json" | The name of the configuration file that contains pipeline configuration. The file is expected to be part of a (Service Fabric) service configuration package and is typically placed in `PackageRoot/Config` folder under the service project folder.|
| `configurationPackageName` | "Config" | The name of the Service Fabric configuration package that contains the pipeline configuration file.|

The recommended place to create the diagnostic pipeline is in the service `Main()` method. The following code will work all types of Service Fabric services (statefull, stateless and actor):

```csharp
public static void Main(string[] args)
{
    try
    {
        using (ManualResetEvent terminationEvent = new ManualResetEvent(initialState: false))
        using (var diagnosticsPipeline = ServiceFabricDiagnosticPipelineFactory.CreatePipeline("MyApplication-MyService-DiagnosticsPipeline"))
        {
            Console.CancelKeyPress += (sender, eventArgs) => Shutdown(diagnosticsPipeline, terminationEvent);

            AppDomain.CurrentDomain.UnhandledException += (sender, unhandledExceptionArgs) =>
            {
                ServiceEventSource.Current.UnhandledException(unhandledExceptionArgs.ExceptionObject?.ToString() ?? "(no exception information)");
                Shutdown(diagnosticsPipeline, terminationEvent);
            };

            ServiceRuntime.RegisterServiceAsync("MyServiceType", ctx => new MyService(ctx)).Wait();

            ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(MyService).Name);

            terminationEvent.WaitOne();
        }
    }
    catch (Exception e)
    {
        ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
        throw;
    }    
}

private static void Shutdown(IDisposable disposable, ManualResetEvent terminationEvent)
{
    try
    {
        disposable.Dispose();
    }
    finally
    {
        terminationEvent.Set();
    }
}
```

The purpose of handling `CancelKeyPress` and `UnhandledException` events (the latter for full .NET Framework only) is to ensure that the EventFlow pipeline is cleanly disposed. This is important for pipeline elements that rely on system-level resources. For example, Event Tracing for Windows (ETW) input creates a system-wide ETW listening session, which must be disposed when the EventFlow pipeline is shut down. Ctrl-C signal is the standard way the Service Fabric runtime uses to notify service processes that they need to perform cleanup and exit. By default the process has 30 seconds to react.

The UnhandledException event method is a very simple addition to the standard ServiceEventSource:

```csharp
    [Event(UnhandledExceptionEventId, Level = EventLevel.Error, Message = "An unhandled exception has occurred")]
    public void UnhandledException(string exception)
    {
       WriteEvent(UnhandledExceptionEventId, exception);
    }
```

Depending on the type of inputs and outputs used, additional startup code may be necessary. For example [Microsoft.Extensions.Logging](#microsoftextensionslogging) input requires a call to `LoggerFactory.AddEventFlow()` method to register EventFlow logger provider.


### Support for Service Fabric settings and application parameters
Version 1.0.1 of the EventFlow Service Fabric NuGet package introduced the ability to refer to Service Fabric settings from EventFlow configuration using special syntax for values:

`servicefabric:/<section-name>/<setting-name>`

where `<section-name>` is the name of Service Fabric configuration section and `<setting-name>` is the name of the Service Fabric configuration setting that is providing the value for some EventFlow setting. For example:

`"basicAuthenticationUserPassword": "servicefabric:/DiagnosticPipelineParameters/ElasticSearchUserPassword"`

The EventFlow configuration entry above means "take the ElasticSearchUserPassword setting from DiagnosticPipelineParameters section of the Service Fabric service configuration and use its value as the value for the EventFlow basicAuthenticationUserPassword setting". As with other Service Fabric settings, the values can also be overriden by Service Fabric application parameters. For more information on this see [Manage application parameters for multiple environments](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-manage-multiple-environment-app-configuration) topic in Service Fabric documentation.

Version 1.1.2 added support for resolving paths to other configuration files that are part of the default service configuration package. The syntax is:

`servicefabricfile:/<configuration-file-name>`

At run time this value will be substituted with a full path to the configuration file with the given name. This is especially useful if an EventFlow pipeline element wraps an existing library that has its own configuration file format (as is the case with Application Insights, for example). 

### Using Application Insights ServerTelemetryChannel
For Service Fabric applications running in production, we recommend to use Application Insights `ServerTelemetryChannel`. This channel [has the capability to store data on disk during periods of intermittent connectivity](https://docs.microsoft.com/en-us/azure/azure-monitor/app/telemetry-channels) and is a better choice for long-running server processes than the default in-memory channel.

To use the `ServerMemoryChannel` you need to create an EventFlow configuration file and an Application Insights configuration file in the same Service Fabric configuration package:

*eventFlowConfig.json*
```jsonc
{
  "inputs": [
    {
      "type": "EventSource",
      "sources": [
        { "providerName": "Microsoft-ServiceFabric-Services" },
        // Other event sources, as necessary...
      ]
    }
  ],
  "filters": [
    // Filters, as necessary
  ],
  "outputs": [
    {
      "type": "ApplicationInsights",      
      "configurationFilePath":  "servicefabricfile:/ApplicationInsights.config"
    }
  ]
}
```

*ApplicationInsights.config*
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ApplicationInsights xmlns="http://schemas.microsoft.com/ApplicationInsights/2013/Settings">

  <TelemetryModules>    
  <!-- Telemetry module configuration, as necessary -->
  </TelemetryModules>

  <TelemetryProcessors>
  <!-- Telemetry processor configuration, as necessary -->    
  </TelemetryProcessors> 

  <TelemetryChannel Type="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel, Microsoft.AI.ServerTelemetryChannel"/>

</ApplicationInsights>
```

Ensure that your service consumes `Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel` NuGet package.

## Logical Expressions
The logical expression allows you to filter events based on the event properties. For example, you can have an expression like `ProviderName == MyEventProvider && EventId == 3`, where you specify the event property name on the left side and the value to compare on the right side. If the value on the right side contains special characters, you can enclose it in double quotes.

Standard and custom (payload) properties are treated equally; there is no need to prefix custom properties in any way. For example, expression `TenantId != Unknown && ProviderName == MyEventProvider` will evaluate to true for events that 
  - were created by `MyEventProvider` provider (ProviderName is a standard property available for all events), and
  - have a TenantId custom property, with value that is different from "Unknown" string.

The following table lists operators supported by logical expressions

| Class | Operators | Description |
| :---- | :-------- | :---------------- |
| Comparison | `==`, `!=`, `<`, `>`, `<=`, `>=` | Evaluates to true if the property (left side of the expression)  successfully compares to the value (right side of the expression). |
| Property presence and absence | `hasproperty` `hasnoproperty` | `hasproperty transactionId` will only allow events that have a property named "`transactionId`". <br/>`hasnoproperty transactionId` does the opposite: it will only allow events that *do not* have a property named "`transactionId`". |
| Bitwise Equality | `&==` | Evaluates to true if bit mask is set, i.e., `(lhsValue & rhsValue) == rhsValue`. This is useful to filter on properties like `Keywords`. |
| Regular Expression | `~=` | Evaluates to true if the property value matches a regular expression pattern on the right. |
| Logical | `&&`, `||`, `!` | Enables building complex logical expressions out of simpler ones. <br/> The precedence is `!` > `&&` > `||` (highest to lowest).
| Grouping | `(expression)` | Grouping can be used to change the evaluation order of expressions with logical operators. |

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

## Extensibility
Every pipeline element type (input, filter, output and health reporter) is a point of extensibility, that is, custom elements of these types can be used inside the pipeline. Contracts for all EventFlow element types are provided by [EventFlow.Core assembly](https://github.com/Azure/diagnostics-eventflow/tree/master/src/Microsoft.Diagnostics.EventFlow.Core/Interfaces) (except from the input type, see below).

### EventData class
EventFlow pipelines operate on [EventData objects](https://github.com/Azure/diagnostics-eventflow/blob/master/src/Microsoft.Diagnostics.EventFlow.Core/Implementations/EventData.cs). Each EventData object represents a single telemetry record (event) and has the following public properties and methods:

| Name | Type | Description |
| :------------ | :---- | :---------------- |
| `Timestamp` | `DateTimeOffset` | Indicates time when the event was created. |
| `ProviderName` | `string` | Identifies the source of the event. |
| `Level` | `LogLevel` (enumeration) | Provides basic severity assesment of the event: is it a critical error, regular error, a warning, etc. |
| `Keywords` | `long` | Provides means to efficiently classify events. The field is supposed to be interpreted as a set of bits (64 bits available). The meaning of each bit is specific to the event provider; EventFlow does not interpret them in any way. |
| `Payload` | `IDictionary<string, object>` | Stores event properties |
| `TryGetMetadata(string kind, out IReadOnlyCollection<EventMetadata> metadata)` | bool | Retrieves metadata of a given kind (if any) from the event. Returns true if metadata of given kind was found, otherwise false. |
| `SetMetadata(EventMetatada metadata)` | void | Adds (attaches) a new piece of metadata to the event. |
| `GetValueFromPayload<T>(string name, ProcessPayload<T> handler)` | bool | Retrieves a payload value from the payload (set of event properties). Although the payload can be accessed directly via `Payload` property, this method is useful because it will check whether the property exists and perform basic type conversion as necessary. |
| `AddPayloadProperty(string key, object value, IHealthReporter healthReporter, string context)` | void | Adds a new payload property to the event, performing property name disambiguation as necessary. The `healthReporter` and `context` parameters are used to produce a warning in case the name disambiguation _is_ necessary (had to be changed because the event already had a property with name equal to `key` parameter). |
| `DeepClone()` | `EventData` | Performs a deep clone operation on the EventData. The resulting copy is independent from the original and either can be modified without affecting the other (e.g. properties or metadata can be added or removed). The only exception is that _payload values_ are not cloned (and thus are shared between the copies). |

Note that EventData type is not thread-safe. Don't try to use it concurrently from multiple threads. 

### EventFlow pipeline element types
#### Inputs
Inputs are producing new events (`EventData` instances). Anything that implements `IObservable<EventData>` can be used as an input for EventFlow. `IObservable<T>` is a standard .NET interface in the System namespace.
#### Filters
Filters have a dual role: they modify events (e.g. by decorating them with metadata) and instruct EventFlow to keep or discard the event. They are expected to implement the `IFilter` interface:
```csharp
public enum FilterResult
{
    KeepEvent = 0,
    DiscardEvent = 1
}
public interface IFilter
{
    FilterResult Evaluate(EventData eventData);
}
```
#### Outputs
Output's purpose is to send data to its final destination. It is expected to implement `IOutput` interface:
```csharp
public interface IOutput
{
    Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken);
}
```
The output receives a batch of events, along with transmission sequence number and a cancellation token. The transmission sequence number can be treated as an identifier for the `SendEventsAsync()` method invocation; it is guaranteed to be unique for each invocation, but there is no guarantee that there will be no "gaps", nor that it will be strictly increasing. The cancellation token should be used to cancel long-running operations if necessary; typically it is passed as a parameter to asynchronous i/o operations.

### Pipeline structure and threading considerations
Every EventFlow pipeline can be created imperatively, i.e. in the program code. The structure of every pipeline is reflected in the constructor of the `DiagnosticPipeline` class and is as follows:

1. One or more inputs produce events.
2. A set of common filters (also known as 'global' filters) does initial filtering.
3. Then the events are sent to one or more outputs (by cloning them as necessary). Each output can have its own set of filters. A combination of an output and a set of filters associated with that output is called a 'sink'.

EventFlow employs the following policies with regards to concurrency:

1. Inputs are free to call OnNext() on associated observers using any thread.
2. EventFlow will ensure that only one filter at a time is evaluating any given EventData object. That said, the same filter can be invoked concurrently for different events.
3. Outputs will invoked concurrently for different batches of data. 

### Using custom pipeline items imperatively
The simplest way to use custom EventFlow elements is to create a pipeline imperatively, in code. You just need to create a read-only collections of the inputs, global filters and sinks and pass them to `DiagnosticPipeline` constructor. Custom and standard elements can be combined freely; each of the standard pipeline elements has a public constructor and associated public configuration class and can be created imperatively.

### Creating a pipeline with custom elements using configuration
To create an EventFlow pipeline with custom elements from configuration each custom element needs a factory. The factory is an object implementing `IPipelineItemFactory<TPipelineItem>` and is expected to have a parameter-less constructor.

The factory's `CreateItem(IConfiguration configuration, IHealthReporter healthReporter)` method will receive a configuration fragment that represents the pipeline item being created. The health reporter is available to report issues in case configuration is corrupt or some other problem occurrs during item creation. The health reporter can also be passed to and used by the created pipeline item.

For EventFlow to know about the item factory it must appear in the 'extensions' section of the configuration file. Each extension record has 3 properties:

1. "category" identifies extension type. Currently types recognized by DiagnosticPipelineFactory are inputFactory, filterFactory, outputFactory or healthReporter. 
2. "type" is the tag that identifies the extension in other parts of the configuration document(s). It is totally up to the user of an extension what she uses here.
3. "qualifiedTypeName" is the string that allows DiagnosticPipelineFactory to instantiate the extension factory

Here is a very simple example that illustrates how to create a custom output and instantiate it from a configuration file.

```csharp
namespace EventFlowCustomOutput
{
    class Program
    {
        static void Main(string[] args)
        {
            using (DiagnosticPipelineFactory.CreatePipeline("eventFlowConfig.json"))
            {
                Trace.TraceError("Hello, world!");
                Thread.Sleep(1000);
            }
        }
    }

    class CustomOutput : IOutput
    {
        public Task SendEventsAsync(IReadOnlyCollection<EventData> events, long transmissionSequenceNumber, CancellationToken cancellationToken)
        {
            foreach(var e in events)
            {
                Console.WriteLine($"CustomOutput: event says '{e.Payload["Message"]?.ToString() ?? "nothing"}'");
            }
            return Task.CompletedTask;
        }
    }

    class CustomOutputFactory : IPipelineItemFactory<CustomOutput>
    {
        public CustomOutput CreateItem(IConfiguration configuration, IHealthReporter healthReporter)
        {
            return new CustomOutput();
        }
    }
}
```

(content of eventFlowConfig.json)

```jsonc
{
  "inputs": [
    {
      "type": "Trace"
    }
  ],
  "filters": [
  ],
  "outputs": [
    {
      "type":  "CustomOutput"
    }
  ],
  "schemaVersion": "2016-08-11",

  "extensions": [
    {
      "category": "outputFactory",
      "type": "CustomOutput",
      "qualifiedTypeName": "EventFlowCustomOutput.CustomOutputFactory, EventFlowCustomOutput"
    }
  ]
}
```

## Platform Support
EventFlow supports full .NET Framework (.NET 4.5 series and 4.6 series) and .NET Core, but not all inputs and outputs are supported on all platforms. 
The following table lists platform support for standard inputs and outputs.  

| Input Name | .NET 4.5.1 | .NET 4.6 | .NET Core |
| :------------ | :---- | :---- | :---- |
| *Inputs* |
| [System.Diagnostics.Trace](#trace) | Yes | Yes | Yes |
| [EventSource](#eventsource) | No | Yes | Yes |
| [PerformanceCounter](#performancecounter) | Yes | Yes | No |
| [Serilog](#serilog) | Yes | Yes | Yes |
| [Microsoft.Extensions.Logging](#microsoftextensionslogging) | Yes | Yes | Yes |
| [ETW (Event Tracing for Windows)](#etw-event-tracing-for-windows) | Yes | Yes | No |
| [Application Insights input](#application-insights-input) | Yes | Yes | Yes |
| [Log4net](#Log4net) | Yes | Yes | Yes |
| [NLog](#NLog) | Yes | Yes | Yes |
| *Outputs* |
| [StdOutput (console output)](#stdoutput) | Yes | Yes | Yes |
| [Application Insights](#application-insights) | Yes | Yes | Yes |
| [Azure EventHub](#event-hub) | Yes | Yes | Yes |
| [Elasticsearch](#elasticsearch) | Yes | Yes | Yes |
| [Azure Monitor Logs](#azure-monitor-logs) | Yes | Yes | Yes |
[HTTP (json via http)](#http) | Yes | Yes | Yes |

## Contributions
Refer to [contribution guide](contributing.md).

## Code of Conduct
Refer to [Code of Conduct guide](CODE_OF_CONDUCT.md).
