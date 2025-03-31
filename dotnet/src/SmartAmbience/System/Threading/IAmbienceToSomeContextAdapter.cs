
namespace System.Threading {

  /// <summary>
  ///   Verbindungsstück zwischen einem technologie-abhängigen Ambience-Container, dessen LifetimeScope vergleichbar mit
  ///   LogicalCallContext, HttpContext oder OperationContext ist (also eine Art Request-Context)
  ///   und unserem RootedAsyncLocal context. Letzterer verwendet den Adapter, um den FallBack auf die externe Wurzel zu realisieren.
  /// </summary>
  public partial interface IAmbienceToSomeContextAdapter {

    /// <summary>
    ///  Teilt mit, ob der Context z.Zt. benutzbar ist - d.h. SetCurrentValue() und TryGetCurrentValue() ohne Exceptions aufrufbar sind.
    /// </summary>
    bool IsUsable { get; }

    /// <summary>
    ///   Diese Methode muss ein Key-Value-Paar in einem ambience-container hinterlegen
    /// </summary>
    /// <param name="key">
    /// </param>
    /// <param name="value">
    ///   Der Wert, der hinterlegt werden soll. 
    ///   Das explizite Setzen von null als Wert muss mit einer 
    /// </param>
    void SetCurrentValue(string key, string value);

    /// <returns> Null, falls für den key kein Wert existiert. </returns>
    string TryGetCurrentValue(string key);

    /// <summary>
    ///   Muss getriggert werden, kurz bevor der technologie-abhängige Ambience-Container terminiert wird.
    /// </summary>
    /// <remarks>
    ///   Die meisten Runtime-Hosts melden dies via Event oder Delegat (z.B. Application_EndRequest oder OperationContext.OperationCompleted).
    ///   Dieses muss hier durchgereicht werden.
    ///   Und: Der hinterlegte Wert muss zum Zeitpunkt dieses Events noch aus dem Ambience-Container auslesbar sein.
    /// </remarks>
    event CurrentContextIsTerminatingEventHandler CurrentContextIsTerminating;
    
  }

  public delegate void CurrentContextIsTerminatingEventHandler();

}
