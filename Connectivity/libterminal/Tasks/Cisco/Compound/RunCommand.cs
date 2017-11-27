using libterminal.JobRunner;
using libterminal.Tasks.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal.Tasks.Cisco.Compound
{
    public class RunCommand : CompoundTask
    {
        public string Destination { get; set; }
        public string Command { get; set; }
        public string ResultName { get; set; }
        public RunCommand(string name, string onSuccess, string destination, string command, string resultName, string description = "")
        {
            Name = name;
            OnSuccess = onSuccess;
            InitialTask = "SendInitialCr";
            Destination = destination;
            Command = command;
            ResultName = resultName;

            if (string.IsNullOrEmpty(description))
                Description = "'Run command {{" + Command + "}} on {{Url(" + Destination + ").StripUserInfo}} and return result in " + resultName;
            else
                Description = description;

            Tasks = new List<JobTask>
            {
                new SendToDevice
                {
                    Name = "SendInitialCr",
                    Description = "Send a carriage return to make sure there is a connection",
                    Destination = "{{" + Destination + "}}",
                    Message = "\r",
                    OnSuccess = "AwaitEnablePrompt",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "AwaitEnablePrompt",
                    Description = "Wait for enable prompt",
                    Destination = "{{" + Destination + "}}",
                    OnFailure = "CriticalError",
                    Expressions = new List<WaitForExpression>
                    {
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                            Name = "PrivExecPrompt",
                            Description = "Privilege mode prompt received",
                            OnSuccess = "SaveStartOfBuffer"
                        }
                    }
                },
                new PushBuffer
                {
                    Name = "SaveStartOfBuffer",
                    Description = "Save the position of the start of the buffer",
                    Destination = "{{" + Destination + "}}",
                    OnSuccess = "SendShowCommand"
                },
                new SendToDevice
                {
                    Name = "SendShowCommand",
                    Description = "Send '{{" + Command + "}}' command to the device",
                    Destination = "{{" + Destination + "}}",
                    Message = "{{" + Command + "}}\r",
                    OnSuccess = "AwaitFinalEnablePrompt",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "AwaitFinalEnablePrompt",
                    Description = "Wait for enable prompt",
                    Destination = "{{" + Destination + "}}",
                    OnFailure = "CriticalError",
                    Expressions = new List<WaitForExpression>
                    {
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                            Name = "PrivExecPrompt",
                            Description = "Privilege mode prompt received",
                            OnSuccess = "DumpBuffer"
                        }
                    }
                },
                new DumpBuffer
                {
                    Name = "DumpBuffer",
                    Description = "Dump the contents of the buffer to the console",
                    Destination = "{{" + Destination + "}}",
                    OnSuccess = "SaveResult"
                },
                new SetResultFromBuffer
                {
                    Name = "SaveResult",
                    Description = "Sends the contents of the buffer to the result variable {{" + ResultName + "}}",
                    Destination = "{{" + Destination + "}}",
                    ResultName = "{{" + resultName + "}}",
                    TrimStartExpression = @"{{EscapeRegex(" + Command + @")}}[\r\n]+",
                    TrimEndExpression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                    OnSuccess = "PopBufferPosition"
                },
                new PopBuffer
                {
                    Name = "PopBufferPosition",
                    Description = "Pop the buffer position from the stack",
                    Destination = "{{" + Destination + "}}",
                    OnSuccess = "Done"
                }
            };
        }
    }
}
