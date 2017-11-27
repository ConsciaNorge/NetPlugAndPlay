using libterminal.JobRunner;
using libterminal.Tasks.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal.Tasks.Cisco.Compound
{
    public class RunExecModeCommand : CompoundTask
    {
        public string Destination { get; set; }
        public string EnablePassword { get; set; }
        public string Command { get; set; }
        public string ResultName { get; set; }
        public RunExecModeCommand(string name, string onSuccess, string destination, string enablePassword, string command, string resultName, string description = "")
        {
            Name = name;
            OnSuccess = onSuccess;
            InitialTask = "NavigateToEnablePrompt";
            Destination = destination;
            Command = command;
            ResultName = resultName;
            EnablePassword = enablePassword;

            if (string.IsNullOrEmpty(description))
                Description = "Run command '{{" + Command + "}}' on {{Url(" + Destination + ").StripUserInfo}} and return result in " + ResultName;
            else
                Description = description;

            Tasks = new List<JobTask>
            {
                new NavigateToEnablePrompt("NavigateToEnablePrompt", "SetTerminal", Destination, enablePassword),
                new SetTerminalLengthAndWidth("SetTerminal", "RunCommand", Destination),
                new RunCommand("RunCommand", "Done", Destination, Command, resultName)
            };
        }
    }
}
