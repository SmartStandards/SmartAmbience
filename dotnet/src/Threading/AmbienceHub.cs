using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Threading {

  public static class AmbienceHub {

    [DebuggerDisplay("{EndpointName}")]
    private class EndpointAdapter {
      public String EndpointName { get; set; }
      public Action<Action<string, string>> CaptureMethod { get; set; }
      public Action<IEnumerable<KeyValuePair<string, string>>> RestoreMethod { get; set; }
      public int AssertRestorePerformanceMs { get; set; }
      public string[] RestoreAfter { get; set; }
    }

    private static EndpointAdapter[] _EndpointAdaptersInRestoreOrder;

    private static PriorityList<string> _EndpointPriorities;

    static AmbienceHub() {
      ResetCustomBindings();
    }

    public static void ResetCustomBindings() {
      _EndpointPriorities = new PriorityList<string>();
      _EndpointAdaptersInRestoreOrder = new EndpointAdapter[] { };

      //the flowing for AmbientFields is always present
      BindAmbientFieldsAsEndpoint();
    }

    /// <summary>
    /// Created a wire-up to a custom endpoint which is participating each time an ambient-snapshot is created or restored.
    /// </summary>
    /// <param name="endpointName"></param>
    /// <param name="captureMethod"></param>
    /// <param name="restoreMethod"></param>
    /// <param name="assertRestorePerformanceMs"></param>
    /// <param name="restoreAfter">one or more endpointName on which the endpoint that is currently registered will rely on</param>
    public static void BindCustomEndpoint(
      string endpointName,
      Action<Action<string, string>> captureMethod,
      Action<IEnumerable<KeyValuePair<string, string>>> restoreMethod,
      int assertRestorePerformanceMs = 500,
      params string[] restoreAfter
    ) {

      if (_EndpointAdaptersInRestoreOrder.Where((a)=> a.EndpointName.EndsWith(endpointName, StringComparison.CurrentCultureIgnoreCase)).Any()){
        throw new Exception($"Cannot {nameof(BindCustomEndpoint)} named '{endpointName}' to the {nameof(AmbienceHub)} - this EndpointName already exists!"); 
      }

      var adapter = new EndpointAdapter();
      adapter.EndpointName = endpointName;
      adapter.CaptureMethod = captureMethod;
      adapter.RestoreMethod = restoreMethod;
      adapter.AssertRestorePerformanceMs = assertRestorePerformanceMs;
      adapter.RestoreAfter = restoreAfter;

      foreach (string higherPrioritizedEndpointName in restoreAfter) {
        _EndpointPriorities.TryDeclarePreference(higherPrioritizedEndpointName.ToLower(), endpointName.ToLower());
      }

      _EndpointAdaptersInRestoreOrder = _EndpointAdaptersInRestoreOrder.Union(new EndpointAdapter [] {adapter}).OrderBy(
        (ep) => _EndpointPriorities.PriorityOf(ep.EndpointName.ToLower())
      ).ToArray();

  }

    /// <summary>
    /// Here a custom logging-method can be injected - it will be invoked, when a endpoint will exceed the maximum
    /// time, that has been granded for its restore-operation. The hook get the endpointName an the elapsed milliseconds.
    /// </summary>
    public static Action<string, int> OnRestorePerformanceAssertFailedMethod { get; set; } = (
      (string endpointName, int elapsedMs) => Trace.TraceWarning($"{nameof(AmbienceHub)}: Restore performance assert failed for endpoint '{endpointName}'! Restore took {elapsedMs}ms!")
    );

    public static void CaptureCurrentValuesTo(IDictionary<string, string> targetContainer) {
      CaptureCurrentValuesTo((string key, string value) => targetContainer[key] = value);
    }

    public static void CaptureCurrentValuesTo(Action<string,string> capturingCallback) {
      foreach (EndpointAdapter endpoint in _EndpointAdaptersInRestoreOrder) {
        endpoint.CaptureMethod.Invoke(
          (string key, string value) => capturingCallback.Invoke(endpoint.EndpointName + "." + key, value) //add the endpoint name as prefix
        );
      }
    }

    public static string CaptureCurrentValuesAsDump(){
      var sb = new StringBuilder(2000);
      CaptureCurrentValuesTo((string key, string value) => sb.AppendLine(key + ": " + value));
      return sb.ToString();
    }

    /// <summary>
    /// WARNING: THIS IS ONLY MADE TO BE USED FROM ABSTRACT TRANSPORT-TECHNOLOGY
    /// </summary>
    /// <param name="sourceToRestore"></param>
    public static void RestoreValuesFrom(IEnumerable<KeyValuePair<string, string>> sourceToRestore) {

      foreach (EndpointAdapter endpoint in _EndpointAdaptersInRestoreOrder) {

        string prefix = endpoint.EndpointName + ".";
        int prefixLength = prefix.Length;

        var sourceEntriesForCurrentEndpoint = sourceToRestore.Where(
          (e) => e.Key.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
        ).Select(
          (e) => new KeyValuePair<string, string>(e.Key.Substring(prefixLength), e.Value)
        ).ToArray();

        DateTime restoreStart = DateTime.Now;

        endpoint.RestoreMethod.Invoke(sourceEntriesForCurrentEndpoint);

        int totalMs = Convert.ToInt32(DateTime.Now.Subtract(restoreStart).TotalMilliseconds);
        if (OnRestorePerformanceAssertFailedMethod != null && totalMs > endpoint.AssertRestorePerformanceMs) {
          OnRestorePerformanceAssertFailedMethod.Invoke(endpoint.EndpointName, totalMs);
        }

      }

    }

    #region " default endpoint for exposed AmbientFields "

    private static void BindAmbientFieldsAsEndpoint() {
      BindCustomEndpoint("Context", captureAmbientFields, restoreAmbientFields);
    }

    private static void captureAmbientFields(Action<string, string> capture) {
      if (AmbientField.ContextAdapter == null) {
        return;
      }
      foreach (AmbientField exposedInstance in AmbientField.ExposedInstances) {
        capture(exposedInstance.Name, exposedInstance.Value);
      };
    }

    private static void restoreAmbientFields(IEnumerable<KeyValuePair<string, string>> sourceToRestore) {
      if (AmbientField.ContextAdapter == null) {
        return;
      }
      foreach (KeyValuePair<string, string> entryToRestore in sourceToRestore) {
        bool restored = false;
        foreach (AmbientField exposedInstance in AmbientField.ExposedInstances) {
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
          AmbientField.ContextAdapter.SetCurrentValue(entryToRestore.Key, entryToRestore.Value);
        }
      };
    }

    #endregion

  }

}
