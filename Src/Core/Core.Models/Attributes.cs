using System;

namespace Core.Models
{
    public class PropertyEditorMetadataAttribute : Attribute
    {
        /// <summary>
        /// Do not display this property in the property editor
        /// </summary>
        public bool Hidden { get; set; }
    }
}
