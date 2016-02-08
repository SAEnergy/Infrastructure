﻿using System;

namespace Core.Interfaces.Components.IoC
{
    public interface IIoCContainer
    {
        void Register<TInterfaceType, TConcreteType>();

        void Register<TInterfaceType, TConcreteType>(LifeCycle lifeCycle);

        TInterfaceType Resolve<TInterfaceType>();

        object Resolve(Type interfaceType);
    }
}