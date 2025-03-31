using System.ComponentModel;

namespace System.Threading {

  public partial class AmbientField {

    private AmbientFieldDebugInfo _DebugInfo = new AmbientFieldDebugInfo();

    /// <summary>
    ///  Some states that have been rescued during get- / set-operations for later diagnostics.
    /// </summary>
    /// <remarks>
    ///    This is only for diagnostics.
    /// </remarks>
    public AmbientFieldDebugInfo DebugInfo {
      get {
        return _DebugInfo;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class AmbientFieldDebugInfo {

      public string LongLivingValueDuringGet { get; set; }

      /// <remarks> ORDER DEPENDENCY! Fetch this value AFTER get_Value()! </remarks>
      public bool ContextWasUsableDuringGet { get; set; }

      public string ContextValueDuringGet { get; set; }

      /// <summary>
      ///  The value that was present during the Get()-operation.
      ///  If is was null, it indicated that the AsyncLocal context died somewhen before and the fallback context was used to fetch the value.
      /// </summary>
      /// <remarks> ORDER DEPENDENCY! Fetch this value AFTER get_Value()! </remarks>
      public string AsyncLocalValueDuringGet { get; set; }

    }

  }

}
