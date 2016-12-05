using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public enum ArchiveState
    {
        New,
        Modified,
        Restored,
        Deleted
    }

    [Flags]
    public enum ComponentUserActions
    {
        NoActions = 0,
        Stop = 1,
        Start = 2,
        Restart = 4,
        Disable = 8,
        Manual = 16,
        All = 31
    }

    public enum ComponentStatus
    {
        Unknown,
        Running,
        Stopped,
        Disabled
    }
}
