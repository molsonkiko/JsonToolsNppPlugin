using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JSON_Tools.JSON_Tools;
using JSON_Tools.Utils;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace JSON_Tools.Tests
{
    /// <summary>
    /// contains benchmarking tests for RemesPath and JsonParser
    /// </summary>
    public class Benchmarker
    {
        /// <summary>
        /// Repeatedly parse the JSON of a large file (big_random.json, about 1MB, containing nested arrays, dicts,
        /// with ints, floats and strings as scalars)<br></br>
        /// Also repeatedly run a Remespath query on the JSON.<br></br>
        /// MOST RECENT RESULTS:<br></br>
        /// To convert JSON string of size 975068 into JNode took 185.589 +/- 53.713 ms over 14 trials
        /// Load times(ms): 214,175,222,181,267,248,229,171,175,248,139,121,114,87
        /// Compiling query "@[@[:].z =~ `(?i)[a-z]{5}`]" took 0.056 ms(one-time cost b/c caching)
        /// To run query "@[@[:].z =~ `(?i)[a-z]{5}`]" on JNode from JSON of size 975068 into took 1.854 +/- 3.915 ms over 14 trials
        /// Query times(ms) : 1.718,1.709,1.024,0.92,0.836,0.756,15.882,0.666,0.438,0.385,0.386,0.364,0.41,0.454<br></br>
        /// For reference, the Python standard library JSON parser is about 10x FASTER than JsonParser.Parse,<br></br>
        /// and my Python remespath implementation is 10-30x SLOWER than this remespath implementation.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="num_trials"></param>
        public static void Benchmark(string query, string fname, int num_trials = 8)
        {
            // setup
            JsonParser jsonParser = new JsonParser();
            Stopwatch watch = new Stopwatch();
            if (!File.Exists(fname))
            {
                Npp.AddLine($"Can't run benchmark tests because file {fname}\ndoes not exist");
                return;
            }
            string jsonstr = File.ReadAllText(fname);
            int len = jsonstr.Length;
            long[] load_times = new long[num_trials];
            JNode json = new JNode();
            // benchmark time to load json
            for (int ii = 0; ii < num_trials; ii++)
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
                    return;
                }
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                load_times[ii] = t;
            }
            // display loading results
            string json_preview = json.ToString().Slice(":300") + "\n...";
            Npp.AddLine($"Preview of json: {json_preview}");
            double[] mu_sd = GetMeanAndSd(load_times);
            Npp.AddLine($"To convert JSON string of size {len} into JNode took {ConvertTicks(mu_sd[0])} +/- {ConvertTicks(mu_sd[1])} " +
                $"ms over {load_times.Length} trials");
            var load_times_str = new string[load_times.Length];
            for (int ii = 0; ii < load_times.Length; ii++)
            {
                load_times_str[ii] = (load_times[ii] / 10000).ToString();
            }
            Npp.AddLine($"Load times (ms): {String.Join(", ", load_times_str)}");
            // time query compiling
            RemesParser parser = new RemesParser();
            Func<JNode, JNode> query_func = null;
            long[] compile_times = new long[num_trials];
            for (int ii = 0; ii < num_trials; ii++)
            {
                watch.Reset();
                watch.Start();
                try
                {
                    List<object> toks = parser.lexer.Tokenize(query, out bool _);
                    query_func = ((CurJson)parser.Compile(toks)).function;
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Couldn't run RemesPath benchmarks because of error while compiling query:\n{ex}");
                    return;
                }
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                compile_times[ii] = t;
            }
            mu_sd = GetMeanAndSd(compile_times);
            double mu = mu_sd[0];
            double sd = mu_sd[1];
            Npp.AddLine($"Compiling query \"{query}\" into took {ConvertTicks(mu)} +/- {ConvertTicks(sd)} ms over {load_times.Length} trials");
            // time query execution
            long[] query_times = new long[num_trials];
            JNode result = new JNode();
            for (int ii = 0; ii < num_trials; ii++)
            {
                watch.Reset();
                watch.Start();
                try
                {
                    result = query_func(json);
                }
                catch (Exception ex)
                {
                    Npp.AddLine($"Couldn't run RemesPath benchmarks because of error while executing compiled query:\n{ex}");
                }
                watch.Stop();
                long t = watch.Elapsed.Ticks;
                query_times[ii] = t;
            }
            // display querying results
            mu_sd = GetMeanAndSd(query_times);
            mu = mu_sd[0];
            sd = mu_sd[1];
            Npp.AddLine($"To run pre-compiled query \"{query}\" on JNode from JSON of size {len} into took {ConvertTicks(mu)} +/- {ConvertTicks(sd)} ms over {load_times.Length} trials");
            var query_times_str = new string[query_times.Length];
            for (int ii = 0; ii < query_times.Length; ii++)
            {
                query_times_str[ii] = Math.Round(query_times[ii] / 1e4, 3).ToString(JNode.DOT_DECIMAL_SEP);
            }
            Npp.AddLine($"Query times (ms): {String.Join(", ", query_times_str)}");
            string result_preview = result.ToString().Slice(":300") + "\n...";
            Npp.AddLine($"Preview of result: {result_preview}");
        }

        public static double[] GetMeanAndSd(long[] times)
        {
            double mu = 0;
            foreach (int t in times) { mu += t; }
            mu /= times.Length;
            double sd = 0;
            foreach (int t in times)
            {
                double diff = t - mu;
                sd += diff * diff;
            }
            sd = Math.Sqrt(sd / times.Length);
            return new double[] { mu, sd };
        }

        public static double ConvertTicks(double ticks, string new_unit = "ms", int sigfigs = 3)
        {
            switch (new_unit)
            {
                case "ms": return Math.Round(ticks / 1e4, 3);
                case "s": return Math.Round(ticks / 1e7, 3);
                case "ns": return Math.Round(ticks * 100, 3);
                case "mus": return Math.Round(ticks / 10, 3);
                default: throw new ArgumentException("Time unit must be s, mus, ms, or ns");
            }
        }
    }
}
