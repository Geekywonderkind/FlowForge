using System;
using System.Collections.Generic;
using System.Text;

namespace FlowForge.Core.Models
{
    public enum JobStatus : short
    {
        Queued = 0,
        Running = 1,
        Succeeded = 2,
        Failed = 3,
        Cancelled = 4
    }
}