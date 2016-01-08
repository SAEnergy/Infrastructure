﻿using System;
using System.Collections.Generic;

namespace Core.Interfaces.Services
{
    public interface IDataService
    {
        bool Delete<T>(T obj) where T : class;

        bool Delete<T>(Func<T, bool> where) where T : class;

        bool Update<T>(int key, T obj) where T : class;

        bool Insert<T>(T obj) where T : class;

        List<T> Find<T>(Func<T, bool> where) where T : class;

        T Find<T>(int key) where T : class;
    }
}
