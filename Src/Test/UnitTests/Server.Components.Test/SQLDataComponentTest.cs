//using System;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Test.Mocks;
//using Core.Interfaces.Components.Logging;
//using Core.Models.Persistent;
//using Core.Interfaces.Components;
//using System.Linq;
//using Server.Components;
//using Scheduler.Interfaces;

//namespace Core.Components.Test
//{
//    [TestClass]
//    public class SQLDataComponentTest
//    {
//        [ClassInitialize]
//        public static void Init(TestContext context)
//        {
//            DatabaseSettings.Instance.ConnectionString = string.Format("Data Source={0}\\HostServiceTestDb.sdf", Environment.CurrentDirectory);
//        }

//        [TestMethod]
//        public void SQLDataComponentTest_InsertTest()
//        {
//            var component = new SQLDataComponent(new LoggerMock());

//            var newJob = new JobConfiguration();
//            newJob.Name = "Test Job";

//            Assert.IsTrue(component.Insert(newJob));
//        }

//        [TestMethod]
//        public void SQLDataComponentTest_UpdateTest()
//        {
//            var component = new SQLDataComponent(new LoggerMock());

//            InsertIfNeeded(component);

//            var result = component.Find<JobConfiguration>(u => u.Name == "Test Job");
//            Assert.IsNotNull(result);

//            var job = result.FirstOrDefault();
//            Assert.IsNotNull(job);
//            string oldName = job.Name;
//            job.Name = "Weeee" + Guid.NewGuid(); //Allows unit test to run over and over again without failing

//            Assert.IsTrue(component.Update(job));

//            var newJob = component.Find<JobConfiguration>(u => u.Name == job.Name).FirstOrDefault();
//            Assert.IsNotNull(newJob);
//            Assert.AreEqual(job.JobConfigurationId, newJob.JobConfigurationId);
//            Assert.AreEqual(job.Name, newJob.Name);

//            var notFound = component.Find<JobConfiguration>(u => u.Name == oldName);
//            Assert.IsNull(notFound);
//        }

//        [TestMethod]
//        public void SQLDataComponentTest_FindTest()
//        {
//            var component = new SQLDataComponent(new LoggerMock());

//            InsertIfNeeded(component);

//            var result = component.Find<JobConfiguration>(u => u.Name == "Test Job");
//            Assert.IsNotNull(result);

//            var job = result.FirstOrDefault();
//            Assert.IsNotNull(job);

//            var job1 = component.Find<JobConfiguration>(job.JobConfigurationId);

//            Assert.IsNotNull(job1);
//            Assert.AreEqual(job.JobConfigurationId, job1.JobConfigurationId);
//            Assert.AreEqual(job.Name, job1.Name);
//        }

//        [TestMethod]
//        public void SQLDataComponentTest_FindWhereTest()
//        {
//            var component = new SQLDataComponent(new LoggerMock());

//            InsertIfNeeded(component);

//            var job = component.Find<JobConfiguration>(u => u.Name == "Test Job");

//            Assert.IsNotNull(job);
//            Assert.AreEqual(1, job.Count);

//            var realJob = job.FirstOrDefault();
//            Assert.IsNotNull(realJob);
//            Assert.AreEqual("Test Job", realJob.Name);

//            var jobToo = component.Find<JobConfiguration>(u => u.JobConfigurationId == -1);
//            Assert.IsNull(jobToo);
//        }

//        [TestMethod]
//        public void SQLDataComponentTest_DeleteTest()
//        {
//            var component = new SQLDataComponent(new LoggerMock());

//            InsertIfNeeded(component);

//            var job = component.Find<JobConfiguration>(u => u.Name == "Test Job");

//            Assert.IsNotNull(job);
//            Assert.AreEqual(1, job.Count);

//            var job1 = job.FirstOrDefault();
//            Assert.AreEqual("Test Job", job1.Name);

//            Assert.IsTrue(component.Delete<JobConfiguration>(job1.JobConfigurationId));
//            var find = component.Find<JobConfiguration>(job1.JobConfigurationId);

//            Assert.IsNull(find);
//        }

//        [TestMethod]
//        public void SQLDataComponentTest_DeleteWhereTest()
//        {
//            var component = new SQLDataComponent(new LoggerMock());

//            InsertIfNeeded(component);

//            Assert.IsTrue(component.Delete<JobConfiguration>(u => u.Name == "Test Job"));

//            Assert.IsNull(component.Find<JobConfiguration>(u => u.Name == "Test Job"));
//        }

//        #region Private Methods

//        private void InsertIfNeeded(IDataComponent component)
//        {
//            if (component.Find<JobConfiguration>(u => u.Name == "Test Job") == null)
//            {
//                var newJob = new JobConfiguration();
//                newJob.Name = "Test Job";

//                Assert.IsTrue(component.Insert<JobConfiguration>(newJob));
//            }
//        }

//        #endregion
//    }
//}
