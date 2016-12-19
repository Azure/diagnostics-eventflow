// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Diagnostics.Tracing;
using System.Diagnostics;

namespace Microsoft.Diagnostics.EventFlow.Inputs
{
    internal static class EventDataExtensions
    {
        public static EventData ToEventData(this EventWrittenEventArgs eventSourceEvent, IHealthReporter healthReporter, string context)
        {
            Debug.Assert(healthReporter != null);

            EventData eventData = new EventData
            {
                ProviderName = eventSourceEvent.EventSource.Name,
                Timestamp = DateTime.UtcNow,
                Level = (LogLevel)(int)eventSourceEvent.Level,
                Keywords = (long)eventSourceEvent.Keywords
            };

            eventSourceEvent.ExtractPayloadData(eventData);

            eventData.AddPayloadProperty("EventId", eventSourceEvent.EventId, healthReporter, context);
            eventData.AddPayloadProperty("EventName", eventSourceEvent.EventName, healthReporter, context);
            eventData.AddPayloadProperty("ActivityID", ActivityPathString(eventSourceEvent.ActivityId), healthReporter, context);
            try
            {
                if (eventSourceEvent.Message != null)
                {
                    // If the event has a badly formatted manifest, the FormattedMessage property getter might throw
                    eventData.AddPayloadProperty("Message",
                        string.Format(CultureInfo.InvariantCulture, eventSourceEvent.Message, eventSourceEvent.Payload.ToArray()),
                        healthReporter,
                        context);
                }
            }
            catch { }

            return eventData;
        }

        private static void ExtractPayloadData(this EventWrittenEventArgs eventSourceEvent, EventData eventData)
        {
            if (eventSourceEvent.Payload == null || eventSourceEvent.PayloadNames == null)
            {
                return;
            }

            IDictionary<string, object> payloadData = eventData.Payload;

            IEnumerator<object> payloadEnumerator = eventSourceEvent.Payload.GetEnumerator();
            IEnumerator<string> payloadNamesEnunmerator = eventSourceEvent.PayloadNames.GetEnumerator();
            while (payloadEnumerator.MoveNext())
            {
                payloadNamesEnunmerator.MoveNext();
                payloadData.Add(payloadNamesEnunmerator.Current, payloadEnumerator.Current);
            }
        }

        /// <summary>
        /// Returns true if 'guid' follow the EventSouce style activity IDs. 
        /// </summary>
        private static unsafe bool IsActivityPath(Guid guid)
        {
            // We compute a very simple checksum which by adding the first 96 bits as 32 bit numbers. 
            uint* uintPtr = (uint*)&guid;
            return (uintPtr[0] + uintPtr[1] + uintPtr[2] + 0x599D99AD == uintPtr[3]);
        }

        /// <summary>
        /// The encoding for a list of numbers used to make Activity  Guids.   Basically
        /// we operate on nibbles (which are nice because they show up as hex digits).  The
        /// list is ended with a end nibble (0) and depending on the nibble value (Below)
        /// the value is either encoded into nibble itself or it can spill over into the
        /// bytes that follow.   
        /// </summary>
        private enum NumberListCodes : byte
        {
            End = 0x0,             // ends the list.   No valid value has this prefix.   
            LastImmediateValue = 0xA,
            PrefixCode = 0xB,
            MultiByte1 = 0xC,   // 1 byte follows.  If this Nibble is in the high bits, it the high bits of the number are stored in the low nibble.   
                                // commented out because the code does not explicitly reference the names (but they are logically defined).  
                                // MultiByte2 = 0xD,   // 2 bytes follow (we don't bother with the nibble optimzation
                                // MultiByte3 = 0xE,   // 3 bytes follow (we don't bother with the nibble optimzation
                                // MultiByte4 = 0xF,   // 4 bytes follow (we don't bother with the nibble optimzation
        }

        /// <summary>
        /// returns a string representation for the activity path.  If the GUID is not an activity path then it returns
        /// the normal string representation for a GUID. 
        /// </summary>
        private static unsafe string ActivityPathString(Guid guid)
        {
            if (!IsActivityPath(guid))
                return guid.ToString();

            StringBuilder sb = new StringBuilder();
            sb.Append('/'); // Use // to start to make it easy to anchor 
            byte* bytePtr = (byte*)&guid;
            byte* endPtr = bytePtr + 12;
            char separator = '/';
            while (bytePtr < endPtr)
            {
                uint nibble = (uint)(*bytePtr >> 4);
                bool secondNibble = false;              // are we reading the second nibble (low order bits) of the byte.
                NextNibble:
                if (nibble == (uint)NumberListCodes.End)
                    break;
                if (nibble <= (uint)NumberListCodes.LastImmediateValue)
                {
                    sb.Append('/').Append(nibble);
                    if (!secondNibble)
                    {
                        nibble = (uint)(*bytePtr & 0xF);
                        secondNibble = true;
                        goto NextNibble;
                    }
                    // We read the second nibble so we move on to the next byte. 
                    bytePtr++;
                    continue;
                }
                else if (nibble == (uint)NumberListCodes.PrefixCode)
                {
                    // This are the prefix codes.   If the next nibble is MultiByte, then this is an overflow ID.  
                    // we we denote with a $ instead of a / separator.  

                    // Read the next nibble.  
                    if (!secondNibble)
                        nibble = (uint)(*bytePtr & 0xF);
                    else
                    {
                        bytePtr++;
                        if (endPtr <= bytePtr)
                            break;
                        nibble = (uint)(*bytePtr >> 4);
                    }

                    if (nibble < (uint)NumberListCodes.MultiByte1)
                    {
                        // If the nibble is less than MultiByte we have not defined what that means 
                        // For now we simply give up, and stop parsing.  We could add more cases here...
                        return guid.ToString();
                    }
                    // If we get here we have a overflow ID, which is just like a normal ID but the separator is $
                    separator = '$';
                    // Fall into the Multi-byte decode case.  
                }

                Debug.Assert((uint)NumberListCodes.MultiByte1 <= nibble);
                // At this point we are decoding a multi-byte number, either a normal number or a 
                // At this point we are byte oriented, we are fetching the number as a stream of bytes. 
                uint numBytes = nibble - (uint)NumberListCodes.MultiByte1;

                uint value = 0;
                if (!secondNibble)
                    value = (uint)(*bytePtr & 0xF);
                bytePtr++;       // Adance to the value bytes

                numBytes++;     // Now numBytes is 1-4 and reprsents the number of bytes to read.  
                if (endPtr < bytePtr + numBytes)
                    break;

                // Compute the number (little endian) (thus backwards).  
                for (int i = (int)numBytes - 1; 0 <= i; --i)
                    value = (value << 8) + bytePtr[i];

                // Print the value
                sb.Append(separator).Append(value);

                bytePtr += numBytes;        // Advance past the bytes.
            }

            if (sb.Length == 0)
                sb.Append('/');
            return sb.ToString();
        }

    }
}