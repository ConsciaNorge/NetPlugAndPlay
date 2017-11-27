using libterminal.JobRunner.MacroParser;

namespace libterminal.JobRunner.MacroProcessor
{
    public abstract class ProcessMemberObject
    {
        public abstract ProcessMemberObject ProcessMember(PoValue value);
    }
}