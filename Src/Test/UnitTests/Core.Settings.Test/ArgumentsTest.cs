using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Mocks;

namespace Core.Settings.Test
{
    [TestClass]
    public class ArgumentsTest
    {
        [TestMethod]
        public void ArgumentsTestLoadRequiredArguments()
        {
            string[] requiredArguments = "/BoolTest /StringTest=IAmAString /IntTest=5".Split(null); //split by whitespace

            ArgumentsMock test = new ArgumentsMock(new LoggerMock());

            test.Load(requiredArguments);

            Assert.IsTrue(test.BoolTest);
            Assert.AreEqual("IAmAString", test.StringTest);
            Assert.AreEqual(5, test.IntTest);
        }

        [TestMethod]
        public void ArgumentsTestLoadAllArguments()
        {
            string[] allArguments = "/BoolTest /StringTest=IAmAString /IntTest=5 /SuperAwesomeBool /SuperAwesomeString=SuperAwesomeStringTest /StringDelimiterOverrideTest#DelimiterOverride /SuperAwesomeInt=20 /IntDelimiterOverrrideTest--1".Split(null); //split by whitespace

            ArgumentsMock test = new ArgumentsMock(new LoggerMock());

            test.Load(allArguments);

            Assert.IsTrue(test.BoolTest);
            Assert.IsTrue(test.BoolNameOverrideTest);
            Assert.IsTrue(test.BoolDefaultOverrideTest);

            Assert.AreEqual("IAmAString", test.StringTest);
            Assert.AreEqual("SuperAwesomeStringTest", test.StringNameOverrideTest);
            Assert.AreEqual("DelimiterOverride", test.StringDelimiterOverrideTest);
            Assert.AreEqual("SuperAwesomeStringString", test.StringDefaultOverrrideTest);

            Assert.AreEqual(5, test.IntTest);
            Assert.AreEqual(20, test.IntNameOverrideTest);
            Assert.AreEqual(-1, test.IntDelimiterOverrrideTest);
            Assert.AreEqual(42, test.IntDefaultOverrideTest);
        }

        [TestMethod]
        public void ArgumentsTestLoadRequiredArgumentsBadValues()
        {
            string[] requiredArguments = "/BoolTest /StringTest= /IntTest=BadVal".Split(null); //split by whitespace

            ArgumentsMock test = new ArgumentsMock(new LoggerMock());

            test.Load(requiredArguments);

            Assert.IsTrue(test.BoolTest);
            Assert.AreEqual(string.Empty, test.StringTest);
            Assert.AreEqual(0, test.IntTest);

            test.IntTest = 5;

            test.Load(requiredArguments); //should reset properties, therefore 5 should be back to 0

            Assert.IsTrue(test.BoolTest);
            Assert.AreEqual(string.Empty, test.StringTest);
            Assert.AreEqual(0, test.IntTest);
        }
    }
}
