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
        /// <param name="fieldName">
        /// The name of the field being resolved
        /// </param>
        /// <param name="templateString">
        /// The template string to resolve, including metadata
        /// </param>
        /// <param name="occurrenceChain">
        /// The set of previously performed occurrences; attempts to add duplicate
        /// entries will result in a recursive impasse
        /// </param>
        /// <param name="depth">
        /// The recursive depth of the current call; attempts to reach a depth
        /// exceeding the configured maximum level of recursion will result in an
        /// impasse
        /// </param>
        /// </summary>
        private ResolvedTemplateString Resolve(
            string fieldName,
            ResolvedTemplateString templateString, 
            Dictionary<string, ResolvedTemplateString> sourceCollection, 
            HashSet<TemplateFieldOccurrence> occurrenceChain, int depth = 0)
        {
            if (depth > _maximumLevelOfRecursion)
            {
                templateString.Value = fieldName;
                templateString.ReachedMaximumLevelOfRecursion = true;
                templateString.RecursiveImpasseFieldNames.Add(fieldName);

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
                    replacement = Resolve(field, replacement, sourceCollection, occurrenceChain, depth + 1);

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
