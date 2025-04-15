using Logging.SmartStandards.Textualization;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;

// ###########################################################################
// # DOWNLOADED FROM https://github.com/SmartStandards/Logging AT 15.04.2025 #
// ###########################################################################

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/DevLogger.PerLevel.cs)

namespace Logging.SmartStandards {

  
  internal partial class DevLogger {

    public static void LogTrace(string sourceContext, long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      Log(0, sourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    public static void LogTrace(string sourceContext, long sourceLineId, Enum kindEnumElement, params object[] args) {
      Log(0, sourceContext, sourceLineId, kindEnumElement, args);
    }

    public static void LogTrace(string sourceContext, long sourceLineId, Exception ex) {
      Log(0, sourceContext, sourceLineId, ex);
    }

    public static void LogDebug(string sourceContext, long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      Log(1, sourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    public static void LogDebug(string sourceContext, long sourceLineId, Enum kindEnumElement, params object[] args) {
      Log(1, sourceContext, sourceLineId, kindEnumElement, args);
    }

    public static void LogDebug(string sourceContext, long sourceLineId, Exception ex) {
      Log(1, sourceContext, sourceLineId, ex);
    }

    public static void LogInformation(string sourceContext, long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      Log(2, sourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    public static void LogInformation(string sourceContext, long sourceLineId, Enum kindEnumElement, params object[] args) {
      Log(2, sourceContext, sourceLineId, kindEnumElement, args);
    }

    public static void LogInformation(string sourceContext, long sourceLineId, Exception ex) {
      Log(2, sourceContext, sourceLineId, ex);
    }

    public static void LogWarning(string sourceContext, long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      Log(3, sourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    public static void LogWarning(string sourceContext, long sourceLineId, Enum kindEnumElement, params object[] args) {
      Log(3, sourceContext, sourceLineId, kindEnumElement, args);
    }

    public static void LogWarning(string sourceContext, long sourceLineId, Exception ex) {
      Log(3, sourceContext, sourceLineId, ex);
    }

    public static void LogError(string sourceContext, long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      Log(4, sourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    public static void LogError(string sourceContext, long sourceLineId, Enum kindEnumElement, params object[] args) {
      Log(4, sourceContext, sourceLineId, kindEnumElement, args);
    }

    public static void LogError(string sourceContext, long sourceLineId, Exception ex) {
      Log(4, sourceContext, sourceLineId, ex);
    }

    public static void LogCritical(string sourceContext, long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      Log(5, sourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    public static void LogCritical(string sourceContext, long sourceLineId, Enum kindEnumElement, params object[] args) {
      Log(5, sourceContext, sourceLineId, kindEnumElement, args);
    }

    public static void LogCritical(string sourceContext, long sourceLineId, Exception ex) {
      Log(5, sourceContext, sourceLineId, ex);
    }
  }
}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/DevLogger.WoSourceContext.cs)

namespace Logging.SmartStandards {

  
  internal partial class DevLogger {

    // NOTE: [MethodImpl(MethodImplOptions.NoInlining)]
    // is used to avoid wrong results from Assembly.GetCallingAssembly()

    #region MessageTemplate only

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogTrace(string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogTrace(currentSourceContext, 0, 0, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogDebug(string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogDebug(currentSourceContext, 0, 0, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogInformation(string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogInformation(currentSourceContext, 0, 0, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogWarning(string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogWarning(currentSourceContext, 0, 0, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogError(string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogError(currentSourceContext, 0, 0, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCritical(string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogCritical(currentSourceContext, 0, 0, messageTemplate, args);
    }

    #endregion

    #region KindEnumElement only

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogTrace(Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogTrace(currentSourceContext, 0, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogDebug(Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogDebug(currentSourceContext, 0, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogInformation(Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogInformation(currentSourceContext, 0, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogWarning(Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogWarning(currentSourceContext, 0, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogError(Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogError(currentSourceContext, 0, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCritical(Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogCritical(currentSourceContext, 0, kindEnumElement, args);
    }

    #endregion

    #region Exception only

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogTrace(Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogTrace(currentSourceContext, 0, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogDebug(Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogDebug(currentSourceContext, 0, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogInformation(Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogInformation(currentSourceContext, 0, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogWarning(Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogWarning(currentSourceContext, 0, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogError(Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogError(currentSourceContext, 0, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCritical(Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      LogCritical(currentSourceContext, 0, ex);
    }

    #endregion

    #region Ids and MessageTemplate

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogTrace(long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(0, currentSourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogDebug(long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(1, currentSourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogInformation(long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(2, currentSourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogWarning(long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(3, currentSourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogError(long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(4, currentSourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCritical(long sourceLineId, int kindId, string messageTemplate, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(5, currentSourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    #endregion

    #region Ids and KindEnumElement

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogTrace(long sourceLineId, Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(0, currentSourceContext, sourceLineId, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogDebug(long sourceLineId, Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(1, currentSourceContext, sourceLineId, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogInformation(long sourceLineId, Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(2, currentSourceContext, sourceLineId, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogWarning(long sourceLineId, Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(3, currentSourceContext, sourceLineId, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogError(long sourceLineId, Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(4, currentSourceContext, sourceLineId, kindEnumElement, args);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCritical(long sourceLineId, Enum kindEnumElement, params object[] args) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(5, currentSourceContext, sourceLineId, kindEnumElement, args);
    }

    #endregion

    #region SourceLineId and Exception

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogTrace(long sourceLineId, Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(0, currentSourceContext, sourceLineId, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogDebug(long sourceLineId, Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(1, currentSourceContext, sourceLineId, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogInformation(long sourceLineId, Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(2, currentSourceContext, sourceLineId, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogWarning(long sourceLineId, Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(3, currentSourceContext, sourceLineId, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogError(long sourceLineId, Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(4, currentSourceContext, sourceLineId, ex);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogCritical(long sourceLineId, Exception ex) {
      string currentSourceContext = Assembly.GetCallingAssembly().GetName().Name;
      Log(5, currentSourceContext, sourceLineId, ex);
    }

    #endregion

  }

}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/ExceptionExtensions.cs)

namespace Logging.SmartStandards {

  
  internal static partial class ExceptionExtensions {

    public static Exception Wrap(this Exception extendee, string message) {

      WrappedException wrappedException = new WrappedException(message, extendee);

      return wrappedException;
    }

    /// <summary>
    ///   Wraps an outer exception 
    /// </summary>
    /// <param name="extendee"> Will become the inner exception. </param>
    /// <param name="kindId"> Will be added as #-suffix to the message (SmartStandards compliant parsable). </param>
    /// <param name="message"> A custom message to add value to the wrapped exception.</param>
    /// <returns> A new (outer) exception.</returns>
    public static Exception Wrap(this Exception extendee, int kindId, string message) {

      WrappedException wrappedException = new WrappedException(message + " #" + kindId.ToString(), extendee);

      return wrappedException;
    }

    internal partial class WrappedException : Exception {

      public WrappedException(string message, Exception inner) : base (message, inner) {
      }

    }

  }

}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Internal/ExceptionAnalyzer.cs)

namespace Logging.SmartStandards.Internal {

  internal partial class ExceptionAnalyzer {

    internal static int InferEventIdByException(Exception ex) {

      // 'Zwiebel' durch Aufrufe via Reflection (InnerException ist mehr repräsentativ)

      if (ex is TargetInvocationException && ex.InnerException != null) {
        return InferEventIdByException(ex.InnerException);
      }

      // 'Zwiebel' durch Task.Run (InnerException ist mehr repräsentativ)

      if (ex is AggregateException) {
        AggregateException castedAggregateException = (AggregateException)ex;
        if (
          castedAggregateException.InnerExceptions != null &&
          castedAggregateException.InnerExceptions.Count == 1 //falls nur 1 enthalten (macht MS gern)
        ) {
          return InferEventIdByException(castedAggregateException.InnerExceptions[0]);
        }
      }

      // An einer Win32Exception hängt i.d.R. bereits eine kindId => diese verwenden

      if (ex is Win32Exception) {
        return ((Win32Exception)ex).NativeErrorCode;
      }

      // Falls der Absender die Konvention "MessageText #{kindId}" einhielt...

      int hashTagIndex = ex.Message.LastIndexOf('#');

      if (hashTagIndex >= 0 && int.TryParse(ex.Message.Substring(hashTagIndex + 1), out int id)) {
        return id;
      }

      // 'Zwiebel' durch Exception.Wrap (InnerException ist mehr repräsentativ)

      if (ex is ExceptionExtensions.WrappedException) {
        return InferEventIdByException(ex.InnerException);
      }

      // Fallback zuletzt: Wir leiten aus dem Exception-Typ eine kindId ab.

      using (var md5 = MD5.Create()) {
        int hash = BitConverter.ToInt32(md5.ComputeHash(Encoding.UTF8.GetBytes(ex.GetType().Name)), 0);
        if (hash < 0) {
          return hash * -1;
        }
        return hash;
      }

    }

  }
}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/TemplateHousekeeping/LogMessageEnumConverter.cs)

namespace Logging.SmartStandards.TemplateHousekeeping {

  
  internal partial class LogMessageEnumConverter : EnumConverter {

    private Array _FlagValues;

    private bool _IsFlagEnum = false;

    private Dictionary<CultureInfo, Dictionary<string, object>> _CachesPerLanguage = new Dictionary<CultureInfo, Dictionary<string, object>>();

    public LogMessageEnumConverter(Type enumType) : base(enumType) {
      if (enumType.GetCustomAttributes(typeof(FlagsAttribute), true).Any()) {
        _IsFlagEnum = true;
        _FlagValues = Enum.GetValues(enumType);
      }
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {

      if (value is string) {
        object result;

        if (_IsFlagEnum) {
          result = GetMessageTemplateFromAttributeByEnumFlag(culture, (string)value);
        } else {
          result = GetMessageTemplateFromCache(culture, (string)value);
        }

        if (result == null) {
          if (value != null) {
            result = base.ConvertFrom(context, culture, value);
          }
        }

        return result;
      }

      return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {

      // at system.xaml.dll

      if (context == null || !(context.GetType().FullName.Equals("System.Windows.Markup.IValueSerializerContext"))) {
        if ((value != null) && (destinationType.Equals(typeof(System.String)))) {
          object result;
          if ((_IsFlagEnum)) {
            result = GetMessageTemplateFromAttributeByEnumFlagValue(culture, value);
          } else {
            result = GetMessageTemplateFromAttributeByEnumValue(culture, value);
          }
          return result;
        }
      }

      return base.ConvertTo(context, culture, value, destinationType);
    }

    public static string ConvertToString(Enum value) {
      if (value != null) {
        TypeConverter converter = TypeDescriptor.GetConverter(value.GetType());
        return converter.ConvertToString(value);
      }
      return string.Empty;
    }

    private object GetMessageTemplateFromAttributeByEnumFlag(CultureInfo culture, string text) {
      Dictionary<string, object> languageSpecificCache = GetLanguageSpecificCache(culture);
      string[] textValues = text.Split(',');
      ulong result = 0;

      foreach (string textValue in textValues) {
        object value = null;
        string trimmedTextValue = textValue.Trim();

        if ((!languageSpecificCache.TryGetValue(trimmedTextValue, out value)))
          return null;

        result = result | Convert.ToUInt32(value);
      }

      return Enum.ToObject(this.EnumType, result);
    }

    private Dictionary<string, object> GetLanguageSpecificCache(CultureInfo culture) {
      lock (_CachesPerLanguage) {
        Dictionary<string, object> result = null;
        if (culture == null) {
          culture = CultureInfo.CurrentCulture;
        }
        if (!_CachesPerLanguage.TryGetValue(culture, out result)) {
          result = new Dictionary<string, object>();
          foreach (var value in this.GetStandardValues()) {
            var text = this.GetMessageTemplateFromAttributeByEnumValue(culture, value);
            if (text != null) {
              result.Add(text, value);
            }
          }
          _CachesPerLanguage.Add(culture, result);
        }
        return result;
      }
    }

    private object GetMessageTemplateFromCache(CultureInfo culture, string text) {
      Dictionary<string, object> languageSpecificCache = this.GetLanguageSpecificCache(culture);
      object result = null;
      languageSpecificCache.TryGetValue(text, out result);
      return result;
    }

    private string GetMessageTemplateFromAttributeByEnumValue(CultureInfo culture, object value) {

      if (value == null) {
        return string.Empty;
      }

      Type type = value.GetType();

      if (!type.IsEnum) {
        return value.ToString();
      }

      LogMessageTemplateAttribute[] attributes = GetEnumFieldAttributes<LogMessageTemplateAttribute>((Enum)value);

      LogMessageTemplateAttribute defaultAttribute = attributes.FirstOrDefault();

      foreach (LogMessageTemplateAttribute attribute in attributes) {
        if (string.IsNullOrEmpty(attribute.Language)) {
          defaultAttribute = attribute;
        } else if (attribute.Language.Equals(culture.Name, StringComparison.InvariantCultureIgnoreCase)) {
          return attribute.LogMessageTemplate;
        }
      }
      if (defaultAttribute != null) {
        return defaultAttribute.LogMessageTemplate;
      } else {
        return Enum.GetName(type, value);
      }
    }

    private string GetMessageTemplateFromAttributeByEnumFlagValue(CultureInfo culture, object value) {
      if (Enum.IsDefined(value.GetType(), value)) {
        return this.GetMessageTemplateFromAttributeByEnumValue(culture, value);
      }
      long lValue = Convert.ToInt32(value);
      string result = null;
      foreach (object flagValue in _FlagValues) {
        long lFlagValue = Convert.ToInt32(flagValue);
        if (this.CheckSingleBit(lFlagValue)) {
          if ((lFlagValue & lValue) == lFlagValue) {
            string valueText = this.GetMessageTemplateFromAttributeByEnumValue(culture, flagValue);
            if (result == null) {
              result = valueText;
            } else {
              result = string.Format("{0}+{1}", result, valueText);
            }
          }
        }
      }

      return result;
    }

    public static List<KeyValuePair<Enum, string>> GetValues(Type enumType) {
      return GetValues(enumType, CultureInfo.CurrentUICulture);
    }

    public static List<KeyValuePair<Enum, string>> GetValues(Type enumType, CultureInfo culture) {
      List<KeyValuePair<Enum, string>> result = new List<KeyValuePair<Enum, string>>();
      TypeConverter converter = TypeDescriptor.GetConverter(enumType);
      foreach (System.Enum value in Enum.GetValues(enumType)) {
        KeyValuePair<Enum, string> pair = new KeyValuePair<Enum, string>(
          value, converter.ConvertToString(null, culture, value)
        );
        result.Add(pair);
      }
      return result;
    }

    private bool CheckSingleBit(long value) {
      switch (value) {
        case 0: {
          return false;
        }
        case 1: {
          return true;
        }
      }
      return ((value & (value - 1)) == 0);
    }

    private TAttribute[] GetEnumFieldAttributes<TAttribute>(Enum enumValue) where TAttribute : Attribute {
      Type enumType = enumValue.GetType();
      string enumFieldName = Enum.GetName(enumType, enumValue);
      if ((enumFieldName == null))
        return new TAttribute[] { };
      else {
        FieldInfo enumField = enumType.GetField(enumFieldName);
        return enumField.GetCustomAttributes(false).OfType<TAttribute>().ToArray();
      }
    }

  }

}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/TemplateHousekeeping/LogMessageTemplateAttribute.cs)

namespace Logging.SmartStandards.TemplateHousekeeping {

  /// <summary>
  ///   Defines a log message template for an enum value (representing the log event kind id).
  /// </summary>
  [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
  
  internal partial class LogMessageTemplateAttribute : Attribute {

    /// <summary>
    ///  Constructor.
    /// </summary>
    /// <param name="logMessageTemplate"></param>
    /// <param name="language"> ISO code like 'en-us' or 'de-de' </param>
    public LogMessageTemplateAttribute(string logMessageTemplate, string language = null) {
      this.LogMessageTemplate = logMessageTemplate;
      this.Language = language;
    }

    public string LogMessageTemplate { get; }

    public string Language { get; }

  }

}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/TemplateHousekeeping/LogMessageTemplateRepository.cs)

namespace Logging.SmartStandards.TemplateHousekeeping {

  
  internal static partial class LogMessageTemplateRepository {

    internal static void GetMessageTemplateByKind(Enum kindEnumElement, out int kindId, out string messageTemplate) {

      kindId = (int)(object)kindEnumElement;

      messageTemplate = null;

      try {
        TypeConverter typeConverter = TypeDescriptor.GetConverter(kindEnumElement);
        if (typeConverter != null && typeConverter.CanConvertTo(typeof(System.String))) {
          messageTemplate = typeConverter.ConvertToString(kindEnumElement);
        }
      } catch {
      }
      if (String.IsNullOrWhiteSpace(messageTemplate)) {
        messageTemplate = Enum.GetName(kindEnumElement.GetType(), kindEnumElement);
      }
    }

  }
}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Textualization/CopyOfPlaceholderExtensions .cs)

namespace Logging.SmartStandards.Textualization {

  internal static partial class CopyOfPlaceholderExtensions {

    /// <summary>
    ///   Executes a callback method for each placeholder in a template string.
    /// </summary>
    /// <param name="extendee">
    ///   A template string containing named placeholders. 
    ///   E.g. "Hello {audience}, the answer is {answer}."
    /// </param>
    /// <param name="onPlaceholderFound">
    ///   bool onPlaceholderFound(string placeholderName).
    ///   Will be called for each placeholder in order of appearance.
    ///   (e.g. "audience", "answer").
    ///   The placeholder name will be passed (without braces), unless omitPlaceholderNames is set (then null will be passed).
    ///   If the callback function returns true (= cancel), the processing will stop immedieately.
    /// </param>
    /// <param name="onRegularSegmentFound">
    ///   void onRegularSegmentFound(int pos, int length).
    ///   Optional. Will be called for each seqgment of the template that is not a placeholder.
    ///   (e.g. "Hello ", ", the answer is ", ".").
    /// </param>
    /// <param name="omitPlaceholderNames">
    ///   Performance optimization. If true, the placeholder name is not extracted from the template.
    /// </param>
    public static void ForEachPlaceholder(
      this string extendee,
      Func<string, bool> onPlaceholderFound,
      Action<int, int> onRegularSegmentFound = null,
      bool omitPlaceholderNames = false
    ) {

      if (extendee is null || extendee.Length < 3) return;

      int cursor = 0;

      do {

        int leftPos = extendee.IndexOf("{", cursor);

        if (leftPos < 0) break;

        int rightPos = extendee.IndexOf("}", cursor);

        if (rightPos < 0 || rightPos < leftPos + 1) return;

        string placeholderName = null;

        if (!omitPlaceholderNames) placeholderName = extendee.Substring(leftPos + 1, rightPos - leftPos - 1);

        onRegularSegmentFound?.Invoke(cursor, leftPos - cursor);

        if (onPlaceholderFound.Invoke(placeholderName)) return;

        cursor = rightPos + 1;

      } while (cursor < extendee.Length);

      onRegularSegmentFound?.Invoke(cursor, extendee.Length - cursor);
    }

    /// <summary>
    ///   Resolves named placeholders in a template string from arguments.
    /// </summary>
    /// <param name="extendee">
    ///   A template string containing named placeholders. 
    ///   E.g. "Hello {audience}, the answer is {answer}."
    /// </param>
    /// <param name="args">
    ///   Arguments containing the placeholder values in order of appearance in the template. Example:
    ///   "World", 42
    /// </param>
    /// <returns>
    ///   Null or a new string instance with resolved placeholders. The example would be resolved to:
    ///   "Hello World, the answer is 42."
    /// </returns>
    public static string ResolvePlaceholders(this string extendee, params object[] args) {

      int maxIndex = args != null ? args.GetUpperBound(0) : -1;

      if (extendee is null || extendee.Length < 3 || maxIndex < 0) return extendee;

      StringBuilder targetStringBuilder = new StringBuilder(extendee.Length * 15 / 10);

      targetStringBuilder.AppendResolved(extendee, args);

      return targetStringBuilder.ToString();
    }

    /// <summary>
    ///   Resolves placeholders within a StringBuilder instance.
    /// </summary>
    /// <param name="extendee"> The StringBuilder instance containing unresolved placeholders. </param>
    /// <param name="args"> Placeholder values in correct order. </param>
    /// <returns> The StringBuilder instance after resolvingvar (to support fluent syntax). </returns>
    /// <remarks>
    ///   The internal behavior of this method is NOT equivalent to the same named string extension.
    ///   Is is NOT performant to convert a string to a StringBuilder and pass it to this extension.
    ///   Only use this extension if you have a StringBuilder instance anyways and you want to keep the instance.
    ///   Otherwise using the string extension is faster.
    /// </remarks>
    public static StringBuilder ResolvePlaceholders(this StringBuilder extendee, params object[] args) {

      if (extendee == null || args == null) { return extendee; }

      if (args.Length == 0) { return extendee; }

      int cursor = 0;

      foreach (object boxedValue in args) {

        int left = -1;

        for (int i = cursor; i < extendee.Length; i++) {
          if (extendee[i] == '{') { left = i; break; }
          ;
        }

        if (left == -1) { break; }

        int right = -1;

        for (int i = left; i < extendee.Length; i++) {
          if (extendee[i] == '}') { right = i; break; }
          ;
        }

        if (right == -1) { break; }

        extendee.Remove(left, right - left + 1);

        string value = boxedValue.ToString();

        extendee.Insert(left, value);

        cursor += value.Length;
      }

      return extendee;
    }

    public static string ResolvePlaceholdersByDictionary(this string extendee, IDictionary<string, string> placeholders) {

      if (extendee is null || extendee.Length < 3 || placeholders is null || placeholders.Count == 0) {
        return extendee;
      }

      string onResolvePlaceholder(string placeholderName) {
        string value = null;
        if (placeholders.TryGetValue(placeholderName, out value)) {
          return value ?? ""; // Value is null => render empty string
        } else {
          return null; // Value not existing => return null => leave placeholder unchanged
        }
      }

      StringBuilder targetStringBuilder = new StringBuilder(extendee.Length * 15 / 10);

      targetStringBuilder.AppendResolving(extendee, onResolvePlaceholder);

      return targetStringBuilder.ToString();
    }

    public static string ResolvePlaceholdersByPropertyBag(this string extendee, object propertyBag) {

      if (extendee is null || extendee.Length < 3 || propertyBag is null) return extendee;

      string onResolvePlaceholder(string placeholderName) {

        PropertyInfo propertyInfo = propertyBag.GetType().GetProperty(
          placeholderName, BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.Instance
        );

        if (propertyInfo != null) {
          return propertyInfo.GetValue(propertyBag).ToString() ?? ""; // Property value is null => render empty string
        } else {
          return null; // Property not existing => return null => leave placeholder unchanged
        }
      }

      StringBuilder targetStringBuilder = new StringBuilder(extendee.Length * 15 / 10);

      targetStringBuilder.AppendResolving(extendee, onResolvePlaceholder);

      return targetStringBuilder.ToString();
    }

    /// <summary>
    ///   Appends a resolved template string to an existing StringBuilder while calling back a resolving method for each placeholder.
    /// </summary>
    /// <param name="template"> A template string containing named placeholders. E.g. "Hello {audience}, the answer is {answer}."</param>
    /// <param name="onResolvePlaceholder">
    ///   string onResolvePlaceholder(name).
    ///   Will be called for each placeholder in order of appearance.
    ///   (e.g. "audience", "answer").
    ///   The placeholder name will be passed (or null, if omitPlaceholderNames is set).
    ///   The resolved placeholder value should be returned. 
    ///   If null is returned, the placeholder will remain unchanged (including braces).
    ///   </param>
    /// <param name="omitPlaceholderNames">
    ///   Performance optimization. If true, the placeholder name is not extracted from the template.
    /// </param>
    /// <returns> The resolved string. </returns>
    public static StringBuilder AppendResolving(
      this StringBuilder extendee,
      string template,
      Func<string, string> onResolvePlaceholder,
      bool omitPlaceholderNames = false
    ) {

      if (extendee == null) return null;

      if (template is null || template.Length < 3 || onResolvePlaceholder is null) {
        extendee.Append(template);
        return extendee;
      }

      bool onPlaceholderFound(string placeholderName) {
        string value = onResolvePlaceholder.Invoke(placeholderName);
        if (value != null) {
          extendee.Append(value);
        } else {
          extendee.Append('{').Append(placeholderName).Append('}');
        }
        return false;
      }

      void onRegularSegmentFound(int pos, int length) => extendee.Append(template, pos, length);

      template.ForEachPlaceholder(onPlaceholderFound, onRegularSegmentFound, omitPlaceholderNames);

      return extendee;
    }

    public static StringBuilder AppendResolved(this StringBuilder extendee, string template, params object[] args) {

      if (extendee == null) return null;

      int maxIndex = args != null ? args.GetUpperBound(0) : -1;

      if (template is null || template.Length < 3 || maxIndex < 0) return extendee;

      int i = -1;

      string onResolvePlaceholder(string dummyName) {
        i++;
        if (i <= maxIndex) {
          return args[i]?.ToString();
        } else {
          return null;
        }
      }

      extendee.AppendResolving(template, onResolvePlaceholder, true);

      return extendee;
    }

  }
}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Textualization/ExceptionRenderer.cs)

namespace Logging.SmartStandards {

  
  internal static partial class ExceptionRenderer {

    /// <summary>
    ///   Serializes an Exception in a way, that InnerExceptions and StackTraces are included
    ///   and returns a message string, which is highly optimized for logging requirements.
    /// </summary>
    public static string Render(Exception ex, bool includeStacktrace = true) {
      StringBuilder sb = new StringBuilder(1000);
      string messageMainLine = ex.Message;
      AppendRecursive(ex, sb, ref messageMainLine, includeStacktrace);
      sb.Insert(0, messageMainLine + Environment.NewLine);
      return sb.ToString();
    }

    private static void AppendRecursive(Exception ex, StringBuilder target, ref string messageMainLine, bool includeStacktrace, bool isInner = false) {

      if (ex == null) {
        return;
      }

      target.Append($"-- {ex.GetType().FullName} --");

      if (isInner) {
        target.Append($" (inner)");
      }

      if (includeStacktrace && !string.IsNullOrWhiteSpace(ex.StackTrace)) {

        StringReader traceReader = new StringReader(ex.StackTrace); // TODO: Performance optimization

        string readLine = traceReader.ReadLine()?.Trim();

        while (readLine != null) {
          target.AppendLine();
          target.Append("@ ");
          target.Append(readLine.Replace(" in ", Environment.NewLine + "@   "));
          readLine = traceReader.ReadLine()?.Trim();
        }
      }

      if ((ex.InnerException != null)) {
        messageMainLine = messageMainLine + " :: " + ex.InnerException.Message;
        target.AppendLine();
        AppendRecursive(ex.InnerException, target, ref messageMainLine, includeStacktrace, true);
      }

    }
  }
}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Textualization/LogParaphRenderer.cs)

namespace Logging.SmartStandards.Textualization {

  /// <summary>
  ///   Tool to build textual representations of log events.
  /// </summary>
  /// <remarks>
  ///   Example LogParaph:
  ///   [LevelAsAlpha3] SourceContext #kindId#  SourceLineId [AudienceToken]: MessageTemplate 
  ///   [Err] MyApp.exe #4711# 2070198253252296432 [Ins]: File not found on Disk! 
  /// </remarks>
  
  internal partial class LogParaphRenderer {

    public static string LevelToAlpha3(int level, StringBuilder targetStringBuilder = null) {

      switch (level) {

        case 5: { // Critical
          targetStringBuilder?.Append("Cri");
          return "Cri";
        }
        case 4: { // Error
          targetStringBuilder?.Append("Err");
          return "Err";
        }
        case 3: { // Warning
          targetStringBuilder?.Append("Wrn");
          return "Wrn";
        }
        case 2: { // Information
          targetStringBuilder?.Append("Inf");
          return "Inf";
        }
        case 1: { // Debug
          targetStringBuilder?.Append("Dbg");
          return "Dbg";
        }
        default: { // Trace
          targetStringBuilder?.Append("Trc");
          return "Trc";
        }
      }
    }

    /// <summary>
    ///   Appends a ready-to-read log paraph (having resolved placeholders) to an existing StringBuilder instance.
    /// </summary>    
    /// <returns>
    ///   Example LogParaph:
    ///   [LevelAsAlpha3] SourceContext #kindId#  SourceLineId [AudienceToken]: MessageTemplate 
    ///   [Err] MyApp.exe #4711# 2070198253252296432 [Ins]: File not found on Disk! 
    /// </returns>
    public static StringBuilder BuildParaphResolved(
      StringBuilder targetStringBuilder,
      string audienceToken, int level, string sourceContext, long sourceLineId,
      int kindId, string messageTemplate, object[] args
    ) {

      LogParaphRenderer.BuildParaphLeftPart(targetStringBuilder, level, sourceContext, kindId);

      LogParaphRenderer.BuildParaphRightPart(targetStringBuilder, sourceLineId, audienceToken, null);

      targetStringBuilder.AppendResolved(messageTemplate, args);

      return targetStringBuilder;
    }

    /// <summary>
    ///   Renders the left part of a log paraph.
    /// </summary>
    /// <remarks>
    ///   The left part contains meta data that would normally be transported as arguments.
    /// </remarks>
    /// <returns>
    ///   S.th. like "[Err] MyApp.exe #4711#"
    /// </returns>
    public static StringBuilder BuildParaphLeftPart(
      StringBuilder targetStringBuilder, int level, string sourceContext, int kindId
    ) {
      targetStringBuilder.Append('[');
      LogParaphRenderer.LevelToAlpha3(level, targetStringBuilder);
      targetStringBuilder.Append("] ");
      targetStringBuilder.Append(sourceContext);
      targetStringBuilder.Append(" #");
      targetStringBuilder.Append(kindId);
      targetStringBuilder.Append('#');
      return targetStringBuilder;
    }

    /// <summary>
    ///   Renders the right part of a log paraph.
    /// </summary>
    /// <remarks>
    ///   The right part contains meta data that cannot be transported as arguments, because most logging APIs do not offer enough
    ///   parameters.
    /// </remarks>
    /// <returns>
    ///   S.th. like " 2070198253252296432 [Ins]: File not found on Disk! "
    /// </returns>
    public static StringBuilder BuildParaphRightPart(
      StringBuilder targetStringBuilder, long sourceLineId, string audienceToken, string messageTemplate
    ) {
      targetStringBuilder.Append(' ');
      targetStringBuilder.Append(sourceLineId);
      targetStringBuilder.Append(" [");
      targetStringBuilder.Append(audienceToken);
      targetStringBuilder.Append("]: ");
      targetStringBuilder.Append(messageTemplate);
      return targetStringBuilder;
    }

  }
}


// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Textualization/LogParaphParser.cs)
namespace Logging.SmartStandards.Textualization {

  
  internal partial class LogParaphParser {

    /// <summary>
    ///   Tokenizes an incoming metaDataRightPart string.
    /// </summary>
    /// <param name="metaDataRightPart">
    ///   " SourceLineId [AudienceToken]: MessageTemplate"
    /// </param>
    /// <param name="sourceLineId"> If metaDataRightPart was malformed: 0. </param>
    /// <param name="audienceToken"> Without the brackets. If metaDataRightPart was malformed: "" </param>
    /// <param name="messageTemplate"> If metaDataRightPart was malformed: metaDataRightPart.</param>
    public static void TokenizeMetaDataRightPart(
      string metaDataRightPart, out long sourceLineId, out string audienceToken, out string messageTemplate
    ) {

      sourceLineId = 0;
      audienceToken = "";
      messageTemplate = metaDataRightPart;

      if (metaDataRightPart == null || metaDataRightPart.Length < 9) return;

      if (metaDataRightPart[0] != ' ') return;

      int rightOfSourceLineId = metaDataRightPart.IndexOf(' ', 1);
      int leftOfAudience = metaDataRightPart.IndexOf('[');
      int rightOfAudience = metaDataRightPart.IndexOf("]:");

      if (
        rightOfSourceLineId > leftOfAudience ||
        leftOfAudience > rightOfAudience ||
        rightOfAudience - leftOfAudience != 4
      ) return;

      string sourceLineIdAsString = metaDataRightPart.Substring(1, rightOfSourceLineId);

      if (!long.TryParse(sourceLineIdAsString, out sourceLineId)) return;

      audienceToken = metaDataRightPart.Substring(leftOfAudience + 1, 3);

      int beginOfMessageTemplate = rightOfAudience + 3;

      if (metaDataRightPart.Length >= beginOfMessageTemplate) {
        messageTemplate = metaDataRightPart.Substring(beginOfMessageTemplate);
      } else {
        messageTemplate = "";
      }
    }
  }
}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Transport/CutomBusFeed.cs)

namespace Logging.SmartStandards.Transport {

  /// <summary>
  ///   (Static) Customizing hook for routing messages to any further target.
  /// </summary>
  
  internal partial class CustomBusFeed {

    public static bool ExceptionRenderingToggle { get; set; }

    public delegate void EmitMessageDelegate(string audience, int level, string sourceContext, long sourceLineId, int kindId, string messageTemplate, object[] args);

    public delegate void EmitExceptionDelegate(string audience, int level, string sourceContext, long sourceLineId, int kindId, Exception ex);

    /// <summary>
    ///   Customizing hook. Will be called by any SmartStandards logger (if enabled).
    ///   Register your routing delegate here to forward a log message to any further target.
    /// </summary>
    /// <remarks>
    ///   It is suggested to use the convenience method Routing.UseCustomBus() instead of doing a manual wire up.
    /// </remarks>
    public static EmitMessageDelegate OnEmitMessage { get; set; }

    /// <summary>
    ///   Customizing hook. Will be called by any SmartStandards logger.
    ///   Register your routing delegate here to forward an exception log message to any further target.
    /// </summary>
    /// <remarks>
    ///   It is suggested to use the convenience method Routing.UseCustomBus() instead of doing a manual wire up.
    /// </remarks>
    public static EmitExceptionDelegate OnEmitException { get; set; }

  }

}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Transport/TraceBusFeed.cs)

namespace Logging.SmartStandards.Transport {

  /// <summary>
  ///   Helper partial class for emitting messages into the (legacy) .NET System.Diagnostics.Trace concept
  /// </summary>
  
  internal partial class TraceBusFeed {

    private Dictionary<string, TraceSource> _TraceSourcePerSourceContext = new Dictionary<string, TraceSource>();

    private MyCircularBuffer<QueuedEvent> _EarlyPhaseBuffer;

    private readonly object _BufferManipulating = new object();

    private static TraceBusFeed _Instance;

    private bool _PatienceExercised;

    public static TraceBusFeed Instance {
      get {

        if (_Instance == null) {
          _Instance = new TraceBusFeed();
          _Instance._EarlyPhaseBuffer = new MyCircularBuffer<QueuedEvent>(1000);
        }

        return _Instance;
      }
    }

    private bool ListenersAvailable {
      get {

        if (Trace.Listeners.Count == 0) return false;

        if (Trace.Listeners.Count == 1) {
          string listenerName = Trace.Listeners[0].Name;
          if (listenerName == "Default") return false;
          if (IgnoredListeners.Contains(listenerName)) return false;
        }

        return true;
      }
    }


    /// <summary>
    ///   Emit textualized exceptions (as message) to the TraceBus (instead of the original exception as arg).
    /// </summary>
    public bool ExceptionsTextualizedToggle { get; set; }

    public HashSet<string> IgnoredListeners { get; set; } = new HashSet<string>();

    private TraceSource GetTraceSourcePerSourceContext(string sourceContext) {

      lock (_TraceSourcePerSourceContext) {

        TraceSource traceSource = null;

        // get or (lazily) create TraceSource

        if (!_TraceSourcePerSourceContext.TryGetValue(sourceContext, out traceSource)) {

          // Optimization: Do not instantiate a TraceSource if there are no listeners:
          if (!this.ListenersAvailable) return null;

          // Instantiate a TraceSource:
          traceSource = new TraceSource(sourceContext);
          traceSource.Switch.Level = SourceLevels.All;

          _TraceSourcePerSourceContext[sourceContext] = traceSource;

          // when a new trace source was created => always keep all listeners of all TraceSource in sync:

          RewireAllSourcesAndListeners();
        }

        return traceSource;
      } // lock
    }

    private void FlushAndShutDownBuffer() {

      lock (_TraceSourcePerSourceContext) {

        if (_EarlyPhaseBuffer == null) return;

        MyCircularBuffer<QueuedEvent> earlyPhaseBuffer = _EarlyPhaseBuffer;
        _EarlyPhaseBuffer = null; // set field to null to avoid unwanted recursion

        foreach (QueuedEvent e in earlyPhaseBuffer) {
          TraceSource traceSource = this.GetTraceSourcePerSourceContext(e.SourceContext);
          traceSource?.TraceEvent(e.EventType, e.KindId, e.MessageTemplate, e.Args);
        }
      }
    }

    private void RewireAllSourcesAndListeners() {

      TraceSource firstTraceSource = null;

      bool awaitedListenerFound = false;

      foreach (KeyValuePair<string, TraceSource> namedTraceSource in _TraceSourcePerSourceContext) {

        if (firstTraceSource == null) {

          firstTraceSource = namedTraceSource.Value;

          firstTraceSource.Listeners.Clear();

          // Cherry-pick available listeners => store into the first TraceSource (representative of all others)

          awaitedListenerFound = CaptureListenersInto(firstTraceSource);

        } else { // subsequent trace sources get the same listeners as the first one
          namedTraceSource.Value.Listeners.Clear();
          namedTraceSource.Value.Listeners.AddRange(firstTraceSource.Listeners);
        }

      } // next namedTraceSource

      if (_EarlyPhaseBuffer != null && awaitedListenerFound) {
        FlushAndShutDownBuffer();
      }

    }

    private bool CaptureListenersInto(TraceSource targetTraceSource) {

      bool awaitedListenerFound = false;

      foreach (TraceListener listener in Trace.Listeners) {

        if (listener.Name == "Default") continue; // The .NET Default listener is a major performance hit => do not support.

        if (this.IgnoredListeners.Contains(listener.Name)) continue;

        targetTraceSource?.Listeners.Add(listener);

        if (listener.Name == "SmartStandards395316649") awaitedListenerFound = true;
      }

      return awaitedListenerFound;
    }

    public void EmitException(string audience, int level, string sourceContext, long sourceLineId, int kindId, Exception ex) {

      string renderedException = "";
      object[] args;

      if (this.ExceptionsTextualizedToggle) {
        renderedException = ExceptionRenderer.Render(ex);
        args = Array.Empty<object>();
      } else {
        args = new object[] { ex };
      }

      this.EmitMessage(audience, level, sourceContext, sourceLineId, kindId, renderedException, args);
    }

    private void KillBufferAfterGracePeriod() {

      Thread.Sleep(10000);

      lock (_TraceSourcePerSourceContext) {

        bool awaitedListenerFound = CaptureListenersInto(null);

        if (awaitedListenerFound) FlushAndShutDownBuffer();

        _EarlyPhaseBuffer = null;
      }
    }

    /// <param name="level">
    ///   5 Critical
    ///   4 Error
    ///   3 Warning
    ///   2 Info
    ///   1 Debug
    ///   0 Trace
    /// </param>
    public void EmitMessage(
      string audience, int level, string sourceContext, long sourceLineId,
      int kindId, string messageTemplate, params object[] args
    ) {

      if (!_PatienceExercised) {
        _PatienceExercised = true;
        Task.Run(KillBufferAfterGracePeriod);
      }

      if (string.IsNullOrWhiteSpace(sourceContext)) sourceContext = "UnknownSourceContext";

      TraceSource traceSource = this.GetTraceSourcePerSourceContext(sourceContext);

      if (traceSource is null && _EarlyPhaseBuffer == null) return;

      if (string.IsNullOrWhiteSpace(audience)) audience = "Dev";

      if (messageTemplate == null) messageTemplate = "";

      TraceEventType eventType;

      switch (level) {

        case 5: { // Critical (aka "Fatal")
          eventType = TraceEventType.Critical; // 1
          break;
        }

        case 4: { // Error
          eventType = TraceEventType.Error; // 2
          break;
        }

        case 3: { // Warning
          eventType = TraceEventType.Warning; // 4
          break;
        }

        case 2: { // Info
          eventType = TraceEventType.Information; // 8
          break;
        }

        case 1: { // Debug
          eventType = TraceEventType.Transfer; // 4096 - ' There is no "Debug" EventType => use something else
                                               // 0 "Trace" (aka "Verbose")
          break;
        }

        default: { // Trace
          eventType = TraceEventType.Verbose; // 16
          break;
        }

      }

      // Because we support named placeholders (like "Hello {person}") instead of old scool indexed place holders
      // (like "Hello {0}") we need to double brace the placeholders - otherwise there will be exceptions coming from
      // the .net TraceEvent Method.

      StringBuilder formatStringBuilder = new StringBuilder(messageTemplate.Length + 20);

      LogParaphRenderer.BuildParaphRightPart(formatStringBuilder, sourceLineId, audience, messageTemplate);

      formatStringBuilder.Replace("{", "{{").Replace("}", "}}");

      // actual emit

      traceSource?.TraceEvent(eventType, kindId, formatStringBuilder.ToString(), args);

      if (_EarlyPhaseBuffer != null) {
        _EarlyPhaseBuffer.SafeEnqueue(new QueuedEvent(sourceContext, eventType, kindId, formatStringBuilder.ToString(), args));
      }

    }

    private partial class QueuedEvent {

      public string SourceContext;

      public TraceEventType EventType { get; set; }

      public int KindId { get; set; }

      public string MessageTemplate { get; set; }

      public object[] Args { get; set; }

      public QueuedEvent(string sourceContext, TraceEventType EventType, int kindId, string messageTemplate, object[] args) {
        this.SourceContext = sourceContext;
        this.EventType = EventType;
        this.KindId = kindId;
        this.MessageTemplate = messageTemplate;
        this.Args = args;
      }

    }

  }

}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Transport/TraceBusFeed.Buffer.cs)

namespace Logging.SmartStandards.Transport {

  
  internal partial class TraceBusFeed {

    /// <remarks>
    ///   Taken from https://stackoverflow.com/a/5924776
    /// </remarks>
    private partial class MyCircularBuffer<T> : IEnumerable<T> {

      readonly int _Size;

      readonly object _Locker;

      int _Count;
      int _Head;
      int _Rear;
      T[] _Values;

      public MyCircularBuffer(int max) {
        _Size = max;
        _Locker = new object();
        _Count = 0;
        _Head = 0;
        _Rear = 0;
        _Values = new T[_Size];
      }

      static int Incr(int index, int size) {
        return (index + 1) % size;
      }

      public object SyncRoot { get { return _Locker; } }

      public void SafeEnqueue(T obj) {
        lock (_Locker) { UnsafeEnqueue(obj); }
      }

      public void UnsafeEnqueue(T obj) {

        _Values[_Rear] = obj;

        if (_Count == _Size) _Head = Incr(_Head, _Size);

        _Rear = Incr(_Rear, _Size);
        _Count = Math.Min(_Count + 1, _Size);
      }

      public IEnumerator<T> GetEnumerator() {
        int index = _Head;

        for (int i = 0; i < _Count; i++) {
          yield return _Values[index];
          index = Incr(index, _Size);
        }

      }

      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
        return this.GetEnumerator();
      }

    }

  }

}

// (File: https://raw.githubusercontent.com/SmartStandards/Logging/refs/heads/master/dotnet/src/SmartStandards.Logging/Logging/SmartStandards/Transport/TraceBusListener.cs)


namespace Logging.SmartStandards.Transport {

  /// <summary>
  ///   Helper partial class to recieve log entries from System.Diagnostics.Trace. 
  /// </summary>
  /// <remarks>
  ///   1. Due to limitations of System.Diagnostics.Trace you can use only one long-living instance per AppDomain.
  ///   
  ///   2. If you copy and paste this partial class (instead of referencing this library) be sure to change namespace and 
  ///      partial class name. Otherwise you'll run into naming collisionsif a 3rd party DLL references this library.
  /// </remarks>
  
  internal partial class TraceBusListener : TraceListener {

    public delegate bool FilterIncomingTraceEventDelegate(int eventType, string sourceName, string formatString);

    public delegate void OnMessageReceivedDelegate(string audience, int level, string sourceContext, long sourceLineId, int kindId, string messageTemplate, object[] args);

    public delegate void OnExceptionReceivedDelegate(string audience, int level, string sourceContext, long sourceLineId, int kindId, Exception ex);

    public bool IsActive { get; set; } = true;

    public OnMessageReceivedDelegate OnMessageReceived { get; set; }

    public OnExceptionReceivedDelegate OnExceptionReceived { get; set; }

    /// <summary>
    ///   3rd party trace sources may not provide an audience. 
    ///   Therefore you can map their source name to a target audience.
    /// </summary>
    public Dictionary<string, string> TraceSourcesToAudienceMapping { get; set; } = new Dictionary<string, string>();

    /// <summary>
    ///   Constructor.
    /// </summary>
    protected TraceBusListener() {
      // Constructor is private, because this must be a single instance
    }

    /// <summary>
    ///   Constructor. Builds up a trace listener and registers it immediately to Trace.Listeners.
    /// </summary>
    /// <param name="onMessageReceived">
    ///   Callback delegate which is called for each recieved log entry. 
    ///   Signature: onLog(string channelName, int level, int id, string messageTemplate, object[] args)
    ///   Implement your actual logging sink here.
    /// </param>
    /// <remarks>
    ///   Order dependency: This has to be done before initializing any trace sources.
    /// </remarks>
    public TraceBusListener(OnMessageReceivedDelegate onMessageReceived, OnExceptionReceivedDelegate onExceptionReceived) {

      this.OnMessageReceived = onMessageReceived;

      this.OnExceptionReceived = onExceptionReceived;

      Trace.Listeners.Add(this); // Self-register to .net runtime

      // Remark: This is a one-way-ticket. Removing (even disposing) a listener is futile, because all TraceSources
      // hold a reference. You would have to iterate all existing TraceSources first an remove the listener there.
      // This is impossible due to the fact that there's no (clean) way of getting all existing TraceSources.
      // See: https://stackoverflow.com/questions/10581448/add-remove-tracelistener-to-all-tracesources

    }

    public override void TraceEvent(
      TraceEventCache eventCache, string sourceName, TraceEventType eventType, int kindId, string formatString, params object[] args
    ) {

      if (!this.IsActive || this.OnMessageReceived == null) {
        return;
      }

      // ...Map EventType => LogLevel...

      int level = 0; // Default: "Trace" (aka "Verbose")

      switch (eventType) {
        case TraceEventType.Critical: level = 5; break; // (aka "Fatal")
        case TraceEventType.Error: level = 4; break;
        case TraceEventType.Warning: level = 3; break;
        case TraceEventType.Information: level = 2; break;
        case TraceEventType.Transfer: level = 1; break; // There is no "Debug" EventType => (mis)use something else        
      }

      // Refine the System.Diagnostics TraceEvent to become a SmartStandards LogEvent...

      long sourceLineId = 0;
      string audienceToken = null;
      string messageTemplate = null;

      LogParaphParser.TokenizeMetaDataRightPart(formatString, out sourceLineId, out audienceToken, out messageTemplate);

      messageTemplate = messageTemplate.Replace("{{", "{").Replace("}}", "}");

      // Fallback: If the audience token could not be parsed from the formatString, try a lookup

      if (audienceToken == "") {
        if (!this.TraceSourcesToAudienceMapping.TryGetValue(sourceName, out audienceToken)) audienceToken = "Dev";
      }

      // Pass the LogEvent to the callback method

      if ((args != null) && (args.Length > 0) && (args[0] is Exception)) {
        Exception ex = (Exception)args[0];
        this.OnExceptionReceived.Invoke(audienceToken, level, sourceName, sourceLineId, kindId, ex);
      } else {
        this.OnMessageReceived.Invoke(audienceToken, level, sourceName, sourceLineId, kindId, messageTemplate, args);
      }
    }

    public override void Write(string message) { } // Not needed

    public override void WriteLine(string message) { }// Not needed

  }
}


namespace Logging.SmartStandards {
 
  internal partial class DevLogger {

    public const string AudienceToken = "Dev";

    public static void Log(
      int level, string sourceContext, long sourceLineId, int kindId, string messageTemplate, params object[] args
    ) {

      if (string.IsNullOrWhiteSpace(sourceContext)) sourceContext = "UnknownSourceContext";

      if (messageTemplate == null) messageTemplate = "";

      if (args == null) args = new object[0];

      Logging.SmartStandards.Transport.TraceBusFeed.Instance.EmitMessage(AudienceToken, level, sourceContext, sourceLineId, kindId, messageTemplate, args);

    }

    public static void Log(int level, string sourceContext, long sourceLineId, Enum kindEnumElement, params object[] args) {
      TemplateHousekeeping.LogMessageTemplateRepository.GetMessageTemplateByKind(kindEnumElement, out int kindId, out string messageTemplate);
      Log(level, sourceContext, sourceLineId, kindId, messageTemplate, args);
    }

    public static void Log(int level, string sourceContext, long sourceLineId, Exception ex) {
      int kindId = Logging.SmartStandards.Internal.ExceptionAnalyzer.InferEventIdByException(ex);
      Logging.SmartStandards.Transport.TraceBusFeed.Instance.EmitException(AudienceToken, level, sourceContext, sourceLineId, kindId, ex); 
    }

  }
}

/*********************** SAMPLES **********************************
     
  [TypeConverter(typeof(LogMessageEnumConverter))]
  internal enum LogMessages {

    /// <summary>       "Weve got a Foo!" </summary>
    [LogMessageTemplate("Weve got a Foo!")]
    Foo = 110011,

    /// <summary>       "Weve got a Bar!" </summary>
    [LogMessageTemplate("Weve got a Bar!")]
    Bar = 220022

  }

  DevLogger.LogError(LogMessages.Bar);
  DevLogger.LogError(0, 2282, "A Freetext-Message");

  DevLogger.LogError(ex);
  DevLogger.LogError(ex.Wrap(22, "Another message"));
  DevLogger.LogError(ex.Wrap("Another message B"));
      
*/

