using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
/*
using System;
using System.Net;
using System.IO;

public class Test
{
    public static void Main (string[] args)
    {
        if (args == null || args.Length == 0)
        {
            throw new ApplicationException ("Specify the URI of the resource to retrieve.");
        }
        WebClient client = new WebClient ();

        // Add a user agent header in case the
        // requested URI contains a query.

        client.Headers.Add ("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

        Stream data = client.OpenRead (args[0]);
        StreamReader reader = new StreamReader (data);
        string s = reader.ReadToEnd ();
        Console.WriteLine (s);
        data.Close ();
        reader.Close ();
    }
} 
*/

namespace JSON_Tools.JSON_Tools
{
	/// <summary>
	/// Reads JSON files based on search patterns, and also fetches JSON from APIS.
	/// Combines all JSON into a map, fname_jsons, from filenames/urls to JSON.
	/// </summary>
	public class JsonGrepper
	{
		/// <summary>
		/// maps filenames and urls to parsed JSON
		/// </summary>
		public JObject fname_jsons;
        public JObject exceptions;
        public Dictionary<string, string> fname_strings;
		public JsonParser json_parser;
        private static readonly WebClient webClient = new WebClient();
        public int max_threads;
        public int max_api_request_threads;

		public JsonGrepper(JsonParser json_parser = null, int max_threads = 4, int max_api_request_threads = 8)
		{
            this.max_threads = max_threads;
            this.max_api_request_threads = max_api_request_threads;
            fname_strings = new Dictionary<string, string>();
			fname_jsons = new JObject();
            exceptions = new JObject();
			if (json_parser == null)
            {
				this.json_parser = new JsonParser(true, true, true, true, true, true);
			}
            else
            {
				this.json_parser = json_parser;
            }
            InitializeWebClient(webClient);
            // https://learn.microsoft.com/en-us/answers/questions/173758/the-request-was-aborted-could-not-create-ssltls-se.html
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
		}

        private void InitializeWebClient(WebClient wc)
        {
            wc.Headers.Clear();
            // add a header saying that this client accepts only JSON
            wc.Headers.Add("Accept", "application/json");
            // add a user-agent header saying who you are
            wc.Headers.Add("User-Agent", "JsonTools Notepad++ plugin");
            // see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#request_context
            // for more info on HTTP request headers
        }

        /// <summary>
        /// Finds files that match the search_pattern (typically just ".json" files) in the directory root_dir
        /// and creates a dictionary mapping each found filename to that file's text.
        /// If recursive is true, this will recursively search all subdirectories for JSON files, not just the root.
        /// </summary>
        /// <param name="root_dir"></param>
        /// <param name="recursive"></param>
        /// <param name="search_pattern"></param>
        private void ReadJsonFiles(string root_dir, bool recursive, string search_pattern)
		{
            DirectoryInfo dir_info;
            try
            {
                dir_info = new DirectoryInfo(root_dir);
            }
            catch { return; }
			foreach (FileInfo file_info in dir_info.EnumerateFiles(search_pattern))
			{
				string fname = file_info.FullName;
                fname_strings[fname] = file_info.OpenText().ReadToEnd();
			}
			if (recursive)
            {
				// recursively search subdirectories for files that match the search pattern
				foreach (DirectoryInfo subdir_info in dir_info.EnumerateDirectories())
                {
					ReadJsonFiles(subdir_info.FullName, recursive, search_pattern);
                }
            }
		}

		/// <summary>
		/// the task of a single thread in ParseJsonStringsThreaded:<br></br>
		/// Loop through a subset of filenames/urls and tries to parse the JSON associated with each filename.
		/// </summary>
		/// <param name="fname_strings"></param>
		/// <param name="results"></param>
		private static void ParseJsonStrings_Task(object[] assigned_fnames, 
                                                  Dictionary<string, string> fname_strings, 
                                                  Dictionary<string, JNode> fname_json_map,
                                                  Dictionary<string, JNode> fname_exception_map,
                                                  JsonParser json_parser)
        {
			foreach (object fnameobj in assigned_fnames)
            {
                string fname = (string)fnameobj;
				string json_str = fname_strings[fname];
                // need to make sure the key is formatted properly and doesn't contain any unescaped special chars
                // by default Windows paths have '\\' as path sep so those need to be escaped
                try
                {
					fname_json_map[JObject.FormatAsKey(fname)] = json_parser.Parse(json_str);
                }
                catch (Exception ex)
                {
                    fname_exception_map[JObject.FormatAsKey(fname)] = new JNode(ex.ToString(), Dtype.STR, 0);
                }
            }
        }

		/// <summary>
		/// Takes a map of filenames/urls to strings,
		/// and attempts to parse each string and add the fname-JNode pair to fname_jsons.<br></br>
		/// Divides up the filenames between at most max_threads threads.
		/// </summary>
		private void ParseJsonStringsThreaded()
        {
			List<Thread> threads = new List<Thread>();
			string[] fnames = fname_strings.Keys.ToArray();
			Array.Sort(fnames);
            foreach (object[] assigned_fnames in DivideObjectsBetweenThreads(fnames, max_threads))
            {
                // JsonParsers store position in the parsed string (ii) and line_num as instance variables.
                // that means that if multiple threads share a JsonParser, you have race conditions associated with ii and line_num.
                // For this reason, we need to give each thread a separate JsonParser.
                Thread thread = new Thread(() => ParseJsonStrings_Task(assigned_fnames, fname_strings, fname_jsons.children, exceptions.children, json_parser.Copy()));
				threads.Add(thread);
				thread.Start();
            }
			foreach (Thread thread in threads)
				thread.Join();
            fname_strings.Clear(); // don't need the strings anymore, only the JSON
        }

        /// <summary>
        /// for each file that matches search_pattern in root_dir
        /// (and all subdirectories of root_dir if recursive is true)<br></br>
        /// attempt to parse that file as JSON using this JsonGrepper's JsonParser.<br></br>
        /// For each file that contains valid JSON (according to the parser)
        /// map that filename to the JNode produced by the parser in fname_jsons.
        /// </summary>
        /// <param name="root_dir"></param>
        /// <param name="recursive"></param>
        /// <param name="search_pattern"></param>
        /// <param name="max_threads"></param>
        public void Grep(string root_dir, bool recursive = false, string search_pattern = "*.json")
        {
            ReadJsonFiles(root_dir, recursive, search_pattern);
            ParseJsonStringsThreaded();
        }

        /// <summary>
        /// Downloads JSON from APIs. Can use multiple threads.
        /// </summary>
        /// <param name="urls"></param>
        public void GetJsonFromApis(string[] urls)
        {
            if (urls.Length == 0)
                return;
            if (max_api_request_threads > 1)
            {
                GetJsonFromApisMultithreaded(urls);
                return;
            }
            foreach (string url in urls)
            {
                try
                {
                    fname_strings[url] = webClient.DownloadString(new Uri(url));
                }
                catch (Exception ex)
                {
                    exceptions[JObject.FormatAsKey(url)] = new JNode(ex.ToString(), Dtype.STR, 0);
                }
            }
            ParseJsonStringsThreaded();
        }

        /// <summary>
        /// Concurrently send API requests to several URLs (divides them among multiple threads)
        /// For each URL where the request succeeds, try to parse the JSON returned.
        /// If the JSON returned is valid, add the JSON to fname_jsons.
        /// Populate a HashSet of all URLs for which the request succeeded
        /// and a dict mapping urls to exception strings
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        private void GetJsonFromApisMultithreaded(string[] urls)
        {
            List<Thread> threads = new List<Thread>();
            foreach (object[] urls_this_thread in DivideObjectsBetweenThreads(urls, max_api_request_threads))
            {
                // if (!fname_jsons.children.ContainsKey(url))
                // could stop if already downloaded, but it is probably better to allow duplication of labor, 
                // so that the user can get new JSON if something changed
                WebClient newWebClient = new WebClient();
                InitializeWebClient(newWebClient);
                Thread thread = new Thread(
                    () => GetJsonFromApis_Task(
                        urls_this_thread,
                        newWebClient,
                        fname_strings,
                        exceptions)
                );
                threads.Add(thread);
                thread.Start();
            }
            foreach (Thread thread in threads)
                thread.Join();
            // now parse all the JSON strings that were downloaded
            ParseJsonStringsThreaded();
        }

        /// <summary>
        /// the task of a single thread in GetJsonFromApisMultithreaded
        /// </summary>
        /// <param name="urls_this_thread"></param>
        private static void GetJsonFromApis_Task(object[] urls_this_thread, 
                                          WebClient webClient,
                                          Dictionary<string, string> fname_strings,
                                          JObject exceptions)
        {
            foreach (object urlobj in urls_this_thread)
            {
                string url = (string)urlobj;
                try
                {
                    fname_strings[url] = webClient.DownloadString(url);
                }
                catch (Exception ex)
                {
                    exceptions[JObject.FormatAsKey(url)] = new JNode(ex.ToString(), Dtype.STR, 0);
                }
            }
        }

        /// <summary>
        /// clear all exceptions, JSON strings, and JSON
        /// </summary>
        public void Reset()
        {
            fname_strings.Clear();
			fname_jsons.children.Clear();
            exceptions.children.Clear();
			//fname_lints.Clear();
        }


        /// <summary>
        /// figure out how to divide up task_count tasks equally among num_threads threads
        /// </summary>
        /// <param name="task_count"></param>
        /// <param name="num_threads"></param>
        /// <returns></returns>
        private IEnumerable<int> TaskCountPerThread(int task_count, int num_threads)
        {
            int start = 0;
            int things_per_thread = task_count / num_threads;
            if (things_per_thread == 0)
                things_per_thread = 1;
            for (int count = 0; count < num_threads; count++)
            {
                int end = start + things_per_thread;
                if (end > task_count // give fewer tasks to the final thread
                    || count == num_threads - 1) // give all the remaining tasks to the final thread
                    end = task_count;
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
        /// <param name="num_threads"></param>
        /// <returns></returns>
        private IEnumerable<object[]> DivideObjectsBetweenThreads(object[] objs, int num_threads)
        {
            int start = 0;
            foreach (int count in TaskCountPerThread(objs.Length, num_threads))
            {
                object[] objs_this_thread = new object[count];
                for (int jj = 0; jj < count; jj++)
                    objs_this_thread[jj] = objs[jj + start];
                start += count;
                yield return objs_this_thread;
            }
        }
    }
}
