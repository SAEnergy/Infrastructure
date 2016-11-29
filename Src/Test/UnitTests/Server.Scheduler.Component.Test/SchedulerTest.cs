using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Mocks;
using Core.Models.Persistent;
using Test.Helpers;
using Test.Plugins.Mocks;
using System.Threading;
using System.Linq;
using Server.Components;
using Scheduler.Interfaces;

namespace Core.Scheduler.Test
{
    [TestClass]
    public class SchedulerTest
    {
        [TestInitialize]
        public void InitializeTest()
        {
            SingletonHelper.Clean(typeof(Scheduler));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            SingletonHelper.Clean(typeof(Scheduler));
        }

        [TestMethod]
        //[Ignore]
        public void SchedulerTest_AddCustomJob()
        {
            XMLDataComponent.Folder = Environment.CurrentDirectory;
            XMLDataComponent.FileName = "SchedulerTestData.xml";
            var scheduler = Scheduler.CreateInstance(new LoggerMock(), new XMLDataComponent(new LoggerMock()));

            scheduler.Start();

            scheduler.AddJob(BuildMeAJob());

            //give the scheduler time to create the instance and run it
            while(UnitTestJob.Instances == null)
            {
                Thread.Sleep(100);
            }

            var job = UnitTestJob.Instances.FirstOrDefault();

            Assert.IsNotNull(job);

            scheduler.Stop();
        }

        private JobConfiguration BuildMeAJob()
        {
            var config = new UnitTestJobConfiugration();
            return config;
        }
    }
}
