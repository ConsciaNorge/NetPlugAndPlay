using libterminal.JobRunner;
using libterminal.Tasks.Common;
using System.Collections.Generic;

namespace libterminal.Tasks.Cisco.Compound
{
    public class NavigateToEnablePrompt : CompoundTask
    {
        public string Destination { get; set; }
        public string EnablePassword { get; set; }
        public NavigateToEnablePrompt(string name, string onSuccess, string destination, string enablePassword, string description = "")
        {
            Name = name;
            OnSuccess = onSuccess;
            InitialTask = "ConnectToDevice";
            Destination = destination;
            EnablePassword = enablePassword;

            if (string.IsNullOrEmpty(description))
                Description = "'Connect to {{Url(" + Destination + ").StripUserInfo}} and navigate to enable prompt";
            else
                Description = description;

            Tasks = new List<JobTask>
            {
                new CiscoLoginTask
                {
                    Name = "ConnectToDevice",
                    Description = "Connect to remote device {{Url(" + Destination +").StripUserInfo}}",
                    Destination = "{{" + Destination + "}}",
                    OnSuccess = "SendInitialCR",
                    OnFailure = "CriticalError"
                },
                new SendToDevice
                {
                    Name = "SendInitialCR",
                    Description = "Send initial carriage return to {{Url(" + Destination +").StripUserInfo}}",
                    Destination = "{{" + Destination + "}}",
                    Message = "\r",
                    OnSuccess = "ProcessInitialPrompt",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "ProcessInitialPrompt",
                    Description = "Wait for initial prompt after login",
                    Destination = "{{" + Destination + "}}",
                    OnFailure = "CriticalError",
                    Expressions = new List<WaitForExpression>
                    {
                        new WaitForExpression
                        {
                            Expression = @"[Pp]ass[Ww]ord\s*:\s*",
                            Name = "UserPasswordRequest",
                            Description = "Password prompt for user",
                            OnSuccess = "SendUserPassword"
                        },
                        new WaitForExpression
                        {
                            Expression = @"([Uu]ser[Nn]ame\s*:?\s*)|(login\s+as\s*:\s*)",
                            Name = "UsernameRequest",
                            Description = "Username requested",
                            OnSuccess = "SendUsername"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                            Name = "PrivExecPrompt",
                            Description = "Privilege mode prompt received",
                            OnSuccess = "Done"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @">\s*",
                            Name = "UserExecPrompt",
                            Description = "User mode prompt received",
                            OnSuccess = "SendEnable"
                        }
                    }
                },
                new SendToDevice
                {
                    Name = "SendEnable",
                    Description = "Send 'enable' to elevate to priv exec",
                    Destination = "{{" + Destination + "}}",
                    Message = "enable\r",
                    OnSuccess = "AwaitEnablePasswordPrompt",
                    OnFailure = "CriticalError"
                },
                new SendToDevice
                {
                    Name = "SendUsername",
                    Description = "Send username to login",
                    Destination = "{{" + Destination + "}}",
                    Message = "{{Url(Destination).Username}}\r",
                    OnSuccess = "ProcessInitialPrompt",
                    OnFailure = "CriticalError"
                },
                new SendToDevice
                {
                    Name = "SendUserPassword",
                    Description = "Send user password to login",
                    Destination = "{{" + Destination + "}}",
                    Message = "{{Url(Destination).Password}}\r",
                    OnSuccess = "ProcessPostLoginPrompt",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "ProcessPostLoginPrompt",
                    Description = "Wait for prompt after login",
                    Destination = "{{" + Destination + "}}",
                    OnFailure = "CriticalError",
                    Expressions = new List<WaitForExpression>
                    {
                        new WaitForExpression
                        {
                            Expression = @"[Pp]ass[Ww]ord\s*:\s*",
                            Name = "UserPasswordRequest",
                            Description = "Wrong password passed",
                            OnSuccess = "CriticalError"
                        },
                        new WaitForExpression
                        {
                            Expression = @"([Uu]ser[Nn]ame\s*:?\s*)|(login\s+as\s*:\s*)",
                            Name = "UsernameRequest",
                            Description = "Username prompt following failed password",
                            OnSuccess = "CriticalError"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                            Name = "PrivExecPrompt",
                            Description = "Privilege mode prompt received",
                            OnSuccess = "Done"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @">\s*",
                            Name = "UserExecPrompt",
                            Description = "User mode prompt received",
                            OnSuccess = "SendEnable"
                        }
                    }
                },
                new WaitForExpressions
                {
                    Name = "AwaitEnablePasswordPrompt",
                    Description = "Wait for valid prompts following enable command",
                    Destination = "{{" + Destination + "}}",
                    OnFailure = "CriticalError",
                    Expressions = new List<WaitForExpression>
                    {
                        new WaitForExpression
                        {
                            Expression = @"[Pp]ass[Ww]ord\s*:\s*",
                            Name = "EnablePasswordPrompt",
                            Description = "Password prompt for enable password",
                            OnSuccess = "SendEnablePassword"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                            Name = "PrivExecPrompt",
                            Description = "Privilege mode prompt received",
                            OnSuccess = "Done"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @">\s*",
                            Name = "UserExecPrompt",
                            Description = "User mode prompt received meaning enable was rejected",
                            OnSuccess = "CriticalError"
                        }
                    }
                },
                new SendToDevice
                {
                    Name = "SendEnablePassword",
                    Description = "Send enable password to elevate to priv exec",
                    Destination = "{{" + Destination + "}}",
                    Message = EnablePassword + "\r",
                    OnSuccess = "AwaitEnablePasswordResult",
                    OnFailure = "CriticalError"
                },
                new WaitForExpressions
                {
                    Name = "AwaitEnablePasswordResult",
                    Description = "Verify result of sending enable password",
                    Destination = "{{" + Destination + "}}",
                    OnFailure = "CriticalError",
                    Expressions = new List<WaitForExpression>
                    {
                        new WaitForExpression
                        {
                            Expression = @"[Pp]ass[Ww]ord\s*:?\s*",
                            Name = "EnablePasswordPrompt",
                            Description = "Password prompt again if the enable password was not accepted",
                            OnSuccess = "CriticalError"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @"#\s*",
                            Name = "PrivExecPrompt",
                            Description = "Privilege mode prompt received",
                            OnSuccess = "Done"
                        },
                        new WaitForExpression
                        {
                            Expression = @"[\r\n]+" + Constants.Rfc1035Label + @">\s*",
                            Name = "UserExecPrompt",
                            Description = "User mode prompt received meaning enable was rejected",
                            OnSuccess = "CriticalError"
                        }
                    }
                }
            };
        }
    }
}
