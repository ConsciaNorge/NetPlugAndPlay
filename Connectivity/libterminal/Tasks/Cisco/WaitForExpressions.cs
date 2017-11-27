using libterminal.JobRunner;
using System;
using System.Collections.Generic;
using System.Linq;

namespace libterminal.Tasks.Cisco
{
    public class WaitForExpressions : JobTask
    {
        public List<WaitForExpression> Expressions = new List<WaitForExpression>();
        public string Destination { get; set; }
        public string OnFailure { get; set; }

        public override string Execute(Job job)
        {
            var fullExpression =
                string.Join('|',
                    Expressions
                        .Select(x =>
                            "(?<" + x.Name + ">(" + x.Expression + "))"
                        )
                        .ToList()
                 );
            System.Diagnostics.Debug.WriteLine("Expressions -> " + fullExpression);

            var destinationString = job.Value(Destination);
            var connection = ConnectionManager.Instance.ConnectionByUri(new Uri(destinationString));
            if (connection == null)
                throw new Exception(Name + " - No connection exists to destination");

            var result = connection.WaitFor(fullExpression, 3000);

            if (result == null || !result.Success)
                return OnFailure;

            var expressionNames = Expressions.Select(x => x.Name).ToList();
            var validMatch = result.Groups.Where(x => x.Length > 0 && expressionNames.Contains(x.Name)).ToList();
            if (validMatch.Count != 1)
            {
                System.Diagnostics.Debug.WriteLine("There seems to be no valid matches.");
                return OnFailure;
            }

            System.Diagnostics.Debug.WriteLine("Matched -> " + validMatch.FirstOrDefault().Name);

            var matchedExpression = Expressions.Where(x => x.Name.Equals(validMatch.FirstOrDefault().Name)).FirstOrDefault();
            if (matchedExpression == null)
                throw new Exception("Somehow we matched an expression with a name that disappeared");

            return matchedExpression.OnSuccess;
        }
    }
}
