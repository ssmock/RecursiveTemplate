using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecursiveTemplate
{
    public struct TemplateFieldOccurrence
    {
        public int AtPosition { get; set; }
        public string Field { get; set; }
        public string Modifier { get; set; }
        public string WithinField { get; set; }

        public TemplateFieldOccurrence(int atPosition, string field, string modifier, string withinField)
        {
            AtPosition = atPosition;
            Field = field;
            Modifier = modifier;
            WithinField = withinField;
        }

        public override int GetHashCode()
        {
            return $"{AtPosition}|{Field}|{WithinField}".GetHashCode();
        }
    }
}
