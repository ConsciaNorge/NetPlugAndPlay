using libterminal.JobRunner.MacroParser;

namespace libterminal.JobRunner.MacroProcessor
{
    public class ProcessMemberStringObject : ProcessMemberObject
    {
        public string Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override ProcessMemberObject ProcessMember(PoValue value)
        {
            if (value == null)
                return new ProcessMemberStringObject { Value = Value };

            // TODO : Handle function call in this context
            if (value.Member is PoFunctionCall)
                return null;

            // TODO : Handle string properties
            if (value.Member is PoLiteralMember)
                return null;

            // TODO : Handle unknown value type
            return null;
        }
    }
}