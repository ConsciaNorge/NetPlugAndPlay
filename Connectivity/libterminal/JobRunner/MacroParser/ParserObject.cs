using System;
using System.Collections.Generic;
using System.Text;

namespace libterminal.JobRunner.MacroParser
{
    public class ParserObject
    {
        public ParserState State { get; set; }
        public string Text { get { return State.Text.Substring(Index, Length); } }
        public int Index { get; set; }
        public int Length { get; set; }
    }
}
