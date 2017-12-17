using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

/*
GigabitEthernet0/0/1.15
GigabitEthernet4
FastEthernet0/9
Dot11Radio1
*/

namespace NetPlugAndPlay.PlugAndPlayTools.Cisco
{
    public class InterfaceName
    {
        public string Name { get; set; }
        public List<int> Indices { get; set; }
        public bool HasSubinterface { get; set; }
        public int Subinterface { get; set; }

        private static Regex interfaceNameExpression = new Regex(@"((?<name>[A-Za-z]+[A-Za-z0-9]*[A-Za-z])(((?<indices>[0-9])+/?)+)(\.(?<subinterface>[0-9]+))?)");

        public InterfaceName subsequent(int offset)
        {
            var newIndices = Indices.Select(x => x).ToList();
            newIndices[newIndices.Count() - 1] = newIndices[newIndices.Count() - 1] + offset; 
            return new InterfaceName
            {
                Name = Name,
                Indices = newIndices,
                HasSubinterface = HasSubinterface,
                Subinterface = Subinterface
            };
        }

        public override string ToString()
        {
            return Name + String.Join('/', Indices.Select(x => x.ToString())) + (HasSubinterface ? ('.' + Subinterface.ToString()) : "");
        }

        static public InterfaceName tryParse(string name)
        {
            var m = interfaceNameExpression.Match(name);
            if(m == null)
            {
                return null;
            }

            var result = new InterfaceName
            {
                Name = m.Groups["name"].Value,
                Indices = m.Groups["indices"].Captures.Select(x => Convert.ToInt32(x.Value)).ToList(),
                HasSubinterface = m.Groups["subinterface"].Success,
                Subinterface = m.Groups["subinterface"].Success ? Convert.ToInt32(m.Groups["subinterface"].Value) : 0
            };

            return result;
        }
    }
}
