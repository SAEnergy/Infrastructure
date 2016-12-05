using System;
using System.Linq;

namespace Core.Models.DataContracts
{
    public class ComponentMetadata : ICloneable<ComponentMetadata>
    {
        public int ComponentId { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public string InterfaceTypeName { get; set; }

        public string ConcreteTypeName { get; set; }

        public ComponentMetadata[] Dependencies { get; set; }

        public ComponentUserActions UserActions { get; set; }

        public ComponentStatus Status { get; set; }

        public bool IsDisabled { get; set; }

        public int CompareTo(ComponentMetadata other)
        {
            throw new NotImplementedException();
        }

        public ComponentMetadata Clone()
        {
            ComponentMetadata newVal = (ComponentMetadata)this.MemberwiseClone();
            if (this.Dependencies != null) { newVal.Dependencies = this.Dependencies.Select(c => c.Clone()).ToArray(); }
            return newVal;
        }

        //other data
    }
}
