using libterminal.JobRunner;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace libterminal.Tasks.Cisco
{
    public class SetResultFromBuffer : JobTask
    {
        public string Destination { get; set; }
        public string OnSuccess { get; set; }
        public string ResultName { get; set; }
        public string TrimStartExpression { get; set; }
        public string TrimEndExpression { get; set; }

        public override string Execute(Job job)
        {
            var destinationString = job.Value(Destination);
            var connection = ConnectionManager.Instance.ConnectionByUri(new Uri(destinationString));
            if (connection == null)
                throw new Exception(Name + " - No connection exists to destination");

            var value = connection.GetActiveBuffer();

            var startIndex = 0;
            var length = value.Length;
            if (!string.IsNullOrEmpty(TrimStartExpression))
            {
                var processedStartExpression = job.Value(TrimStartExpression);
                var regex = new Regex(processedStartExpression);
                var m = regex.Match(value);
                if (m != null && m.Success)
                {
                    startIndex = m.Index + m.Length;
                    length -= m.Length;
                }
            }

            if(!string.IsNullOrEmpty(TrimEndExpression))
            {
                var processedEndExpression = job.Value(TrimEndExpression);
                var regex = new Regex(processedEndExpression);
                var m = regex.Match(value, startIndex);
                if(m != null && m.Success)
                    length = m.Index - startIndex;
            }

            var processedResultName = job.Value(ResultName);

            job.SetResultValue(processedResultName, value.Substring(startIndex, length));

            //Console.WriteLine("Buffer" + value);

            return OnSuccess;
        }
    }
}
