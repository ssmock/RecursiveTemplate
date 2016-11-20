using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RecursiveTemplate.Tests
{
    // TODO: Our recursive block will cause problems when the same field shows
    //       up more than once with different modifiers.  We need some tests 
    //       and some different logic for this.

    public class TemplateStringCollectionResolverTests
    {
        TemplateStringCollectionResolver _resolver;

        public TemplateStringCollectionResolverTests()
        {
            _resolver = new TemplateStringCollectionResolver();
        }

        [Fact]
        public void ResolvesRecursively()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{c}}",
                ["b"] = "hello {{a}}",
                ["c"] = "world",
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("world", resolved["a"].Value);
            Assert.Equal("hello world", resolved["b"].Value);
            Assert.Equal("world", resolved["c"].Value);
        }

        [Fact]
        public void ResolvesRecursivelyWithModifiers()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{c:exp1}}",
                ["b"] = "hello {{a:exp2}}",
                ["c"] = "world",
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("world", resolved["a"].Value);
            Assert.Equal("hello world", resolved["b"].Value);
            Assert.Equal("world", resolved["c"].Value);
        }

        [Fact]
        public void ResolvesUnmatchedFieldsUsingKeysThemselves()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{x}}",
                ["b"] = "hello {{a}}"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("x", resolved["a"].Value);
            Assert.Equal("hello x", resolved["b"].Value);
        }

        [Fact]
        public void ResolvesUnmatchedFieldsWithModifiersUsingKeysThemselves()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{x:exp1:exp2}}",
                ["b"] = "hello {{a}}"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("x", resolved["a"].Value);
            Assert.Equal("hello x", resolved["b"].Value);
        }

        [Fact]
        public void ResolvesLongChain()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{b}}",
                ["b"] = "{{c}}",
                ["c"] = "{{d}}",
                ["d"] = "{{e}}",
                ["e"] = "{{f}}",
                ["f"] = "{{g}}",
                ["g"] = "{{h}}",
                ["h"] = "yep",
            };

            var resolved = _resolver.Resolve(collection);

            Assert.All(resolved.Values, (b) => Assert.Equal("yep", b.Value));
        }

        [Fact]
        public void ReachesDefaultMaximumLevelOfRecursionWithVeryLongChain()
        {
            var collection = Enumerable.Range(0, 10 + 1)
                .Select(i =>
                    new KeyValuePair<string, string>(
                        $"{(char)('A' + i)}",
                        $"{{{{{(char)('A' + i + 1)}}}}}"))
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value);

            var resolved = _resolver.Resolve(collection);

            Assert.All(resolved, (pair) =>
            {
                Assert.True(pair.Value.ReachedMaximumLevelOfRecursion);
                Assert.Equal("K", pair.Value.Value);
            });
        }

        [Fact]
        public void ResolvesDirectSelfReferenceUsingTheyKeyItself()
        {
            var collection = new Dictionary<string, string>
            {
                ["itself"] = "{{itself}}"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("itself", resolved["itself"].Value);
            Assert.Contains(resolved["itself"].RecursiveImpasseFieldNames,
                name => name == "itself");
        }

        [Fact]
        public void ResolvesDirectSelfReferenceWithModifiersUsingTheyKeyItself()
        {
            var collection = new Dictionary<string, string>
            {
                ["itself"] = "{{itself:exp1:exp2}}"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("itself", resolved["itself"].Value);
            Assert.Contains(resolved["itself"].RecursiveImpasseFieldNames,
                name => name == "itself");
        }

        [Fact]
        public void ResolvesIndirectSelfReferenceUsingTheKeyItself()
        {
            var collection = new Dictionary<string, string>
            {
                ["me"] = "{{myself}}",
                ["myself"] = "{{i}}",
                ["i"] = "{{me}}"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.All(resolved, (pair) =>
            {
                Assert.Contains(pair.Value.RecursiveImpasseFieldNames, 
                    name => name == "myself");
            });
        }

        [Fact]
        public void ResolvesIndirectSelfReferenceWithModifiersUsingTheKeyItself()
        {
            var collection = new Dictionary<string, string>
            {
                ["me"] = "{{myself:exp1:exp2}}",
                ["myself"] = "{{i:exp1}}",
                ["i"] = "{{me:exp3}}"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.All(resolved, (pair) =>
            {
                Assert.Contains(pair.Value.RecursiveImpasseFieldNames,
                    name => name == "myself");
            });
        }

        [Fact]
        public void ResolvesReferencedSelfReferenceUsingTheKeyItself()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{b}}",
                ["b"] = "{{c}}",
                ["c"] = "{{d}}",
                ["d"] = "{{c}}"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.All(resolved, (pair) =>
            {
                Assert.Contains(pair.Value.RecursiveImpasseFieldNames,
                    name => name == "d");
            });
        }

        [Fact]
        public void ResolvesComplexExpressions()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "foo",
                ["b"] = "bar",
                ["c"] = "baz",
                ["d"] = "{{f}}: {{a}} {{b}}",
                ["e"] = "{{c}} ... {{d}}",
                ["f"] = "okay"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("okay: foo bar", resolved["d"].Value);
            Assert.Equal("baz ... okay: foo bar", resolved["e"].Value);
        }

        [Fact]
        public void ResolvesSimpleDoubleOccurenceOfField()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{b}} {{b}}",
                ["b"] = "foo"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("foo foo", resolved["a"].Value);
        }

        [Fact]
        public void ResolvesDoubleOccurrenceOfFieldWithModifiers()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{b:mod1}} {{b:mod2}}",
                ["b"] = "foo"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("foo foo", resolved["a"].Value);
        }

        [Fact]
        public void ResolvesChainedDoubleOccurrenceOfField()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{b}} {{d}}",
                ["b"] = "{{c}} {{d}}",
                ["c"] = "foo",
                ["d"] = "bar"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("foo bar bar", resolved["a"].Value);
            Assert.Equal("foo bar", resolved["b"].Value);
        }

        [Fact]
        public void ResolvesChainedDoubleOccurrenceOfFieldWithModifiers()
        {
            var collection = new Dictionary<string, string>
            {
                ["a"] = "{{b:mod1}} {{d:mod2}}",
                ["b"] = "{{c:mod1}} {{d:mod3}}",
                ["c"] = "foo",
                ["d"] = "bar"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("foo bar bar", resolved["a"].Value);
            Assert.Equal("foo bar", resolved["b"].Value);
        }

        [Fact]
        public void ResolvesPartialFailuresUsingTheKeysThemselves()
        {
            var collection = new Dictionary<string, string>
            {
                ["fail"] = "{{fail}}",
                ["no key"] = "{{no such thing}}",
                ["with fail"] = "{{fail}} ({{truth}})",
                ["with no key"] = "{{truth}} -- there's {{no key}}",
                ["truth"] = "it's true"
            };

            var resolved = _resolver.Resolve(collection);

            Assert.Equal("fail", resolved["fail"].Value);
            Assert.Contains(resolved["fail"].RecursiveImpasseFieldNames, 
                name => name == "fail");

            Assert.Equal("no such thing", resolved["no key"].Value);
            Assert.Contains(resolved["no key"].UnreplacedFieldNames,
                name => name == "no such thing");

            Assert.Equal("fail (it's true)", resolved["with fail"].Value);
            Assert.Contains(resolved["with fail"].RecursiveImpasseFieldNames,
                name => name == "fail");
            
            Assert.Equal("it's true -- there's no such thing", 
                resolved["with no key"].Value);
            Assert.Contains(resolved["with no key"].UnreplacedFieldNames,
                name => name == "no such thing");
        }
    }
}
