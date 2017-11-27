using libterminal.JobRunner;
using libterminal.Tasks.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal.Tasks.Cisco.Compound
{
    public class SetTerminalLengthAndWidth : CompoundTask
    {
        public string Destination { get; set; }
        public SetTerminalLengthAndWidth(string name, string onSuccess, string destination, string description = "")
        {
            Name = name;
            OnSuccess = onSuccess;
            InitialTask = "SendInitialCr";
            Destination = destination;

            if (string.IsNullOrEmpty(description))
                Description = "Read output of 'show terminal' and configure terminal width and length to 0";
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
                    OnSuccess = "SendShowTerminal"
                },
                new SendToDevice
                {
                    Name = "SendShowTerminal",
                    Description = "Send 'show terminal' to the device",
                    Destination = "{{" + Destination + "}}",
                    Message = "show terminal\r",
                    OnSuccess = "AwaitEnableOrMorePrompt",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "AwaitEnableOrMorePrompt",
                    Description = "Wait for enable prompt or more prompt",
                    Destination = "{{" + Destination + "}}",
                    OnFailure = "CriticalError",
                    Expressions = new List<WaitForExpression>
                    {
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                            Name = "PrivExecPrompt",
                            Description = "Privilege mode prompt received",
                            OnSuccess = "IsTerminalLength0"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+\s*--More--\s*",
                            Name = "MorePrompt",
                            Description = "Received when there is more data still queued",
                            OnSuccess = "SendSpace"
                        }
                    }
                },
                new SendToDevice
                {
                    Name = "SendSpace",
                    Description = "Send <space> to the device to get more text",
                    Destination = "{{" + Destination + "}}",
                    Message = " ",
                    OnSuccess = "AwaitEnableOrMorePrompt",
                    OnFailure = "CriticalError"
                },
                new MatchBuffer
                {
                    Name = "IsTerminalLength0",
                    Description = "Search the buffer contents for the terminal length being 0",
                    Destination =  "{{" + Destination + "}}",
                    Expression = @"[\r\n]+Length:\s+0\s+lines,\s+Width:\s+[0-9]+\s+columns",
                    OnSuccess = "IsTerminalWidth0",
                    OnFailure = "SendTerminalLength0"
                },
                new SendToDevice
                {
                    Name = "SendTerminalLength0",
                    Description = "Send 'terminal length 0' to device",
                    Destination = "{{" + Destination + "}}",
                    Message = "terminal length 0\r",
                    OnSuccess = "AwaitEnablePromptAfterTerminalLength0",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "AwaitEnablePromptAfterTerminalLength0",
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
                            OnSuccess = "IsTerminalWidth0"
                        }
                    }
                },
                new MatchBuffer
                {
                    Name = "IsTerminalWidth0",
                    Description = "Search the buffer contents for the terminal width being 0",
                    Destination =  "{{" + Destination + "}}",
                    Expression = @"[\r\n]+Length:\s+[0-9]+\s+lines,\s+Width:\s+0\s+columns",
                    OnSuccess = "PopStartOfBuffer",
                    OnFailure = "SendTerminalWidth0"
                },
                new SendToDevice
                {
                    Name = "SendTerminalWidth0",
                    Description = "Send 'terminal width 0' to device",
                    Destination = "{{" + Destination + "}}",
                    Message = "terminal width 0\r",
                    OnSuccess = "AwaitEnablePromptAfterTerminalWidth0",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "AwaitEnablePromptAfterTerminalWidth0",
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
                            OnSuccess = "PopStartOfBuffer"
                        }
                    }
                },
                new PopBuffer
                {
                    Name = "PopStartOfBuffer",
                    Description = "Pops the position of the start of the buffer",
                    Destination = "{{" + Destination + "}}",
                    OnSuccess = "Done"
                }
            };
        }
    }
}
