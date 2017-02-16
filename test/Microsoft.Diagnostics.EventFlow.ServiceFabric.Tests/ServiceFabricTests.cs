using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.EventFlow.ServiceFabric.Tests
{
    public class ServiceFabricTests
    {
        [Fact]
        public void ConfigurationIsNotChangedIfNoValueReferencesExist()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var configurationSource = new Dictionary<string, string>()
            {
                ["alpha"] = "Alpha",
                ["bravo:charlie"] = "BravoCharlie"
            };

            IConfigurationRoot configuration = (new ConfigurationBuilder()).AddInMemoryCollection(configurationSource).Build();
            ServiceFabricDiagnosticPipelineFactory.ApplyFabricConfigurationOverrides(configuration, healthReporterMock.Object);

            string verificationError;
            bool isOK = VerifyConfguration(configuration.AsEnumerable(), configurationSource, out verificationError);
            Assert.True(isOK, verificationError);
            healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void ConfigurationIsNotChangedIfValueReferenceNotResolved()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var configurationSource = new Dictionary<string, string>()
            {
                ["alpha"] = "Alpha",
                ["bravo:charlie"] = "BravoCharlie",
                ["delta"] = "servicefabric:/bravo/foxtrot"
            };

            IConfigurationRoot configuration = (new ConfigurationBuilder()).AddInMemoryCollection(configurationSource).Build();
            ServiceFabricDiagnosticPipelineFactory.ApplyFabricConfigurationOverrides(configuration, healthReporterMock.Object);

            string verificationError;
            bool isOK = VerifyConfguration(configuration.AsEnumerable(), configurationSource, out verificationError);
            Assert.True(isOK, verificationError);

            healthReporterMock.Verify(o => o.ReportWarning(
                        It.Is<string>(s => s.Contains("no corresponding configuration value was found")),
                        It.Is<string>(s => s == EventFlowContextIdentifiers.Configuration)),
                    Times.Exactly(1));
        }

        [Fact]
        public void ConfigurationUpdatedWithValueReferences()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var configurationSource = new Dictionary<string, string>()
            {
                ["alpha"] = "Alpha",
                ["bravo:charlie"] = "BravoCharlie",
                ["delta"] = "servicefabric:/bravo/charlie"
            };

            IConfigurationRoot configuration = (new ConfigurationBuilder()).AddInMemoryCollection(configurationSource).Build();
            ServiceFabricDiagnosticPipelineFactory.ApplyFabricConfigurationOverrides(configuration, healthReporterMock.Object);
            healthReporterMock.Verify(o => o.ReportProblem(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            healthReporterMock.Verify(o => o.ReportWarning(It.IsAny<string>(), It.IsAny<string>()), Times.Never());

            string verificationError;
            bool isOK = VerifyConfguration(configuration.AsEnumerable(), configurationSource, out verificationError);
            Assert.False(isOK, verificationError);

            configurationSource["delta"] = "BravoCharlie";
            isOK = VerifyConfguration(configuration.AsEnumerable(), configurationSource, out verificationError);
            Assert.True(isOK, verificationError);
        }

        private bool VerifyConfguration<TKey, TValue>(
            IEnumerable<KeyValuePair<TKey, TValue>> configuration,
            IDictionary<TKey, TValue> expected,
            out string verificationError,
            IEqualityComparer<TValue> valueComparer = null)
        {
            verificationError = string.Empty;

            var keyComparer = EqualityComparer<TKey>.Default;
            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;

            foreach (var kvp in expected)
            {
                KeyValuePair<TKey, TValue>? correspondingPair = configuration.Where(otherKvp => keyComparer.Equals(otherKvp.Key, kvp.Key))
                    .Cast<KeyValuePair<TKey, TValue>?>()
                    .FirstOrDefault();

                if (correspondingPair == null)
                {
                    verificationError = $"Configuration is missing expected key '{kvp.Key}'";
                    return false;
                }

                if (!valueComparer.Equals(kvp.Value, correspondingPair.Value.Value))
                {
                    verificationError = $"The value for key '{kvp.Key}' was expected to be '{kvp.Value}' but instead it is '{correspondingPair.Value.Value}')";
                    return false;
                }
            }
            return true;
        }
    }
}
