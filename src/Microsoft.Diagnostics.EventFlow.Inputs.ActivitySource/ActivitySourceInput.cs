// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Validation;

using Microsoft.Diagnostics.EventFlow.Configuration;
using System.Data.Common;

namespace Microsoft.Diagnostics.EventFlow.Inputs.ActivitySource
{
    public class ActivitySourceInput : IObservable<EventData>, IDisposable
    {
        private const int SamplingDecisionCacheFlushThreshold = 1024;

        // Using a static dictionary like this is faster than doing Enum.GetName()
        private static readonly IDictionary<ActivityKind, string> ActivityKindNames =
            Enum.GetValues(typeof(ActivityKind)).Cast<ActivityKind>().ToDictionary(k => k, k => k.ToString());

        private EventFlowSubject<EventData> subject_;
        private ActivityListener activityListener_;
        private ActivitySourceInputConfiguration configuration_;
        private ConcurrentDictionary<string, (ActivitySamplingResult CapturedData, CapturedActivityEvents CapturedEvents)> activitySampling_;
        private bool hasUnrestrictedSources_;
        private IHealthReporter healthReporter_;

        public ActivitySourceInput(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            var inputConfiguration = new ActivitySourceInputConfiguration();
            try
            {
                configuration.Bind(inputConfiguration);
            }
            catch
            {
                healthReporter.ReportProblem(
                    $"Invalid {nameof(ActivitySourceInput)} configuration encountered: '{configuration.ToString()}'",
                   EventFlowContextIdentifiers.Configuration);
                throw;
            }

            Initialize(inputConfiguration, healthReporter);
        }

        public ActivitySourceInput(ActivitySourceInputConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            Initialize(configuration, healthReporter);
        }

        public ActivitySourceInputConfiguration Configuration => configuration_;

        public void Dispose()
        {
            activityListener_.Dispose();
            subject_.Dispose();
        }

        public IDisposable Subscribe(IObserver<EventData> observer)
        {
            return subject_.Subscribe(observer);
        }

        private void Initialize(ActivitySourceInputConfiguration configuration, IHealthReporter healthReporter)
        {
            healthReporter_ = healthReporter;
            configuration_ = configuration.DeepClone();
            subject_ = new EventFlowSubject<EventData>();
            activitySampling_ = new ConcurrentDictionary<string, (ActivitySamplingResult CapturedData, CapturedActivityEvents CapturedEvents)>();
            activityListener_ = new ActivityListener();

            if (configuration_.Sources.Count == 0)
            {
                healthReporter.ReportWarning(
                    $"{nameof(ActivitySourceInput)}: configuration has no data sources. No activity data will be captured.",
                    EventFlowContextIdentifiers.Configuration);
            }

            WarnAboutIneffectiveEntries(configuration_, healthReporter);

            hasUnrestrictedSources_ = configuration_.Sources.Any(s => string.IsNullOrWhiteSpace(s.ActivitySourceName));

            activityListener_.Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) 
                => DetermineActivitySampling(activityOptions.Source.Name, activityOptions.Name).CapturedData;
            activityListener_.SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions)
                => DetermineActivitySampling(activityOptions.Source.Name, activityOptions.Name).CapturedData;
            activityListener_.ShouldListenTo = ShouldListenTo;
            activityListener_.ActivityStarted = OnActivityStarted;
            activityListener_.ActivityStopped = OnActivityStopped;

            System.Diagnostics.ActivitySource.AddActivityListener(activityListener_);
        }

        private (ActivitySamplingResult CapturedData, CapturedActivityEvents CapturedEvents) DetermineActivitySampling(string activitySourceName, string activityName)
        {
            string activityKey = activitySourceName + ":" + activityName;

            if (activitySampling_.TryGetValue(activityKey, out var samplingSpec))
            {
                return samplingSpec;
            }

            foreach(var sc in configuration_.Sources)
            {
                bool sourceMatches = string.IsNullOrWhiteSpace(sc.ActivitySourceName) || StringComparer.OrdinalIgnoreCase.Equals(activitySourceName, sc.ActivitySourceName);
                bool nameMatches = string.IsNullOrWhiteSpace(sc.ActivityName) || StringComparer.OrdinalIgnoreCase.Equals(activityName, sc.ActivityName);

                if (sourceMatches && nameMatches)
                {
                    FlushSamplingInfoCacheIfNeeded();
                    
                    activitySampling_.AddOrUpdate(activityKey, (sc.CapturedData, sc.CapturedEvents), (oldData, oldEvents) => (sc.CapturedData, sc.CapturedEvents));

                    return (sc.CapturedData, sc.CapturedEvents);
                }
            }

            return (ActivitySamplingResult.None, CapturedActivityEvents.None);
        }

        private bool ShouldListenTo(System.Diagnostics.ActivitySource activitySource)
        {
            if (hasUnrestrictedSources_)
            {
                return true;
            }

            bool found = configuration_.Sources.Any(s => 
                StringComparer.OrdinalIgnoreCase.Equals(activitySource.Name, s.ActivitySourceName) &&
                s.CapturedEvents != CapturedActivityEvents.None);
            return found;
        }

        private void OnActivityStarted(Activity activity)
        {
            (var capturedData, var capturedEvents) = DetermineActivitySampling(activity.Source.Name, activity.DisplayName);
            if (
                capturedData == ActivitySamplingResult.None ||
                capturedEvents == CapturedActivityEvents.None ||
                (capturedEvents & CapturedActivityEvents.Start) == 0)
            {
                return;
            }

            EventData e = ToEventData(activity, capturedData);
            subject_.OnNext(e);
        }

        private void OnActivityStopped(Activity activity)
        {
            (var capturedData, var capturedEvents) = DetermineActivitySampling(activity.Source.Name, activity.DisplayName);
            if (
                capturedData == ActivitySamplingResult.None ||
                capturedEvents == CapturedActivityEvents.None ||
                (capturedEvents & CapturedActivityEvents.Stop) == 0)
            {
                return;
            }

            EventData e = ToEventData(activity, capturedData);
            subject_.OnNext(e);
        }

        private EventData ToEventData(Activity activity, ActivitySamplingResult capturedData)
        {
            EventData e = new EventData
            { 
                ProviderName = nameof(ActivitySourceInput),
                Timestamp = activity.StartTimeUtc,
                Level = LogLevel.Informational,
                Keywords = (long) activity.ActivityTraceFlags
            };

            // Property names following OpenTelemetry conventions https://github.com/open-telemetry/opentelemetry-specification
            e.Payload["Name"] = activity.DisplayName;
            e.Payload["SpanId"] = activity.Id;
            e.Payload["ParentSpanId"] = activity.ParentSpanId.ToHexString();
            e.Payload["StartTime"] = e.Timestamp;
            if (activity.Duration != TimeSpan.Zero)
            {
                e.Payload["EndTime"] = activity.StartTimeUtc + activity.Duration;
            }
            e.Payload["TraceId"] = activity.TraceId.ToHexString();
            if (ActivityKindNames.TryGetValue(activity.Kind, out string activityKindName))
            {
                e.Payload["SpanKind"] = activityKindName;
            }
            e.Payload["IsRecording"] = activity.Recorded;

            // The following property additions may cause name conflicts, so using AddPayloadProperty() to handle them.
            foreach(var el in activity.Baggage)
            {
                AddPayloadProperty(e, el.Key, el.Value);
            }

            AddPayloadProperty(e, "ActivitySourceName", activity.Source.Name);

            if (capturedData == ActivitySamplingResult.AllData || capturedData == ActivitySamplingResult.AllDataAndRecorded)
            {
                // Activity.Tags is a subset of Activity.TagObjects that have string value,
                // so it is sufficient to just iterate over Activity.TagObjects to capture all activity tags.
                foreach(var tagObject in activity.TagObjects)
                {
                    AddPayloadProperty(e, tagObject.Key, tagObject.Value);
                }

                if (activity.Events.Any())
                {
                    AddPayloadProperty(e, "Events", activity.Events);
                }

                if (activity.Links.Any())
                {
                    AddPayloadProperty(e, "Links", activity.Links);
                }
            }

            return e;
        }

        private void AddPayloadProperty(EventData e, string propertyName, object propertyValue)
        {
            e.AddPayloadProperty(propertyName, propertyValue, healthReporter_, nameof(ActivitySourceInput));
        }

        private void FlushSamplingInfoCacheIfNeeded()
        {
            if (activitySampling_.Count > SamplingDecisionCacheFlushThreshold)
            {
                lock(activitySampling_)
                {
                    if (activitySampling_.Count > SamplingDecisionCacheFlushThreshold)
                    {
                        activitySampling_.Clear();
                    }
                }
            }
        }

        private void WarnAboutIneffectiveEntries(ActivitySourceInputConfiguration configuration_, IHealthReporter healthReporter)
        {
            // If (SomeSource, *) configuration entry is succeeded by (SomeSource, SomeActivity) configuration entry,
            // the latter will never be used. So issue a warning.
            // Similarly, if (*, SomeActivity) configuration entry is succeeded by (SomeSource, SomeActivity) configuration entry,
            // the latter will never be used either.
            // Finally, anything following (*, *) is not going to be used.

            // The presence of an entry in this dictionary means an "capture all activities" entry for a given ActivitySource
            // has been encountered. We want to limit the warnings to one per ActivitySource, 
            // so we flip the value from false to true after we issue a warning.
            var captureAllActivitiesEntries = new Dictionary<string, bool>();

            // The presence of an entry in this dictionary means an "capture all sources emitting activity XXX"
            // has been encountered. The values are used in the same way as with captureAllActivitiesEntries.
            var captureAllSourcesEntries = new Dictionary<string, bool>();

            bool captureEverythingEntry = false;

            foreach(var source in configuration_.Sources)
            {
                if (captureEverythingEntry)
                {
                    healthReporter.ReportWarning(
                        $"{nameof(ActivitySourceInput)}: configuration has an entry that applies to all activities from all sources " +
                        "(both activity name and activity source name are blank). Following entries will not be used. " +
                        "See input documentation for more information.",
                        EventFlowContextIdentifiers.Configuration);
                    break;
                }

                bool captureAllSources = string.IsNullOrWhiteSpace(source.ActivitySourceName);
                bool captureAllActivities = string.IsNullOrWhiteSpace(source.ActivityName);

                if (captureAllSources && captureAllActivities)
                {
                    captureEverythingEntry = true;
                    continue;
                }

                bool warningIssued = false;

                bool hasAllActivityEntry = !captureAllSources && captureAllActivitiesEntries.TryGetValue(source.ActivitySourceName, out warningIssued);
                if (hasAllActivityEntry && !warningIssued)
                {
                    captureAllActivitiesEntries[source.ActivitySourceName] = true;

                    healthReporter.ReportWarning(
                        $"{nameof(ActivitySourceInput)}: configuration for source '{source.ActivitySourceName}' has an entry that applies to all activities (activity name is blank)" +
                        " followed by other entries. The other entries will not be used. See input documentation for more information.",
                            EventFlowContextIdentifiers.Configuration);
                }
                else if (captureAllActivities)
                {
                    captureAllActivitiesEntries[source.ActivitySourceName] = false;
                }


                bool hasAllSourcesEntry = !captureAllActivities && captureAllSourcesEntries.TryGetValue(source.ActivityName, out warningIssued);
                if (hasAllSourcesEntry && !warningIssued)
                {
                    captureAllSourcesEntries[source.ActivityName] = true;

                    healthReporter.ReportWarning(
                        $"{nameof(ActivitySourceInput)}: configuration has an entry that captures activity '{source.ActivityName}' for all sources." +
                        " That entry is followed by other entries referring to the same activity. The other entries will not be used. See input documentation for more information.",
                        EventFlowContextIdentifiers.Configuration);
                }
                else if (captureAllSources)
                {
                    captureAllSourcesEntries[source.ActivityName] = false;
                }
            }
        }
    }
}