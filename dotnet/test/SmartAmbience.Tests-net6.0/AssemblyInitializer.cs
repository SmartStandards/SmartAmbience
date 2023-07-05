using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.CompilerServices;

namespace System.Threading {

  [TestClass()]
  public class AssemblyInitializer {

    [AssemblyInitialize()]
    public static void AssemblyInitialize(TestContext testContext) {

      AmbientField.ContextAdapter = new AmbienceToAppdomainAdapter();


    }

  }

}