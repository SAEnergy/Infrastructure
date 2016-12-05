using Core.Models;
using System;

namespace Core.Interfaces.Components.IoC
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentRegistrationAttribute : Attribute
    {
        public Type InterfaceType { get; set; }

        public bool DoNotRegister { get; set; }

        public ComponentRegistrationAttribute(Type interfaceType)
        {
            InterfaceType = interfaceType;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentMetadataAttribute : Attribute
    {
        public string Description { get; set; }

        public string FriendlyName { get; set; }

        public ComponentUserActions AllowedActions { get; set; }

        public ComponentMetadataAttribute()
        {
            AllowedActions = ComponentUserActions.NoActions;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ProxyDecoratorAttribute : Attribute
    {
        public Type[] ProxyTypes { get; set; }

        public ProxyDecoratorAttribute(params Type[] types)
        {
            ProxyTypes = types;
        }
    }
}
