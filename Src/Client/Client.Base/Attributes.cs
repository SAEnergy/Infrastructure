using System;

namespace Client.Base
{
    public class PanelMetadataAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public string IconPath { get; set; }
        public bool Hidden { get; set; }
    }

    public class ClientSettingsMetadataAttribute : Attribute
    {
        /// <summary>
        /// Do not display this settings class in the settings dialog
        /// </summary>
        public bool Hidden { get; set; }
    }
}
