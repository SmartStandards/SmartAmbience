//using System.Collections.Generic;
//using System.Reflection;

//namespace System.Threading {

//  public static partial class ThreadUtility {

//    /// <summary>
//    ///   Returns a named data slot instance, only if it exists. Won't add one implicitely (in contrast to GetNamedDataSlot()).
//    /// </summary>
//    /// <param name="name"> Name of the data slot to search for. </param>
//    /// <param name="dataSlot"> The found data slot or null. </param>
//    /// <returns> 
//    ///   True, if the existence of the data slot could be determined.
//    ///   False, if the lookup failed due to an incompatible version of .net.
//    /// </returns>
//    /// <remarks>
//    ///   The internal implementation is a hack with strong dependency to specific .net versions (currently 4.8).
//    ///   It uses reflection to access private members of the .net Thread type.
//    /// </remarks>
//    public static bool TryGetNamedDataSlot(string name, ref LocalDataStoreSlot dataSlot) {

//      var pi = typeof(Thread).GetProperty("LocalDataStoreManager", BindingFlags.NonPublic | BindingFlags.Static);
//      if (pi is null)
//        return false;

//      var localDataStoreManager = pi.GetValue(null);
//      if (localDataStoreManager is null)
//        return false;

//      var keyToSlotMapInfo = localDataStoreManager.GetType().GetField("m_KeyToSlotMap", BindingFlags.NonPublic | BindingFlags.Instance);
//      if (keyToSlotMapInfo is null)
//        return false;

//      Dictionary<string, LocalDataStoreSlot> keyToSlotMap = (Dictionary<string, LocalDataStoreSlot>)keyToSlotMapInfo.GetValue(localDataStoreManager);
//      if (keyToSlotMap is null)
//        return false;

//      if (!keyToSlotMap.TryGetValue(name, out dataSlot))
//        dataSlot = null;

//      return true;
//    }

//  }

//}
