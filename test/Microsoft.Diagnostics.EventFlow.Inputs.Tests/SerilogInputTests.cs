﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Diagnostics.EventFlow.Metadata;
using Moq;
using Serilog;
using Xunit;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.TestHelpers;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class SerilogInputTests
    {
        [Fact]
        public void ReportsSimpleInformation()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                string message = "Just an information";
                logger.Information(message);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data => 
                       data.Payload["Message"].Equals(message) 
                    && data.Level == LogLevel.Informational
                )));
            }
        }

        [Fact]
        public void ReportsInformationWithCustomProperties()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                // Render the string value without quotes; render the double value with fixed decimal point, 1 digit after decimal point
                string message = "{alpha:l}{bravo:f1}{charlie}";
                logger.Information(message, "aaa", 75.5, false);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("aaa75.5" + Boolean.FalseString)
                    && data.Level == LogLevel.Informational
                    && data.Payload["alpha"].Equals("aaa")
                    && data.Payload["bravo"].Equals(75.5)
                    && data.Payload["charlie"].Equals(false)
                )));
            }
        }

        [Fact]
        public void ReportsExceptions()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                Exception e = new Exception();
                e.Data["ID"] = 23;
                string message = "Something bad happened but we do not care that much";
                logger.Information(e, message);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals(message)
                    && data.Level == LogLevel.Informational
                    && (data.Payload["Exception"] as Exception).Data["ID"].Equals(23)
                )));
            }
        }

        [Fact]
        public void ReportsError() {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object)) {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                Exception e = new Exception();
                e.Data["ID"] = 23;
                string message = "Something bad happened but we do not care that much";
                logger.Error(e, message);


                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                                                                   data.Payload["Message"].Equals(message)
                                                                   && ContainsException(data, e)
                                                                   && data.Level == LogLevel.Error
                                                                   && (data.Payload["Exception"] as Exception).Data["ID"].Equals(23)
                                              )));
            }
        }

        private bool ContainsException(EventData data, Exception expectedException) {
            IReadOnlyCollection<EventMetadata> metaData;
            data.TryGetMetadata(ExceptionData.ExceptionMetadataKind, out metaData);
            foreach (EventMetadata eventMetadata in metaData) {
                Exception exception = (Exception)data.Payload[eventMetadata.Properties[ExceptionData.ExceptionPropertyMoniker]];
                return exception == expectedException;
            }
            return false;
        }

        [Fact]
        public void ReportsMessageTemplate()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                string messageTemplate = "Hello, {Name}!";
                logger.Information(messageTemplate, "World");

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("Hello, \"World\"!")
                    && data.Level == LogLevel.Informational
                    && data.Payload["MessageTemplate"].Equals(messageTemplate)
                )));
            }
        }

        [Fact]
        public void ReportsLevelsProperly()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Sink(serilogInput)
                    .CreateLogger();

                logger.Information("Info");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Informational)));
                observer.ResetCalls();

                logger.Debug("Debug");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));
                observer.ResetCalls();

                logger.Verbose("Verbose");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Verbose)));
                observer.ResetCalls();

                logger.Warning("Warning");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Warning)));
                observer.ResetCalls();

                logger.Error("Error");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Error)));
                observer.ResetCalls();

                logger.Fatal("Fatal");
                observer.Verify(s => s.OnNext(It.Is<EventData>(data => data.Level == LogLevel.Critical)));
                observer.ResetCalls();
            }
        }

        [Fact]
        public void HandlesDuplicatePropertyNames()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                Exception e = new Exception("Whoa!");
                const string message = "I say: {Message} and you pay attention, no {Exception:l}";
                logger.Warning(e, message, "Keyser Söze", "excuses");                

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                       data.Payload["Message"].Equals("I say: \"Keyser Söze\" and you pay attention, no excuses")
                    && data.Payload["Exception"].Equals(e)
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("Message") && !key.EndsWith("Template") && key != "Message")].Equals("Keyser Söze")
                    && data.Payload[data.Payload.Keys.First(key => key.StartsWith("Exception") && key != "Exception")].Equals("excuses")
                )), Times.Exactly(1));

                healthReporterMock.Verify(o => o.ReportWarning(
                        It.Is<string>(s => s.Contains("already exist in the event payload")), 
                        It.Is<string>(s => s == nameof(SerilogInput))), 
                    Times.Exactly(2));
            }
        }

        [Fact]
        public void RepresentsStructuresAsRawDictionaries()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                var structure = new { A = "alpha", B = "bravo" };
                logger.Information("Here is {@AStructure}", structure);

                observer.Verify(s => s.OnNext(It.Is<EventData>(data =>
                    ((IDictionary<string, object>)data.Payload["AStructure"])["A"].Equals("alpha") &&
                    ((IDictionary<string, object>)data.Payload["AStructure"])["B"].Equals("bravo")
                )));
            }
        }

        [Fact]
        public void ConfigurationCanFindAndWriteToAnInputWithinAPipeline()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var mockOutput = new Mock<IOutput>();
            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (var pipeline = new DiagnosticPipeline(
                    healthReporterMock.Object,
                    new[] { serilogInput },
                    new IFilter[0],
                    new[] { new EventSink(mockOutput.Object, new IFilter[0]) }))
            {
                var logger = new LoggerConfiguration()
                    .WriteTo.EventFlow(pipeline)
                    .CreateLogger();

                logger.Information("Hello, world!");
            }
            mockOutput.Verify(output => output.SendEventsAsync(It.Is<IReadOnlyCollection<EventData>>(c => c.Count == 1),
                It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Fact]
        public void DestructureDepthEqualsOne()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            IDictionary<string, object> spiedPayload = null;
            observer.Setup(p => p.OnNext(It.IsAny<EventData>()))
                .Callback<EventData>((p) => { spiedPayload = p.Payload; });

            using (var serilogInput = new SerilogInput(healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                var structure = new { A = "alpha", B = "bravo", C = new { Y = "yankee", Z = "zulu" } };
                logger.Information("Here is {@AStructure}", structure);
            }
            Assert.Equal("alpha", ((IDictionary<string, object>)spiedPayload["AStructure"])["A"]);
            Assert.Equal("bravo", ((IDictionary<string, object>)spiedPayload["AStructure"])["B"]);
            Assert.Equal("{ Y: \"yankee\", Z: \"zulu\" }", ((IDictionary<string, object>)spiedPayload["AStructure"])["C"]);
        }

        [Fact]
        public void DestructureDepthGreaterThanOne()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            IDictionary<string, object> spiedPayload = null;
            observer.Setup(p => p.OnNext(It.IsAny<EventData>()))
                .Callback<EventData>((p) => { spiedPayload = p.Payload; });
            var inMemorySettings = new Dictionary<string, string>
            {
                {nameof(SerilogInputConfiguration.IgnoreSerilogDepthLevel), "false"},
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            using (var serilogInput = new SerilogInput(configuration, healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                var structure = new { A = "alpha", B = "bravo", C = new { Y = "yankee", Z = "zulu" } };
                logger.Information("Here is {@AStructure}", structure);
            }
            Assert.Equal("alpha", ((IDictionary<string, object>)spiedPayload["AStructure"])["A"]);
            Assert.Equal("bravo", ((IDictionary<string, object>)spiedPayload["AStructure"])["B"]);
            Assert.Equal("yankee", ((IDictionary<string, object>)((IDictionary<string, object>)spiedPayload["AStructure"])["C"])["Y"]);
            Assert.Equal("zulu", ((IDictionary<string, object>)((IDictionary<string, object>)spiedPayload["AStructure"])["C"])["Z"]);
        }

        [Fact]
        public void CanReadConfigurationFromJson()
        {
            var healthReporterMock = new Mock<IHealthReporter>();

            using (var configFile = new TemporaryFile())
            {
                string inputConfiguration = @"
                {
                    ""type"": ""Serilog""
                }";
                configFile.Write(inputConfiguration);
                var cb = new ConfigurationBuilder();
                cb.AddJsonFile(configFile.FilePath);
                var configuration = cb.Build();
                
                var input = new SerilogInput(configuration, healthReporterMock.Object);

                Assert.True(input.inputConfiguration.IgnoreSerilogDepthLevel);
            }

            using (var configFile = new TemporaryFile())
            {
                string inputConfiguration = @"
                {
                    ""type"": ""Serilog"",
                    ""ignoreSerilogDepthLevel"": ""true""
                }";
                configFile.Write(inputConfiguration);
                var cb = new ConfigurationBuilder();
                cb.AddJsonFile(configFile.FilePath);
                var configuration = cb.Build();

                var input = new SerilogInput(configuration, healthReporterMock.Object);

                Assert.True(input.inputConfiguration.IgnoreSerilogDepthLevel);
            }

            using (var configFile = new TemporaryFile())
            {
                string inputConfiguration = @"
                {
                    ""type"": ""Serilog"",
                    ""ignoreSerilogDepthLevel"": ""false""
                }";
                configFile.Write(inputConfiguration);
                var cb = new ConfigurationBuilder();
                cb.AddJsonFile(configFile.FilePath);
                var configuration = cb.Build();

                var input = new SerilogInput(configuration, healthReporterMock.Object);

                Assert.False(input.inputConfiguration.IgnoreSerilogDepthLevel);
            }
        }

        [Fact]
        public void UsesSerilogMaxDestructuringDepth()
        {

        }

        [Fact]
        public void CanSerializeCircularObjectGraphs()
        {

        }

        private class EntityWithChildren
        {
            public string Name { get; set; }
            public List<EntityWithChildren> Children { get; } = new List<EntityWithChildren>();
        }
    }
}
