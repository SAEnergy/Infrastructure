using Server.Components;
using Core.Models.Persistent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using Test.Mocks;
using Scheduler.Interfaces;

namespace Core.Components.Test
{
    [TestClass]
    public class XMLDataComponentTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            string timeStamp = string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.UtcNow);

            string fileName = string.Format("RunTime_{0}.xml", timeStamp);

            File.Copy("XMLTestData\\DataComponent.xml", fileName);

            XMLDataComponent.Folder = Environment.CurrentDirectory;
            XMLDataComponent.FileName = fileName;
        }

        [TestMethod]
        public void XMLDataComponentTest_InsertTest()
        {
            var component = new XMLDataComponent(new LoggerMock());

            var newJob = new RunProgramJobConfiguration();
            newJob.JobConfigurationId = 1000;
            newJob.Name = "Test Job";

            Assert.IsTrue(component.Insert<JobConfiguration>(newJob));

            var realNewJob = component.Find<JobConfiguration>(1000);
            Assert.IsNotNull(realNewJob);
            Assert.AreEqual("Test Job", realNewJob.Name);
        }

        [TestMethod]
        public void XMLDataComponentTest_DeleteTest()
        {
            var component = new XMLDataComponent(new LoggerMock());

            var newJob = new RunProgramJobConfiguration();
            newJob.JobConfigurationId = 1;
            newJob.Name = "Test Job";

            Assert.IsTrue(component.Delete<JobConfiguration>(newJob));
            var realNewJob = component.Find<JobConfiguration>(1);
            Assert.IsNull(realNewJob);
        }

        [TestMethod]
        public void XMLDataComponentTest_DeleteWhereTest()
        {
            var component = new XMLDataComponent(new LoggerMock());

            Assert.IsTrue(component.Delete<JobConfiguration>(x => x.Name == "BobbyDeleteWhere"));

            var realJob = component.Find<JobConfiguration>(3);
            Assert.IsNull(realJob);
        }

        [TestMethod]
        public void XMLDataComponentTest_DeleteKeyTest()
        {
            var component = new XMLDataComponent(new LoggerMock());

            Assert.IsTrue(component.Delete<JobConfiguration>(2));

            var realJob = component.Find<JobConfiguration>(2);
            Assert.IsNull(realJob);
        }

        [TestMethod]
        public void XMLDataComponentTest_FindKeyTest()
        {
            var component = new XMLDataComponent(new LoggerMock());

            var user = component.Find<JobConfiguration>(999);

            Assert.IsNotNull(user);
            Assert.AreEqual(999, user.JobConfigurationId);
            Assert.AreEqual("BobbyInsertAfter", user.Name);
        }

        [TestMethod]
        public void XMLDataComponentTest_FindWhereTest()
        {
            var component = new XMLDataComponent(new LoggerMock());

            var list = component.Find<JobConfiguration>(u => u.Name == "BobbyInsertAfter");

            Assert.IsNotNull(list);
            Assert.AreEqual(1, list.Count);

            var realJob = list.FirstOrDefault();

            Assert.IsNotNull(realJob);
            Assert.AreEqual(999, realJob.JobConfigurationId);
            Assert.AreEqual("BobbyInsertAfter", realJob.Name);
        }

    }
}
