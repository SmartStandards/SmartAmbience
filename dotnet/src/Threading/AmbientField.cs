using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Threading {

  /// <summary>
  ///    Contains a named string value that is stored AsyncLocal and in an external short living context ("Operation", "Request", ...) as fallback.
  /// </summary>
  [DebuggerDisplay(nameof(AmbientField) + " ({Name})")]
  public partial class AmbientField {

    private static IAmbienceToSomeContextAdapter _ContextAdapter;

    private bool _ContextAdapterEventHandlersAdded;

    private static string _ContextAdapterSetterCaller;

    private AmbientFieldDebugInfo _DebugInfo = new AmbientFieldDebugInfo();

    private static List<AmbientField> _ExposedInstances = new List<AmbientField>();

    private AsyncLocal<string> _InnerAsyncLocal = new AsyncLocal<string>();

    private bool _RootValueIsWriteOnce;

    private AmbientField() {
    }

    /// <summary>
    ///   Creates a new instance of an AmbientField. 
    ///   Optionally, registers it to the ExposedInstances catalog.
    /// </summary>
    /// <param name="name">
    ///   The name of the field (best practice is to use the same name here and for the field that is holding the AmbientField).
    ///   The name will be used as (part of the) key when storing the value in the external fallback context.
    ///   The name might also be used when exposed (in flowing scenarios) - in this case it should be globally unique.
    /// </param>
    /// <param name="exposedInstance">
    ///   Tags the AmbientField as "exposed". It will be registered in the ExposedInstances catalog.
    ///   The intended purpose is flowing: Exposed AmbientFields can be collected by a flowing engine (to be flowed accross web service hops).
    /// </param>
    public AmbientField(string name, bool exposedInstance = false, [CallerFilePath] string callerFileFullName = null) {

      this.ConstructorCallerFileFullName = callerFileFullName;

      this.Owner = Path.GetFileNameWithoutExtension(callerFileFullName);

      this.Name = name;

      this.Key = this.Owner + "." + name + "'" + this.GetHashCode().ToString();

      if (exposedInstance) {
        _ExposedInstances.Add(this);
      }

    }

    private void AssertContextAdapter() {

      if (_ContextAdapter is null) {
        throw new Exception($"{nameof(ContextAdapter)} must be initialized before using {this.Name}!");
      }

      if (!_ContextAdapterEventHandlersAdded) {
        _ContextAdapter.CurrentContextIsTerminating += this.ContextAdapter_IsTerminating;
        _ContextAdapterEventHandlersAdded = true;
      }

    }

    public void InvokeUnderTemporaryBranch(string overridingValue, Action actionToInvoke) {

      string rescuedCurrentValue = _InnerAsyncLocal.Value;

      try {
        _InnerAsyncLocal.Value = overridingValue;
        actionToInvoke.Invoke();
      }
      finally {
        _InnerAsyncLocal.Value = rescuedCurrentValue;

      }

    }

    public string ConstructorCallerFileFullName { get; private set; }

    /// <summary>
    ///   The adapter for storing the root item in an external fallback context depending upon the hosting technology.
    /// </summary>
    /// <remarks>
    ///   This must be initialized before any AmbientField can be used.
    /// </remarks>
    public static IAmbienceToSomeContextAdapter ContextAdapter{
      get {
        return _ContextAdapter;
      }
      set {
        if (_ContextAdapter != null) {
          //throw new InvalidOperationException($"{nameof(ContextAdapter)} has already been set to \"{_ContextAdapter.GetType().Name}\" and cannot be changed to \"{value.GetType().Name}\" by \"{callerFileFullName}\"");
          throw new InvalidOperationException($"{nameof(ContextAdapter)} has already been set to \"{_ContextAdapter.GetType().Name}\" and cannot be changed to \"{value.GetType().Name}\"");
        }
        _ContextAdapter = value;
        //_ContextAdapterSetterCaller = callerFileFullName;
      }
    }

    /// <summary>
    ///   Some states that have been rescued during get- and set-operations for later diagnostics.
    /// </summary>
    /// <remarks>
    ///   This is only for diagnostics.
    /// </remarks>
    public AmbientFieldDebugInfo DebugInfo {
      get {
        return _DebugInfo;
      }
    }

    public static List<AmbientField> ExposedInstances {
      get {
        return _ExposedInstances;
      }
    }

    /// <summary>
    ///    Protects the root value from accidental changes; subsequent changes would throw an exception.
    ///    Useful, when the root value is kind of a binding identifier for e.g. a lifetime scope container (like request, session, etc.)
    /// </summary>
    public bool RootValueIsWriteOnce {
      get {
        return _RootValueIsWriteOnce;
      }
      set {
        _RootValueIsWriteOnce = value;
        //_DebugInfo.RootValueIsWriteOnceSetterCaller = callerFileFullName;
      }
    }

    /// <summary>
    ///   The CallerMemberName of the method which set the context adapter.
    /// </summary>
    /// <remarks>
    ///   This is only for diagnostics. It is helpful to ensure that the ContextAdapter has been set by the component that was supposed to do that.
    /// </remarks>
    public static string ContextAdapterSetterCaller {
      get {
        return _ContextAdapterSetterCaller;
      }
    }

    /// <summary>
    ///   This is a key string built from "{Owner}.{Name}'{HashNumber}", which is used as key for the external fallback context.
    ///   The guid avoids key collisions to entries from 3rd party components, that may exist in the external fallback context.
    /// </summary>
    public string Key { get; private set; }

    /// <summary>
    ///   The class name (hopefully, read remarks) which called the constructor.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    ///   Actually it's the file name without extension of the code file from where th constructor was called.
    ///   But as there exists the convention, that all file names should be the same as the contained class, that should work.
    /// </remarks>
    public string Owner { get; private set; }

    /// <summary>
    ///   Must be set via constructor parameter. See documentation there.
    /// </summary>
    public string Name { get; private set; }

    public Action<string> OnTerminatingMethod { get; set; }

    /// <summary>
    ///   Gets or sets the ambient value.
    /// </summary>
    /// <returns>
    ///   Nothing, if the value was never set.
    ///   If you recieve nothing, although you surely set a value, this is a bug due to a not properly working fallback context.
    /// </returns>
    /// <remarks>
    ///   You cannot set null as value, because null is reserved for 'never set' 
    ///   (and also, we cannot assume that the external fallback context is supporting null as value).
    /// </remarks>
    public string Value {
      get {

        this.AssertContextAdapter();

        string rootValue = ContextAdapter.TryGetCurrentValue(this.Key);
        string asyncLocalValue = _InnerAsyncLocal.Value;

        _DebugInfo.RootValueDuringGet = rootValue;
        _DebugInfo.AsyncLocalValueDuringGet = asyncLocalValue;

        if (asyncLocalValue != null) {
          return asyncLocalValue;
        }
        else {
          _InnerAsyncLocal.Value = rootValue; // "Reparatur"
          return rootValue;
        }

        // Null cannot be used because AsnycLocal.Value returns null when it was lost
        // - so we cannot distinguish that accidental null from an intentional null

        // Ensure the root (= fallback) value is only set once.
        // Subsequent value changes have to be treated as "overriding sessions".

        // Actually store the value

      } // Immer setzen
      set {

        this.AssertContextAdapter();

        if (value is null) {
          throw new ArgumentException($"Null can not be carried as value for \"{this.Owner}.{this.Name}\"!");
        }

        if (_RootValueIsWriteOnce) {
          string currentRootValue = ContextAdapter.TryGetCurrentValue(this.Key);
          if (!string.IsNullOrEmpty(currentRootValue) && !currentRootValue.Equals(value)) {
            //throw new InvalidOperationException($"Root value of \"{this.Owner}.{this.Name}\" has already been set to \"{currentRootValue}\" and cannot be changed to \"{value}\" by \"{callerFileFullName}\"");
            throw new InvalidOperationException($"Root value of \"{this.Owner}.{this.Name}\" has already been set to \"{currentRootValue}\" and cannot be changed to \"{value}\"");
          }
        }

        ContextAdapter.SetCurrentValue(this.Key, value);
        _InnerAsyncLocal.Value = value;
        //_DebugInfo.RootValueSetterCaller = callerFileFullName;
      }
    }

    private void ContextAdapter_IsTerminating() {
      AmbientField.ContextAdapter.CurrentContextIsTerminating -= this.ContextAdapter_IsTerminating;
      if (this.OnTerminatingMethod != null) {
        string dyingValue = ContextAdapter.TryGetCurrentValue(this.Name);
        this.OnTerminatingMethod.Invoke(dyingValue);
      }
    }

    public partial class AmbientFieldDebugInfo {

      ///// <summary>
      /////   The CallerMemberName of the method which set the root value.
      ///// </summary>
      ///// <returns></returns>
      //public string RootValueSetterCaller { get; set; }

      /// <remarks> ORDER DEPENDENCY! Fetch this value AFTER get_Value()! </remarks>
      public string RootValueDuringGet { get; set; }

      /// <summary>
      ///   The value that was present during the Get()-operation.
      ///   If is was null, it indicated that the AsyncLocal context died somewhen before and the fallback context was used to fetch the value.
      /// </summary>
      /// <remarks> ORDER DEPENDENCY! Fetch this value AFTER get_Value()! </remarks>
      public string AsyncLocalValueDuringGet { get; set; }

      //public string RootValueIsWriteOnceSetterCaller { get; set; }

    }

  }

}
