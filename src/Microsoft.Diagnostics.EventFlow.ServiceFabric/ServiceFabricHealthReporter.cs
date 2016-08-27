// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Fabric;
using System.Fabric.Health;

namespace Microsoft.Diagnostics.EventFlow.ServiceFabric
{
    public class ServiceFabricHealthReporter : IHealthReporter
    {
        private FabricClient fabricClient;
        private Uri applicatioName;
        private string serviceManifestName;
        private string nodeName;
        private string entityIdentifier;
        private HealthState problemHealthState;

        public ServiceFabricHealthReporter(string entityIdentifier, HealthState problemHealthState = HealthState.Warning)
        {
            if (string.IsNullOrWhiteSpace(entityIdentifier))
            {
                throw new ArgumentException("entityIdentifier cannot be null or empty", "entityIdentifier");
            }
            this.entityIdentifier = entityIdentifier;

            this.problemHealthState = problemHealthState;

            this.fabricClient = new FabricClient(
                new FabricClientSettings()
                {
                    HealthReportSendInterval = TimeSpan.FromSeconds(5)
                }
                );

            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            this.applicatioName = new Uri(activationContext.ApplicationName);
            this.serviceManifestName = activationContext.GetServiceManifestName();
            NodeContext nodeContext = FabricRuntime.GetNodeContext();
            this.nodeName = nodeContext.NodeName;
        }

        public void ReportHealthy(string description = "Healthy", string context = null)
        {
            ReportMessage(HealthState.Ok, description);
        }

        public void ReportProblem(string description, string context = null)
        {
            ReportMessage(HealthState.Error, description);
        }

        public void ReportWarning(string description, string context = null)
        {
            ReportMessage(HealthState.Warning, description);
        }

        private void ReportMessage(HealthState healthState, string description)
        {
            HealthInformation healthInformation = new HealthInformation(this.entityIdentifier, "Connectivity", healthState);
            healthInformation.Description = description;

            DeployedServicePackageHealthReport healthReport = new DeployedServicePackageHealthReport(
                this.applicatioName,
                this.serviceManifestName,
                this.nodeName,
                healthInformation);

            this.fabricClient.HealthManager.ReportHealth(healthReport);
        }

        public void Dispose()
        {
            // Recycle resource when necessary.
        }
    }
}