using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;

namespace JSON_Tools.Tests
{
    /// <summary>
    /// contains benchmarking tests for RemesPath, JsonParser, and JsonSchemaValidator
    /// </summary>
    public class Benchmarker
    {
        /// <summary>
        /// Repeatedly parse the JSON of a large file (big_random.json, about 1MB, containing nested arrays, dicts,
        /// with ints, floats and strings as scalars)<br></br>
        /// Also repeatedly run Remespath queries on the JSON.<br></br>
        /// queriesAndDescriptions is an array of 2-string arrays (query, description)<br></br>
        /// For the most recent benchmarking results, see "most recent errors.txt" in the main repository.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="numParseTrials"></param>
        public static bool BenchmarkParserAndRemesPath(string[][] queriesAndDescriptions, 
                                     string fname,
                                     int numParseTrials,
                                     int numQueryTrials)
        {
            // setup
            JsonParser jsonParser = new JsonParser();
            Stopwatch watch = new Stopwatch();
            if (!File.Exists(fname))
            {
                Npp.AddLine($"Can't run benchmark tests because file {fname}\ndoes not exist");
                return true;
            }
            string jsonstr = File.ReadAllText(fname);
            if (!BenchmarkParser(jsonstr, numParseTrials, jsonParser, watch, out JNode json))
                return true;
            // time query compiling
            RemesParser parser = new RemesParser();
            foreach (string[] queryAndDesc in queriesAndDescriptions)
            {
                string query = queryAndDesc[0];
                string description = queryAndDesc[1];
                Npp.AddLine($@"=========================
Performance tests for RemesPath ({description})
=========================
");
                List<object> tokens = null;
                watch.Reset();
                watch.Start();
                try
                {
                    tokens = parser.lexer.Tokenize(query);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Couldn't run RemesPath benchmarks because of error while lexing query:\n{ex}");
                    return true;
                }
                watch.Stop();
                double lexTimeMS = ConvertTicks(watch.Elapsed.Ticks);
                JNode queryFunc = null;
                watch.Reset();
                watch.Start();
                try
                {
                    queryFunc = parser.Compile(query);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Couldn't run RemesPath benchmarks because of error while compiling query:\n{ex}");
                    return true;
                }
                watch.Stop();
                double compileTimeMS = ConvertTicks(watch.Elapsed.Ticks);
                Npp.AddLine($"Compiling query \"{query}\" took {compileTimeMS} ms the first time, including approximately {lexTimeMS} ms to tokenize the query. Subsequent executions are effectively free due to caching.");
                // time query execution
                long[] queryTimes = new long[numQueryTrials];
                int testEqualityInterval = numQueryTrials / 6;
                if (testEqualityInterval < 1)
                    testEqualityInterval = 1;
                JNode result = new JNode();
                JNode oldResult = null;
                for (int ii = 0; ii < numQueryTrials; ii++)
                {
                    // if query mutates input, need to copy json for every iteration to ensure consistency
                    JNode operand = queryFunc.IsMutator ? json.Copy() : json;
                    watch.Reset();
                    watch.Start();
                    try
                    {
                        if (queryFunc.CanOperate)
                        {
                            result = queryFunc.Operate(operand);
                            if (oldResult is null)
                                oldResult = result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Npp.AddLine($"Couldn't run RemesPath benchmarks because of error while executing compiled query:\n{ex}");
                    }
                    watch.Stop();
                    long t = watch.Elapsed.Ticks;
                    queryTimes[ii] = t;
                    if (ii % testEqualityInterval == 0)
                    {
                        if (!result.TryEquals(oldResult, out _))
                        {
                            Npp.AddLine($"Expected running query {query} on the same JSON to always return the same thing, but it didn't");
                            return true;
                        }
                    }
                }
                // display querying results
                (double mean, double sd) = GetMeanAndSd(queryTimes);
                Npp.AddLine($"To run pre-compiled query \"{query}\" on JNode from JSON of size {jsonstr.Length} into took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} ms over {numQueryTrials} trials");
                var queryTimesStr = new string[queryTimes.Length];
                for (int ii = 0; ii < queryTimes.Length; ii++)
                {
                    queryTimesStr[ii] = Math.Round(queryTimes[ii] / 1e4, 3).ToString(JNode.DOT_DECIMAL_SEP);
                }
                Npp.AddLine($"Query times (ms): {String.Join(", ", queryTimesStr)}");
                string resultPreview = result.ToString().Slice(":300") + "\n...";
                Npp.AddLine($"Preview of result: {resultPreview}");
            }
            return false;
        }

        //public static bool BenchmarkParsingAndLintingJsonWithErrors(int numTrials)
        //{
        //    JsonParser jsonParser = new JsonParser();
        //    Stopwatch watch = new Stopwatch();
        //    // this is just the text of testfiles/small/bad_json.json, but with some closing brackets and a comma at the end, so we can chain them together to make an array
        //    string singleBadJson = "{ // this file has ALL the bad things\r\n\\u0348z草 false, # yes, all of them\r\n\"b\": None /* so much bad */\r\n'9' :False, \r\n\"😀\"\r\n      { \r\n      \"u\\\r\ny\":\r\n            [\r\n            1\r\n            \"FO\\x4fBAR\r\n   \"         +2,　\r\n            NaN,\r\n            .75 \r\n-0xF            +0xabcde\r\n0xABCDE\r\n            , // missing ']' here\r\n      \"y\r\nu\":\r\n            [\r\n                  [\r\n                  -.5,\r\n                        {\r\n                        \"y\" : 'b\\9\\x0a':\r\n                        \"m8\": 9.,\r\n                        }\r\n                  ],\r\n            Infinity\r\n            -Infinity\r\n            ],\r\n      $unquồted_‍key\\u00df \"are *very naughty*\"\r\n      _st\\u1eb2tus‌_ｪ : \"jubaЯ\"\r\n      ],\r\n\"9\\\"a\\\"\t\"    \r\n:null,\r\n\"\\u0042lutentharst\":\r\n      [\r\n      \"\\n'\\\"DOOM\\\" BOOM\b, AND I CONSUME', said Bludd, the mighty Blood God.\\n\\t\",\r\n      True,\r\n      undefined\r\n      [ // some unclosed things too\r\n      { /* \r\n      and finally an unclosed multiline comment\r\n      */\r\n      }]]},\r\n";
        //    string jsonstr = "[" + ArgFunction.StrMulHelper(singleBadJson, 100);
        //    BenchmarkParser(jsonstr, numTrials, jsonParser, watch, out _);
        //    for (int ii = 0; ii < numTrials; ii++)
        //    {

        //    }
        //}

        private static bool BenchmarkParser(string jsonstr, int numTrials, JsonParser jsonParser, Stopwatch watch, out JNode json)
        {
            json = new JNode();
            int len = jsonstr.Length;
            long[] loadTimes = new long[numTrials];
            // benchmark time to load json
            for (int ii = 0; ii < numTrials; ii++)
            {
                watch.Reset();
                watch.Start();
                try
                {
                    json = jsonParser.Parse(jsonstr);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Couldn't run benchmark tests because parsing error occurred:\n{ex}");
                    return false;
                }
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                loadTimes[ii] = t;
            }
            // display loading results
            string jsonPreview = json.ToString().Slice(":300") + "\n...";
            Npp.AddLine($"Preview of json: {jsonPreview}");
            (double mean, double sd) = GetMeanAndSd(loadTimes);
            Npp.AddLine($"To convert JSON string of size {len} into JNode took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} " +
                $"ms over {loadTimes.Length} trials");
            var loadTimesStr = new string[loadTimes.Length];
            for (int ii = 0; ii < loadTimes.Length; ii++)
            {
                loadTimesStr[ii] = (loadTimes[ii] / 10000).ToString();
            }
            Npp.AddLine($"Load times (ms): {String.Join(", ", loadTimesStr)}");
            return true;
        }

        public static bool BenchmarkJNodeToString(int numTrials, string fname)
        {
            // setup
            JsonParser jsonParser = new JsonParser();
            Stopwatch watch = new Stopwatch();
            if (!File.Exists(fname))
            {
                Npp.AddLine($"Can't run benchmark tests because file {fname}\ndoes not exist");
                return true;
            }
            string jsonstr = File.ReadAllText(fname);
            int len = jsonstr.Length;
            JNode json = new JNode();
            try
            {
                json = jsonParser.Parse(jsonstr);
            }
            catch (Exception ex)
            {
                Npp.AddLine($"Couldn't run benchmark tests because parsing error occurred:\n{ex}");
                return true;
            }
            string jsonPreview = json.ToString().Slice(":300") + "\n...";
            Npp.AddLine($"Preview of json: {jsonPreview}");
            // compression benchmark
            long[] toStringTimes = new long[numTrials];
            for (int ii = 0; ii < numTrials; ii++)
            {
                watch.Reset();
                watch.Start();
                json.ToString(keyValueSep: ":", itemSep: ",");
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                toStringTimes[ii] = t;
            }
            (double mean, double sd) = GetMeanAndSd(toStringTimes);
            Npp.AddLine($"To compress JNode from JSON string of {len} took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} " +
                $"ms over {toStringTimes.Length} trials (minimal whitespace, sortKeys=TRUE)");
            // compression, but with sortKeys=False
            long[] toStringNoSortTimes = new long[numTrials];
            for (int ii = 0; ii < numTrials; ii++)
            {
                watch.Reset();
                watch.Start();
                json.ToString(sortKeys:false, keyValueSep: ":", itemSep: ",");
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                toStringNoSortTimes[ii] = t;
            }
            (mean, sd) = GetMeanAndSd(toStringNoSortTimes);
            Npp.AddLine($"To compress JNode from JSON string of {len} took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} " +
                $"ms over {toStringTimes.Length} trials (minimal whitespace, sortKeys=FALSE)");
            // pretty-print benchmark
            foreach (var style in new[] {PrettyPrintStyle.Google, PrettyPrintStyle.Whitesmith, PrettyPrintStyle.PPrint})
            {
                long[] prettyPrintTimes = new long[numTrials];
                for (int ii = 0; ii < numTrials; ii++)
                {
                    watch.Reset();
                    watch.Start();
                    json.PrettyPrint(style:style);
                    watch.Stop();
                    long t = watch.Elapsed.Ticks;
                    prettyPrintTimes[ii] = t;
                }
                // display loading results
                (mean, sd) = GetMeanAndSd(prettyPrintTimes);
                Npp.AddLine($"To {style}-style pretty-print JNode from JSON string of {len} took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} " +
                    $"ms over {prettyPrintTimes.Length} trials (sortKeys=true, indent=4)");
            }
            return false;
        }

        public static bool BenchmarkRandomJsonAndSchemaValidation(int numTrials)
        {
            var parser = new JsonParser();
            var tweetSchema = (JObject)parser.Parse(File.ReadAllText(Path.Combine(Npp.pluginDllDirectory, "testfiles", "tweet_schema.json")));
            // restrict to exactly 15 tweets for consistency and 3 items per other array for consistency 
            int numTweets = 15;
            tweetSchema["minItems"] = new JNode((long)numTweets, Dtype.INT, 0);
            tweetSchema["maxItems"] = new JNode((long)numTweets, Dtype.INT, 0);
            long[] makeRandomTimes = new long[numTrials];
            long[] validateTimes = new long[numTrials];
            long[] compileTimes = new long[numTrials];
            JNode randomTweets = new JNode();
            var watch = new Stopwatch();
            for (int ii = 0; ii < numTrials; ii++)
            {
                watch.Reset();
                watch.Start();
                try
                {
                    randomTweets = RandomJsonFromSchema.RandomJson(tweetSchema, 3, 3, true);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"While trying to create random tweets from tweet schema, got exception:\r\n{ex}");
                    break;
                }
                watch.Stop();
                makeRandomTimes[ii] = watch.ElapsedTicks;
                watch.Reset();
                watch.Start();
                // validation will succeed because the tweets are generated from that schema
                JsonSchemaValidator.ValidationFunc validator;
                try
                {
                    validator = JsonSchemaValidator.CompileValidationFunc(tweetSchema, 0, true);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"While trying to compile tweet schema to validation function, got exception:\r\n{ex}");
                    break;
                }
                watch.Stop();
                compileTimes[ii] = watch.ElapsedTicks;
                watch.Reset();
                watch.Start();
                try
                {
                    bool validates = validator(randomTweets, out List<JsonLint> lints);
                    if (!validates)
                    {
                        Npp.AddLine("BAD! JsonSchemaValidator found that tweets made from the tweet schema did not adhere to the tweet schema. Got validation problem\r\n"
                            + JsonSchemaValidator.LintsAsJArrayString(lints));
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"While trying to validate random tweets under tweet schema, got exception:\r\n{ex}");
                    break;
                }
                watch.Stop();
                validateTimes[ii] = watch.ElapsedTicks;
            }
            var len = randomTweets.ToString().Length;
            (double mean, double sd) = GetMeanAndSd(makeRandomTimes);
            Npp.AddLine($"To create a random set of tweet JSON of size {len} ({numTweets} tweets) based on the matching schema took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} ms over {numTrials} trials");
            (mean, sd) = GetMeanAndSd(compileTimes);
            Npp.AddLine($"To compile the tweet schema to a validation function took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} ms over {numTrials} trials");
            (mean, sd) = GetMeanAndSd(validateTimes);
            Npp.AddLine($"To validate tweet JSON of size {len} ({numTweets} tweets) based on the compiled schema took {ConvertTicks(mean)} +/- {ConvertTicks(sd)} ms over {numTrials} trials");
            return false;
        }


        public static (double mean, double sd) GetMeanAndSd(long[] times)
        {
            double mean = 0;
            foreach (long t in times) { mean += t; }
            mean /= times.Length;
            double sd = 0;
            foreach (long t in times)
            {
                double diff = t - mean;
                sd += diff * diff;
            }
            sd = Math.Sqrt(sd / times.Length);
            return (mean, sd);
        }

        public static double ConvertTicks(double ticks, string newUnit = "ms", int sigfigs = 3)
        {
            switch (newUnit)
            {
                case "ms": return Math.Round(ticks / 1e4, sigfigs);
                case "s": return Math.Round(ticks / 1e7, sigfigs);
                case "ns": return Math.Round(ticks * 100, sigfigs);
                case "mus": return Math.Round(ticks / 10, sigfigs);
                default: throw new ArgumentException("Time unit must be s, mus, ms, or ns");
            }
        }
    }
}
