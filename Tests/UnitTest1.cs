using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnityEngine.UI;

namespace Tests
{
    [TestClass]
    public class TestInjection
    {
        [TestMethod]
        public void TestPatch()
        {
            InputFieldRTLAdapter.__forTest = 1;
        }
    }
}
