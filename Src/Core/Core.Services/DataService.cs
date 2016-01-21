﻿using Core.Database;
using Core.Interfaces.Base;
using Core.Interfaces.Logging;
using Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Core.Services
{
    public sealed class DataService : Singleton<IDataService>, IDataService
    {
        #region Fields

        private readonly ILogger _logger;

        #endregion

        #region Constructor

        private DataService(ILogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public static IDataService CreateInstance(ILogger logger)
        {
            return Instance = new DataService(logger);
        }

        public bool Delete<T>(Func<T, bool> where) where T : class
        {
            bool retVal = false;

            using (ServerContext db = new ServerContext())
            {
                var set = db.Set<T>();
                var results = set.Where(where).ToList();

                foreach(var result in results)
                {
                    set.Remove(result);
                }

                retVal = db.SaveChanges() > 0;
            }

            return retVal;
        }

        public bool Delete<T>(T obj) where T : class
        {
            bool retVal = false;

            if (ValidateObject(obj))
            {
                using (ServerContext db = new ServerContext())
                {
                    var set = db.Set<T>();
                    set.Attach(obj);
                    set.Remove(obj);

                    //indicates at least one object was removed, if you have cascading deletes it may be greater than 1.
                    retVal = db.SaveChanges() > 0;
                }
            }

            return retVal;
        }

        public bool Delete<T>(int key) where T : class
        {
            bool retVal = false;

            using (ServerContext db = new ServerContext())
            {
                var set = db.Set<T>();
                var obj = set.Find(key);

                if (obj != null)
                {
                    set.Remove(obj);
                    retVal = db.SaveChanges() > 0;
                }
            }

            return retVal;
        }

        public T Find<T>(int key) where T : class
        {
            T result = null;

            using (ServerContext db = new ServerContext())
            {
                var set = db.Set<T>();
                result = set.Find(key);
            }

            return result;
        }

        public List<T> Find<T>(Func<T, bool> where) where T : class
        {
            List<T> results;

            using (ServerContext db = new ServerContext())
            {
                var set = db.Set<T>();
                results = set.Where(where).ToList();

                if(results != null)
                {
                    results = results.Count > 0 ? results : null;
                }
            }

            return results;
        }

        public bool Insert<T>(T obj) where T : class
        {
            bool retVal = false;

            if (ValidateObject(obj))
            {
                using (ServerContext db = new ServerContext())
                {
                    var set = db.Set<T>();
                    set.Add(obj);
                    retVal = db.SaveChanges() > 0;
                }
            }

            return retVal;
        }

        public bool Update<T>(T obj) where T : class
        {
            bool result = false;

            using (ServerContext db = new ServerContext())
            {
                var set = db.Set<T>();
                T dbObj = set.Attach(obj);

                if (dbObj != null)
                {
                    db.Entry(dbObj).State = EntityState.Modified;
                    result = db.SaveChanges() > 0;
                }
            }

            return result;
        }

        #endregion

        #region Private Methods

        private bool ValidateObject(object obj)
        {
            bool retVal = true;

            if (obj == null)
            {
                retVal = false;
                _logger.Log("Null value detected sent to DataService...", LogMessageSeverity.Error);
            }

            return retVal;
        }

        #endregion
    }
}
