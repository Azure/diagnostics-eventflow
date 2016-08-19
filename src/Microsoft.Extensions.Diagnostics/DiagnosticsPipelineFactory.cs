// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Validation;
using Microsoft.Extensions.Diagnostics.Configuration;
using System.Diagnostics;

namespace Microsoft.Extensions.Diagnostics
{
    public class DiagnosticsPipelineFactory
    {
        public static IDisposable CreatePipeline(IConfiguration configuration, IHealthReporter healthReporter)
        {
            Requires.NotNull(configuration, nameof(configuration));
            Requires.NotNull(healthReporter, nameof(healthReporter));

            IDictionary<string, string> inputFactories;
            IDictionary<string, string> outputFactories;
            IDictionary<string, string> filterFactories;
            CreateItemFactories(configuration, healthReporter, out inputFactories, out outputFactories, out filterFactories);


            // Step 1: instantiate inputs
            IConfigurationSection inputConfigurationSection = configuration.GetSection("inputs");
            if (inputConfigurationSection == null)
            {
                healthReporter.ReportProblem($"{nameof(DiagnosticsPipelineFactory)}: 'inputs' configuration section missing");
                return EmptyDisposable.Instance;
            }

            List<ItemWithChildren<IObservable<EventData>, object>> inputCreationResult;
            if (!ProcessSection<IObservable<EventData>, object>(
                inputConfigurationSection, 
                healthReporter, 
                inputFactories,
                childFactories: null,
                childSectionName: null, 
                isOptional:false, 
                createdItems: out inputCreationResult))
            {
                return EmptyDisposable.Instance;
            }
            List<IObservable<EventData>> inputs = inputCreationResult.Select(item => item.Item).ToList();
            Debug.Assert(inputs.Count > 0);


            // Step 2: instantiate global filters (if any)
            IConfigurationSection globalFilterConfigurationSection = configuration.GetSection("filters");
            List<ItemWithChildren<IEventFilter<EventData>, object>> globalFilterCreationResult;
            // It completely fine to have a pipeline with no globals filters section, or an empty one
            ProcessSection<IEventFilter<EventData>, object>(
                globalFilterConfigurationSection, 
                healthReporter, 
                filterFactories,
                childFactories: null,
                childSectionName: null, 
                isOptional: true, 
                createdItems: out globalFilterCreationResult);
            List<IEventFilter<EventData>> globalFilters = globalFilterCreationResult.Select(item => item.Item).ToList();


            // Step 3: instantiate outputs
            IConfigurationSection outputConfigurationSection = configuration.GetSection("outputs");
            if (outputConfigurationSection == null)
            {
                healthReporter.ReportProblem($"{nameof(DiagnosticsPipelineFactory)}: 'outputs' configuration section missing");
                DisposeOf(inputs);
                return EmptyDisposable.Instance;
            }

            List<ItemWithChildren<IEventSender<EventData>, IEventFilter<EventData>>> outputCreationResult;
            if (!ProcessSection<IEventSender<EventData>, IEventFilter<EventData>>(
                outputConfigurationSection,
                healthReporter,
                outputFactories,
                filterFactories,
                childSectionName: "filters",
                isOptional: false,
                createdItems: out outputCreationResult))
            {
                DisposeOf(inputs);
                return EmptyDisposable.Instance;
            }


            // Step 4: assemble and return the pipeline
            IReadOnlyCollection<EventSink<EventData>> sinks = outputCreationResult.Select(outputResult =>
                new EventSink<EventData>(outputResult.Item, globalFilters.Concat(outputResult.Children))
            ).ToList();

            DiagnosticsPipeline<EventData> pipeline = new DiagnosticsPipeline<EventData>(healthReporter, inputs, sinks);
            return pipeline;
        }

        private static bool ProcessSection<PipelineItemType, PipelineItemChildType>(
            IConfigurationSection configurationSection,
            IHealthReporter healthReporter,            
            IDictionary<string, string> itemFactories,
            IDictionary<string, string> childFactories,
            string childSectionName,
            bool isOptional,
            out List<ItemWithChildren<PipelineItemType, PipelineItemChildType>> createdItems)
        {
            Debug.Assert(isOptional || configurationSection != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(configurationSection.Key));
            Debug.Assert(healthReporter != null);            
            Debug.Assert(itemFactories != null);
            Debug.Assert((string.IsNullOrEmpty(childSectionName) && childFactories == null) || (!string.IsNullOrEmpty(childSectionName) && childFactories != null));

            createdItems = new List<ItemWithChildren<PipelineItemType, PipelineItemChildType>>();

            if (isOptional && configurationSection == null)
            {
                return true;
            }

            List<IConfigurationSection> itemConfigurationFragments = configurationSection.GetChildren().ToList();
            if (itemConfigurationFragments.Count == 0)
            {
                if (!isOptional)
                {
                    ReportSectionEmpty(configurationSection, healthReporter);
                }
                return isOptional;
            }

            foreach (var itemFragment in itemConfigurationFragments)
            {
                ItemConfiguration itemConfiguration = new ItemConfiguration();
                try
                {
                    itemFragment.Bind(itemConfiguration);
                }
                catch
                {
                    ReportInvalidConfigurationFragment(healthReporter, itemFragment);
                    continue;
                }

                string itemFactoryTypeName;
                if (!itemFactories.TryGetValue(itemConfiguration.Type, out itemFactoryTypeName))
                {
                    ReportUnknownItemType(configurationSection, healthReporter, itemConfiguration);
                    continue;
                }

                IPipelineItemFactory<PipelineItemType> factory;
                PipelineItemType item;
                try
                {
                    var itemFactoryType = Type.GetType(itemFactoryTypeName);
                    factory = Activator.CreateInstance(itemFactoryType) as IPipelineItemFactory<PipelineItemType>; 
                    if (factory == null)
                    {
                        ReportItemCreationFailed(healthReporter, itemConfiguration.Type);
                    }
                    item = factory.CreateItem(itemFragment, healthReporter);
                }
                catch (Exception e)
                {
                    ReportItemCreationFailed(healthReporter, itemConfiguration.Type, e);
                    continue;
                }
                 
                // The factory will do its own error reporting, so if it returns null, no further error reporting is necessary.
                if (item == null)
                {
                    continue;
                }

                List<ItemWithChildren<PipelineItemChildType, object>> children = null;
                if (!string.IsNullOrEmpty(childSectionName))
                {
                    IConfigurationSection childrenSection = itemFragment.GetSection(childSectionName);
                    ProcessSection<PipelineItemChildType, object>(
                        childrenSection, 
                        healthReporter, 
                        childFactories, 
                        childFactories: null,       // Only one level of nexting is supported
                        childSectionName: null,
                        isOptional: true, 
                        createdItems: out children);

                    createdItems.Add(new ItemWithChildren<PipelineItemType, PipelineItemChildType>(item, children.Select(c => c.Item).ToList()));
                }
                else
                {
                    createdItems.Add(new ItemWithChildren<PipelineItemType, PipelineItemChildType>(item, null));
                }                
            }

            if (createdItems.Count == 0 && !isOptional)
            {
                ReportNoItemsCreated(configurationSection, healthReporter);
                return false;
            }

            return true;
        }

        private static void ReportItemCreationFailed(IHealthReporter healthReporter, string itemType, Exception e = null)
        {
            string errorMessage = $"{nameof(DiagnosticsPipelineFactory)}: item of type '{itemType}' could not be created";
            if (e != null)
            {
                errorMessage += Environment.NewLine + e.ToString();
            }
            healthReporter.ReportWarning(errorMessage);
        }

        private static void ReportInvalidConfigurationFragment(IHealthReporter healthReporter, IConfigurationSection itemFragment)
        {
            healthReporter.ReportWarning($"{nameof(DiagnosticsPipelineFactory)}: invalid configuration fragment:{Environment.NewLine}{itemFragment.Value}");
        }

        private static void ReportSectionEmpty(IConfigurationSection configurationSection, IHealthReporter healthReporter)
        {
            healthReporter.ReportWarning($"{nameof(DiagnosticsPipelineFactory)}: '{configurationSection.Key}' configuration section is empty");
        }

        private static void ReportNoItemsCreated(IConfigurationSection configurationSection, IHealthReporter healthReporter)
        {
            healthReporter.ReportWarning($"{nameof(DiagnosticsPipelineFactory)}: could not create any pipeline items out of configuration section '{configurationSection.Key}'");
        }

        private static void ReportUnknownItemType(IConfigurationSection configurationSection, IHealthReporter healthReporter, ItemConfiguration itemConfiguration)
        {
            healthReporter.ReportWarning($"{nameof(DiagnosticsPipelineFactory)}: unknown type '{itemConfiguration.Type}' in configuration section '{configurationSection.Key}'");
        }

        private static void CreateItemFactories(
            IConfiguration configuration, 
            IHealthReporter healthReporter,
            out IDictionary<string, string> inputFactories,
            out IDictionary<string, string> outputFactories,
            out IDictionary<string, string> filterFactories)
        {
            // TODO: finalize the set of "well-known" pipeline elements

            // TODO: add proper PublicKeyToken to factory references when compiling relase bits

            Debug.Assert(configuration != null);
            Debug.Assert(healthReporter != null);

            inputFactories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            inputFactories["EventSource"] = "Microsoft.Extensions.Diagnostics.ObservableEventListenerFactory, Microsoft.Extensions.Diagnostics.Inputs.EventSource, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            inputFactories["PerformanceCounter"] = "Microsoft.Extensions.Diagnostics.PerformanceCounterListenerFactory, Microsoft.Extensions.Diagnostics.Inputs.PerformanceCounter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";


            outputFactories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            outputFactories["ApplicationInsights"] = "Microsoft.Extensions.Diagnostics.ApplicationInsightsSenderFactory, Microsoft.Extensions.Diagnostics.Outputs.ApplicationInsights, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            outputFactories["StdOutput"] = "Microsoft.Extensions.Diagnostics.Outputs.StdOutputFactory, Microsoft.Extensions.Diagnostics.Outputs.StdOutput, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            filterFactories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            filterFactories["metadata"] = "Microsoft.Extensions.Diagnostics.EventMetadataFilterFactory, Microsoft.Extensions.Diagnostics.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

            // TODO: implement 3rd party input/output/filter instantiation driven by the contents of "extensions" section
        }

        private static void DisposeOf(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                (item as IDisposable)?.Dispose();
            }
        }

        private class ItemWithChildren<ItemType, ChildType>
        {
            public ItemWithChildren(ItemType item, IEnumerable<ChildType> children)
            {
                Debug.Assert(item != null);
                Item = item;
                Children = children;
            }

            public ItemType Item;
            public IEnumerable<ChildType> Children;
        }
    }
}
