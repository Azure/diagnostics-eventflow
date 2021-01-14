// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#if NET5_0 || NETCOREAPP2_1 || NETCOREAPP3_1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Validation;

using Microsoft.Diagnostics.EventFlow.Configuration;
using Microsoft.Diagnostics.EventFlow.Inputs.ActivitySource;
using Microsoft.Diagnostics.EventFlow.TestHelpers;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

namespace Microsoft.Diagnostics.EventFlow.Inputs.Tests
{
    public class ActivitySourceInputTests
    {
        private static readonly string SourceOneName = "EventFlow-ActivitySource-One";
        private static readonly string SourceTwoName = "EventFlow-ActivitySource-Two";
        private static readonly System.Diagnostics.ActivitySource SourceOne = new System.Diagnostics.ActivitySource(SourceOneName);
        private static readonly System.Diagnostics.ActivitySource SourceTwo = new System.Diagnostics.ActivitySource(SourceTwoName);

        private static readonly string WellKnownTraceId =    "0af7651916cd43dd8448eb211c80319c";
        private static readonly string WellKnownTraceIdTwo = "ce0d159c1755814f16fba28033db9940";
        private static readonly string SpanIdOne =           "b7ad6b7169203331";
        private static readonly string SpanIdTwo =           "00f067aa0ba902b7";
        private static readonly string SpanIdThree =         "7fe76cfa20e81932";

        [Fact]
        public void BasicActivityTracking()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityName = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = ActivityName,
                    CapturedData = ActivitySamplingResult.AllData,
                    CapturedEvents = CapturedActivityEvents.Both
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);

                var ctx = new ActivityContext(
                    ActivityTraceId.CreateFromString(WellKnownTraceId),
                    ActivitySpanId.CreateFromString(SpanIdOne),
                    ActivityTraceFlags.None);
                var activity = SourceOne.StartActivity(ActivityName, ActivityKind.Internal, ctx);
                activity.Stop();
            }

            healthReporter.VerifyNoOtherCalls();

            Assert.Equal(2, observer.Data.Count);
            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            Assert.True(observer.Data.TryDequeue(out EventData e));
            VerifyActivityEvent(e, ActivityName, SourceOneName, CapturedActivityEvents.Start, WellKnownTraceId, SpanIdOne);
            Assert.True(observer.Data.TryDequeue(out e));
            VerifyActivityEvent(e, ActivityName, SourceOneName, CapturedActivityEvents.Stop, WellKnownTraceId, SpanIdOne);
        }

        [Fact]
        public void CapturedEventsSettingIsEffective()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityNameSuffix = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedEventsNone" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.AllData,
                    CapturedEvents = CapturedActivityEvents.None
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedDataNone" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.None,
                    CapturedEvents = CapturedActivityEvents.Both
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedEventsStart" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.AllData,
                    CapturedEvents = CapturedActivityEvents.Start
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedEventsStop" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.AllData,
                    CapturedEvents = CapturedActivityEvents.Stop
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedEventsBoth" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.AllData,
                    CapturedEvents = CapturedActivityEvents.Both
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedEventsDefault" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.AllData
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);
                var ctx = new ActivityContext(
                        ActivityTraceId.CreateFromString(WellKnownTraceId),
                        ActivitySpanId.CreateFromString(SpanIdOne),
                        ActivityTraceFlags.None);

                foreach (var s in sources)
                {
                    var activity = SourceOne.StartActivity(s.ActivityName, ActivityKind.Internal, ctx);
                    activity?.Stop();
                }
            }

            healthReporter.VerifyNoOtherCalls();

            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            var observed = observer.Data.ToArray();

            // No events from CapturedEventsNone and CapturedDataNone activities
            Assert.DoesNotContain(observed, o => OrdinalEquals(o.Payload["Name"], "CapturedEventsNone" + ActivityNameSuffix));
            Assert.DoesNotContain(observed, o => OrdinalEquals(o.Payload["Name"], "CapturedDataNone" + ActivityNameSuffix));

            // Only activity start event captured for CapturedEventsStart activity
            Assert.Equal(1, observed.Count(o => 
                OrdinalEquals(o.Payload["Name"], "CapturedEventsStart" + ActivityNameSuffix) &&
                !o.Payload.ContainsKey("EndTime")
            ));
            Assert.DoesNotContain(observed, o =>
            OrdinalEquals(o.Payload["Name"], "CapturedEventsStart" + ActivityNameSuffix) &&
                o.Payload.ContainsKey("EndTime")
            );

            // Only activity stop event captured for CapturedEventStop activity
            Assert.Equal(1, observed.Count(o =>
                OrdinalEquals((string)o.Payload["Name"], "CapturedEventsStop" + ActivityNameSuffix) &&
                o.Payload.ContainsKey("EndTime")
            ));
            Assert.DoesNotContain(observed, o =>
            OrdinalEquals((string)o.Payload["Name"], "CapturedEventsStop" + ActivityNameSuffix) &&
                !o.Payload.ContainsKey("EndTime")
            );

            // Two events (start and stop) are captured for both CapturedEventsBoth and one for CapturedEventDefault activity
            Assert.Equal(2, observed.Count(o => OrdinalEquals((string)o.Payload["Name"], "CapturedEventsBoth" + ActivityNameSuffix)));
            Assert.Equal(1, observed.Count(o => OrdinalEquals((string)o.Payload["Name"], "CapturedEventsDefault" + ActivityNameSuffix)));
        }

        [Fact]
        public void CapturedDataSettingIsEffective()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityNameSuffix = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedDataPropagation" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.PropagationData
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedDataAll" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.AllData
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedDataAllRecorded" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.AllDataAndRecorded
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "CapturedDataDefault" + ActivityNameSuffix
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "Outer" + ActivityNameSuffix
                },
            });

            string outerActivityId = null;

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);

                var ctx = new ActivityContext(
                    ActivityTraceId.CreateFromString(WellKnownTraceId),
                    ActivitySpanId.CreateFromString(SpanIdOne),
                    ActivityTraceFlags.None);
                var outer = SourceOne.StartActivity("Outer" + ActivityNameSuffix, ActivityKind.Server, ctx);
                outer.AddBaggage("Outer.Baggage1", "Outer.Baggage1.Value");
                outerActivityId = outer.SpanId.ToHexString();

                var activity = SourceOne.StartActivity("CapturedDataPropagation" + ActivityNameSuffix, ActivityKind.Internal, null,
                    new Dictionary<string, object>
                    {
                        ["CapturedDataPropagation.Tag1"] = "CapturedDataPropagation.Tag1Value",
                        ["CapturedDataPropagation.Tag2"] = new NamedObject("CapturedDataPropagation.Tag2Value")
                    },
                    new[] {
                        new ActivityLink(new ActivityContext(
                            ActivityTraceId.CreateFromString(WellKnownTraceIdTwo),
                            ActivitySpanId.CreateFromString(SpanIdTwo),
                            ActivityTraceFlags.None
                        ))
                    });
                Thread.Sleep(30);
                activity.AddEvent(new ActivityEvent("CapturedDataPropagation.Event1"));
                activity.Stop();

                // Add extra baggage to outer activity and make sure it is captured by subsequent activities
                outer.AddBaggage("Outer.Baggage2", "Outer.Baggage2.Value");

                activity = SourceOne.StartActivity("CapturedDataAll" + ActivityNameSuffix, ActivityKind.Internal, null,
                    new Dictionary<string, object>
                    {
                        ["CapturedDataAll.Tag1"] = "CapturedDataAll.Tag1Value",
                        ["CapturedDataAll.Tag2"] = new NamedObject("CapturedDataAll.Tag2Value")
                    },
                    new[] {
                        new ActivityLink(new ActivityContext(
                            ActivityTraceId.CreateFromString(WellKnownTraceIdTwo),
                            ActivitySpanId.CreateFromString(SpanIdThree),
                            ActivityTraceFlags.None
                        ))
                    });
                Thread.Sleep(30);
                activity.AddEvent(new ActivityEvent("CapturedDataAll.Event1"));
                activity.Stop();

                activity = SourceOne.StartActivity("CapturedDataAllRecorded" + ActivityNameSuffix, ActivityKind.Internal, null,
                    new Dictionary<string, object>
                    {
                        ["CapturedDataAllRecorded.Tag1"] = "CapturedDataAllRecorded.Tag1Value",
                        ["CapturedDataAllRecorded.Tag2"] = new NamedObject("CapturedDataAllRecorded.Tag2Value")
                    },
                    new[] {
                        new ActivityLink(new ActivityContext(
                            ActivityTraceId.CreateFromString(WellKnownTraceIdTwo),
                            ActivitySpanId.CreateFromString(SpanIdThree),
                            ActivityTraceFlags.None
                        ))
                    });
                Thread.Sleep(30);
                activity.AddEvent(new ActivityEvent("CapturedDataAllRecorded.Event1"));
                activity.Stop();

                ctx = new ActivityContext(
                    ActivityTraceId.CreateFromString(WellKnownTraceId),
                    ActivitySpanId.CreateFromString(outerActivityId),
                    ActivityTraceFlags.None);
                activity = SourceOne.StartActivity("CapturedDataDefault" + ActivityNameSuffix, ActivityKind.Internal, null,
                    new Dictionary<string, object>
                    {
                        ["CapturedDataDefault.Tag1"] = "CapturedDataDefault.Tag1Value",
                        ["CapturedDataDefault.Tag2"] = new NamedObject("CapturedDataDefault.Tag2Value")
                    },
                    new[] {
                        new ActivityLink(new ActivityContext(
                            ActivityTraceId.CreateFromString(WellKnownTraceIdTwo),
                            ActivitySpanId.CreateFromString(SpanIdTwo),
                            ActivityTraceFlags.None
                        ))
                    });
                Thread.Sleep(30);
                activity.AddEvent(new ActivityEvent("CapturedDataDefault.Event1"));
                activity.Stop();

                outer.Stop();
            }

            healthReporter.VerifyNoOtherCalls();

            // We expect one event the outer activity and 4 (stop) events for the inner ones
            Assert.Equal(5, observer.Data.Count);
            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            var observed = observer.Data.ToArray();

            // CapturedData == Propaation should capture baggage, but not links, tags, or events
            var ae = observed.First(e => 
                        OrdinalEquals(e.Payload["Name"], "CapturedDataPropagation" + ActivityNameSuffix) 
                        && e.Payload.ContainsKey("EndTime"));
            VerifyActivityEvent(ae,
                "CapturedDataPropagation" + ActivityNameSuffix, SourceOneName, CapturedActivityEvents.Stop,
                WellKnownTraceId, outerActivityId, false, ActivityKind.Internal,
                new Dictionary<string, Func<object, bool>> {
                    ["Outer.Baggage1"] = (val) => OrdinalEquals(val, "Outer.Baggage1.Value")
                },
                new[] { "Links", "Tags", "Events" });

            // CaptureDataAll and CaptureDataAllRecorded should have baggage, links, tags, and events
            // They only differ by IsRecorded flag value
            ae = observed.First(e => 
                    OrdinalEquals(e.Payload["Name"], "CapturedDataAll" + ActivityNameSuffix) 
                    && e.Payload.ContainsKey("EndTime"));
            VerifyActivityEvent(ae,
                "CapturedDataAll" + ActivityNameSuffix, SourceOneName, CapturedActivityEvents.Stop,
                WellKnownTraceId, outerActivityId, false, ActivityKind.Internal,
                new Dictionary<string, Func<object, bool>>
                {
                    ["Outer.Baggage1"] = (val) => OrdinalEquals(val, "Outer.Baggage1.Value"),
                    ["Outer.Baggage2"] = (val) => OrdinalEquals(val, "Outer.Baggage2.Value"),
                    ["Events"] = (evnts) => 
                    {
                        Assert.Collection<ActivityEvent>((IEnumerable<ActivityEvent>) evnts, e => Assert.Equal("CapturedDataAll.Event1", e.Name, StringComparer.Ordinal));
                        return true;
                    },
                    ["CapturedDataAll.Tag1"] = (val) => OrdinalEquals(val, "CapturedDataAll.Tag1Value"),
                    ["CapturedDataAll.Tag2"] = (val) => OrdinalEquals(((NamedObject) val).Name, "CapturedDataAll.Tag2Value"),
                    ["Links"] = (links) =>
                    {
                        Assert.Collection<ActivityLink>((IEnumerable<ActivityLink>)links,
                            l => Assert.True(
                                OrdinalEquals(l.Context.TraceId.ToHexString(), WellKnownTraceIdTwo) && 
                                OrdinalEquals(l.Context.SpanId.ToHexString(), SpanIdThree))
                        );
                        return true;
                    }
                }
            );

            ae = observed.First(e => 
                OrdinalEquals(e.Payload["Name"], "CapturedDataAllRecorded" + ActivityNameSuffix) 
                && e.Payload.ContainsKey("EndTime"));
            VerifyActivityEvent(ae,
                "CapturedDataAllRecorded" + ActivityNameSuffix, SourceOneName, CapturedActivityEvents.Stop,
                WellKnownTraceId, outerActivityId, true, ActivityKind.Internal,
                new Dictionary<string, Func<object, bool>>
                {
                    ["Outer.Baggage1"] = (val) => OrdinalEquals(val, "Outer.Baggage1.Value"),
                    ["Outer.Baggage2"] = (val) => OrdinalEquals(val, "Outer.Baggage2.Value"),
                    ["Events"] = (evnts) =>
                    {
                        Assert.Collection<ActivityEvent>((IEnumerable<ActivityEvent>)evnts, e => Assert.Equal("CapturedDataAllRecorded.Event1", e.Name, StringComparer.Ordinal));
                        return true;
                    },
                    ["CapturedDataAllRecorded.Tag1"] = (val) => OrdinalEquals(val, "CapturedDataAllRecorded.Tag1Value"),
                    ["CapturedDataAllRecorded.Tag2"] = (val) => OrdinalEquals(((NamedObject)val).Name, "CapturedDataAllRecorded.Tag2Value"),
                    ["Links"] = (links) =>
                    {
                        Assert.Collection<ActivityLink>((IEnumerable<ActivityLink>)links,
                            l => Assert.True(
                                OrdinalEquals(l.Context.TraceId.ToHexString(), WellKnownTraceIdTwo) &&
                                OrdinalEquals(l.Context.SpanId.ToHexString(), SpanIdThree))
                        );
                        return true;
                    }
                }
            );

            // The default for data capturing is AllData, so all data should be captured, but the recording flag should not be set.
            ae = observed.First(e => 
                OrdinalEquals(e.Payload["Name"], "CapturedDataDefault" + ActivityNameSuffix) 
                && e.Payload.ContainsKey("EndTime"));
            VerifyActivityEvent(ae,
                "CapturedDataDefault" + ActivityNameSuffix, SourceOneName, CapturedActivityEvents.Stop,
                WellKnownTraceId, outerActivityId, false, ActivityKind.Internal,
                new Dictionary<string, Func<object, bool>>
                {
                    ["Outer.Baggage1"] = (val) => OrdinalEquals(val, "Outer.Baggage1.Value"),
                    ["Outer.Baggage2"] = (val) => OrdinalEquals(val, "Outer.Baggage2.Value"),
                    ["Events"] = (evnts) =>
                    {
                        Assert.Collection<ActivityEvent>((IEnumerable<ActivityEvent>)evnts, e => Assert.Equal("CapturedDataDefault.Event1", e.Name, StringComparer.Ordinal));
                        return true;
                    },
                    ["CapturedDataDefault.Tag1"] = (val) => OrdinalEquals(val, "CapturedDataDefault.Tag1Value"),
                    ["CapturedDataDefault.Tag2"] = (val) => OrdinalEquals(((NamedObject)val).Name, "CapturedDataDefault.Tag2Value"),
                    ["Links"] = (links) =>
                    {
                        Assert.Collection<ActivityLink>((IEnumerable<ActivityLink>)links,
                            l => Assert.True(
                                OrdinalEquals(l.Context.TraceId.ToHexString(), WellKnownTraceIdTwo) &&
                                OrdinalEquals(l.Context.SpanId.ToHexString(), SpanIdTwo))
                        );
                        return true;
                    }
                }
            );

            // Phew! That was a lot of checking :-)
        }

        [Fact]
        public void CaptureEverythingConfigurationWorks()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityNameA = GetRandomName();
            var ActivityNameB = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                // Empty configuration means capture all activities from all sources,
                // with all data, and both start and stop events
                new ActivitySourceConfiguration {}
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);

                var activity = SourceOne.StartActivity(ActivityNameA);
                activity.Stop();

                activity = SourceTwo.StartActivity(ActivityNameB);
                activity.AddEvent(new ActivityEvent("ActivityB.Event1"));
                activity.Stop();
            }

            healthReporter.VerifyNoOtherCalls();

            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            var observed = observer.Data.ToArray();

            // We expect one stop evente for both activities, total 2.
            // There might be more because we are capturing, well, everyting in the system %-)
            Assert.Equal(2, observed.Count(e => 
                (OrdinalEquals(e.Payload["Name"], ActivityNameA) && OrdinalEquals(e.Payload["ActivitySourceName"], SourceOneName)) ||
                (OrdinalEquals(e.Payload["Name"], ActivityNameB) && OrdinalEquals(e.Payload["ActivitySourceName"], SourceTwoName))
            ));

            // Verify that all data was captured by checking for activity event associated with activity B
            var activityB_Stop = observed.First(e =>
                OrdinalEquals(e.Payload["Name"], ActivityNameB) && 
                OrdinalEquals(e.Payload["ActivitySourceName"], SourceTwoName) &&
                e.Payload.ContainsKey("EndTime")
            );
            Assert.Equal(1, ((IEnumerable<ActivityEvent>)activityB_Stop.Payload["Events"]).Count(ae => OrdinalEquals(ae.Name, "ActivityB.Event1")));
        }

        [Fact]
        public void AllActivitiesCapturedWhenActivityNameIsOmitted()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityNameA = GetRandomName();
            var ActivityNameB = GetRandomName();
            var ActivityNameC = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration 
                {
                    ActivitySourceName = SourceOneName,
                    CapturedEvents = CapturedActivityEvents.Stop
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);

                var activity = SourceOne.StartActivity(ActivityNameA);
                activity.Stop();

                activity = SourceOne.StartActivity(ActivityNameB);
                activity.Stop();

                activity = SourceTwo.StartActivity(ActivityNameC);
                Assert.Null(activity); // Nobody is listening to source 2
            }

            healthReporter.VerifyNoOtherCalls();

            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            var observed = observer.Data.ToArray();

            Assert.Equal(2, observed.Count());
            Assert.Equal(1, observed.Count(e => OrdinalEquals(e.Payload["Name"], ActivityNameA)));
            Assert.Equal(1, observed.Count(e => OrdinalEquals(e.Payload["Name"], ActivityNameB)));
        }

        [Fact]
        public void AllSourcesCapturedWhenActivitySourceNameIsOmitted()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityNameA = GetRandomName();
            var ActivityNameB = GetRandomName();
            var ActivityNameC = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration
                {
                    ActivityName = ActivityNameA,
                    CapturedEvents = CapturedActivityEvents.Stop
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);

                var activity = SourceOne.StartActivity(ActivityNameA);
                activity.Stop();

                activity = SourceOne.StartActivity(ActivityNameB);
                Assert.Null(activity); // Nobody is listening

                activity = SourceTwo.StartActivity(ActivityNameA);
                activity.Stop();

                activity = SourceTwo.StartActivity(ActivityNameC);
                Assert.Null(activity);
            }

            healthReporter.VerifyNoOtherCalls();

            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            var observed = observer.Data.ToArray();

            Assert.Equal(2, observed.Count());
            Assert.All(observed, e => Assert.True(OrdinalEquals(e.Payload["Name"], ActivityNameA)));
        }

        [Fact]
        public void WarningIssuedIfNoSources()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var input = new ActivitySourceInput(
                new ActivitySourceInputConfiguration { Sources = new List<ActivitySourceConfiguration>() }, 
                healthReporter.Object);
            input.Dispose();

            healthReporter.Verify(hr => hr.ReportWarning(
                It.Is<string>(s => s.Contains("no data sources", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(ctx => OrdinalEquals(ctx, EventFlowContextIdentifiers.Configuration))
            ), Times.Exactly(1));
            healthReporter.VerifyNoOtherCalls();
        }

        [Fact]
        public void CanReadJsonConfiguration()
        {
            string inputConfiguration = @"
                {
                    ""type"": ""ActivitySource"",
                    ""sources"": [
                        {
                            ""ActivitySourceName"": ""Alpha""
                        },
                        {
                            ""ActivityName"": ""SuperImportant""
                        },
                        {
                            ""ActivitySourceName"": ""Bravo"",
                            ""ActivityName"": ""BravoOne""
                        },
                        {
                            ""ActivitySourceName"": ""Bravo"",
                            ""ActivityName"": ""BravoTwo"",
                            ""CapturedData"": ""PropagationData"",
                            ""CapturedEvents"": ""Start""
                        },
                    ]
                }
            ";

            using (var configFile = new TemporaryFile())
            {
                configFile.Write(inputConfiguration);
                var cb = new ConfigurationBuilder();
                cb.AddJsonFile(configFile.FilePath);
                var configuration = cb.Build();

                var healthReporter = new Mock<IHealthReporter>();
                var input = new ActivitySourceInput(configuration, healthReporter.Object);

                healthReporter.VerifyNoOtherCalls();
                Assert.Collection(input.Configuration.Sources,
                    sc => {
                        Assert.Equal("Alpha", sc.ActivitySourceName, StringComparer.Ordinal);
                        Assert.True(string.IsNullOrEmpty(sc.ActivityName));
                        Assert.Equal(ActivitySamplingResult.AllData, sc.CapturedData);
                        Assert.Equal(CapturedActivityEvents.Stop, sc.CapturedEvents);
                    },
                    sc => {
                        Assert.True(string.IsNullOrEmpty(sc.ActivitySourceName));
                        Assert.Equal("SuperImportant", sc.ActivityName, StringComparer.Ordinal);
                        Assert.Equal(ActivitySamplingResult.AllData, sc.CapturedData);
                        Assert.Equal(CapturedActivityEvents.Stop, sc.CapturedEvents);
                    },
                    sc => {
                        Assert.Equal("Bravo", sc.ActivitySourceName, StringComparer.Ordinal);
                        Assert.Equal("BravoOne", sc.ActivityName, StringComparer.Ordinal);
                        Assert.Equal(ActivitySamplingResult.AllData, sc.CapturedData);
                        Assert.Equal(CapturedActivityEvents.Stop, sc.CapturedEvents);
                    },
                    sc => {
                        Assert.Equal("Bravo", sc.ActivitySourceName, StringComparer.OrdinalIgnoreCase);
                        Assert.Equal("BravoTwo", sc.ActivityName, StringComparer.Ordinal);
                        Assert.Equal(ActivitySamplingResult.PropagationData, sc.CapturedData);
                        Assert.Equal(CapturedActivityEvents.Start, sc.CapturedEvents);
                    }
                );
            }
        }

        [Fact]
        public void BaggageTakesPrecedenceOverTags()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityName = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration 
                {
                    ActivitySourceName = SourceOneName,
                    CapturedEvents = CapturedActivityEvents.Stop
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);

                var activity = SourceOne.StartActivity(ActivityName);
                activity.AddTag("Alpha", "AlphaTag");
                activity.AddBaggage("Alpha", "AlphaBaggage");
                activity.Stop();
            }

            // Complaining about Alpha property name conflict
            healthReporter.Verify(hr => hr.ReportWarning(
                It.Is<string>(s => s.Contains("Alpha", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(ctx => OrdinalEquals(ctx, nameof(ActivitySourceInput)))
            ), Times.Exactly(1));
            healthReporter.VerifyNoOtherCalls();

            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            var observed = observer.Data.ToArray();
            var e = Assert.Single(observed);
            Assert.True(OrdinalEquals(e.Payload["Alpha"], "AlphaBaggage") && OrdinalEquals(e.Payload["Alpha_1"], "AlphaTag"));
        }

        [Fact]
        public void SourceCanBeCreatedAfterInput()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityName = GetRandomName();

            const string TestSourceName = "EventFlowTestActivitySource";
            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = TestSourceName,
                    CapturedEvents = CapturedActivityEvents.Stop
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);

                using (var activitySouce = new System.Diagnostics.ActivitySource("EventFlowTestActivitySource"))
                {
                    var activity = activitySouce.StartActivity(ActivityName);
                    activity.Stop();
                }
            }

            healthReporter.VerifyNoOtherCalls();

            Assert.True(observer.Completed);
            Assert.Null(observer.Error);

            var observed = observer.Data.ToArray();
            var e = Assert.Single(observed);
            Assert.Equal(ActivityName, e.Payload["Name"]);
        }

        [Fact]
        public void SubscriptionsAreDisposedUponInputDisposal()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var observer = new TestObserver();
            var ActivityName = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    CapturedEvents = CapturedActivityEvents.Stop
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
                input.Subscribe(observer);
            }

            // The input is configured to listen for all activities, but it has been disposed, so no one should be listening anymore
            // and no activity should be created.
            Assert.Null(SourceOne.StartActivity(ActivityName));

            Assert.True(observer.Completed);
            Assert.Null(observer.Error);
            Assert.Empty(observer.Data);
        }

        private void VerifyActivityEvent(
            EventData e,
            string activityName,
            string activitySourceName,
            CapturedActivityEvents activityEventType,
            string traceId,
            string parentSpanId,
            bool recorded = false,
            ActivityKind activityKind = ActivityKind.Internal,
            IDictionary<string, Func<object, bool>> requiredProps = null,
            IEnumerable<string> nonexistentProps = null
        ) {
            Assert.Equal(activityName, e.Payload["Name"]);
            Assert.Equal(activitySourceName, e.Payload["ActivitySourceName"]);
            Assert.True(e.Payload.ContainsKey("StartTime"));
            if (activityEventType == CapturedActivityEvents.Stop)
            {
                Assert.True(e.Payload.ContainsKey("EndTime"));
            }
            Assert.Equal(traceId, e.Payload["TraceId"]);
            Assert.Equal(parentSpanId, e.Payload["ParentSpanId"]);
            Assert.Equal(recorded, e.Payload["IsRecording"]);
            Assert.Equal(activityKind, Enum.Parse(typeof(ActivityKind), (string) e.Payload["SpanKind"]));

            if (requiredProps != null)
            {
                foreach(var p in requiredProps)
                {
                    Assert.True(p.Value(e.Payload[p.Key]), $"Assertion failed for activity '{activityName}': property '{p.Key}' has unexpected value: '{e.Payload[p.Key]}'");
                }
            }

            if (nonexistentProps != null)
            {
                foreach(var np in nonexistentProps)
                {
                    Assert.DoesNotContain(e.Payload, p => p.Key.Equals(np, StringComparison.Ordinal));
                }
            }
        }

        [Fact]
        public void WarnsAboutInnefectiveConfigurationEntries()
        {
            var healthReporter = new Mock<IHealthReporter>();
            var ActivityNameSuffix = GetRandomName();

            var sources = new List<ActivitySourceConfiguration>(new[]
            {
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "ActivityA" + ActivityNameSuffix
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    CapturedData = ActivitySamplingResult.PropagationData,
                    CapturedEvents = CapturedActivityEvents.Stop
                },
                new ActivitySourceConfiguration
                {
                    ActivitySourceName = SourceOneName,
                    ActivityName = "ActivityB" + ActivityNameSuffix,
                    CapturedData = ActivitySamplingResult.None
                }
            });

            using (var input = new ActivitySourceInput(new ActivitySourceInputConfiguration { Sources = sources }, healthReporter.Object))
            {
            }

            // We expect a warning about an ActivitySource configured with CapturedData = None, since that has no effect
            healthReporter.Verify(hr => hr.ReportWarning(
                It.Is<string>(s => s.Contains("entries will not be used", StringComparison.OrdinalIgnoreCase)),
                It.Is<string>(ctx => OrdinalEquals(ctx, EventFlowContextIdentifiers.Configuration))
            ), Times.Exactly(1));
            healthReporter.VerifyNoOtherCalls();
        }


        private bool OrdinalEquals(object o1, object o2) => StringComparer.Ordinal.Equals(o1, o2);

        private class NamedObject
        {
            public string Name { get; private set; }

            public NamedObject(string name) 
            {
                Requires.NotNull(name, nameof(name));
                Name = name;
            }

            public override bool Equals(object obj)
            {
                NamedObject other = obj as NamedObject;
                if (other == null) return false;
                return StringComparer.Ordinal.Equals(Name, other.Name);
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }

        // To ensure that tests can be run in parallel we might use random activity source and activity names for each execution
        private readonly Random random_ = new Random();
        private string GetRandomName()
        {
            const int Len = 8;
            const char Offset = 'a';
            const int Range = 26; // a..z: length = 26  

            var builder = new StringBuilder(Len);
            for (var i = 0; i < Len; i++)
            {
                var c = (char)random_.Next(Offset, Offset + Range);
                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}

#endif