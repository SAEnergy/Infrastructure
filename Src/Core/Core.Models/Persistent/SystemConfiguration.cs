﻿using Core.Models.ComplexTypes;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Persistent
{
    public class SystemConfiguration
    {
        [Key]
        public int SystemConfigurationId { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public AuditInfo AuditInfo { get; set; }
    }
}
