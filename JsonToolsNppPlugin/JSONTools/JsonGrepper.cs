using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Windows.Forms;
using JSON_Tools.Utils;
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
        public bool api_requests_async;

		public JsonGrepper(JsonParser json_parser = null, int max_threads = 4, bool api_requests_async = true)
		{
            this.max_threads = max_threads;
            this.api_requests_async = api_requests_async;
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
            webClient.Headers.Clear();
            // add a header saying that this client accepts only JSON
            webClient.Headers.Add("Accept", "application/json");
            // add a user-agent header saying who you are
            webClient.Headers.Add("User-Agent", "JsonTools Notepad++ plugin");
            // see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#request_context
            // for more info on HTTP request headers
            webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(
                DownloadOneJsonString
            );
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
		private static void ParseJsonStrings_Task(string[] assigned_fnames, 
                                                  Dictionary<string, string> fname_strings, 
                                                  Dictionary<string, JNode> fname_jsons,
                                                  JsonParser json_parser)
        {
			foreach (string fname in assigned_fnames)
            {
				string json_str = fname_strings[fname];
                try
                {
                    string fname_redone = new JNode(fname, Dtype.STR, 0).ToString().Slice("1:-1");
					fname_jsons[fname_redone] = json_parser.Parse(json_str);
                }
                catch { } // just ignore badly formatted files
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
			int start = 0;
			int fnames_per_thread = fnames.Length / max_threads;
			if (fnames_per_thread == 0) 
                fnames_per_thread = 1;
            for (int ii = 0; ii < max_threads; ii++)
            {
                int end = start + fnames_per_thread;
                if (end > fnames.Length // give the final thread fewer files than the rest
                    || ii == max_threads - 1) // give the final thread all the remaining files
                {
                    end = fnames.Length;
                }
				if (start == end) break;
                int count = end - start;
				var assigned_fnames = new string[count];
				for (int jj = 0; jj < count; jj++)
                    assigned_fnames[jj] = fnames[start + jj];
                // JsonParsers store position in the parsed string (ii) and line_num as instance variables.
                // that means that if multiple threads share a JsonParser, you have race conditions associated with ii and line_num.
                // For this reason, we need to give each thread a separate JsonParser.
                Thread thread = new Thread(() => ParseJsonStrings_Task(assigned_fnames, fname_strings, fname_jsons.children, json_parser.Copy()));
				threads.Add(thread);
				thread.Start();
				start = end;
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
        /// Downloads JSON from APIs. If api_requests_async, does this asynchronously.<br></br>
        /// Otherwise, does it synchronously.
        /// </summary>
        /// <param name="urls"></param>
        public void GetJsonFromApis(string[] urls)
        {
            if (api_requests_async)
            {
                GetJsonFromApisAsync(urls);
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
                    exceptions[url] = new JNode(ex.ToString(), Dtype.STR, 0);
                }
            }
            ParseJsonStringsThreaded();
        }

        /// <summary>
        /// Asynchronously send API requests to several URLs.
        /// For each URL where the request succeeds, try to parse the JSON returned.
        /// If the JSON returned is valid, add the JSON to fname_jsons.
        /// Populate a HashSet of all URLs for which the request succeeded
        /// and a dict mapping urls to exception strings
        /// </summary>
        /// <param name="urls"></param>
        /// <returns></returns>
        private void GetJsonFromApisAsync(string[] urls)
        {
            Task[] tasks = new Task[urls.Length];
            for (int ii = 0; ii < urls.Length; ii++)
            {
                string url = urls[ii];
                // if (!fname_jsons.children.ContainsKey(url))
                // could stop if already downloaded, but it is probably better to allow duplication of labor, 
                // so that the user can get new JSON if something changed
                tasks[ii] = new Task(() => webClient.DownloadStringAsync(new Uri(url), url));
                tasks[ii].Start();
                // the second arg to DownloadStringAsync is a unique identifier
                // of the asynchronous task
            }
            // wait for all those downloads to complete
            Task.WaitAll(tasks);
            // now parse all the JSON strings that were downloaded
            ParseJsonStringsThreaded();
        }

        private void DownloadOneJsonString(object sender, DownloadStringCompletedEventArgs e)
        {
            string src_url = (string)e.UserState;
            if (e.Error != null)
            {
                exceptions[src_url] = new JNode(e.Error.ToString(), Dtype.STR, 0);
                return;
            }
            fname_strings[src_url] = e.Result;
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
	}
}
