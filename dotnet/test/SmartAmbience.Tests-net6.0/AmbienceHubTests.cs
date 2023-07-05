using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace System.Threading {

  [TestClass()]
  public partial class AmbienceHubTests {

    [TestInitialize]
    public void Initialize() {
      AmbienceHub.ResetCustomBindings();
    }

    [TestMethod()]
    public void AmbienceHub_CapturingShouldAddPrefix() {

      string restoreLog = "";

      AmbienceHub.BindCustomEndpoint(
        "A",
        (capture) => capture("a1", "Foo"),
        (sourceToRestore) => restoreLog += "a"
      );

      AmbienceHub.BindCustomEndpoint(
        "B",
        (capture) => capture("b1", "Bar"),
        (sourceToRestore) => restoreLog += "b"
      );

      var dump = AmbienceHub.CaptureCurrentValuesAsDump();

      Assert.AreEqual("A.a1: Foo" + Environment.NewLine + "B.b1: Bar" + Environment.NewLine, dump);

    }

    [TestMethod()]
    public void AmbienceHub_EndpointsShouldRestoreOnlyEpDedicatedValues() {

      bool ep1Restored = false;
      bool ep2Restored = false;

      AmbienceHub.BindCustomEndpoint(
        "A",
        (capture) => { },
        (sourceToRestore) => {
          ep1Restored = true;
          Assert.AreEqual(1, sourceToRestore.Count());
          Assert.AreEqual("a1", sourceToRestore.First().Key);
          Assert.AreEqual("Foo", sourceToRestore.First().Value);
        }
      );

      AmbienceHub.BindCustomEndpoint(
        "B",
        (capture) => { },
        (sourceToRestore) => {
          ep2Restored = true;
          Assert.AreEqual(2, sourceToRestore.Count());
          Assert.AreEqual("b1", sourceToRestore.First().Key);
          Assert.AreEqual("Bar", sourceToRestore.First().Value);
          Assert.AreEqual("b2", sourceToRestore.Last().Key);
          Assert.AreEqual("Baz", sourceToRestore.Last().Value);
        }
      );

      var valuesToRestore = new Dictionary<string, string>();
      valuesToRestore.Add("B.b1", "Bar");
      valuesToRestore.Add("A.a1", "Foo");
      valuesToRestore.Add("B.b2", "Baz");

      AmbienceHub.RestoreValuesFrom(valuesToRestore);

      Assert.IsTrue(ep1Restored);
      Assert.IsTrue(ep2Restored);

    }

    [TestMethod()]
    public void AmbienceHub_EndpointsShouldRestoreInCorrectOrder() {

      string restoreLog = "";

      AmbienceHub.BindCustomEndpoint(
        "B",
        (capture) => { },
        (sourceToRestore) => restoreLog += "b",500,
        "A"
      );

      AmbienceHub.BindCustomEndpoint(
        "E",
        (capture) => { },
        (sourceToRestore) => restoreLog += "e", 500
      );

      AmbienceHub.BindCustomEndpoint(
        "A",
        (capture) => { },
        (sourceToRestore) => restoreLog += "a", 500
      );

      AmbienceHub.BindCustomEndpoint(
        "D",
        (capture) => { },
        (sourceToRestore) => restoreLog += "d", 500,
        "A", "C"
      );

      AmbienceHub.BindCustomEndpoint(
        "C",
        (capture) => { },
        (sourceToRestore) => restoreLog += "c", 500,
        "A"
      );

      AmbienceHub.BindCustomEndpoint(
        "F",
        (capture) => { },
        (sourceToRestore) => restoreLog += "f", 500
      );

      var emptyDummy = new Dictionary<string,string> ();
      AmbienceHub.RestoreValuesFrom(emptyDummy);

      Assert.AreEqual("abcdef", restoreLog);


    }

    [TestMethod()]
    public void AmbienceHub_RestorePerformanceAssertShouldWork() {

      AmbienceHub.BindCustomEndpoint(
        "MyEP",
        (capture) => { },
        (sourceToRestore) => {
          Thread.Sleep(150);
        },
        100
      );

      bool hookWasCalled = false;

      AmbienceHub.OnRestorePerformanceAssertFailedMethod = (
        (string endpointName, int msElapsed) => {
          hookWasCalled = true;
          Assert.AreEqual("MyEP", endpointName);
          Assert.IsTrue(msElapsed >= 150);
        }
      );

      var emptyDummy = new Dictionary<string, string>();
      AmbienceHub.RestoreValuesFrom(emptyDummy);

      Assert.IsTrue(hookWasCalled);

    }

    [TestMethod()]
    public void AmbienceHub_RestorePerformanceAssertShouldNotMakeFalsePositive() {

      AmbienceHub.BindCustomEndpoint(
        "MyEP",
        (capture) => { },
        (sourceToRestore) => {
          Thread.Sleep(50);
        },
        300
      );

      bool hookWasCalled = false;

      AmbienceHub.OnRestorePerformanceAssertFailedMethod = (
        (string endpointName, int msElapsed) => {
          hookWasCalled = true;
        }
      );

      var emptyDummy = new Dictionary<string, string>();
      AmbienceHub.RestoreValuesFrom(emptyDummy);

      Assert.IsFalse(hookWasCalled);

    }

  }

}
