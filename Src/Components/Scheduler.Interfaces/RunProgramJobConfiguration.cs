using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scheduler.Interfaces
{
    public class RunProgramJobConfiguration : JobConfiguration
    {
        [StringLength(255)]
        public string FileName { get; set; }

        [StringLength(255)]
        public string Arguments { get; set; }

        [StringLength(255)]
        public string WorkingDirectory { get; set; }

        public bool CaptureOutput { get; set; }

        public bool KillProcOnCancel { get; set; }

    }
}
