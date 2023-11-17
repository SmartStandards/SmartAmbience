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
    private static string[] _PrefixesOfDedicatedEndpoints;

    private static PriorityList<string> _EndpointPriorities;

    public delegate void OnRestoreNullHandlerMethod(ref IDictionary<string, string> defaultsToUse);

    /// <summary>
    /// In default there will be a handler method, which throws an exception!
    /// You can apply your own method to this hook in order to let the hub pass an
    /// customized/empty set of items to the endpoints OR
    /// just set the OnRestoreNullHandler=null to skip any restore silently.
    /// Note: this quite different from calling each endpoint with an empty set of entries
    /// - here the endpoints WONT GET ANY TRIGGER!!!
    /// </summary>
    public static OnRestoreNullHandlerMethod OnRestoreNullHandler = (
      (ref IDictionary<string, string> defaultsToUse) => {
        throw new Exception($"AmbienceHub received NULL as sourceToRestore! If this is an allowed scenario, you need to hook up the '{nameof(AmbienceHub)}.{nameof(AmbienceHub.OnRestoreNullHandler)}'!");
      }
    );

    static AmbienceHub() {
      ResetCustomBindings();
    }

    public static void ResetCustomBindings() {
      _EndpointPriorities = new PriorityList<string>();
      _EndpointAdaptersInRestoreOrder = new EndpointAdapter[] { };
      _PrefixesOfDedicatedEndpoints = new string[] { };

      //the flowing for AmbientFields is always present
      BindAmbientFieldsAsEndpoint();
    }

    /// <summary>
    /// Created a wire-up to a custom endpoint which is participating each time an ambient-snapshot is created or restored.
    /// </summary>
    /// <param name="endpointName">
    /// Note: prepending an "^" befor the name will treat the endpoint as ROOT.
    /// In this case the name will NOT be added as prefix to the fieldnames when transporting them.
    /// Any prefixing needs to be dpne by the endpoint itself.
    /// </param>
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

      _PrefixesOfDedicatedEndpoints = _EndpointAdaptersInRestoreOrder.Where((e) => !e.EndpointName.StartsWith("^")).Select((e) => e.EndpointName + ".").ToArray();

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
      var alreadyCapturedKeys = new HashSet<string>();

      foreach (EndpointAdapter endpoint in _EndpointAdaptersInRestoreOrder) {
        bool isGlobalEndpoint = endpoint.EndpointName.StartsWith("^");
        endpoint.CaptureMethod.Invoke(
          (string key, string value) => {

            if (!alreadyCapturedKeys.Add(key)) {
              //same key was already captured before
              throw new Exception(
                $"The AmbientEndpoint '{endpoint.EndpointName}' attemted to capture '{key}', which was already captured."
              );
            }

            if (isGlobalEndpoint) {

              //GUARD: keys of global endpoints have no mandatory prefix, so they can collide with
              //keys, comming from dedicated endpoints. If a dedicated endpoint was registered, then
              //the prefix (accordingly to the endpoint name) is reserved for that endpoint and must not
              //be used by global endpoints!
              if (key.StartsWithAny(_PrefixesOfDedicatedEndpoints, StringComparison.InvariantCultureIgnoreCase)) {
                string concretePrefixForErrorMessage = key.Substring(0, key.IndexOf('.') + 1);
                throw new Exception(
                  $"The AmbientEndpoint '{endpoint.EndpointName}' has caputured a value for '{key}'. This is forbidden, because the prefix '{concretePrefixForErrorMessage}' is reserved for the accordingly named explicit endpoint!"
                );
              }

              capturingCallback.Invoke(key, value);
            }
            else {
              //add the endpoint name as prefix
              capturingCallback.Invoke(endpoint.EndpointName + "." + key, value);
            }
          }
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

      if(sourceToRestore == null) {
        IDictionary<string, string> defaultsToRestore = new Dictionary<string, string>();
        if(OnRestoreNullHandler != null) {
          OnRestoreNullHandler.Invoke(ref defaultsToRestore);//in default this thows an exception!      
        }
        else { //skips the restore!
          return;
          // Note: this quite different from calling each endpoint with an empty set of entries
          // - here the endpoints WONT GET ANY TRIGGER!!!
        }
        sourceToRestore = defaultsToRestore;
      }

      foreach (EndpointAdapter endpoint in _EndpointAdaptersInRestoreOrder) {

        bool isGlobalEndpoint = endpoint.EndpointName.StartsWith("^");
        var filteredSource = sourceToRestore;
        int prefixLength = 0;

        if (!isGlobalEndpoint) {
          string prefixForFiltering = endpoint.EndpointName + ".";
          prefixLength = prefixForFiltering.Length;

          filteredSource = sourceToRestore.Where(
            (e) => e.Key.StartsWith(prefixForFiltering, StringComparison.InvariantCultureIgnoreCase)
          );

        }
        else {

          filteredSource = sourceToRestore.Where(
            (e) => !e.Key.StartsWithAny(_PrefixesOfDedicatedEndpoints, StringComparison.InvariantCultureIgnoreCase)
          );

        }

        var sourceEntriesForCurrentEndpoint = filteredSource.Select(
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
      BindCustomEndpoint("^AmbientFields", captureAmbientFields, restoreAmbientFields);
    }

    private static void captureAmbientFields(Action<string, string> capture) {
      if (AmbientField.ContextAdapter == null) {
        return;
      }
      foreach (AmbientField exposedInstance in AmbientField.ExposedInstances.Values) {
        capture(exposedInstance.Name, exposedInstance.Value);
      };
    }

    private static void restoreAmbientFields(IEnumerable<KeyValuePair<string, string>> sourceToRestore) {
      if (AmbientField.ContextAdapter == null) {
        return;
      }
      foreach (KeyValuePair<string, string> entryToRestore in sourceToRestore) {
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
          AmbientField.ContextAdapter.SetCurrentValue(entryToRestore.Key, entryToRestore.Value);
        }
      };
    }

    #endregion

  }

}
