using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecursiveTemplate
{
    public class ResolvedTemplateString
    {
        public string Value { get; set; }        
        public HashSet<string> UnreplacedFieldNames { get; private set; }
        public HashSet<string> RecursiveImpasseFieldNames { get; private set; }
        public bool ReachedMaximumLevelOfRecursion { get; set; }

        public ResolvedTemplateString(string value)
        {
            Value = value;
            UnreplacedFieldNames = new HashSet<string>();
            RecursiveImpasseFieldNames = new HashSet<string>();
        }
    }
}
