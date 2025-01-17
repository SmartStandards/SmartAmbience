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

      IEnumerable<AmbientField> includedInstances = AmbientField.ExposedInstances.Values.Where(
        (f)=> contract.IsAmbientFieldIncluded(f.Name)
      );

      foreach (AmbientField exposedInstance in includedInstances) {
        capture(exposedInstance.Name, exposedInstance.Value);
      };

    }

    private static void RestoreAmbientFields(FlowingContractDefinition contract, IEnumerable<KeyValuePair<string, string>> sourceToRestore) {
      
      if (AmbientField.ContextAdapter == null || !AmbientField.ContextAdapter.IsUsable) {
        return;
      }

      foreach (KeyValuePair<string, string> entryToRestore in sourceToRestore) {
        if (contract.IsAmbientFieldIncluded(entryToRestore.Key)) {

          bool restored = false;

          foreach (AmbientField exposedInstance in AmbientField.ExposedInstances.Values) {
            if(exposedInstance.Name.Equals(entryToRestore.Key, StringComparison.CurrentCultureIgnoreCase)) {
              Debug.WriteLine($"{nameof(AmbienceHub)} setting value '{entryToRestore.Value}' for AmbientField '{entryToRestore.Key}'");
              exposedInstance.Value = entryToRestore.Value; 
              restored = true;
              break;  
            }
          }

          if (!restored) {
            //prestage value
            Debug.WriteLine($"{nameof(AmbienceHub)} staging value '{entryToRestore.Value}' for AmbientField '{entryToRestore.Key}'");
            AmbientField.InjectPreStagedValue(entryToRestore.Key, entryToRestore.Value);
          }

        }
      };

    }

  }

}
