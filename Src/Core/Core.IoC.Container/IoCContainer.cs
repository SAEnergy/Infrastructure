using Core.Interfaces.Base;
using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.IoC.Container
{
    public class IoCContainer : IIoCContainer
    {
        #region Fields

        #region Statics

        private static IoCContainer _instance;
        private static object _syncObject = new object();

        #endregion

        private Dictionary<Type, RegisteredObject> _registeredObjects;

        #endregion

        #region Properties

        public static IoCContainer Instance
        {
            get
            {
                if(_instance == null)
                {
                    lock(_syncObject)
                    {
                        if(_instance == null)
                        {
                            _instance = new IoCContainer();
                        }
                    }
                }

                return _instance;
            }
        }

        #endregion

        #region Constructor

        private IoCContainer()
        {
            _registeredObjects = new Dictionary<Type, RegisteredObject>();
        }

        #endregion

        #region Public Methods

        public List<KeyValuePair<Type, Type>> GetRegisteredTypes()
        {
            return _registeredObjects.Select(r => new KeyValuePair<Type, Type>(r.Key, r.Value.ConcreteType)).ToList();
        }

        public void Register<TInterfaceType, TConcreteType>()
        {
            Register<TInterfaceType, TConcreteType>(LifeCycle.Singleton);
        }

        public void Register<TInterfaceType, TConcreteType>(LifeCycle lifeCycle)
        {
            Register(typeof(TInterfaceType), typeof(TConcreteType), lifeCycle);
        }

        public void Register(Type concreteType)
        {
            var atty = concreteType.GetAttribute<ComponentRegistrationAttribute>();

            if (atty == null) { throw new InvalidOperationException("Attempt to register component without ComponentRegistration attribute."); }
            
            var lifeCycle = typeof(SingletonBase).IsAssignableFrom(concreteType) ? LifeCycle.Singleton : LifeCycle.Transient;

            Register(atty.InterfaceType, concreteType, lifeCycle);
        }

        public void Register(Type interfaceType, Type concreteType, LifeCycle lifeCycle)
        {
            RegisteredObject obj = new RegisteredObject(interfaceType, concreteType, lifeCycle);

            _registeredObjects.Add(interfaceType, obj);
        }

        public object Resolve(Type interfaceType)
        {
            object retVal = null;

            RegisteredObject obj = null;

            if (!_registeredObjects.TryGetValue(interfaceType, out obj))
            {
                if (interfaceType == typeof(IIoCContainer))
                {
                    retVal = _instance;
                }
                else
                {
                    throw new KeyNotFoundException(string.Format("The type \"{0}\" has not been registered.", interfaceType.Name));
                }
            }
            else
            {
                retVal = obj.GetInstance();

                //initialize transient objects here
                if(obj.ObjectLifeCycle == LifeCycle.Transient)
                {
                    if(typeof(IInitializable).IsAssignableFrom(interfaceType))
                    {
                        var initializable = retVal as IInitializable;

                        if(initializable != null && !initializable.IsInitialized)
                        {
                            initializable.Initialize();
                        }
                    }
                }
            }

            return retVal;
        }

        public TInterfaceType Resolve<TInterfaceType>()
        {
            return (TInterfaceType)Resolve(typeof(TInterfaceType));
        }

        #endregion
    }
}
