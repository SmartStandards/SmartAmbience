using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.CompilerServices;

namespace System.Threading {

  [TestClass()]
  public partial class AmbientFieldTests {

    private static AmbientField _AmbientField = new AmbientField("AmbientField");

    private static AmbientField _WriteOnceAmbientField = new AmbientField("WriteOnceAmbientField", true);

    private static AmbientField _TemporaryBranchTestField = new AmbientField("TemporaryBranchTestField");

    [TestMethod()]
    public void AmbientField_SettingDifferentValues_ShouldWorkAsExpected() {

      // Owner, Name and Key should have been set correctly

      Assert.AreEqual(nameof(AmbientFieldTests), _AmbientField.Owner);
      Assert.AreEqual("AmbientField", _AmbientField.Name);
      Assert.IsTrue(_AmbientField.Key.IndexOf($"{nameof(AmbientFieldTests)}.AmbientField'") >= 0);

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

      Assert.AreEqual($"Null can not be carried as value for \"{nameof(AmbientFieldTests)}.AmbientField\"!", caughtException.Message);

      // By default (no constructor parameter), it's not an "ExposedField"

      Assert.IsFalse(AmbientField.ExposedInstances.Exists((AmbientField f) => f.Name == "AmbientField"));

    }

    [TestMethod()]
    public void AmbientField_WriteOnceAndExposed_ShouldWorkAsExpected() {

      Assert.IsNull(_WriteOnceAmbientField.Value);

      // Although we set the RootValueIsWriteOnce state in advance...

      _WriteOnceAmbientField.RootValueIsWriteOnce = true;

      // ... it should be possible to set a value once..

      _WriteOnceAmbientField.Value = "Highlander";

      Assert.AreEqual("Highlander", _WriteOnceAmbientField.Value);

      // ... but not twice - the 2nd time setting the value should throw an Exception...

      Exception caughtException = null;

      try {
        _WriteOnceAmbientField.Value = "Es kann nur einen geben.";
      }
      catch (Exception ex) {
        caughtException = ex;
      }

      //Assert.AreEqual($"Root value of \"{nameof(AmbientFieldTests)}.WriteOnceAmbientField\" has already been set to \"Highlander\" and cannot be changed to \"Es kann nur einen geben.\"  by \"{callerFileFullName}\"", caughtException.Message);
      Assert.AreEqual($"Root value of \"{nameof(AmbientFieldTests)}.WriteOnceAmbientField\" has already been set to \"Highlander\" and cannot be changed to \"Es kann nur einen geben.\"", caughtException.Message);

      // setting the same value is allowed

      _WriteOnceAmbientField.Value = "Highlander";

      // unsetting the ReadOnly should allow further changes:

      _WriteOnceAmbientField.RootValueIsWriteOnce = false;

      _WriteOnceAmbientField.Value = "oder auch nicht.";

      // "ExposedField=Ture" should appear in ExposedInstances

      Assert.IsTrue(AmbientField.ExposedInstances.Exists((AmbientField f) => f.Name == "WriteOnceAmbientField"));

    }

    [TestMethod()]
    public void AmbientField_InvokeUnderTemporaryBranch_ShouldWorkAsExpected() {

      _TemporaryBranchTestField.Value = "OriginalValue";

      Exception caughtException = null;

      try {

        _TemporaryBranchTestField.InvokeUnderTemporaryBranch("OverriddenValue", () => {
          Assert.AreEqual("OverriddenValue", _TemporaryBranchTestField.Value);
          var zero = default(int);
          int boom = 1 / zero;
        });
      }
      catch (Exception ex) {
        caughtException = ex;
      }

      Assert.AreEqual(typeof(DivideByZeroException), caughtException.GetType());

      Assert.AreEqual("OriginalValue", _TemporaryBranchTestField.Value);

    }

  }

}