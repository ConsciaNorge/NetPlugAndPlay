using libterminal.JobRunner.MacroParser;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace libterminal.JobRunner.MacroProcessor
{

    public class JobProcessor
    {
        public Job Job { get; set; }

        public JobProcessor(Job job)
        {
            Job = job;
        }

        public string ProcessParts(PoParts parts)
        {
            //System.Diagnostics.Debug.WriteLine(JsonConvert.SerializeObject(parts));

            return string.Join("", parts.Parts.Select(x => ProcessPart(x)));
        }

        public string ProcessPart(PoPart part)
        {
            if (part is PoMacro)
                return ProcessMacro(part as PoMacro);
            if (part is PoText)
                return ProcessText(part as PoText);
            return "<<Error processing part '" + part.Text + "' which is of type " + part.GetType().ToString() + ">>";
        }

        public string ProcessMacro(PoMacro macro)
        {
            return ProcessExpression(macro.Expression);
        }

        public static string ProcessText(PoText text)
        {
            return text.Text;
        }

        public string ProcessExpression(PoExpression expression)
        {
            return ProcessValue(expression.Value);
        }

        public string ProcessValue(PoValue value)
        {
            var memberResult = ProcessMember(value.Member);
            if (memberResult == null)
                return "<<Error processing member [" + value.Member.Text + "]>>";
            if (value.Value != null)
            {
                var processResult = memberResult.ProcessMember(value.Value);
                if (processResult == null)
                    return "<<Error processing property of member [" + value.Member.Text + "] - [" + value.Value.Text + "]>>";

                return processResult.ToString();
            }

            return memberResult.ToString();
        }

        

        public ProcessMemberObject ProcessMember(PoMember member)
        {
            if(member is PoFunctionCall)
                return ProcessFunctionCall(member as PoFunctionCall);
            if (member is PoLiteralMember)
                return ProcessLiteralMember(member as PoLiteralMember);
            return null;
        }

        public ProcessMemberObject ProcessFunctionCall(PoFunctionCall functionCall)
        {
            var expressionResult = string.Empty;
            if (functionCall.Expression != null)
            {
                expressionResult = ProcessExpression(functionCall.Expression);
                // TODO : Error handling for error result
                if (expressionResult.StartsWith("<<"))
                    return null;
            }

            // TODO : Error handling for literal member
            if (functionCall.Literal == null)
                return null;

            // TODO : Change from literal to something else here?
            var literalValue = functionCall.Literal.Text;

            // TODO : Error handling for empty literal result
            if (string.IsNullOrEmpty(literalValue))
                return null;

            // TODO : Use reflection for processing types instead of hard coding a switch statement
            switch(literalValue)
            {
                case "Url":
                    return new ProcessMemberUrlObject
                    {
                        Value = new Uri(expressionResult)
                    };
                case "EscapeRegex":
                    return new ProcessMemberStringObject
                    {
                        Value = Regex.Escape(expressionResult)
                    };
                default:
                    // TODO : Error handling for unknown global member type
                    return null;
            }
        }

        public ProcessMemberObject ProcessLiteralMember(PoLiteralMember literalMember)
        {
            return new ProcessMemberStringObject
            {
                Value = ProcessLiteral(literalMember.Literal)
            };
        }

        public string ProcessLiteral(PoLiteral literal)
        {
            var parameter = Job.Parameters.Where(x => x.Name == literal.Text).FirstOrDefault();
            if (parameter != null)
                return parameter.Value;

            return "<<Error processing literal [" + literal.Text + "]>>";
        }
    }
}