using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RecursiveTemplate
{
    public class TemplateStringCollectionResolver
    {
        Regex _matcher;

        readonly int _maximumLevelOfRecursion;

        public TemplateStringCollectionResolver(int maximumLevelOfRecursion = 9)
        {
            _maximumLevelOfRecursion = maximumLevelOfRecursion;

            _matcher = new Regex("{{((.+?)(:.+?)?)}}");
        }

        public Dictionary<string, ResolvedTemplateString> Resolve(Dictionary<string, string> collection)
        {
            var asResolveTemplateStrings = collection.ToDictionary(
                pair => pair.Key,
                pair => new ResolvedTemplateString(pair.Value));

            var keys = collection.Keys.ToArray();

            for (var i = 0; i < collection.Count; i++)
            {
                var key = keys[i];

                var keyChain = new HashSet<TemplateFieldOccurrence>();

                asResolveTemplateStrings[key] = Resolve(
                    key,
                    asResolveTemplateStrings[key], 
                    asResolveTemplateStrings, 
                    keyChain);
            }

            return asResolveTemplateStrings;
        }

        /// <summary>
        /// Replaces template fields in the specified template string, and
        /// updates its metadata, using resolved strings from the specified
        /// sourceCollection, which is itself progressively updated.
        /// </summary>
        private ResolvedTemplateString Resolve(
            string fieldName,
            ResolvedTemplateString templateString, 
            Dictionary<string, ResolvedTemplateString> sourceCollection, 
            HashSet<TemplateFieldOccurrence> occurrenceChain, int level = 0)
        {
            if (level > _maximumLevelOfRecursion)
            {
                templateString.Value = fieldName;
                templateString.ReachedMaximumLevelOfRecursion = true;

                return templateString;
            }

            if (!_matcher.IsMatch(templateString.Value))
            {
                return templateString;
            }

            var evaluator = new MatchEvaluator((match) =>
            {
                var atPosition = match.Index;

                var field = match.Groups[2].Value;
                var modifier = match.Groups[3].Value;

                var occurrence = new TemplateFieldOccurrence(atPosition, field, modifier, fieldName);
                
                if(!occurrenceChain.Add(occurrence))
                {
                    templateString.RecursiveImpasseFieldNames.Add(field);

                    return field;
                }

                ResolvedTemplateString replacement;

                if(sourceCollection.TryGetValue(field, out replacement))
                {
                    replacement = Resolve(field, replacement, sourceCollection, occurrenceChain, level + 1);

                    templateString.ReachedMaximumLevelOfRecursion = 
                        replacement.ReachedMaximumLevelOfRecursion;

                    AddItemsTo(templateString.RecursiveImpasseFieldNames,
                        replacement.RecursiveImpasseFieldNames);

                    AddItemsTo(templateString.UnreplacedFieldNames,
                        replacement.UnreplacedFieldNames);
                    
                    return replacement.Value;
                }

                templateString.UnreplacedFieldNames.Add(field);

                return field;                                                
            });

            templateString.Value = _matcher.Replace(templateString.Value, evaluator);

            return templateString;
        }

        private void AddItemsTo<T>(ICollection<T> target, IEnumerable<T> source)
        {
            foreach(var item in source)
            {
                target.Add(item);
            }
        }
    }
}
