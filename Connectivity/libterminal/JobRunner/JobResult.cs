using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal.JobRunner
{
    public class JobResult
    {
        public List<JobValue> Values { get; set; }
        public bool Success { get; set; }
    }
}
