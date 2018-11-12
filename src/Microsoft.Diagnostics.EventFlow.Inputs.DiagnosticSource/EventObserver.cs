// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.EventFlow.Inputs.DiagnosticSource
{
    internal class EventObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly IHealthReporter _healthReporter;
        private readonly IObserver<EventData> _output;
        private readonly string _providerName;

        public EventObserver(string providerName, IObserver<EventData> output, IHealthReporter healthReporter)
        {
            _providerName = providerName;
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _healthReporter = healthReporter ?? throw new ArgumentNullException(nameof(healthReporter));
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> pair)
        {
            var eventData = new EventData { ProviderName = _providerName, Timestamp = DateTimeOffset.UtcNow };
            eventData.Payload["EventName"] = pair.Key;
            eventData.Payload["Value"] = pair.Value;

            var activity = Activity.Current;
            if (activity != null)
            {
                var id = activity.Id;
                if (id != null)
                {
                    eventData.Payload["ActivityId"] = id;
                }

                var parentId = activity.ParentId;
                if (parentId != null)
                {
                    eventData.Payload["ActivityParentId"] = parentId;
                }

                var duration = activity.Duration;
                if (duration != TimeSpan.Zero)
                {
                    eventData.Payload["Duration"] = duration;
                }

                var tags = activity.Tags;
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        eventData.AddPayloadProperty(tag.Key, tag.Value, _healthReporter, nameof(DiagnosticSourceInput));
                    }
                }

                var baggage = activity.Baggage;
                if (baggage != null)
                {
                    foreach (var bag in baggage)
                    {
                        eventData.AddPayloadProperty(bag.Key, bag.Value, _healthReporter, nameof(DiagnosticSourceInput));
                    }
                }
            }

            _output.OnNext(eventData);
        }
    }
}
