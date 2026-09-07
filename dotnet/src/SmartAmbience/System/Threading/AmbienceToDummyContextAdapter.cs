
namespace System.Threading {

  /// <summary>
  ///   For the conscious initialization of the AmbientField middleware with a volatile
  ///   dummy context that is 'IsUsable', but without the ability to store any values.
  ///   This results in only the AsyncLocal values of the ambient fields are used,
  ///   and no fallback contexts.
  /// </summary>
  public class AmbienceToDummyContextAdapter : IAmbienceToSomeContextAdapter {

    public bool IsUsable {
      get {
        return true;
      }
    }

    public event CurrentContextIsTerminatingEventHandler CurrentContextIsTerminating;

    public void SetCurrentValue(string key, string value) {
    }

    public string TryGetCurrentValue(string key) {
      return null;
    }

  }

}
