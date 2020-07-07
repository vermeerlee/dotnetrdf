﻿using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace VDS.RDF.JsonLd
{
    public class JsonLdTestSuiteBase
    {
        public JsonLdTestSuiteBase()
        {
            // Ensure that we are using modern TLS2 for HTTPS connections (required to access the GitHub-hosted context files)
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public virtual void ExpandTests(string testId, JsonLdTestType testType, string inputPath, string contextPath,
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            var processorOptions = MakeProcessorOptions(inputPath, baseIri, processorMode, expandContextPath,
                compactArrays, rdfDirection);
            var inputJson = File.ReadAllText(inputPath);
            var inputElement = JToken.Parse(inputJson);

            // Expand tests should not have a context parameter
            Assert.Null(contextPath);

            switch (testType)
            {
                case JsonLdTestType.PositiveEvaluationTest:
                    var actualOutputElement = JsonLdProcessor.Expand(inputElement, processorOptions);
                    var expectedOutputJson = File.ReadAllText(expectedOutputPath);
                    var expectedOutputElement = JToken.Parse(expectedOutputJson);
                    Assert.True(DeepEquals(actualOutputElement, expectedOutputElement),
                        $"Error processing expand test {Path.GetFileName(inputPath)}.\nActual output does not match expected output.\nExpected:\n{expectedOutputElement}\n\nActual:\n{actualOutputElement}");
                    break;
                case JsonLdTestType.NegativeEvaluationTest:
                    var exception =
                        Assert.Throws<JsonLdProcessorException>(
                            () => JsonLdProcessor.Expand(inputElement, processorOptions));
                    Assert.Equal(expectErrorCode, exception.ErrorCode);
                    break;
                case JsonLdTestType.PositiveSyntaxTest:
                    // Expect test to run without throwing processing errors
                    var _ = JsonLdProcessor.Expand(inputElement, processorOptions);
                    break;
                case JsonLdTestType.NegativeSyntaxTest:
                    Assert.ThrowsAny<JsonLdProcessorException>(() => JsonLdProcessor.Expand(inputElement, processorOptions));
                    break;
            }
        }

        
        public virtual void CompactTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            var processorOptions = MakeProcessorOptions(inputPath, baseIri, processorMode, expandContextPath,
                compactArrays, rdfDirection);
            var inputJson = File.ReadAllText(inputPath);
            var contextJson = contextPath == null ? null : File.ReadAllText(contextPath);
            var inputElement = JToken.Parse(inputJson);
            var contextElement = contextJson == null ? new JObject() : JToken.Parse(contextJson);
            switch (testType)
            {
                case JsonLdTestType.PositiveEvaluationTest:
                    var expectedOutputJson = File.ReadAllText(expectedOutputPath);
                    var expectedOutputElement = JToken.Parse(expectedOutputJson);
                    var actualOutputElement = JsonLdProcessor.Compact(inputElement, contextElement, processorOptions);
                    Assert.True(DeepEquals(actualOutputElement, expectedOutputElement),
                        $"Error processing compact test {Path.GetFileName(inputPath)}.\nActual output does not match expected output.\nExpected:\n{expectedOutputElement}\n\nActual:\n{actualOutputElement}");
                    break;
                case JsonLdTestType.NegativeEvaluationTest:
                    var exception = Assert.Throws<JsonLdProcessorException>(() =>
                        JsonLdProcessor.Compact(inputElement, contextElement, processorOptions));
                    Assert.Equal(expectedErrorCode, exception.ErrorCode);
                    break;
                default:
                    Assert.True(false, $"Test type {testType} has not been implemented for Compact tests");
                    break;
            }
        }

        public virtual void FlattenTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            var processorOptions = MakeProcessorOptions(inputPath, baseIri, processorMode, expandContextPath,
                compactArrays, rdfDirection);
            var inputJson = File.ReadAllText(inputPath);
            var contextJson = contextPath == null ? null : File.ReadAllText(contextPath);
            var inputElement = JToken.Parse(inputJson);
            var contextElement = contextJson == null ? null : JToken.Parse(contextJson);

            switch (testType)
            {
                case JsonLdTestType.PositiveEvaluationTest:
                    var expectedOutputJson = File.ReadAllText(expectedOutputPath);
                    var expectedOutputElement = JToken.Parse(expectedOutputJson);

                    var actualOutputElement = JsonLdProcessor.Flatten(inputElement, contextElement, processorOptions);
                    Assert.True(DeepEquals(actualOutputElement, expectedOutputElement),
                        $"Error processing flatten test {Path.GetFileName(inputPath)}.\nActual output does not match expected output.\nExpected:\n{expectedOutputElement}\n\nActual:\n{actualOutputElement}");
                    break;
                case JsonLdTestType.NegativeEvaluationTest:
                    var exception = Assert.Throws<JsonLdProcessorException>(() =>
                        JsonLdProcessor.Flatten(inputElement, contextElement, processorOptions));
                    Assert.Equal(expectedErrorCode, exception.ErrorCode);
                    break;
                default:
                    Assert.True(false, $"Test type {testType} has not been implemented for Flatten tests");
                    break;
            }
        }

        public virtual void JsonLdParserTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            var processorOptions = MakeProcessorOptions(inputPath, baseIri, processorMode, expandContextPath,
                compactArrays, rdfDirection);
            var jsonldParser = new JsonLdParser(processorOptions);
            var actualStore = new TripleStore();

            switch (testType)
            {
                case JsonLdTestType.PositiveEvaluationTest:
                    var nqParser = new NQuadsParser(NQuadsSyntax.Rdf11);
                    var expectedStore = new TripleStore();
                    nqParser.Load(expectedStore, expectedOutputPath);
                    FixStringLiterals(expectedStore);
                    jsonldParser.Load(actualStore, inputPath);
                    Assert.True(expectedStore.Graphs.Count.Equals(actualStore.Graphs.Count) ||
                                (expectedStore.Graphs.Count == 0 && actualStore.Graphs.Count == 1 &&
                                 actualStore.Graphs[null].IsEmpty),
                        $"Test failed for input {Path.GetFileName(inputPath)}.\r\nActual graph count {actualStore.Graphs.Count} does not match expected graph count {expectedStore.Graphs.Count}.");
                    AssertStoresEqual(expectedStore, actualStore, Path.GetFileName(inputPath));
                    break;

                case JsonLdTestType.NegativeEvaluationTest:
                    var exception =
                        Assert.Throws<JsonLdProcessorException>(() => jsonldParser.Load(actualStore, inputPath));
                    Assert.Equal(expectedErrorCode, exception.ErrorCode);
                    break;

                case JsonLdTestType.PositiveSyntaxTest:
                    // Positive syntax test should load input file without raising any exceptions
                    jsonldParser.Load(actualStore, inputPath);
                    break;

                default:
                    Assert.True(false, $"Test type {testType} is not currently supported for the JSON-LD Parser tests");
                    break;
            }
        }

        private void AssertStoresEqual(ITripleStore expectedTripleStore, ITripleStore actualTripleStore, string testFile)
        {
            foreach (var graphUri in expectedTripleStore.Graphs.GraphUris)
            {
                if (graphUri == null)
                {
                    Assert.True(actualTripleStore.HasGraph(null), 
                        $"Test failed for input {testFile}.\r\nExpected a default graph to be present.");
                    AssertGraphsEqual(expectedTripleStore[null], actualTripleStore[null],
                        expectedTripleStore, actualTripleStore, testFile);
                } 
                else if (graphUri.ToString().StartsWith("nquads:bnode:"))
                {
                    Uri matchedGraph = null;
                    foreach (var actualGraphUri in actualTripleStore.Graphs.GraphUris.Where(u =>
                        u != null && u.ToString().StartsWith("nquads:bnode:")))
                    {
                        var expectedGraph = expectedTripleStore[graphUri];
                        var actualGraph = actualTripleStore[actualGraphUri];
                        if (actualGraph.Equals(expectedGraph, out _))
                        {
                            matchedGraph = actualGraphUri;
                            break;
                        }
                    }

                    if (matchedGraph == null)
                    {
                        var expectedLines = MakeNQuadsList(expectedTripleStore);
                        var actualLines = MakeNQuadsList(actualTripleStore);
                        Assert.True(false,
                            $"Test failed for input {testFile}.\r\nFailed to find a match for graph {graphUri}.\r\nExpected:\r\n{expectedLines}\r\nActual:\r\n{actualLines}");
                    }
                    else
                    {
                        actualTripleStore.Graphs.Remove(matchedGraph);
                    }
                }
                else
                {
                    if (!actualTripleStore.Graphs.Contains(graphUri))
                    {
                        var expectedLines = MakeNQuadsList(expectedTripleStore);
                        var actualLines = MakeNQuadsList(actualTripleStore);
                        Assert.True(false,
                            $"Test failed for input {testFile}.\r\nFailed to find a match for graph {graphUri}.\r\nExpected:\r\n{expectedLines}\r\nActual:\r\n{actualLines}");
                    }
                    AssertGraphsEqual(expectedTripleStore[graphUri], actualTripleStore[graphUri],
                        expectedTripleStore, actualTripleStore, testFile);
                }
            }
        }

        private void AssertGraphsEqual(IGraph expectedGraph, IGraph actualGraph, ITripleStore expectedStore, ITripleStore actualStore, string testFile)
        {
            var graphsEqual = actualGraph.Equals(expectedGraph, out _);
            if (!graphsEqual)
            {
                var expectedLines = MakeNQuadsList(expectedStore);
                var actualLines = MakeNQuadsList(actualStore);
                Assert.True(graphsEqual,
                    $"Test failed for input {testFile}.\r\nGraph {expectedGraph.BaseUri} differs in actual output from expected output.\r\nExpected:\r\n{expectedLines}\r\nActual:\r\n{actualLines}");
            }
        }
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters
        public virtual void JsonLdWriterTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, bool useNativeTypes, bool useRdfType, bool ordered, string rdfDirection)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
        {
            var nqParser = new NQuadsParser(NQuadsSyntax.Rdf11);
            var input = new TripleStore();
            nqParser.Load(input, inputPath);
            FixStringLiterals(input);
            var writerOptions = new JsonLdWriterOptions
                {UseNativeTypes = useNativeTypes, UseRdfType = useRdfType, Ordered = ordered};
            if (rdfDirection != null)
            {
                switch (rdfDirection)
                {
                    case "i18n-datatype":
                        writerOptions.RdfDirection = JsonLdRdfDirectionMode.I18NDatatype;
                        break;
                    case "compound-literal":
                        writerOptions.RdfDirection = JsonLdRdfDirectionMode.CompoundLiteral;
                        break;
                    default:
                        throw new Exception($"Test {testId} specifies an unrecognized value for the rdfDirection option: {rdfDirection}.");
                }
            }
            var jsonLdWriter = new JsonLdWriter(writerOptions);

            switch (testType)
            {
                case JsonLdTestType.PositiveEvaluationTest:
                {
                    var actualOutput = jsonLdWriter.SerializeStore(input);
                    var expectedOutputJson = File.ReadAllText(expectedOutputPath);
                    var expectedOutput = JToken.Parse(expectedOutputJson);

                    try
                    {
                        Assert.True((bool) DeepEquals(expectedOutput, actualOutput, true, true),
                            $"Test failed for input {Path.GetFileName(inputPath)}\nExpected:\n{expectedOutput}\nActual:\n{actualOutput}");
                    }
                    catch (DeepEqualityFailure ex)
                    {
                        Assert.True(false,
                            $"Test failed for input {Path.GetFileName(inputPath)}\nExpected:\n{expectedOutput}\nActual:\n{actualOutput}\nMatch Failured: {ex}");
                    }

                    break;
                }
                case JsonLdTestType.NegativeEvaluationTest:
                {
                    var exception = Assert.Throws<JsonLdProcessorException>(() => jsonLdWriter.SerializeStore(input));
                    Assert.Equal(expectErrorCode, exception.ErrorCode);
                    break;
                }
                case JsonLdTestType.PositiveSyntaxTest:
                    var _ = jsonLdWriter.SerializeStore(input);
                    break;
                case JsonLdTestType.NegativeSyntaxTest:
                    Assert.ThrowsAny<JsonLdProcessorException>(() => jsonLdWriter.SerializeStore(input));
                    break;
            }
        }

        public virtual void JsonLdFramingTests(string testId, JsonLdTestType testType, string inputPath, string framePath, 
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, bool pruneBlankNodeIdentifiers)
        {
            var inputJson = File.ReadAllText(inputPath);
            var frameJson = File.ReadAllText(framePath);
            var options = new JsonLdProcessorOptions {PruneBlankNodeIdentifiers = pruneBlankNodeIdentifiers};
            var inputElement = JToken.Parse(inputJson);
            var frameElement = JToken.Parse(frameJson);
            /*
            switch (testType)
            {
                case JsonLdTestType.PositiveEvaluationTest:
                    var expectedOutputJson = File.ReadAllText(expectedOutputPath);
                    var expectedOutputElement = JToken.Parse(expectedOutputJson);
                    var actualOutput = JsonLdProcessor.Frame(inputElement, frameElement, options);
                    Assert.True(JToken.DeepEquals(expectedOutputElement, actualOutput),
                        $"Test failed for input {Path.GetFileName(inputPath)}\nExpected:\n{expectedOutputElement}\nActual:\n{actualOutput}");
                    break;
                case JsonLdTestType.NegativeEvaluationTest:
                    var exception = Assert.ThrowsAny<JsonLdProcessorException>(() =>
                        JsonLdProcessor.Frame(inputElement, frameElement, options));
                    Assert.Equal(expectErrorCode, exception.ErrorCode);
                    break;
                case JsonLdTestType.PositiveSyntaxTest:
                    JsonLdProcessor.Frame(inputElement, frameElement, options);
                    break;
                case JsonLdTestType.NegativeSyntaxTest:
                    Assert.ThrowsAny<JsonLdProcessorException>(() =>
                        JsonLdProcessor.Frame(inputElement, frameElement, options));
                    break;
            }
            */
        }

        private static bool DeepEquals(JToken t1, JToken t2, bool arraysAreOrdered = false)
        {
            if (t1 == null) return t2 == null;
            if (t2 == null) return false;
            if (t1.Type == JTokenType.Null && t2.Type == JTokenType.Null) return true;
            if (t1.Type == JTokenType.Null && t2.Type == JTokenType.String) return t2.Value<string>() == null;
            if (t2.Type == JTokenType.Null && t1.Type == JTokenType.String) return t1.Value<string>() == null;
            if (t1.Type != t2.Type) return false;
            switch (t1.Type)
            {
                case JTokenType.Array:
                    return DeepEquals(t1 as JArray, t2 as JArray, arraysAreOrdered);
                case JTokenType.Object:
                    return DeepEquals(t1 as JObject, t2 as JObject, arraysAreOrdered);
                default:
                    return t1.Equals(t2);
            }
        }

        private static bool DeepEquals(JArray a1, JArray a2, bool arraysAreOrdered = false)
        {
            if (a1.Count != a2.Count) return false;
            if (arraysAreOrdered)
            {
                return !a1.Where((t, i) => !DeepEquals(t, a2[i], arraysAreOrdered)).Any();
            }
            var a2Clone = new JArray(a2);
            foreach (var item in a1)
            {
                var matched = false;
                for (var j = 0; j < a2Clone.Count; j++)
                {
                    if (DeepEquals(item, a2Clone[j], arraysAreOrdered))
                    {
                        a2Clone.RemoveAt(j);
                        matched = true;
                        break;
                    }
                }
                if (!matched) return false;
            }
            return true;
        }

        private static bool DeepEquals(JObject o1, JObject o2, bool arraysAreOrdered = false)
        {
            if (o1.Count != o2.Count) return false;
            return o1.Properties().All(p => o2.ContainsKey(p.Name)) &&
                   o1.Properties().All(p => DeepEquals(p.Value, o2[p.Name], arraysAreOrdered));
        }

        private static string MakeNQuadsList(ITripleStore store)
        {
            var ser = new NQuadsWriter();
            using (var expectedTextWriter = new System.IO.StringWriter())
            {
                ser.Save(store, expectedTextWriter);
                var lines = expectedTextWriter.ToString().Split('\n').Select(x => x.Trim()).ToList();
                lines.Sort();
                return String.Join(Environment.NewLine, lines);
            }
        }

        private static bool DeepEquals(JToken token1, JToken token2, bool ignoreArrayOrder, bool throwOnMismatch)
        {
            if (token1.Type != token2.Type)
            {
                if (throwOnMismatch) throw new DeepEqualityFailure(token1, token2);
                return false;
            }
            switch (token1.Type)
            {
                case JTokenType.Object:
                    var o1 = token1 as JObject;
                    var o2 = token2 as JObject;
                    foreach (var p in o1.Properties())
                    {
                        if (o2[p.Name] == null)
                        {
                            if (throwOnMismatch) throw new DeepEqualityFailure(token1, token2);
                            return false;
                        }
                        if (!DeepEquals(o1[p.Name], o2[p.Name], ignoreArrayOrder, throwOnMismatch))
                        {
                            if (throwOnMismatch) throw new DeepEqualityFailure(o1[p.Name], o2[p.Name]);
                            return false;
                        }
                    }
                    if (o2.Properties().Any(p2 => o1.Property(p2.Name) == null))
                    {
                        if (throwOnMismatch) throw new DeepEqualityFailure(token1, token2);
                        return false;
                    }
                    return true;
                case JTokenType.Array:
                    var a1 = token1 as JArray;
                    var a2 = token2 as JArray;
                    if (a1.Count != a2.Count)
                    {
                        if (throwOnMismatch) throw new DeepEqualityFailure(token1, token2);
                        return false;
                    }

                    if (!ignoreArrayOrder)
                    {
                        for (var i = 0; i < a1.Count; i++)
                        {
                            if (!DeepEquals(a1[i], a2[i], ignoreArrayOrder, throwOnMismatch))
                            {
                                if (throwOnMismatch) throw new DeepEqualityFailure(token1, token2);
                                return false;
                            }

                        }
                        return true;
                    }
                    var unmatchedItems = (token2 as JArray).ToList();
                    foreach (var item1 in a1)
                    {
                        if (unmatchedItems.Count == 0)
                        {
                            if (throwOnMismatch) throw new DeepEqualityFailure(token1, token2);
                            return false;
                        }
                        var match = unmatchedItems.FindIndex(x => DeepEquals(item1, x, ignoreArrayOrder, false));
                        if (match >= 0)
                        {
                            unmatchedItems.RemoveAt(match);
                        }
                        else
                        {
                            if (throwOnMismatch) throw new DeepEqualityFailure(token1, token2);
                            return false;
                        }
                    }
                    return unmatchedItems.Count == 0;
                default:
                    return JToken.DeepEquals(token1, token2);
            }
        }

        private class DeepEqualityFailure : Exception
        {
            public DeepEqualityFailure(JToken expected, JToken actual) : base(
                $"DeepEquality failed at {expected.Path}.\nExpected: {expected}\nActual: {actual}")
            {
                
            }
        }

        private static void FixStringLiterals(TripleStore store)
        {
            var xsdString = new Uri("http://www.w3.org/2001/XMLSchema#string");
            foreach (var t in store.Triples.ToList())
            {
                var literalNode = t.Object as ILiteralNode;
                if (literalNode != null && String.IsNullOrEmpty(literalNode.Language) && literalNode.DataType == null)
                {
                    var graphToUpdate = t.Graph;
                    graphToUpdate.Retract(t);
                    graphToUpdate.Assert(
                        new Triple(t.Subject, t.Predicate,
                            graphToUpdate.CreateLiteralNode(literalNode.Value, xsdString),
                            graphToUpdate.BaseUri));
                }
            }
        }

        private static JsonLdProcessorOptions MakeProcessorOptions(string inputPath, 
            string baseIri,
            string processorMode,
            string expandContextPath, 
            bool compactArrays,
            string rdfDirection)
        {
            var processorOptions = new JsonLdProcessorOptions
            {
                Base = baseIri != null
                    ? new Uri(baseIri)
                    : new Uri("http://json-ld.org/test-suite/tests/" + Path.GetFileName(inputPath))
            };
            if (processorMode != null)
                processorOptions.ProcessingMode = processorMode.Equals("json-ld-1.1")
                    ? JsonLdProcessingMode.JsonLd11
                    : JsonLdProcessingMode.JsonLd10;
            if (expandContextPath != null)
            {
                var expandContextJson = File.ReadAllText(expandContextPath);
                processorOptions.ExpandContext = JObject.Parse(expandContextJson);
            }
            processorOptions.CompactArrays = compactArrays;
            if (!string.IsNullOrEmpty(rdfDirection))
            {
                switch (rdfDirection)
                {
                    case "i18n-datatype":
                        processorOptions.RdfDirection = JsonLdRdfDirectionMode.I18NDatatype;
                        break;
                    case "compound-literal":
                        processorOptions.RdfDirection = JsonLdRdfDirectionMode.CompoundLiteral;
                        break;
                    default:
                        throw new Exception($"Unexpected value for rdfDirection option in test {inputPath}");
                }
            }
            return processorOptions;
        }
    }

    public class JsonLdOrgTestSuite : JsonLdTestSuiteBase
    {

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.ExpandTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        //[MemberData(nameof(JsonLdTestSuiteDataSource.W3CExpandTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void ExpandTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, string expectedOutputPath, JsonLdErrorCode expectErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            base.ExpandTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectErrorCode, baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.CompactTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void CompactTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            base.CompactTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectedErrorCode,
                baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.FlattenTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void FlattenTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            base.FlattenTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectedErrorCode,
                baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.ToRdfTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void JsonLdParserTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            base.JsonLdParserTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectedErrorCode,
                baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.FromRdfTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void JsonLdWriterTests(string testId, JsonLdTestType testType, string inputPath, string contextPath, 
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, bool useNativeTypes, bool useRdfType, bool ordered, string rdfDirection)
        {
            base.JsonLdWriterTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectErrorCode,
                useNativeTypes, useRdfType, ordered, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.FrameTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void JsonLdFramingTests(string testId, JsonLdTestType testType, string inputPath, string framePath,
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, bool pruneBlankNodeIdentifiers)
        {
            base.JsonLdFramingTests(testId, testType, inputPath, framePath, expectedOutputPath, expectErrorCode, pruneBlankNodeIdentifiers);
        }
    }


    public class W3CJsonLd11TestSuite : JsonLdTestSuiteBase
    {

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.W3CExpandTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void ExpandTests(string testId, JsonLdTestType testType, string inputPath, string contextPath,
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            base.ExpandTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectErrorCode,
                baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.W3CCompactTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void CompactTests(string testId, JsonLdTestType testType, string inputPath, string contextPath,
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri,
            string processorMode, string expandContextPath, bool compactArrays, string rdfDirection)
        {
            base.CompactTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectedErrorCode,
                baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }


        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.W3CFlattenTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void FlattenTests(string testId, JsonLdTestType testType, string inputPath, string contextPath,
            string expectedOutputPath,
            JsonLdErrorCode expectedErrorCode, string baseIri, string processorMode, string expandContextPath,
            bool compactArrays, string rdfDirection)
        {
            base.FlattenTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectedErrorCode,
                baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }

        private readonly Dictionary<string, string> SkippedParserTests = new Dictionary<string, string>
        {
            {"#t0120", "Test fails due to .NET URI parsing library"},
            {"#t0121", "Test fails due to .NET URI parsing library"},
            {"#t0122", "Test fails due to .NET URI parsing library"},
            {"#t0123", "Test fails due to .NET URI parsing library"},
            {"#t0124", "Test fails due to .NET URI parsing library"},
            {"#t0125", "Test fails due to .NET URI parsing library"},
            {"#t0126", "Test fails due to .NET URI parsing library"},
            {"#te075", "Test uses a blank-node property"},
            {"#tjs12", "Test depends on decimal representation of a float"},
        };

        [SkippableTheory(typeof(SkipException))]
        //[Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.W3CToRdfTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void JsonLdParserTests(string testId, JsonLdTestType testType, string inputPath, string contextPath,
            string expectedOutputPath, JsonLdErrorCode expectedErrorCode, string baseIri, string processorMode,
            string expandContextPath, bool compactArrays, string rdfDirection)
        {
            if (SkippedParserTests.ContainsKey(testId)) throw new SkipException(SkippedParserTests[testId]);
            base.JsonLdParserTests(testId, testType, inputPath, contextPath, expectedOutputPath,
                expectedErrorCode, baseIri, processorMode, expandContextPath, compactArrays, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.W3CFromRdfTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void JsonLdWriterTests(string testId, JsonLdTestType testType, string inputPath, string contextPath,
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, bool useNativeTypes, bool useRdfType, bool ordered, string rdfDirection)
        {
            base.JsonLdWriterTests(testId, testType, inputPath, contextPath, expectedOutputPath, expectErrorCode, useNativeTypes, useRdfType, ordered, rdfDirection);
        }

        [Theory]
        [MemberData(nameof(JsonLdTestSuiteDataSource.W3CFrameTests), MemberType = typeof(JsonLdTestSuiteDataSource))]
        public override void JsonLdFramingTests(string testId, JsonLdTestType testType, string inputPath, string framePath,
            string expectedOutputPath, JsonLdErrorCode expectErrorCode, bool pruneBlankNodeIdentifiers)
        {
            base.JsonLdFramingTests(testId, testType, inputPath, framePath, expectedOutputPath, expectErrorCode, pruneBlankNodeIdentifiers);
        }
    }

    public enum JsonLdTestType
    {
        PositiveEvaluationTest,
        NegativeEvaluationTest,
        PositiveSyntaxTest,
        NegativeSyntaxTest
    }

    public static class JsonLdTestSuiteDataSource
    {
        public static IEnumerable<object[]> ExpandTests => ProcessManifest(Path.Combine("resources","jsonld"), "expand-manifest.jsonld");

        public static IEnumerable<object[]> CompactTests => ProcessManifest(Path.Combine("resources", "jsonld"), "compact-manifest.jsonld");

        public static IEnumerable<object[]> FlattenTests => ProcessManifest(Path.Combine("resources", "jsonld"), "flatten-manifest.jsonld");

        public static IEnumerable<object[]> ToRdfTests => ProcessManifest(Path.Combine("resources", "jsonld"), "toRdf-manifest.jsonld", "toRdf-skip.txt");

        public static IEnumerable<object[]> FromRdfTests => ProcessFromRdfManifest(Path.Combine("resources", "jsonld"), "fromRdf-manifest.jsonld");

        public static IEnumerable<object[]> FrameTests => ProcessFrameManifest(Path.Combine("resources", "jsonld"), "frame-manifest.jsonld");

        public static IEnumerable<object[]> W3CExpandTests =>
            ProcessManifest(Path.Combine("resources", "jsonld11"), "expand-manifest.jsonld");

        public static IEnumerable<object[]> W3CCompactTests =>
            ProcessManifest(Path.Combine("resources", "jsonld11"), "compact-manifest.jsonld");

        public static IEnumerable<object[]> W3CFlattenTests =>
            ProcessManifest(Path.Combine("resources", "jsonld11"), "flatten-manifest.jsonld");

        public static IEnumerable<object[]> W3CToRdfTests =>
            ProcessManifest(Path.Combine("resources", "jsonld11"), "toRdf-manifest.jsonld");

        public static IEnumerable<object[]> W3CFromRdfTests =>
            ProcessFromRdfManifest(Path.Combine("resources", "jsonld11"), "fromRdf-manifest.jsonld");

        public static IEnumerable<object[]> W3CFrameTests =>
            ProcessFrameManifest(Path.Combine("resources", "jsonld-framing11"), "frame-manifest.jsonld");


        private static IEnumerable<object[]> ProcessManifest(string resourceDirPath, string manifestPath, string skipTestsPath = null)
        {
            var resourceDir = new DirectoryInfo(resourceDirPath);
            manifestPath = Path.Combine(resourceDir.FullName, manifestPath);
            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JObject.Parse(manifestJson);
            var skipTests = new List<string>();
            if (skipTestsPath != null)
            {
                skipTestsPath = Path.Combine(resourceDir.FullName, skipTestsPath);
                skipTests = File.ReadAllLines(skipTestsPath).ToList();
            }

            var testSuiteBaseIri = manifest.Property("baseIri").Value.Value<string>();
            var sequence = manifest.Property("sequence").Value as JArray;
            foreach (var testConfiguration in sequence.OfType<JObject>())
            {
                var testId = testConfiguration.Property("@id").Value.Value<string>();
                var testType = GetTestType(testConfiguration);
                // For now ignore type as everything in this manifest is a positive test
                var input = testConfiguration.Property("input").Value.Value<string>();
                var defaultBaseIri = testSuiteBaseIri + input;
                if (skipTests.Contains(input)) continue;
                var context = testConfiguration.Property("context")?.Value.Value<string>();

                var expect = testType == JsonLdTestType.PositiveEvaluationTest
                    ? testConfiguration.Property("expect").Value.Value<string>()
                    : null;
                var expectErrorCodeString = testType == JsonLdTestType.NegativeEvaluationTest
                    ? testConfiguration.Property("expectErrorCode").Value.Value<string>()
                    : null;
                var expectErrorCode = GetErrorCode(expectErrorCodeString);
                var optionsProperty = testConfiguration.Property("option");
                string baseIri = defaultBaseIri,
                    processorMode = null,
                    expandContext = null,
                    specVersion = null,
                    rdfDirection = null;
                var compactArrays = true;
                var options = optionsProperty?.Value as JObject;
                if (options != null)
                {
                    foreach (var p in options.Properties())
                    {
                        switch (p.Name)
                        {
                            case "base":
                                baseIri = p.Value.Value<string>();
                                break;
                            case "processingMode":
                                processorMode = p.Value.Value<string>();
                                break;
                            case "expandContext":
                                expandContext = Path.Combine(resourceDir.FullName, p.Value.Value<string>());
                                break;
                            case "compactArrays":
                                compactArrays = p.Value.Value<bool>();
                                break;
                            case "rdfDirection":
                                rdfDirection = p.Value.Value<string>();
                                break;
                            case "specVersion":
                                specVersion = p.Value.Value<string>();
                                break;
                        }
                    }
                }
                if ("json-ld-1.0".Equals(specVersion)) continue; // Tests that are specifically against the 1.0 version of the spec will not work with a 1.1 processor
                yield return new object[] {
                    testId,
                    testType,
                    Path.Combine(resourceDir.FullName, input),
                    context == null ? null : Path.Combine(resourceDir.FullName, context),
                    expect == null ? null : Path.Combine(resourceDir.FullName, expect),
                    expectErrorCode,
                    baseIri,
                    processorMode,
                    expandContext,
                    compactArrays,
                    rdfDirection
                };
            }
        }

        private static JsonLdTestType GetTestType(JObject testConfiguration)
        {
            var types = testConfiguration.Property("@type")?.Value.Values<string>().ToArray();
            if (types == null) throw new ArgumentException("No @types property on test configuration");
            if (types.Contains("jld:PositiveEvaluationTest")) return JsonLdTestType.PositiveEvaluationTest;
            if (types.Contains("jld:NegativeEvaluationTest")) return JsonLdTestType.NegativeEvaluationTest;
            if (types.Contains("jld:PositiveSyntaxTest")) return JsonLdTestType.PositiveSyntaxTest;
            if (types.Contains("jld:NegativeSyntaxTest")) return JsonLdTestType.NegativeSyntaxTest;
            throw new ArgumentException($"Unable to determine test type from @type={types}");
        }

        private static JsonLdErrorCode? GetErrorCode(string errorCodeString)
        {
            if (string.IsNullOrEmpty(errorCodeString)) return null;
            var errorCodeName = string.Join("", errorCodeString.Split(new [] {' ', '@', '-'}, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Capitalize()));
            if (Enum.IsDefined(typeof(JsonLdErrorCode), errorCodeName))
            {
                return (JsonLdErrorCode)Enum.Parse(typeof(JsonLdErrorCode), errorCodeName);
            }

            throw new ArgumentException($"Unable to process string '{errorCodeString}' as a JsonLdErrorCode enum value",
                nameof(errorCodeString));
        }

        public static string Capitalize(this string s)
        {
            return s.Substring(0, 1).ToUpper() + s.Substring(1).ToLower();
        }

        private static IEnumerable<object[]> ProcessFromRdfManifest(string resourceDirPath, string manifestPath, string skipTestsPath = null)
        {
            var resourceDir = new DirectoryInfo(resourceDirPath);
            manifestPath = Path.Combine(resourceDir.FullName, manifestPath);
            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JObject.Parse(manifestJson);
            var skipTests = ReadSkipTests(skipTestsPath, resourceDir);
            var sequence = manifest.Property("sequence").Value as JArray;
            foreach (var testConfiguration in sequence.OfType<JObject>())
            {
                var testId = testConfiguration.Property("@id").Value.Value<string>();
                var testType = GetTestType(testConfiguration);
                var input = testConfiguration.Property("input").Value.Value<string>();
                if (skipTests.Contains(input)) continue;
                var context = testConfiguration.Property("context")?.Value.Value<string>();
                var expect = testType == JsonLdTestType.PositiveEvaluationTest
                    ? testConfiguration.Property("expect").Value.Value<string>()
                    : null;
                var expectErrorCode = testType == JsonLdTestType.NegativeEvaluationTest
                    ? GetErrorCode(testConfiguration.Property("expectErrorCode").Value.Value<string>())
                    : null;
                var optionsProperty = testConfiguration.Property("option");
                bool useNativeTypes = false, useRdfType = false, ordered=false;
                string specVersion = null, rdfDirection = null;
                var options = optionsProperty?.Value as JObject;
                if (options != null)
                {
                    foreach (var p in options.Properties())
                    {
                        switch (p.Name)
                        {
                            case "useNativeTypes":
                                useNativeTypes = p.Value.Value<bool>();
                                break;
                            case "useRdfType":
                                useRdfType = p.Value.Value<bool>();
                                break;
                            case "rdfDirection":
                                rdfDirection = p.Value.Value<string>();
                                break;
                            case "specVersion":
                                specVersion = p.Value.Value<string>();
                                break;
                            case "ordered":
                                ordered = p.Value.Value<bool>();
                                break;
                        }
                    }
                }
                if ("json-ld-1.0".Equals(specVersion)) continue; // Tests that are specifically against the 1.0 version of the spec will not work with a 1.1 processor
                yield return new object[] {
                    testId,
                    testType,
                    Path.Combine(resourceDir.FullName, input),
                    context == null ? null : Path.Combine(resourceDir.FullName, context),
                    expect == null ? null : Path.Combine(resourceDir.FullName, expect),
                    expectErrorCode,
                    useNativeTypes,
                    useRdfType,
                    ordered,
                    rdfDirection
                };
            }
        }

        private static List<string> ReadSkipTests(string skipTestsPath, DirectoryInfo resourceDir)
        {
            var skipTests = new List<string>();
            if (skipTestsPath != null)
            {
                skipTestsPath = Path.Combine(resourceDir.FullName, skipTestsPath);
                skipTests = File.ReadAllLines(skipTestsPath).ToList();
            }
            return skipTests;
        }

        private static IEnumerable<object[]> ProcessFrameManifest(string resourceDirPath, string manifestPath, string skipTestsPath = null)
        {
            var resourceDir = new DirectoryInfo(resourceDirPath);
            var skipTests = ReadSkipTests(skipTestsPath, resourceDir);
            manifestPath = Path.Combine(resourceDir.FullName, manifestPath);
            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JObject.Parse(manifestJson);
            var sequence = manifest.Property("sequence").Value as JArray;
            foreach (var testConfiguration in sequence.OfType<JObject>())
            {
                var testId = testConfiguration.Property("@id").Value.Value<string>();
                var testType = GetTestType(testConfiguration);
                var input = testConfiguration.Property("input").Value.Value<string>();
                if (skipTests.Contains(input)) continue;
                var frame = testConfiguration.Property("frame")?.Value.Value<string>();
                var expect = testType == JsonLdTestType.PositiveEvaluationTest
                    ? testConfiguration.Property("expect").Value.Value<string>()
                    : null;
                var expectErrorCode = testType == JsonLdTestType.NegativeEvaluationTest
                    ? testConfiguration.Property("expectErrorCode").Value.Value<string>()
                    : null;
                var optionsProperty = testConfiguration.Property("option");
                var pruneBlankNodeIdentifiers = false;
                var options = optionsProperty?.Value as JObject;
                if (options != null)
                {
                    foreach (var p in options.Properties())
                    {
                        switch (p.Name)
                        {
                            case "pruneBlankNodeIdentifiers":
                                pruneBlankNodeIdentifiers = p.Value.Value<bool>();
                                break;
                        }
                    }
                }
                yield return new object[] {
                    testId,
                    testType,
                    Path.Combine(resourceDir.FullName, input),
                    Path.Combine(resourceDir.FullName, frame),
                    expect == null ? null : Path.Combine(resourceDir.FullName, expect),
                    expectErrorCode,
                    pruneBlankNodeIdentifiers
                };
            }
        }
    }
}
