using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System {

  internal partial class PriorityList<T> {

    private List<T> _ItemsByPriority = new List<T>();
    private List<Tuple<T, T>> _Preferences = new List<Tuple<T, T>>();

    public PriorityList() {
      _ItemsByPriority.Add(default);
    }

    /// <summary>
    /// Generates a Report for Diagnostics and Troubleshooting
    /// </summary>
    public string DumpPreferences() {
      var result = new StringBuilder();
      result.AppendLine("RULES:");
      foreach (var p in _Preferences)
        result.AppendLine($"  >> prefer '{ItemToString(p.Item1)}' before '{ItemToString(p.Item2)}'");
      result.Append("FINAL ORDER:");
      foreach (var i in _ItemsByPriority)
        result.Append($" '{ItemToString(i)}'");
      result.AppendLine();
      return result.ToString();
    }

    private string ItemToString(T item) {
      if (item == null) {
        return "<DEFAULT>";
      }
      if (typeof(T) == typeof(Type)) {
        return ((Type)(object)item).FullName;
      }
      else {
        return item.ToString() + "(" + item.GetType().FullName + ")";
      }
    }

    #region  Item-Management 

    public bool Contains(T item) {
      lock (_ItemsByPriority) {
        if (_ItemsByPriority.Contains(item)) {
          return true;
        }
      }
      return false;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] ItemsByPriority {
      get {
        lock (_ItemsByPriority)
          return _ItemsByPriority.Where(i => i != null).ToArray();
      }
    }

    #endregion

    /// <summary>
    /// retuns false, if the rule cant be applied because of cyclic references or
    /// when trying to priorize fallback-items before preferred items...
    /// throws an exception, if one of the given items is not present in the list
    /// </summary>
    /// <param name="higherPriorityItem">use null to address any unknown</param>
    /// <param name="lowerPriorityItem">use null to address any unknown</param>
    /// <returns></returns>
    public bool TryDeclarePreference(T higherPriorityItem, T lowerPriorityItem) {

      if (higherPriorityItem == null && lowerPriorityItem == null) {
        throw new ArgumentNullException("only one of the given args can be null!");
      }
      if (higherPriorityItem != null && lowerPriorityItem != null && ReferenceEquals(higherPriorityItem, lowerPriorityItem)) {
        throw new ArgumentNullException("the given args cannot be equal");
      }

      lock (_Preferences) {

        lock (_ItemsByPriority) {
          if (!_ItemsByPriority.Contains(higherPriorityItem)) {
            int indexOfNull = _ItemsByPriority.IndexOf(default);
            _ItemsByPriority.Insert(indexOfNull, higherPriorityItem);
          }

          if (!_ItemsByPriority.Contains(lowerPriorityItem)) {
            int indexOfNull = _ItemsByPriority.IndexOf(default);
            _ItemsByPriority.Insert(indexOfNull, lowerPriorityItem);
          }
        }

        var newPreference = new Tuple<T, T>(higherPriorityItem, lowerPriorityItem);
        var extendedPReferences = _Preferences.Union(new[] { newPreference });

        if (TrySort(extendedPReferences)) {
          _Preferences.Add(newPreference);
          return true;
        }

        return false;
      }

    }

    private bool TrySort(IEnumerable<Tuple<T, T>> preferences) {
      var snapshot = _ItemsByPriority.ToList();

      // do the complete sorting multiple times, because
      // later processed rules could corrupt the results
      // from other rules - so we do this as often, as there
      // are rules present +1. if were are not compliant after
      // that, we can be sure, that the rules are cyclic...
      for (int i = 1, loopTo = preferences.Count() + 1; i <= loopTo; i++) {

        bool wasReordered = false;
        foreach (var pref in preferences) // move items for every pereference
        {

          int indexOfPrefered = snapshot.IndexOf(pref.Item1);
          int indexOfOverridden = snapshot.IndexOf(pref.Item2);

          if (indexOfPrefered > indexOfOverridden) {
            snapshot.RemoveAt(indexOfPrefered);
            snapshot.Insert(indexOfOverridden, pref.Item1);
            wasReordered = true;
          }

        }

        if (!wasReordered) {
          _ItemsByPriority = snapshot;
          return true;
        }

      }

      return false; // cyclic reference detected
    }

    public int PriorityOf(T item) {
      lock (_ItemsByPriority) {
        int indexOfNull = _ItemsByPriority.IndexOf(default);
        int idx = _ItemsByPriority.IndexOf(item);
        if (idx == -1) {
          return indexOfNull;
        }
        else {
          return idx;
        }
      }
    }

    #region  Sort 

    public IOrderedEnumerable<T> SortByPriority(IEnumerable<T> input) {
      lock (_ItemsByPriority) {
        int indexOfNull = _ItemsByPriority.IndexOf(default);
        return input.OrderBy(e =>
        {
          int idx = _ItemsByPriority.IndexOf(e);
          if (idx == -1) {
            return indexOfNull;
          }
          else {
            return idx;
          }
        });
      }
    }

    public IOrderedEnumerable<T> SortByPriorityDesc(IEnumerable<T> input) {
      lock (_ItemsByPriority) {
        int indexOfNull = _ItemsByPriority.IndexOf(default);
        return input.OrderByDescending(e =>
        {
          int idx = _ItemsByPriority.IndexOf(e);
          if (idx == -1) {
            return indexOfNull;
          }
          else {
            return idx;
          }
        });
      }
    }

    #endregion

  }

}
