// ------------------------------------------------------------
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
                {nameof(SerilogInputConfiguration.UseSerilogDepthLevel), "true"},
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

                Assert.False(input.inputConfiguration.UseSerilogDepthLevel);
            }

            using (var configFile = new TemporaryFile())
            {
                string inputConfiguration = @"
                {
                    ""type"": ""Serilog"",
                    ""useSerilogDepthLevel"": ""false""
                }";
                configFile.Write(inputConfiguration);
                var cb = new ConfigurationBuilder();
                cb.AddJsonFile(configFile.FilePath);
                var configuration = cb.Build();

                var input = new SerilogInput(configuration, healthReporterMock.Object);

                Assert.False(input.inputConfiguration.UseSerilogDepthLevel);
            }

            using (var configFile = new TemporaryFile())
            {
                string inputConfiguration = @"
                {
                    ""type"": ""Serilog"",
                    ""useSerilogDepthLevel"": ""true""
                }";
                configFile.Write(inputConfiguration);
                var cb = new ConfigurationBuilder();
                cb.AddJsonFile(configFile.FilePath);
                var configuration = cb.Build();

                var input = new SerilogInput(configuration, healthReporterMock.Object);

                Assert.True(input.inputConfiguration.UseSerilogDepthLevel);
            }
        }

        [Fact]
        public void UsesSerilogMaxDestructuringDepth()
        {
            // Create object structure 5 levels deep
            var bravo = new EntityWithChildren() { Name = "Bravo" };
            var alpha = new EntityWithChildren("Alpha", new EntityWithChildren[] { bravo });

            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            IDictionary<string, object> spiedPayload = null;
            observer.Setup(p => p.OnNext(It.IsAny<EventData>()))
                .Callback<EventData>((p) => { spiedPayload = p.Payload; });

            SerilogInputConfiguration configuration = new SerilogInputConfiguration() { UseSerilogDepthLevel = true };


            using (var serilogInput = new SerilogInput(configuration, healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).Destructure.ToMaximumDepth(2).CreateLogger();

                logger.Information("Here is an {@entity}", alpha);
            }

            // At depth 2 there should be no children
            Assert.Equal("Alpha", ((IDictionary<string, object>)spiedPayload["entity"])["Name"]);
            Assert.Empty((IEnumerable<object>)((IDictionary<string, object>)spiedPayload["entity"])["Children"]);


            using (var serilogInput = new SerilogInput(configuration, healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).Destructure.ToMaximumDepth(3).CreateLogger();

                logger.Information("Here is an {@entity}", alpha);
            }

            // At depth 3 there should be one child of Alpha, but all its properties should be blank
            Assert.Equal("Alpha", ((IDictionary<string, object>)spiedPayload["entity"])["Name"]);
            var childrenOfAlpha = ((IDictionary<string, object>)spiedPayload["entity"])["Children"] as object[];
            Assert.Single(childrenOfAlpha);
            var b = childrenOfAlpha.First() as IDictionary<string, object>;
            Assert.True(b.ContainsKey("Name"));
            Assert.Null(b["Name"]);
            Assert.True(b.ContainsKey("Children"));
            Assert.Null(b["Children"]);


            using (var serilogInput = new SerilogInput(configuration, healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).Destructure.ToMaximumDepth(4).CreateLogger();

                logger.Information("Here is an {@entity}", alpha);
            }

            // At depth 4 the child of Alpha (Bravo) should have its properties set, "Name" in particular.
            Assert.Equal("Alpha", ((IDictionary<string, object>)spiedPayload["entity"])["Name"]);
            childrenOfAlpha = ((IDictionary<string, object>)spiedPayload["entity"])["Children"] as object[];
            Assert.Single(childrenOfAlpha);
            b = childrenOfAlpha.First() as IDictionary<string, object>;
            Assert.Equal("Bravo", b["Name"]);
        }

        [Fact]
        public void CanSerializeCircularObjectGraphs()
        {
            var charlie = new EntityWithChildren() { Name = "Charlie" };
            var bravo = new EntityWithChildren("Bravo", new EntityWithChildren[] { charlie });
            var alpha = new EntityWithChildren("Alpha", new EntityWithChildren[] { bravo });
            charlie.Children.Add(alpha); // Establish a cycle Alpha -> Bravo -> Charlie -> Alpha

            // Also form a cycle of direct "Sibling" references the other way round, i.e Charlie -> Bravo -> Alpha -> Charlie
            charlie.Sibling = bravo;
            bravo.Sibling = alpha;
            alpha.Sibling = charlie;

            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            IDictionary<string, object> spiedPayload = null;
            observer.Setup(p => p.OnNext(It.IsAny<EventData>()))
                .Callback<EventData>((p) => { spiedPayload = p.Payload; });

            SerilogInputConfiguration configuration = new SerilogInputConfiguration() { UseSerilogDepthLevel = true };

            const int MaxDepth = 10;
            using (var serilogInput = new SerilogInput(configuration, healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).Destructure.ToMaximumDepth(MaxDepth).CreateLogger();

                logger.Information("Here is an {@entity}", alpha);
            }

            // The fact that the loging of alpha succeeds at all (instead of going into an infinite loop) is already a good sign :-)

            IDictionary<string, object> entity = (IDictionary<string, object>)spiedPayload["entity"];
            object[] children = null;
            int depth = 0;
            while (entity != null && entity.ContainsKey("Children"))
            {
                children = entity["Children"] as object[];
                entity = null;
                if (children != null && children.Length > 0)
                {
                    entity = children[0] as IDictionary<string, object>;
                }

                depth++;
            }

            // Every child increases depth by two because the Children property is and array.
            Assert.Equal(MaxDepth / 2, depth);
        }

        [Fact]
        public void DestructuresNestedArrays()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            IDictionary<string, object> spiedPayload = null;
            observer.Setup(p => p.OnNext(It.IsAny<EventData>()))
                .Callback<EventData>((p) => { spiedPayload = p.Payload; });

            SerilogInputConfiguration configuration = new SerilogInputConfiguration() { UseSerilogDepthLevel = true };
            using (var serilogInput = new SerilogInput(configuration, healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).CreateLogger();

                var structure = new { A = "alpha", B = new[] { new { Y = "yankee", Z = new[] { new { Alpha = "A" } } }, new { Y = "zulu", Z = new[] { new { Alpha = "B" } } } } };
                logger.Information("Here is {@AStructure}", structure);
            }
            Assert.Equal("alpha", ((IDictionary<string, object>)spiedPayload["AStructure"])["A"]);
            object b = ((IDictionary<string, object>)spiedPayload["AStructure"])["B"];
            Assert.True(b is object[]);
            Assert.Equal(2, ((object[])b).Length);
            var bb = (IDictionary<string, object>)((object[])b)[0];
            Assert.Equal("yankee", bb["Y"]);
            object z = bb["Z"];
            Assert.True(z is object[]);
            Assert.Single((object[])z);
            Assert.True(((object[])z)[0] is IDictionary<string, object>);
            var zz = (IDictionary<string, object>)((object[])z)[0];
            Assert.Equal("A", zz["Alpha"]);
            Assert.True(((object[])b)[1] is IDictionary<string, object>);
            bb = (IDictionary<string, object>)((object[])b)[1];
            Assert.Equal("zulu", bb["Y"]);
            z = bb["Z"];
            Assert.True(z is object[]);
            Assert.Single((object[])z);
            Assert.True(((object[])z)[0] is IDictionary<string, object>);
            zz = (IDictionary<string, object>)((object[])z)[0];
            Assert.Equal("B", zz["Alpha"]);
        }

        [Fact]
        public void ObservesDepthLimitWhileDestructuringNestedArrays()
        {
            var healthReporterMock = new Mock<IHealthReporter>();
            var observer = new Mock<IObserver<EventData>>();
            IDictionary<string, object> spiedPayload = null;
            observer.Setup(p => p.OnNext(It.IsAny<EventData>()))
                .Callback<EventData>((p) => { spiedPayload = p.Payload; });

            SerilogInputConfiguration configuration = new SerilogInputConfiguration() { UseSerilogDepthLevel = true };
            using (var serilogInput = new SerilogInput(configuration, healthReporterMock.Object))
            using (serilogInput.Subscribe(observer.Object))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(serilogInput).Destructure.ToMaximumDepth(4).CreateLogger();

                var structure = new { A = "alpha", B = new[] { new { Y = "yankee", Z = new[] { new { Alpha = "A" } } }, new { Y = "zulu", Z = new[] { new { Alpha = "B" } } } } };
                logger.Information("Here is {@AStructure}", structure);
            }
            Assert.Equal("alpha", ((IDictionary<string, object>)spiedPayload["AStructure"])["A"]);
            object b = ((IDictionary<string, object>)spiedPayload["AStructure"])["B"];
            Assert.True(b is object[]);
            Assert.Equal(2, ((object[])b).Length);
            var bb = (IDictionary<string, object>)((object[])b)[0];
            Assert.Equal("yankee", bb["Y"]);
            object z = bb["Z"];
            Assert.True(z is object[]);
            Assert.Empty((object[])z);
        }

        private class EntityWithChildren
        {
            public string Name { get; set; }
            public List<EntityWithChildren> Children { get; } = new List<EntityWithChildren>();
            public EntityWithChildren Sibling { get; set; }

            public EntityWithChildren() { }
            public EntityWithChildren(string name, IEnumerable<EntityWithChildren> children)
            {
                Name = name;
                Children.AddRange(children);
            }
        }
    }
}
