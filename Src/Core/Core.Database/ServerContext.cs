using Core.Models.Persistent;
using Scheduler.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Database
{
    public class ServerContext : DbContext
    {
        #region Properties

        public DbSet<JobConfiguration> JobConfigurations { get; set; }

        public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<DirectoryItem> ActiveDirectoryEntries { get; set; }

        #endregion

        #region Constructors

        public ServerContext() : this(DatabaseSettings.Instance.ConnectionString) { }

        //main constructor
        public ServerContext(string connectionString) : base(connectionString)
        {
            System.Data.Entity.Database.SetInitializer(new MigrateDatabaseToLatestVersion<ServerContext, ServerContextConfig>());
        }

        #endregion

        #region Public Methods

        public override int SaveChanges()
        {
            var objContextAdapter = this as IObjectContextAdapter;

            if (objContextAdapter != null)
            {
                var objContext = objContextAdapter.ObjectContext;
                objContext.DetectChanges();

                foreach (var insert in objContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added))
                {
                    EnsureMinDateTime(insert.Entity);
                }

                foreach (var update in objContext.ObjectStateManager.GetObjectStateEntries(EntityState.Modified))
                {
                    EnsureMinDateTime(update.Entity);
                }
            }

            return base.SaveChanges();
        }

        #endregion

        #region Private and Protected Methods

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        //SQL has a min datetime of Jan 1st 1753, where a new datetime is Jan 1st 0001.  This makes SQL CE and Server very mad
        private void EnsureMinDateTime(object entity)
        {
            var stack = new Stack<Tuple<object,PropertyInfo>>();
            GetComplextTypes(entity.GetType()).ForEach(t => stack.Push(new Tuple<object,PropertyInfo>(entity, t)));

            while(stack.Count > 0)
            {
                var complexTuple = stack.Pop();
                var obj = complexTuple.Item2.GetValue(complexTuple.Item1);

                if (obj == null) { continue; }

                CheckAndUpdateDataTimeProperties(obj);

                GetComplextTypes(complexTuple.Item2.PropertyType).ForEach(t => stack.Push(new Tuple<object, PropertyInfo>(obj, t)));
            }

            CheckAndUpdateDataTimeProperties(entity);
        }

        private void CheckAndUpdateDataTimeProperties(object obj)
        {
            var props = obj.GetType().GetProperties().Where(p => p.PropertyType == typeof(DateTime));

            foreach (var prop in props)
            {
                DateTime value = (DateTime)prop.GetValue(obj);

                if (value < (DateTime)SqlDateTime.MinValue)
                {
                    prop.SetValue(obj, (DateTime)SqlDateTime.MinValue);
                }
            }
        }

        private List<PropertyInfo> GetComplextTypes(Type type)
        {
            return type.GetProperties().Where(p => p.PropertyType.GetCustomAttributes(typeof(ComplexTypeAttribute), false).Any()).ToList();
        }

        #endregion
    }
}
