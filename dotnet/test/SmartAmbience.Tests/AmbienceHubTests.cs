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
      bool ep3Restored = false;

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

      AmbienceHub.BindCustomEndpoint(
        "^AGlobalEndpoint",//this one works at root level without a prefix for the keys
        (capture) => { },
        (sourceToRestore) => {
          ep3Restored = true;
          Assert.AreEqual(2, sourceToRestore.Count());
          Assert.AreEqual("Global1", sourceToRestore.First().Key);
          Assert.AreEqual("C.Global2", sourceToRestore.Last().Key); //with the suffix, because were on root level
          //the other keys must not passed to this endpoint because for suffix A. & B. we have dedicated endpoints
        }
      );

      var valuesToRestore = new Dictionary<string, string>();
      valuesToRestore.Add("B.b1", "Bar");
      valuesToRestore.Add("Global1", "xxx");
      valuesToRestore.Add("A.a1", "Foo");
      valuesToRestore.Add("B.b2", "Baz");
      valuesToRestore.Add("C.Global2", "yyy");

      AmbienceHub.RestoreValuesFrom(valuesToRestore);

      Assert.IsTrue(ep1Restored);
      Assert.IsTrue(ep2Restored);
      Assert.IsTrue(ep3Restored);

    }

    [TestMethod()]
    public void AmbienceHub_MixedCaptureWithGlobalEndpointShouldWork() {

      bool ep1Captured = false;
      bool ep2Captured = false;
      bool ep3Captured = false;

      AmbienceHub.BindCustomEndpoint(
        "A",
        (capture) => { 
          ep1Captured = true;
          capture("a1", "Foo");
        },
        (sourceToRestore) => {  }
      );

      AmbienceHub.BindCustomEndpoint(
        "^AGlobalEndpoint",//this one works at root level without a prefix for the keys
        (capture) => { 
          ep2Captured = true;
          capture("Global1","xxx");
          capture("C.Global2","yyy");
        },
        (sourceToRestore) => {  }
      );

      AmbienceHub.BindCustomEndpoint(
        "B",
        (capture) => {
          capture("b1", "Bar");
          capture("Baz", "Baz");
          ep3Captured = true;
        },
        (sourceToRestore) => {  }
      );

      var buffer = new Dictionary<string, string>();
      AmbienceHub.CaptureCurrentValuesTo(buffer);

      Assert.IsTrue(ep1Captured);
      Assert.IsTrue(ep2Captured);
      Assert.IsTrue(ep3Captured);

      Assert.AreEqual(5,buffer.Count);

    }

    [TestMethod()]
    public void AmbienceHub_ShouldThrowOnKeyCollisionDuringCapture1() {

      bool epCaptured = false;

      AmbienceHub.BindCustomEndpoint(
        "A",
        (capture) => {
          epCaptured = true;
          capture("WillCollide", "1");
          capture("WillCollide", "2");
        },
        (sourceToRestore) => { }
      );

      var buffer = new Dictionary<string, string>();
      Exception catchedException = null;

      try {
        AmbienceHub.CaptureCurrentValuesTo(buffer);
      }
      catch (Exception ex) {
        catchedException = ex;
      }

      Assert.IsTrue(epCaptured);

      Assert.IsNotNull(catchedException);
      Assert.IsTrue(catchedException.Message.Contains("WillCollide"));

      Assert.AreEqual(1, buffer.Count);

    }

    [TestMethod()]
    public void AmbienceHub_ShouldThrowOnKeyCollisionDuringCapture2() {

      bool ep1Captured = false;
      bool ep2Captured = false;

      AmbienceHub.BindCustomEndpoint(
        "^GlobalEpA",
        (capture) => {
          ep1Captured = true;
          capture("A", "a");
          capture("WillCollide", "1");
        },
        (sourceToRestore) => { }
      );

      AmbienceHub.BindCustomEndpoint(
        "^GlobalEpB",
        (capture) => {
          ep2Captured = true;
          capture("B", "b");
          capture("WillCollide", "2");
        },
        (sourceToRestore) => { }
      );

      var buffer = new Dictionary<string, string>();
      Exception catchedException = null;

      try {
        AmbienceHub.CaptureCurrentValuesTo(buffer);
      }
      catch (Exception ex) {
        catchedException = ex;
      }

      Assert.IsTrue(ep1Captured);
      Assert.IsTrue(ep2Captured);

      Assert.IsNotNull(catchedException);
      Assert.IsTrue(catchedException.Message.Contains("WillCollide"));

      Assert.AreEqual(3, buffer.Count);

    }

    [TestMethod()]
    public void AmbienceHub_ShouldThrowOnPrefixTransgressionWhenCapturingGlobal() {

      bool ep1Captured = false;
      bool ep2Captured = false;

      AmbienceHub.BindCustomEndpoint(
        "A",
        (capture) => {
          ep1Captured = true;
        },
        (sourceToRestore) => { }
      );

      AmbienceHub.BindCustomEndpoint(
        "^MyGlobalEndpoint",//this one works at root level without a prefix for the keys
        (capture) => {
          ep2Captured = true;
          capture("Global1", "xxx");
          capture("AnyManualPrefix.Global2", "yyy");
          capture("A.WillBeCollision", "uiuiui!");
        },
        (sourceToRestore) => { }
      );

      var buffer = new Dictionary<string, string>();
      Exception catchedException = null;

      try {
        AmbienceHub.CaptureCurrentValuesTo(buffer);
      }
      catch (Exception ex) {
        catchedException = ex;
      }

      Assert.IsNotNull(catchedException);
      Assert.IsTrue(catchedException.Message.Contains("A.WillBeCollision"));

      Assert.AreEqual(2, buffer.Count);

      Assert.IsTrue(ep1Captured);
      Assert.IsTrue(ep2Captured);

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
