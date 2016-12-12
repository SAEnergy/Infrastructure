using Core.Interfaces.Components.Logging;
using Core.Interfaces.Components;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Core.Interfaces.Components.IoC;
using Core.Models;
using System.Data.Entity.Infrastructure;

namespace Server.Components
{

    public interface IDatabaseContextFactory
    {
        DbContext NewContext();
    }


    [ComponentRegistration(typeof(IDataComponent))]
    [ComponentMetadata(Description = "Data access layer for SQL.", FriendlyName = "SQL Data Component")]
    public sealed class SQLDataComponent : IDataComponent
    {
        #region Fields

        private readonly ILogger _logger;
        private readonly IDatabaseContextFactory _contextFactory;

        #endregion

        #region Constructor

        public SQLDataComponent(ILogger logger, IDatabaseContextFactory contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        #endregion

        #region Public Methods

        public bool Delete<T>(Func<T, bool> where) where T : class
        {
            bool retVal = false;

            using (DbContext db = _contextFactory.NewContext())
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
                using (DbContext db = _contextFactory.NewContext())
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

            using (DbContext db = _contextFactory.NewContext())
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

            using (DbContext db = _contextFactory.NewContext())
            {
                var set = db.Set<T>();
                result = set.Find(key);
            }

            return result;
        }

        public List<T> Find<T>(Func<T, bool> where) where T : class
        {
            List<T> results;

            using (DbContext db = _contextFactory.NewContext())
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
                using (DbContext db = _contextFactory.NewContext())
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

            using (DbContext db = _contextFactory.NewContext())
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
                _logger.Log("Null value detected sent to DataComponent...", severity: LogMessageSeverity.Error);
            }

            return retVal;
        }

        #endregion
    }
}
