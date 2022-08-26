using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using JSON_Tools.Utils;

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
			fname_jsons = new JObject(0, new Dictionary<string, JNode>());
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
		/// and adds them to this instance's filename-json map (fname_jsons).
		/// If recursive is true, this will recursively search all subdirectories for JSON files, not just the root.
		/// </summary>
		/// <param name="root_dir"></param>
		/// <param name="recursive"></param>
		/// <param name="search_pattern"></param>
		public void ReadJsonFiles(string root_dir, bool recursive = false, string search_pattern = "*.json")
		{
            DirectoryInfo dir_info = new DirectoryInfo(root_dir);
			// this could throw a DirectoryNotFoundException; maybe I should do some error handling?
			var json_strings = new Dictionary<string, Task<string>>();
			foreach (FileInfo file_info in dir_info.EnumerateFiles(search_pattern))
			{
				string fname = file_info.FullName;
				string json_str = file_info.OpenText().ReadToEnd();
                try
                {
					fname_jsons[fname] = json_parser.Parse(json_str);
					if (json_parser.lint != null && json_parser.lint.Count > 0)
					{
						fname_lints[fname] = json_parser.lint.LazySlice(":").ToArray();
					}
				}
                catch { } // just ignore badly formatted files
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
		/// Send an asynchronous request for JSON to an API.
		/// If the request succeeds, return the JSON string.
		/// If the request raises an exception, return the error message.
		/// See https://docs.microsoft.com/en-us/dotnet/csharp/tutorials/console-webapiclient
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		//public async Task<string> GetJsonStringFromApiAsync(string url)
		//{
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
		//		string json = await stringTask;
		//		return json;
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
		//public async Task<(HashSet<string> urls_requested, Dictionary<string, string> exceptions)> GetJsonFromAllUrls(string[] urls)
		//{
		//	var json_tasks = new Dictionary<string, Task<string>>();
		//	foreach (string url in urls)
		//	{
		//		// if (!fname_jsons.children.ContainsKey(url))
		//		//// it is probably better to allow duplication of labor, so that the user can get new JSON
		//		//// if something changed
		//		json_tasks[url] = GetJsonStringFromApiAsync(url);
		//	}
		//	string[] jsons = await Task.WhenAll(json_tasks.Values);
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
