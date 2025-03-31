using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace System.Threading {

  public partial class AmbientField {

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal partial class TestApi {

      public static IAmbienceToSomeContextAdapter DirectContextAdapter {
        get {
          return _ContextAdapter;
        }
        set {
          _ContextAdapter = value;
        }
      }

    }

  }

}
