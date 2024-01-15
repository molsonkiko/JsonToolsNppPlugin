using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
	/// <summary>
	/// Reads JSON files based on search patterns, and also fetches JSON from APIS.
	/// Combines all JSON into a map, fnameJsons, from filenames/urls to JSON.
	/// </summary>
	public class JsonGrepper
	{
		/// <summary>
		/// maps filenames and urls to parsed JSON
		/// </summary>
		public JObject fnameJsons;
        public JObject exceptions;
        public Dictionary<string, string> fnameStrings;
		public JsonParser jsonParser;
        private static readonly HttpClient httpClient = new HttpClient();
        public int maxThreadsParsing;

		public JsonGrepper(JsonParser jsonParser = null, 
            int maxThreads = 4)
		{
            this.maxThreadsParsing = maxThreads;
            fnameStrings = new Dictionary<string, string>();
			fnameJsons = new JObject();
            exceptions = new JObject();
			if (jsonParser == null)
            {
				this.jsonParser = new JsonParser(LoggerLevel.JSON5, true);
			}
            else
            {
				this.jsonParser = jsonParser;
            }
            this.jsonParser.throwIfFatal = true;
            this.jsonParser.throwIfLogged = true;
            // security protocol addresses issue: https://learn.microsoft.com/en-us/answers/questions/173758/the-request-was-aborted-could-not-create-ssltls-se.html
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Finds files that match the searchPattern (typically just ".json" files) in the directory rootDir
        /// and creates a dictionary mapping each found filename to that file's text.
        /// If recursive is true, this will recursively search all subdirectories for JSON files, not just the root.
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="recursive"></param>
        /// <param name="searchPattern"></param>
        private void ReadJsonFiles(string rootDir, bool recursive, string searchPattern)
		{
            DirectoryInfo dirInfo;
            try
            {
                dirInfo = new DirectoryInfo(rootDir);
            }
            catch { return; }
			foreach (FileInfo fileInfo in dirInfo.EnumerateFiles(searchPattern))
			{
				string fname = fileInfo.FullName;
                using (var fp = fileInfo.OpenText())
                {
                    fnameStrings[fname] = fp.ReadToEnd();
                }
			}
			if (recursive)
            {
				// recursively search subdirectories for files that match the search pattern
				foreach (DirectoryInfo subdirInfo in dirInfo.EnumerateDirectories())
                {
					ReadJsonFiles(subdirInfo.FullName, recursive, searchPattern);
                }
            }
		}

		/// <summary>
		/// the task of a single thread in ParseJsonStringsThreaded:<br></br>
		/// Loop through a subset of filenames/urls and tries to parse the JSON associated with each filename.
		/// </summary>
		/// <param name="fnameStrings"></param>
		/// <param name="results"></param>
		private static void ParseJsonStrings_Task(object[] assignedFnames, 
                                                  Dictionary<string, string> fnameStrings, 
                                                  Dictionary<string, JNode> fnameJsonMap,
                                                  Dictionary<string, JNode> fnameExceptionMap,
                                                  JsonParser jsonParser)
        {
			foreach (object fnameobj in assignedFnames)
            {
                string fname = (string)fnameobj;
                string jsonStr = fnameStrings[fname];
                // need to make sure the key is formatted properly and doesn't contain any unescaped special chars
                // by default Windows paths have '\\' as path sep so those need to be escaped
                try
                {
                    lock (fnameJsonMap)
                    {
                        fnameJsonMap[fname] = jsonParser.Parse(jsonStr);
                    }
                }
                catch (Exception ex)
                {
                    lock (fnameExceptionMap)
                    {
                        fnameExceptionMap[fname] = new JNode(ex.ToString(), Dtype.STR, 0);
                    }
                }
            }
        }

		/// <summary>
		/// Takes a map of filenames/urls to strings,
		/// and attempts to parse each string and add the fname-JNode pair to fnameJsons.<br></br>
		/// Divides up the filenames between at most maxThreads threads.
		/// </summary>
		private void ParseJsonStringsThreaded()
        {
			List<Thread> threads = new List<Thread>();
			string[] fnames = fnameStrings.Keys.ToArray();
			Array.Sort(fnames);
            foreach (object[] assignedFnames in DivideObjectsBetweenThreads(fnames, maxThreadsParsing))
            {
                // JsonParsers store position in the parsed string (ii) and lineNum as instance variables.
                // that means that if multiple threads share a JsonParser, you have race conditions associated with ii and lineNum.
                // For this reason, we need to give each thread a separate JsonParser.
                Thread thread = new Thread(() => ParseJsonStrings_Task(assignedFnames, fnameStrings, fnameJsons.children, exceptions.children, jsonParser.Copy()));
				threads.Add(thread);
				thread.Start();
            }
			foreach (Thread thread in threads)
				thread.Join();
            fnameStrings.Clear(); // don't need the strings anymore, only the JSON
        }

        /// <summary>
        /// for each file that matches searchPattern in rootDir
        /// (and all subdirectories of rootDir if recursive is true)<br></br>
        /// attempt to parse that file as JSON using this JsonGrepper's JsonParser.<br></br>
        /// For each file that contains valid JSON (according to the parser)
        /// map that filename to the JNode produced by the parser in fnameJsons.
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="recursive"></param>
        /// <param name="searchPattern"></param>
        /// <param name="maxThreads"></param>
        public void Grep(string rootDir, bool recursive = false, string searchPattern = "*.json")
        {
            ReadJsonFiles(rootDir, recursive, searchPattern);
            ParseJsonStringsThreaded();
        }

        /// <summary>
        /// Asynchronously send API requests to several URLs
        /// For each URL where the request succeeds, try to parse the JSON returned.
        /// If the JSON returned is valid, add the JSON to fnameJsons.
        /// Populate a HashSet of all URLs for which the request succeeded
        /// and a dict mapping urls to exception strings
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        public async Task GetJsonFromApis(string[] urls)
        {
            var jsonTasks = new Task[urls.Length];
            for (int ii = 0; ii < urls.Length; ii++)
            {
                // if (!fnameJsons.children.ContainsKey(url))
                // it is probably better to allow duplication of labor,
                // so that the user can get new JSON if something changed
                jsonTasks[ii] = GetJsonStringFromApiAsync(urls[ii]);
            }
            await Task.WhenAll(jsonTasks);
            // now parse all the JSON strings that were downloaded
            ParseJsonStringsThreaded();
        }

        /// <summary>
		/// Send an asynchronous request for JSON to an API.
		/// If the request succeeds, add the JSON string to fnameStrings
		/// If the request raises an exception, add the exception string to exceptions
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public async Task GetJsonStringFromApiAsync(string url)
        {
            InitializeHttpClient(httpClient);
            try
            {
                Task<string> stringTask = httpClient.GetStringAsync(url);
                fnameStrings[url] = await stringTask;
            }
            catch (Exception ex)
            {
                exceptions[url] = new JNode(ex.ToString(), Dtype.STR, 0);
            }
        }

        private static void InitializeHttpClient(HttpClient hc)
        {
            hc.DefaultRequestHeaders.Clear();
            // add a header saying that this client accepts only JSON
            hc.DefaultRequestHeaders.Add("Accept", "application/json");
            // add a user-agent header saying who you are
            hc.DefaultRequestHeaders.Add("User-Agent", $"JsonTools Notepad++ plugin v{Npp.AssemblyVersionString()}");
            // see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#request_context
            // for more info on HTTP request headers
        }

        /// <summary>
        /// clear all exceptions, JSON strings, and JSON
        /// </summary>
        public void Reset()
        {
            fnameStrings.Clear();
			fnameJsons.children.Clear();
            exceptions.children.Clear();
			//fnameLints.Clear();
        }

        /// <summary>
        /// figure out how to divide up taskCount tasks equally among numThreads threads
        /// </summary>
        /// <param name="taskCount"></param>
        /// <param name="numThreads"></param>
        /// <returns></returns>
        private static IEnumerable<int> TaskCountPerThread(int taskCount, int numThreads)
        {
            int start = 0;
            int thingsPerThread = taskCount / numThreads;
            if (thingsPerThread == 0)
                thingsPerThread = 1;
            for (int count = 0; count < numThreads; count++)
            {
                int end = start + thingsPerThread;
                if (end > taskCount // give fewer tasks to the final thread
                    || count == numThreads - 1) // give all the remaining tasks to the final thread
                    end = taskCount;
                if (start == end)
                    break;
                yield return end - start;
                start = end;
            }
        }

        /// <summary>
        /// for each thread that gets any tasks, create a new array containing all the objects
        /// that were assigned to that thread
        /// </summary>
        /// <param name="objs"></param>
        /// <param name="numThreads"></param>
        /// <returns></returns>
        private static IEnumerable<object[]> DivideObjectsBetweenThreads(object[] objs, int numThreads)
        {
            int start = 0;
            foreach (int count in TaskCountPerThread(objs.Length, numThreads))
            {
                object[] objsThisThread = new object[count];
                for (int jj = 0; jj < count; jj++)
                    objsThisThread[jj] = objs[jj + start];
                start += count;
                yield return objsThisThread;
            }
        }
    }
}
