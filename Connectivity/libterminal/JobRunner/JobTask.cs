using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libterminal.JobRunner
{
    public abstract class JobTask
    {
        public string TaskType
        {
            get
            {
                return string.Join(".", GetType().ToString().Split('.').Skip(2).ToList());
            }
        }

        public Guid Id { get; } = Guid.NewGuid();

        public string Name { get; set; }

        public string Description { get; set; }

        public abstract string Execute(Job job);
    }
}
