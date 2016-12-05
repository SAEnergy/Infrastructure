using Core.Interfaces.Components.Base;
using Core.Interfaces.Components.IoC;
using Core.Models;
using Core.Util;

namespace Core.IoC.Container
{
    public static class ComponentRegister
    {
        public static void Register()
        {
            var components = TypeLocator.FindTypes("*Component*.dll", typeof(IComponent));

            foreach (var component in components)
            {
                if (component.IsAbstract) { continue; }//skip abstract classes

                var atty = component.GetAttribute<ComponentRegistrationAttribute>();

                if (atty != null)
                {
                    if (!atty.DoNotRegister)
                    {
                        var lifeCycle = typeof(SingletonBase).IsAssignableFrom(component) ? LifeCycle.Singleton : LifeCycle.Transient;

                        IoCContainer.Instance.Register(atty.InterfaceType, component, lifeCycle);
                    }
                }
            }
        }
    }
}
