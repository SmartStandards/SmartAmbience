using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

[assembly: AssemblyMetadata("SourceContext", "SmartAmbience")]

namespace System {

  internal static class Extensions {

    public static bool StartsWithAny(this string extendee, IEnumerable<string> prefixes,StringComparison comparisonType) {
      foreach (string prefix in prefixes) {
        if (extendee.StartsWith(prefix, comparisonType)) {
          return true;
        }
      }
      return false;
    }

  }

}
