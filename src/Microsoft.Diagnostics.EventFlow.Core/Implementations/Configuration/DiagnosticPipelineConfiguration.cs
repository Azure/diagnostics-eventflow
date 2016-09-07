// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Diagnostics.EventFlow.Configuration
{
    public class DiagnosticPipelineConfiguration
    {
        public int PipelineBufferSize { get; set; }
        public int MaxEventBatchSize { get; set; }
        public int MaxBatchDelayMsec { get; set; }
        public int MaxConcurrency { get; set; }
        public int PipelineCompletionTimeoutMsec { get; set; }

        public DiagnosticPipelineConfiguration()
        {
            PipelineBufferSize = 1000;
            MaxEventBatchSize = 100;
            MaxBatchDelayMsec = 500;
            MaxConcurrency = 8;
            PipelineCompletionTimeoutMsec = 30000;
        }
    }
}
