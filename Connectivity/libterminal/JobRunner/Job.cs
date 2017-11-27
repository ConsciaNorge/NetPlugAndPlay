using libterminal.JobRunner.MacroParser;
using libterminal.JobRunner.MacroProcessor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace libterminal.JobRunner
{
    public class Job
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Description { get; set; }
        public string InitialTask { get; set; }
        public List<JobTask> Tasks { get; set; }
        public List<JobValue> Parameters { get; set; }
        public List<JobValue> Results { get; set; }

        public string Value(string input)
        {
            var parser = new Parser();
            var tree = parser.ParseParts(new ParserState { Text = input });

            var processor = new JobProcessor(this);
            var text = processor.ProcessParts(tree);

            return text;
        }

        public void SetResultValue(string name, string value)
        {
            if (Results == null)
            {
                Results = new List<JobValue>
                {
                    new JobValue { Name = name, Value = value }
                };
                return;
            }

            var existingItem = Results.Where(x => x.Name == name).FirstOrDefault();
            if (existingItem == null)
                Results.Add(new JobValue { Name = name, Value = value });
            else
                existingItem.Value = value;
        }
    }
}