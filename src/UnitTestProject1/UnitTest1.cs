using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMC;

namespace UnitTestProject1
{

    // Imports FlsGliderSync.Helper

    [TestClass()]
    public class UnitTest1
    {
        private Proffix Proffix { get; set; }

        /// <summary>
    /// Testen des Lesen der Konfigurationsdateien und der Verbindung zu PROFFIX
    /// </summary>
    /// <remarks></remarks>
        [TestMethod()]
        public void TestConnectToProffix()
        {
            // PROFFIX Initialisieren
            Proffix = new Proffix("25sdf-ds829-artea-32666", "FlsGliderSync", Environment.CurrentDirectory + @"\");
            // Application.StartupPath + "\")
            // Konfigurationsdateien lesen
            if (!Proffix.Settings.IsLoaded)
            {
                Proffix.Settings.Load();
            }

            // Die Verbindung zu PROFFIX wird aufgebaut
            Proffix.Settings.DefaultDataBase = "TESTFLS";
            Proffix.LoadConnection = true;
            Assert.IsTrue(Proffix.Open());
        }

        [TestMethod()]
        public void Test1()
        {
            Assert.AreEqual("hall", "hallo");
        }

        [TestMethod()]
        public void Test2()
        {
            // Dim generalHelper As New Helper.GeneralHelper

            // Assert.Equals(Now, generalhelper.GetSinceDate(Now))

            // Assert.IsTrue(generalhelper.GetSinceDate(Now) = DateTime.minvalue
        }
    }
}