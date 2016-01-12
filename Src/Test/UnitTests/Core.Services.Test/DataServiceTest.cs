﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Database;
using Test.Mocks;
using Core.Interfaces.Logging;
using Core.Models.Persistent;
using Core.Interfaces.Services;
using System.Linq;

namespace Core.Services.Test
{
    [TestClass]
    public class DataServiceTest
    {
        [TestInitialize]
        public void Init()
        {
            DatabaseSettings.Instance.ConnectionString = string.Format("Data Source={0}\\HostServiceTestDb.sdf", Environment.CurrentDirectory);
        }

        [TestMethod]
        public void DataServiceTest_InsertTest()
        {
            var service = BuildMeADaService();

            var newUser = new User();
            newUser.UserName = "Bobby";

            Assert.IsTrue(service.Insert<User>(newUser));
        }

        [TestMethod]
        public void DataServiceTest_UpdateTest()
        {
            var service = BuildMeADaService();

            InsertIfNeeded(service);

            var result = service.Find<User>(u => u.UserName == "Bobby");
            Assert.IsNotNull(result);

            var user = result.FirstOrDefault();
            Assert.IsNotNull(user);
            string oldName = user.UserName;
            user.UserName = "Weeee" + Guid.NewGuid(); //Allows unit test to run over and over again without failing

            Assert.IsTrue(service.Update(user));

            var newUser = service.Find<User>(u => u.UserName == user.UserName).FirstOrDefault();
            Assert.IsNotNull(newUser);
            Assert.AreEqual(user.UserId, newUser.UserId);
            Assert.AreEqual(user.UserName, newUser.UserName);

            var notFound = service.Find<User>(u => u.UserName == oldName);
            Assert.IsNull(notFound);
        }

        [TestMethod]
        public void DataServiceTest_FindTest()
        {
            var service = BuildMeADaService();

            InsertIfNeeded(service);

            var result = service.Find<User>(u => u.UserName == "Bobby");
            Assert.IsNotNull(result);

            var user = result.FirstOrDefault();
            Assert.IsNotNull(user);

            var user1 = service.Find<User>(user.UserId);

            Assert.IsNotNull(user1);
            Assert.AreEqual(user.UserId, user1.UserId);
            Assert.AreEqual(user.UserName, user1.UserName);
        }

        [TestMethod]
        public void DataServiceTest_FindWhereTest()
        {
            var service = BuildMeADaService();

            InsertIfNeeded(service);

            var user = service.Find<User>(u => u.UserName == "Bobby");

            Assert.IsNotNull(user);
            Assert.AreEqual(1, user.Count);

            var realUser = user.FirstOrDefault();
            Assert.IsNotNull(realUser);
            Assert.AreEqual("Bobby", realUser.UserName);

            var userToo = service.Find<User>(u => u.UserId == -1);
            Assert.IsNull(userToo);
        }

        [TestMethod]
        public void DataServiceTest_DeleteTest()
        {
            var service = BuildMeADaService();

            InsertIfNeeded(service);

            var user = service.Find<User>(u => u.UserName == "Bobby");

            Assert.IsNotNull(user);
            Assert.AreEqual(1, user.Count);

            var bobby = user.FirstOrDefault();
            Assert.AreEqual("Bobby", bobby.UserName);

            Assert.IsTrue(service.Delete<User>(bobby.UserId));
            var find = service.Find<User>(bobby.UserId);

            Assert.IsNull(find);
        }

        [TestMethod]
        public void DataServiceTest_DeleteWhereTest()
        {
            var service = BuildMeADaService();

            InsertIfNeeded(service);

            Assert.IsTrue(service.Delete<User>(u => u.UserName == "Bobby"));

            Assert.IsNull(service.Find<User>(u => u.UserName == "Bobby"));
        }

        #region Private Methods

        private void InsertIfNeeded(IDataService service)
        {
            if (service.Find<User>(u => u.UserName == "Bobby") == null)
            {
                var newUser = new User();
                newUser.UserName = "Bobby";

                Assert.IsTrue(service.Insert<User>(newUser));
            }
        }

        private IDataService BuildMeADaService()
        {
            ILogger mockLogger = new LoggerMock();

            return new DataService(mockLogger);
        }

        #endregion
    }
}
