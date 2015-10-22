using Microsoft.ServiceFabric.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace AirTrafficControl.Web
{
    [EventSource(Name = "MyCompany-AirTrafficControlApplication-AirTrafficControlWeb")]
    internal sealed class ServiceEventSource : EventSource
    {
        public static ServiceEventSource Current = new ServiceEventSource();

        static ServiceEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            Task.Run(() => { }).Wait();
        }

        public const long InvalidExecutionId = -1;

        private long serviceExecutionId = InvalidExecutionId;

        // Constructor is private to enforce singleton semantics
        private ServiceEventSource() : base() { }

        [NonEvent]
        public void Message(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Message(finalMessage, this.serviceExecutionId);
            }
        }

        // For very high-frequency events it might be advantageous to raise events using WriteEventCore API.
        // This results in more efficient parameter handling, but requires explicit allocation of EventData structure and unsafe code.
        // To enable this code path, define UNSAFE conditional compilation symbol and turn on unsafe code support in project properties.
        private const int MessageEventId = 1;
        [Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
        private
#if UNSAFE
        unsafe
#endif
        void Message(string message, long serviceExecutionId)
        {
#if !UNSAFE
            WriteEvent(MessageEventId, message, serviceExecutionId);
#else
            const int numArgs = 2;
            fixed (char* pMessage = message)
            {
                EventData* eventData = stackalloc EventData[numArgs];
                eventData[0] = new EventData { DataPointer = (IntPtr) pMessage, Size = SizeInBytes(message) };
                eventData[1] = new EventData { DataPointer = (IntPtr) (&serviceExecutionId), Size = sizeof(long) };

                WriteEventCore(MessageEventId, numArgs, eventData);
            }
#endif
        }

        [Event(3, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}")]
        public void ServiceTypeRegistered(int hostProcessId, string serviceType)
        {
            WriteEvent(3, hostProcessId, serviceType);
        }

        [NonEvent]
        public void ServiceHostInitializationFailed(Exception e)
        {
            ServiceHostInitializationFailed(e.ToString());
        }

        [Event(4, Level = EventLevel.Error, Message = "Service host initialization failed")]
        private void ServiceHostInitializationFailed(string exception)
        {
            WriteEvent(4, exception);
        }

        [Event(5, Level =EventLevel.Error, Message = "Unexpected error from {0} REST API")]
        public void RestApiFrontEndError(string apiName, string exception)
        {
            WriteEvent(5, apiName, exception);
        }


        [NonEvent]
        public void RestApiOperationStart(StatelessServiceInitializationParameters serviceInitializationParameters, [CallerMemberName] string operationName="")
        {
            if (this.IsEnabled())
            {
                
                RestApiOperationStart(
                    serviceInitializationParameters.ServiceName.ToString(),
                    serviceInitializationParameters.ServiceTypeName,
                    serviceInitializationParameters.InstanceId,
                    serviceInitializationParameters.PartitionId,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationName,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationTypeName,
                    FabricRuntime.GetNodeContext().NodeName,
                    operationName);
            }
        }

        [NonEvent]
        public void RestApiOperationStart(StatefulServiceInitializationParameters serviceInitializationParameters, [CallerMemberName] string operationName = "")
        {
            if (this.IsEnabled())
            {
                RestApiOperationStart(
                    serviceInitializationParameters.ServiceName.ToString(),
                    serviceInitializationParameters.ServiceTypeName,
                    serviceInitializationParameters.ReplicaId,
                    serviceInitializationParameters.PartitionId,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationName,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationTypeName,
                    FabricRuntime.GetNodeContext().NodeName,
                    operationName);
            }
        }

        [Event(6, Level = EventLevel.Informational, Message = "REST operation {7} started")]
        private void RestApiOperationStart(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string operationName)
        {
            if (this.IsEnabled())
            {
                WriteEvent(6, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, operationName);
            }
        }

        [Event(7, Level = EventLevel.Informational, Message = "REST operation {0} ended")]
        public void RestApiOperationStop([CallerMemberName] string operationName = "")
        {
            if (this.IsEnabled())
            {
                WriteEvent(7, operationName);
            }
        }

        [Event(8, Level = EventLevel.Error, Message = "Unexpected Nancy main module initialization error. The service won't be able to process any requests.")]
        public void RestApiInitializationError(string exception)
        {
            WriteEvent(8, exception);
        }

        [NonEvent]
        public void ServiceExecutionStarted(StatelessServiceInitializationParameters serviceInitializationParameters)
        {
            if (this.IsEnabled())
            {
                GenerateExecutionId();

                ServiceExecutionStarted(
                    serviceInitializationParameters.ServiceName.ToString(),
                    serviceInitializationParameters.ServiceTypeName,
                    serviceInitializationParameters.InstanceId,
                    serviceInitializationParameters.PartitionId,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationName,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationTypeName,
                    FabricRuntime.GetNodeContext().NodeName,
                    this.serviceExecutionId);
            }
        }

        [NonEvent]
        public void ServiceExecutionStarted(StatefulServiceInitializationParameters serviceInitializationParameters)
        {
            if (this.IsEnabled())
            {
                GenerateExecutionId();

                ServiceExecutionStarted(
                    serviceInitializationParameters.ServiceName.ToString(),
                    serviceInitializationParameters.ServiceTypeName,
                    serviceInitializationParameters.ReplicaId,
                    serviceInitializationParameters.PartitionId,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationName,
                    serviceInitializationParameters.CodePackageActivationContext.ApplicationTypeName,
                    FabricRuntime.GetNodeContext().NodeName,
                    this.serviceExecutionId);
            }
        }

        private const int ServiceExecutionStartedEventId = 9;
        [Event(ServiceExecutionStartedEventId, Level = EventLevel.Informational, Message = "Service execution started (execution id: {7})")]
        private void ServiceExecutionStarted(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            long serviceExecutionId)
        {
            WriteEvent(ServiceExecutionStartedEventId, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, serviceExecutionId);
        }

        #region Private methods
        [NonEvent]
        private void GenerateExecutionId()
        {
            while (this.serviceExecutionId == InvalidExecutionId)
            {
                var bytes = new byte[sizeof(Int64)];
                RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();
                Gen.GetBytes(bytes);
                this.serviceExecutionId = BitConverter.ToInt64(bytes, 0);
            }
        }

#if UNSAFE
        private int SizeInBytes(string s)
        {
            if (s == null)
            {
                return 0;
            }
            else
            {
                return (s.Length + 1) * sizeof(char);
            }
        }
#endif
        #endregion
    }
}
