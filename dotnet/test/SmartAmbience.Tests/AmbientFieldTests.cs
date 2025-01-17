using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace System.Threading {

  [TestClass()]
  public class AmbientFieldTests {

    private static AmbientField _AmbientField = new AmbientField("AmbientField");

    private static AmbientField _SealedAmbientField = new AmbientField("SealedAmbientField", true);

    private static AmbientField _TemporaryBranchTestField = new AmbientField("TemporaryBranchTestField");

    private static AmbientField _AmbientFieldWithPreStagedValue; // Hier bewusst kein "New"!

    [TestMethod()]
    public void AmbientField_SettingDifferentValues_ShouldWorkAsExpected() {

      // Owner, Name and Key should have been set correctly

      Assert.AreEqual("AmbientField", _AmbientField.Name);
      Assert.IsTrue(_AmbientField.Key.IndexOf("AmbientField'") >= 0);

      // By default, changing the (root) value is allowed

      Assert.IsNull(_AmbientField.Value);

      _AmbientField.Value = "Eins";

      Assert.AreEqual("Eins", _AmbientField.Value);

      _AmbientField.Value = "Zwei";

      Assert.AreEqual("Zwei", _AmbientField.Value);

      // Empty string is allowed

      _AmbientField.Value = "";

      Assert.AreEqual("", _AmbientField.Value);

      // Null string is not allowed

      Exception caughtException = null;

      try {
        _AmbientField.Value = null;
      }
      catch (Exception ex) {
        caughtException = ex;
      }

      //Assert.AreEqual($"Null can not be carried as value for \"AmbientField\"!", caughtException.Message);
      Assert.AreEqual(null, caughtException);
      Assert.AreEqual(null, _AmbientField.Value);

      // By default (no constructor parameter), it's not an "ExposedField"

      Assert.IsFalse(AmbientField.ExposedInstances.Keys.Contains("AmbientField"));
    }

    [TestMethod()]
    public void AmbientField_SealingAndExposed_ShouldWorkAsExpected() {
      Exception caughtException = null;

      Assert.IsNull(_SealedAmbientField.Value);

      // Sealing before setting a value should throw an Exception

      Assert.AreEqual("(NotPresent)", _SealedAmbientField.ContextValueIsSealed);

      try {
        _SealedAmbientField.SealContextValue();
      }
      catch (Exception ex) {
        caughtException = ex;
      }

      Assert.AreEqual($"Context value for \"{_SealedAmbientField.Key}\" cannot be sealed, because it does not exist!", caughtException.Message);

      // Setting a value and sealing it afterwards should work...

      _SealedAmbientField.Value = "Highlander";

      Assert.AreEqual("Highlander", _SealedAmbientField.Value);

      _SealedAmbientField.SealContextValue();

      Assert.AreEqual("True", _SealedAmbientField.ContextValueIsSealed);

      // ... trying to change the value should throw an Exception...

      try {
        _SealedAmbientField.Value = "Es kann nur einen geben.";
      }
      catch (Exception ex) {
        caughtException = ex;
      }

      Assert.AreEqual($"Sealed context value for \"SealedAmbientField\" has already been set to \"Highlander\" and cannot be changed to \"Es kann nur einen geben.\"!", caughtException.Message);

      // ... the value should not have changed ...

      Assert.AreEqual("Highlander", _SealedAmbientField.Value);

      // ... setting the same value is allowed (no Exception):

      _SealedAmbientField.Value = "Highlander";

      // "ExposedField=Ture" should appear in ExposedInstances

      Assert.IsTrue(AmbientField.ExposedInstances.Keys.Contains("SealedAmbientField"));
    }

    [TestMethod()]
    public void AmbientField_InvokeUnderTemporaryBranch_ShouldWorkAsExpected() {
      _TemporaryBranchTestField.Value = "OriginalValue";

      Exception caughtException = null;

      try {
        _TemporaryBranchTestField.InvokeUnderTemporaryBranch("OverriddenValue", () =>
        {
          Assert.AreEqual("OverriddenValue", _TemporaryBranchTestField.Value);
          int zero = 0;
          int boom = (1 / zero);
        }
);
      }
      catch (Exception ex) {
        caughtException = ex;
      }

      Assert.AreEqual(typeof(DivideByZeroException), caughtException.GetType());

      Assert.AreEqual("OriginalValue", _TemporaryBranchTestField.Value);
    }

    [TestMethod()]
    public void AmbientField_InjectingPreStagedValue_ShouldWorkAsExpected() {
      AmbientField.InjectPreStagedValue("AmbientFieldWithPreStagedValue", "InjectedDummyValue");

      _AmbientFieldWithPreStagedValue = new AmbientField("AmbientFieldWithPreStagedValue", true);

      Assert.AreEqual("InjectedDummyValue", _AmbientFieldWithPreStagedValue.Value);

      Exception caughtException = null;

      try {
        _AmbientFieldWithPreStagedValue = new AmbientField("AmbientFieldWithPreStagedValue", true);
      }
      catch (Exception ex) {
        caughtException = ex;
      }

      Assert.IsTrue(caughtException.Message.IndexOf("same key") >= 0);
    }

    [TestMethod()]
    public void AmbientField_ContextAdapterNull_ShouldHaveSpecifiedErrorHandling() {
      IAmbienceToSomeContextAdapter rescuedContextAdapter = AmbientField.TestApi.DirectContextAdapter;

      try {
        AmbientField.TestApi.DirectContextAdapter = null;

        AmbientField myAmbientField = new AmbientField("ErrorHandlingTestAmbientField");

        // Constructor should not throw an exception although ContextAdapter is null...

        Exception caughtException = null;

        try {
          string dummyValue = _AmbientField.Value; // ... getting the value should thrown an exception ...
        }
        catch (Exception ex) {
          caughtException = ex;
        }

        Assert.IsTrue(caughtException.Message.IndexOf("must be initialized before using") >= 0);

        try {
          _AmbientField.Value = "dummy"; // ... setting the value should thrown an exception ...
        }
        catch (Exception ex) {
          caughtException = ex;
        }

        Assert.IsTrue(caughtException.Message.IndexOf("must be initialized before using") >= 0);
      }
      finally {
        AmbientField.TestApi.DirectContextAdapter = rescuedContextAdapter;
      }

    }

  }

}
