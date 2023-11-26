using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading;
using System;

namespace DistributedDataFlow {

  public static partial class AmbienceHub {

    internal delegate void CaptureMethodWithContractInfo(FlowingContractDefinition contract, Action<string, string> target);
    internal delegate void RestoreMethodWithContractInfo(FlowingContractDefinition contract, IEnumerable<KeyValuePair<string, string>> source);

    [DebuggerDisplay("{EndpointName}")]
    private class EndpointAdapter {
      public String EndpointName { get; set; }
      public CaptureMethodWithContractInfo CaptureMethod { get; set; }
      public RestoreMethodWithContractInfo RestoreMethod { get; set; }
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
    /// - in the first case the endpoints WONT GET ANY TRIGGER!!!
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
      int assertRestorePerformanceMs = 200,
      params string[] restoreAfter
    ) {

      BindCustomEndpoint(
        endpointName,     
        (contractName, target) => captureMethod.Invoke(target),
        (contractName, source) => restoreMethod.Invoke(source),
        assertRestorePerformanceMs,
        restoreAfter
      );

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
    internal static void BindCustomEndpoint(
      string endpointName,
      CaptureMethodWithContractInfo captureMethod,
      RestoreMethodWithContractInfo restoreMethod,
      int assertRestorePerformanceMs = 200,
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

    /// <summary>
    /// WARNING: THIS IS ONLY MADE TO BE USED FROM ABSTRACT TRANSPORT-TECHNOLOGY
    /// </summary>
    /// <param name="targetContainer">
    /// Container to which a Snapshot of ambient data should be added. 
    /// Existing entries will be overwritten on key collition, other remaining entries will be preserved.
    /// </param>
    public static void CaptureCurrentValuesTo(IDictionary<string, string> targetContainer) {
      //NOTE: this overload-method is needed because if we would use an optional parameter flowingContractName=null
      //in the called method, if wont be compatible for the usage as delegate in the way will be done in some usecases
      CaptureCurrentValuesTo(targetContainer, null);
    }

    /// <summary>
    /// WARNING: THIS IS ONLY MADE TO BE USED FROM ABSTRACT TRANSPORT-TECHNOLOGY
    /// </summary>
    /// <param name="targetContainer">
    /// Container to which a Snapshot of ambient data should be added. 
    /// Existing entries will be overwritten on key collition, other remaining entries will be preserved.
    /// </param>
    /// <param name="flowingContractName">
    /// Name of a 'FlowingContract' (optional Feature - see the 'DefineFlowingContract(..)' method)
    /// which shall be used to select an explicitely defined subset of custom Endpoints and/or 
    /// AmbientFields to be included in the snapshot.
    /// </param>
    public static void CaptureCurrentValuesTo(IDictionary<string, string> targetContainer, string flowingContractName) {
      CaptureCurrentValuesTo((string key, string value) => targetContainer[key] = value, flowingContractName);
    }

    /// <summary>
    /// WARNING: THIS IS ONLY MADE TO BE USED FROM ABSTRACT TRANSPORT-TECHNOLOGY
    /// </summary>
    /// <param name="capturingCallback">Callback for processing an Snapshot of ambient data</param>
    public static void CaptureCurrentValuesTo(Action<string, string> capturingCallback) {
      //NOTE: this overload-method is needed because if we would use an optional parameter flowingContractName=null
      //in the called method, if wont be compatible for the usage as delegate in the way will be done in some usecases
      CaptureCurrentValuesTo(capturingCallback, null);
    }

    /// <summary>
    /// WARNING: THIS IS ONLY MADE TO BE USED FROM ABSTRACT TRANSPORT-TECHNOLOGY
    /// </summary>
    /// <param name="capturingCallback">Callback for processing an Snapshot of ambient data</param>
    /// <param name="flowingContractName">
    /// Name of a 'FlowingContract' (optional Feature - see the 'DefineFlowingContract(..)' method)
    /// which shall be used to select an explicitely defined subset of custom Endpoints and/or 
    /// AmbientFields to be included in the snapshot.
    /// </param>
    public static void CaptureCurrentValuesTo(Action<string,string> capturingCallback, string flowingContractName) {
      var alreadyCapturedKeys = new HashSet<string>();

      FlowingContractDefinition contract = AmbienceHub.GetFlowingContractDefinition(flowingContractName);

      IEnumerable<EndpointAdapter> includedEndpoints = _EndpointAdaptersInRestoreOrder.Where((ep) => contract.IsEndpointIncluded(ep.EndpointName));

      foreach (EndpointAdapter endpoint in includedEndpoints) {
        bool isGlobalEndpoint = endpoint.EndpointName.StartsWith("^");
        endpoint.CaptureMethod.Invoke(
          contract,
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

              ////// CAPTURE //////
              capturingCallback.Invoke(key, value);
            }
            else {

              ////// CAPTURE //////
              capturingCallback.Invoke(endpoint.EndpointName + "." + key, value);
              //                              ^^^ add the endpoint name as prefix
            }
          }
        );
      }

    }

    /// <summary>
    /// Captures ambient values and dumps them into human readable string, that can be used for Troubleshooting.
    /// </summary>
    /// <param name="flowingContractName">
    /// Name of a 'FlowingContract' (optional Feature - see the 'DefineFlowingContract(..)' method)
    /// which shall be used to select an explicitely defined subset of custom Endpoints and/or 
    /// AmbientFields to be included in the snapshot.
    /// </param>
    /// <returns>an human readable Snapshot of ambient data</returns>
    public static string CaptureCurrentValuesAsDump(string flowingContractName = null) {
      var sb = new StringBuilder(2000);
      CaptureCurrentValuesTo((string key, string value) => sb.AppendLine(key + ": " + value), flowingContractName);
      return sb.ToString();
    }

    /// <summary>
    /// WARNING: THIS IS ONLY MADE TO BE USED FROM ABSTRACT TRANSPORT-TECHNOLOGY
    /// </summary>
    /// <param name="sourceToRestore">an Snapshot of ambient data</param>
    public static void RestoreValuesFrom(IEnumerable<KeyValuePair<string, string>> sourceToRestore) {
      //NOTE: this overload-method is needed because if we would use an optional parameter flowingContractName=null
      //in the called method, if wont be compatible for the usage as delegate in the way will be done in some usecases
      RestoreValuesFrom(sourceToRestore, null);
    }

    /// <summary>
    /// WARNING: THIS IS ONLY MADE TO BE USED FROM ABSTRACT TRANSPORT-TECHNOLOGY
    /// </summary>
    /// <param name="sourceToRestore">an Snapshot of ambient data</param>
    /// <param name="flowingContractName">
    /// Name of a 'FlowingContract' (optional Feature - see the 'DefineFlowingContract(..)' method)
    /// which shall be used to select an explicitely defined subset of custom Endpoints and/or 
    /// AmbientFields to be restored from the given snapshot.
    /// </param>
    public static void RestoreValuesFrom(IEnumerable<KeyValuePair<string, string>> sourceToRestore, string flowingContractName) {

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

      FlowingContractDefinition contract = AmbienceHub.GetFlowingContractDefinition(flowingContractName);

      IEnumerable<EndpointAdapter> includedEndpoints = _EndpointAdaptersInRestoreOrder.Where((ep) => contract.IsEndpointIncluded(ep.EndpointName));

      foreach (EndpointAdapter endpoint in includedEndpoints) {

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

        ////// RESTORE //////
        endpoint.RestoreMethod.Invoke(contract, sourceEntriesForCurrentEndpoint);

        int totalMs = Convert.ToInt32(DateTime.Now.Subtract(restoreStart).TotalMilliseconds);
        if (OnRestorePerformanceAssertFailedMethod != null && totalMs > endpoint.AssertRestorePerformanceMs) {
          OnRestorePerformanceAssertFailedMethod.Invoke(endpoint.EndpointName, totalMs);
        }

      }

    }

    #region " 'FlowingContracts' "

    private static Dictionary<string, FlowingContractDefinition> _FlowingContracts = null;

    public static void DefineFlowingContract(string contractName, Action<FlowingContractDefinition> definitionMethod) {
    
      //safes performance during processing
      if (contractName != contractName.ToLower()) {
        throw new ArgumentException("The name for a FlowingContract must be in lower casing!");
      }
    
      if (_FlowingContracts != null && _FlowingContracts.ContainsKey(contractName)) {
        throw new ArgumentException($"A FlowingContract with name '{contractName}' already exisits!");
      }

      var newDefinition = new FlowingContractDefinition(false);
      definitionMethod.Invoke(newDefinition);
      newDefinition.MakeImmutable();

      if (_FlowingContracts == null) {
        _FlowingContracts = new Dictionary<string, FlowingContractDefinition>();
      }
      _FlowingContracts.Add(contractName, newDefinition);

    }

    private static FlowingContractDefinition GetFlowingContractDefinition(string flowingContractName) {
      FlowingContractDefinition contract = null;
      if (_FlowingContracts == null) {
        if (!string.IsNullOrWhiteSpace(flowingContractName)) {
          throw new Exception($"The '{nameof(AmbienceHub)}' was NOT YET configured to use FlowingContracts! see '{nameof(AmbienceHub.DefineFlowingContract)}'");
        }
        contract = FlowingContractDefinition.DefaultDefinition;
      }
      else {
        if (string.IsNullOrWhiteSpace(flowingContractName)) {
          throw new Exception($"The '{nameof(AmbienceHub)}' was configured to require a FlowingContract - please provide a flowingContractName when calling the capture method!");
        }
        if (!_FlowingContracts.TryGetValue(flowingContractName, out contract)) {
          throw new Exception($"The '{nameof(AmbienceHub)}' cannot address the correct set of ambient data: there is no FlowingContract named '{flowingContractName}'!");
        }
      }
      return contract;
    }

    public static void DisableAndResetFlowingContracts() {
      _FlowingContracts = null;
    }

    #endregion

  }

}
