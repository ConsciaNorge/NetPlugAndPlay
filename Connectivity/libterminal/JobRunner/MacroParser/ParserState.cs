using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace libterminal.JobRunner.MacroParser
{
    public class ParserState
    {
        public string Text { get; set; }
        public int Index { get; set; }
        public List<int> Stack { get; set; } = new List<int>();
        public int Push()
        {
            Stack.Add(Index);
            return Index;
        }
        public void Pop()
        {
            if (Stack.Count == 0)
                throw new Exception("There are no items on the stack to pop");

            Index = Stack[Stack.Count - 1];
            Stack.RemoveAt(Stack.Count - 1);
        }
        public void Release(int count = 1)
        {
            if (Stack.Count < count)
                throw new Exception("There are not enough items on the stack to pop");

            while (count > 0)
            {
                Stack.RemoveAt(Stack.Count - 1);
                count--;
            }
        }

        public Match MatchText(string Expression)
        {
            if (Index >= Text.Length)
                return null;
            
            var regex = new Regex(@"\G" + Expression);

            var m = regex.Match(Text, Index);
            if(m != null && m.Success && m.Index == Index)
            {
                Index += m.Length;
                return m;
            }
            return null;
        }
    }
}
