using Logging.SmartStandards.CopyForSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DistributedDataFlow {

  public static partial class AmbienceHub {

    internal const string AmbientFieldEndpointName = "^AmbientFields";

    private static void BindAmbientFieldsAsEndpoint() {

      AmbienceHub.BindCustomEndpoint(
        AmbientFieldEndpointName,
        AmbienceHub.CaptureAmbientFields,
        AmbienceHub.RestoreAmbientFields
      );

    }

    private static void CaptureAmbientFields(FlowingContractDefinition contract, Action<string, string> capture) {

      if (AmbientField.ContextAdapter == null) {
        return;
      }

      Dictionary<string,string> flowingEntries = new Dictionary<string,string>();
      foreach (AmbientField exposedInstance in AmbientField.ExposedInstances.Values) {
        if (contract.IsAmbientFieldIncluded(exposedInstance.Name)) {
          flowingEntries[exposedInstance.Name] = exposedInstance.Value;
        }
      }

      contract.AssertAmbientFieldValues(flowingEntries);

      foreach (KeyValuePair<string,string> flowingEntry in flowingEntries) {
        capture.Invoke(flowingEntry.Key, flowingEntry.Value);
      }

    }

    private static void RestoreAmbientFields(FlowingContractDefinition contract, IEnumerable<KeyValuePair<string, string>> sourceToRestore) {
      
      if (AmbientField.ContextAdapter == null || !AmbientField.ContextAdapter.IsUsable) {
        return;
      }

      Dictionary<string, string> flowingEntries = new Dictionary<string, string>();
      foreach (KeyValuePair<string, string> entryToRestore in sourceToRestore) {
        if (contract.IsAmbientFieldIncluded(entryToRestore.Key)) {
          flowingEntries[entryToRestore.Key] = entryToRestore.Value;
        }
      }

      contract.AssertAmbientFieldValues(flowingEntries);

      foreach (KeyValuePair<string, string> flowingEntry in flowingEntries) {

        bool restored = false;

        foreach (AmbientField exposedInstance in AmbientField.ExposedInstances.Values) {
          if(exposedInstance.Name.Equals(flowingEntry.Key, StringComparison.CurrentCultureIgnoreCase)) {
            //DevLogger.LogTrace($"{nameof(AmbienceHub)} setting value '{flowingEntry.Value}' for AmbientField '{flowingEntry.Key}'");
            exposedInstance.Value = flowingEntry.Value; 
            restored = true;
            break;  
          }
        }

        //prestage value (because the AmbientField instance may not yet be created in this context)
        if (!restored) {
          //DevLogger.LogTrace($"{nameof(AmbienceHub)} staging value '{flowingEntry.Value}' for AmbientField '{flowingEntry.Key}'");
          AmbientField.InjectPreStagedValue(flowingEntry.Key, flowingEntry.Value);
        }

      }

    }

  }

}
