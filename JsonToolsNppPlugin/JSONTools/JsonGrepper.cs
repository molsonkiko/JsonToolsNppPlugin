using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using JSON_Tools.Utils;
using System.Text;
using System.Windows.Forms;

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
		public JsonParser json_parser;
		public Dictionary<string, JsonLint[]> fname_lints;

		public JsonGrepper(JsonParser json_parser = null)
		{
			fname_jsons = new JObject();
			if (json_parser == null)
            {
				this.json_parser = new JsonParser(true, true, true, true, true, true);
			}
            else
            {
				this.json_parser = json_parser;
            }
			fname_lints = new Dictionary<string, JsonLint[]>();
		}

		/// <summary>
		/// Finds files that match the search_pattern (typically just ".json" files) in the directory root_dir
		/// and creates a dictionary mapping each found filename to that file's text.
		/// If recursive is true, this will recursively search all subdirectories for JSON files, not just the root.
		/// </summary>
		/// <param name="root_dir"></param>
		/// <param name="recursive"></param>
		/// <param name="search_pattern"></param>
		private void ReadJsonFiles(string root_dir, bool recursive, string search_pattern, Dictionary<string, string> fname_strs)
		{
			if (fname_strs == null)
				fname_strs = new Dictionary<string, string>();
            DirectoryInfo dir_info = new DirectoryInfo(root_dir);
			// this could throw a DirectoryNotFoundException; maybe I should do some error handling?
			foreach (FileInfo file_info in dir_info.EnumerateFiles(search_pattern))
			{
				string fname = file_info.FullName;
                fname_strs[fname] = file_info.OpenText().ReadToEnd();
			}
			if (recursive)
            {
				// recursively search subdirectories for files that match the search pattern
				foreach (DirectoryInfo subdir_info in dir_info.EnumerateDirectories())
                {
					ReadJsonFiles(subdir_info.FullName, recursive, search_pattern, fname_strs);
                }
            }
		}

		/// <summary>
		/// the task of a single thread in ParseJsonStringsThreaded:<br></br>
		/// Loop through a subset of filenames/urls and tries to parse the JSON associated with each filename.
		/// </summary>
		/// <param name="fname_strs"></param>
		/// <param name="results"></param>
		private static void ParseJsonStrings_Task(string[] assigned_fnames, 
                                                  Dictionary<string, string> fname_strs, 
                                                  Dictionary<string, JNode> fname_jsons,
                                                  JsonParser json_parser)
        {
			foreach (string fname in assigned_fnames)
            {
				string json_str = fname_strs[fname];
                try
                {
					fname_jsons[fname] = json_parser.Parse(json_str);
                }
                catch { } // just ignore badly formatted files
            }
        }

		/// <summary>
		/// Takes a map of filenames/urls to strings,
		/// and attempts to parse each string and add the fname-JNode pair to fname_jsons.<br></br>
		/// Divides up the filenames between at most max_threads threads.
		/// </summary>
		/// <param name="fname_strs"></param>
		/// <param name="max_threads"></param>
		/// <returns></returns>
		private void ParseJsonStringsThreaded(Dictionary<string, string> fname_strs, int max_threads)
        {
			List<Thread> threads = new List<Thread>();
			string[] fnames = fname_strs.Keys.ToArray();
			Array.Sort(fnames);
			int start = 0;
			int fnames_per_thread = fnames.Length / max_threads;
			if (fnames_per_thread == 0) 
                fnames_per_thread = 1;
            for (int ii = 0; ii < max_threads; ii++)
            {
                int end = start + fnames_per_thread;
                if (end > fnames.Length // give the final thread fewer files than the rest
                    || (end < fnames.Length && ii == max_threads - 1)) // give the final thread all the remaining files
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
                Thread thread = new Thread(() => ParseJsonStrings_Task(assigned_fnames, fname_strs, fname_jsons.children, json_parser.Copy()));
				threads.Add(thread);
				thread.Start();
				start = end;
            }
			foreach (Thread thread in threads)
				thread.Join();
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
        public void Grep(string root_dir, bool recursive = false, string search_pattern = "*.json", int max_threads = 4)
        {
            Dictionary<string, string> fname_strs = new Dictionary<string, string>();
            ReadJsonFiles(root_dir, recursive, search_pattern, fname_strs);
            ParseJsonStringsThreaded(fname_strs, max_threads);
        }

        /// <summary>
        /// Send an asynchronous request for JSON to an API.
        /// If the request succeeds, return the JSON string.
        /// If the request raises an exception, return the error message.
        /// See https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/console-webapiclient
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        //private async Task<string> GetJsonStringFromOneUrl(string url)
        //{
        //    return "";
            //	// below is an example of how to set headers
            //	// see https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers#request_context
            //	client.DefaultRequestHeaders.Accept.Clear();
            //	// the below Accept header is necessary to specify what kind of media response you will accept 
            //          client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //	// the User-Agent header tells the API who you are
            //	client.DefaultRequestHeaders.Add("User-Agent", "JSON viewer API request tool");
            //	try
            //          {
            //		Task<string> stringTask = client.GetStringAsync(url);
            //		string json_str = await stringTask;
            //		return json_str;
            //	}
            //	catch (Exception ex)
            //          {
            //		return ex.ToString();
            //          }
        //}

        ///// <summary>
        ///// Asynchronously send API requests to several URLs.
        ///// For each URL where the request succeeds, try to parse the JSON returned.
        ///// If the JSON returned is valid, add the JSON to fname_jsons.
        ///// Return a list of all URLs for which the request succeeded,
        ///// a dict mapping urls to exception strings,
        ///// and a dict mapping urls to JsonLint arrays (lists of syntax errors in JSON)
        ///// </summary>
        ///// <param name="urls"></param>
        ///// <returns></returns>
        //private async void GetJsonStringsFromUrls(string[] urls, 
        //                                        HashSet<string> urls_requested,
        //                                        Dictionary<string, string> exceptions)
        //{
        //    var json_tasks = new Dictionary<string, Task<string>>();
        //    string[] json_strs = new string[urls.Length];
        //    foreach (string url in urls)
        //    {
        //        // if (!fname_jsons.children.ContainsKey(url))
        //        //// it is probably better to allow duplication of labor, so that the user can get new JSON
        //        //// if something changed
        //        json_tasks[url] = GetJsonStringFromApiAsync(url);
        //    }
            //	// keep track of which urls were actually requested, excluding the ones where the request failed
            //	var urls_requested = new HashSet<string>();
            //	var exceptions = new Dictionary<string, string>();
            //	for (int ii = 0; ii < urls.Length; ii++)
            //          {
            //		string url = urls[ii];
            //		string json = jsons[ii];
            //		try
            //              {
            //			fname_jsons[url] = json_parser.Parse(json);
            //			if (json_parser.lint != null && json_parser.lint.Count > 0)
            //                  {
            //				fname_lints[url] = json_parser.lint.LazySlice(":").ToArray();
            //			}
            //			urls_requested.Add(url);
            //		}
            //              catch (Exception ex)
            //              {
            //			// GetJsonStringFromApiAsync returns the exception message if the request failed.
            //			exceptions[url] = json;
            //              }
            //	}
            //	return (urls_requested, exceptions);
        //}

        ///<summary>
        /// sends a GET request to each url in urls, assuming that the
        /// response will be a JSON string.<br></br>
        /// If the API responds with valid JSON, map the url to the JNode produced.
        ///</summary>
        public void GetJsonFromApis(string[] urls)
        {

        }

        /// <summary>
        /// clear the map from filenames to JSON objects, and get rid of any lint
        /// </summary>
        public void Reset()
        {
			fname_jsons.children.Clear();
			fname_lints.Clear();
        }
	}
}
