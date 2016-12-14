using Core.Interfaces.Components.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Interfaces.Components
{
    public interface IDataComponent : IComponent 
    {
        bool Delete<T>(int key) where T : class;

        bool Delete<T>(T obj) where T : class;

        bool Delete<T>(Func<T, bool> where) where T : class;

        bool Update<T>(T obj) where T : class;

        bool Insert<T>(T obj) where T : class;

        List<T> Find<T>(Func<T, bool> where) where T : class;

        IQueryable<T> All<T>() where T : class;

        T Find<T>(int key) where T : class;
    }
}
