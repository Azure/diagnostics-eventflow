using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MsgPack.Serialization;
using Microsoft.Diagnostics.EventFlow.Utilities.Etw;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal class MsgPackTraceEventSession : StandardTraceEventSession
    {

        /// <summary>
        /// MsgPackTraceEventSession allows to decode MsgPack-encoded binary events.
        /// </summary>
        /// <param name="sessionNamePrefix">See @base</param>
        /// <param name="cleanupOldSessions">See @base</param>
        /// <param name="reuseExisting">See @base</param>
        /// <param name="healthReporter">See @base</param>
        public MsgPackTraceEventSession(string sessionNamePrefix, bool cleanupOldSessions, bool reuseExisting, IHealthReporter healthReporter) :
            base(sessionNamePrefix, cleanupOldSessions, reuseExisting, healthReporter)
        {
        }

        /// <summary>
        /// Convert TraceEvent in MsgPack format to EventData
        /// </summary>
        /// <param name="data"></param>
        /// <param name="reporter"></param>
        /// <returns></returns>
        internal static EventData DecodeMessagePack(TraceEvent data, IHealthReporter reporter)
        {
            EventData eventData = new EventData
            {
                ProviderName = data.ProviderName,
                Timestamp = data.TimeStamp,
                Level = (LogLevel)data.Level,
                Keywords = (long)data.Keywords,
            };

            if (data.EventDataLength==0)
            {
                reporter.ReportProblem("MsgPack decoding failed: empty payload!");
                return eventData;
            }

            byte[] managedArray = new byte[data.EventDataLength];
            Marshal.Copy(data.DataStart, managedArray, 0, data.EventDataLength);

            try
            {
                var obj = MessagePackSerializer.UnpackMessagePackObject(managedArray);
                if (obj.IsDictionary)
                {
                    foreach (var kv in obj.AsDictionary())
                    {
                        var key = kv.Key.AsString();
                        object val = null;
                        switch (Type.GetTypeCode(kv.Value.UnderlyingType))
                        {
                            case TypeCode.String:
                                val = kv.Value.AsString();
                                break;

                            case TypeCode.Boolean:
                                val = kv.Value.AsBoolean();
                                break;

                            case TypeCode.Decimal:
                                val = kv.Value.AsInt64();
                                break;

                            case TypeCode.Int16:
                                val = kv.Value.AsInt16();
                                break;

                            case TypeCode.Int32:
                                val = kv.Value.AsInt32();
                                break;

                            case TypeCode.Int64:
                                val = kv.Value.AsInt64();
                                break;

                            case TypeCode.UInt16:
                                val = kv.Value.AsUInt16();
                                break;

                            case TypeCode.UInt32:
                                val = kv.Value.AsUInt32();
                                break;

                            case TypeCode.UInt64:
                                val = kv.Value.AsUInt64();
                                break;

                            case TypeCode.Double:
                                val = kv.Value.AsDouble();
                                break;

                            case TypeCode.Single:
                                val = kv.Value.AsSingle();
                                break;

                            default:
                                reporter.ReportWarning("Unknown type: " + kv.GetType());
                                val = kv.Value.ToString();
                                break;
                        }
                        eventData.Payload[key] = val;
                    }
                }
                else
                {
                    eventData.Payload["Message"] = obj.ToString();
                }

            }
            catch (ArgumentNullException ex)
            {
                reporter.ReportProblem("MsgPack decoding failed: " + ex.ToString());
                return eventData;
            };

            return eventData;
        }

        /// <summary>
        /// Process incoming ETW-MsgPack events
        /// </summary>
        /// <param name="onEvent"></param>
        public override void Process(Action<EventData> onEvent)
        {
            if (this.inner == null)
            {
                throw new ObjectDisposedException(nameof(StandardTraceEventSession));
            }

            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            if (!isProcessing)
            {
                isProcessing = true;
                this.inner.Source.AllEvents += (traceEvent) =>
                {
                    if (!TplActivities.TplEventSourceGuid.Equals(traceEvent.ProviderGuid))
                    {
                        /* TODO: For forward protocol where one single traceEvent contains
                         * several events we need to extract them and forward individually
                         */
                        onEvent(DecodeMessagePack(traceEvent, this.healthReporter));
                    }
                };
                this.inner.Source.Process();
            }
        }

    }

}
