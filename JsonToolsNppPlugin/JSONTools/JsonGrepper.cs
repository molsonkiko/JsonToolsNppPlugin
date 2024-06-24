using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JSON_Tools.Utils;

namespace JSON_Tools.JSON_Tools
{
    public class TooMuchTextToParseException : Exception
    {
        public const int MAX_COMBINED_LENGTH_TEXT_TO_PARSE = int.MaxValue / 5;

        public int lengthOfTextToParse;

        private TooMuchTextToParseException(int lengthOfTextToParse)
        {
            this.lengthOfTextToParse = lengthOfTextToParse;
        }

        public override string ToString()
        {
            return $"The total length of text ({lengthOfTextToParse}) to be parsed exceeded the maximum length ({MAX_COMBINED_LENGTH_TEXT_TO_PARSE})";
        }

        /// <summary>
        /// throw an exception of this type if lengthOfTextToParse is greater than <see cref="MAX_COMBINED_LENGTH_TEXT_TO_PARSE"/>,
        /// based on the expectation that the computer would run out of memory while attempting to parse everything.<br></br>
        /// Does nothing if an exception is not thrown.
        /// </summary>
        /// <param name="lengthOfTextToParse"></param>
        /// <exception cref="TooMuchTextToParseException"></exception>
        public static void ThrowIfTooMuchText(int lengthOfTextToParse)
        {
            if (lengthOfTextToParse > MAX_COMBINED_LENGTH_TEXT_TO_PARSE)
                throw new TooMuchTextToParseException(lengthOfTextToParse);
        }
    }

	/// <summary>
	/// Reads JSON files based on search patterns, and also fetches JSON from APIS.
	/// Combines all JSON into a map, fnameJsons, from filenames/urls to JSON.
	/// </summary>
	public class JsonGrepper
	{
        /// <summary>
        /// do not report progress unless there are at least this many files
        /// </summary>
        private const int PROGRESS_REPORT_FILE_MIN_COUNT = 16;
        /// <summary>
        /// do not report progress unless the combined length of all files to be parsed is at least this great
        /// </summary>
        private const int PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH = 8_000_000;
        /// <summary>
        /// when reporting progress using a progress bar, the progress bar is only updated this many times.
        /// </summary>
        private const int NUM_PROGRESS_REPORT_CHECKPOINTS = 25;
		/// <summary>
		/// maps filenames and urls to parsed JSON
		/// </summary>
		public JObject fnameJsons;
        public JObject exceptions;
        public Dictionary<string, string> fnameStrings;
		public JsonParser jsonParser;
        private static readonly HttpClient httpClient = new HttpClient();
        /// <summary>
        /// the combined length of all files to be parsed
        /// </summary>
        private int totLengthToParse;
        //private int totLengthAlreadyParsed;
        //private int checkPointLength;
        //private bool reportProgress;
        //private Form progressBarForm;
        //private ProgressBar progressBar;
        ///// <summary>
        ///// could be used to enable the user to cancel grepping by clicking a button
        ///// when the progress bar pops up.
        ///// </summary>
        //private CancellationToken cancellationToken;
        //private int nextCheckPoint;

		public JsonGrepper(JsonParser jsonParser = null)
		{
            fnameStrings = new Dictionary<string, string>();
			fnameJsons = new JObject();
            exceptions = new JObject();
			if (jsonParser == null)
            {
				this.jsonParser = new JsonParser(LoggerLevel.JSON5);
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
        /// Finds files that match any of the searchPatterns (typically just ".json" files) in the directory rootDir
        /// and creates a dictionary mapping each found filename to that file's text.
        /// If recursive is true, this will recursively search all subdirectories for JSON files, not just the root.
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="recursive"></param>
        /// <param name="searchPattern"></param>
        /// <exception cref="TooMuchTextToParseException">Thrown if the total length of files to parse would exceed <see cref="TooMuchTextToParseException.MAX_COMBINED_LENGTH_TEXT_TO_PARSE"/></exception>
        private void ReadJsonFiles(string rootDir, bool recursive, params string[] searchPatterns)
		{
            DirectoryInfo dirInfo;
            try
            {
                dirInfo = new DirectoryInfo(rootDir);
            }
            catch { return; }
            totLengthToParse = 0;
            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (string searchPattern in searchPatterns)
            {
			    foreach (FileInfo fileInfo in dirInfo.EnumerateFiles(searchPattern, searchOption))
			    {
				    string fname = fileInfo.FullName;
                    if (!fnameStrings.ContainsKey(fname))
                    {
                        using (var fp = fileInfo.OpenText())
                        {
                            string text = fp.ReadToEnd();
                            totLengthToParse += text.Length;
                            TooMuchTextToParseException.ThrowIfTooMuchText(totLengthToParse);
                            fnameStrings[fname] = text;
                        }
                    }
			    }
            }
		}

        private (string fname, JNode parsedOrError, bool error) ParseOrGetError(string fname, string jsonStr, JsonParser jsonParserTemplate)
        {
            JNode parsedOrError;
            bool error = false;
            try
            {
                JsonParser parser = jsonParserTemplate.Copy();
                parsedOrError = jsonParser.Parse(jsonStr);
            }
            catch (Exception ex)
            {
                error = true;
                parsedOrError = new JNode(ex.ToString());
            }
            //if (reportProgress)
            //{
            //    // Update the progress bar if the next checkpoint has been reached, then advance the checkpoint.
            //    // Otherwise, keep moving toward the next checkpoint.
            //    int alreadyParsed = Interlocked.Add(ref totLengthAlreadyParsed, jsonStr.Length);
            //    if (alreadyParsed >= nextCheckPoint)
            //    {
            //        int newNextCheckPoint = alreadyParsed + checkPointLength;
            //        newNextCheckPoint = newNextCheckPoint > totLengthToParse ? totLengthToParse : newNextCheckPoint;
            //        Interlocked.Exchange(ref nextCheckPoint, newNextCheckPoint);
            //        progressBar.Invoke(new Action(() => { progressBar.Value = alreadyParsed; }));
            //    }
            //}
            return (fname, parsedOrError, error);
        }

        /// <summary>
        /// Takes a map of filenames/urls to strings,
        /// and attempts to parse each string and add the fname-JNode pair to fnameJsons.
        /// </summary>
        private void ParseJsonStringsThreaded()
        {
		    string[] fnames = fnameStrings.Keys.ToArray();
            var results = fnames
                .Select(fname => ParseOrGetError(fname, fnameStrings[fname], jsonParser))
                .AsParallel()
                .OrderBy(x => x.fname)
                .ToArray();
            //// below lines are for progress reporting using a visual bar.
            //reportProgress = totLengthToParse >= PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH && fnameStrings.Count >= PROGRESS_REPORT_FILE_MIN_COUNT;
            //progressBarForm = null;
            //if (reportProgress)
            //{
            //    progressBar = new ProgressBar
            //    {
            //        Name = "progress",
            //        Minimum = 0,
            //        Maximum = totLengthToParse,
            //        Style = ProgressBarStyle.Blocks,
            //        Left = 20,
            //        Width = 450,
            //        Top = 200,
            //        Height = 50,
            //    };
            //    string totLengthToParseMB = (totLengthToParse / 1e6).ToString("F3", JNode.DOT_DECIMAL_SEP);
            //    Label label = new Label
            //    {
            //        Name = "title",
            //        Text = "All JSON documents have been read into memory.\r\n" +
            //              $"Now parsing {fnameStrings.Count} documents with combined length of about {totLengthToParseMB} MB.",
            //        TextAlign=ContentAlignment.TopCenter,
            //        Top = 20,
            //        AutoSize = true,
            //    };
            //    progressBarForm = new Form
            //    {
            //        Text = "JSON parsing in progress",
            //        Controls = { label, progressBar },
            //        Width=500,
            //        Height=300,
            //    };
            //    progressBarForm.Show();
            //}
            //totLengthAlreadyParsed = 0;
            //checkPointLength = totLengthToParse / NUM_PROGRESS_REPORT_CHECKPOINTS;
            foreach ((string fname, JNode parsedOrError, bool error) in results)
            {
                if (error)
                    exceptions[fname] = parsedOrError;
                else
                    fnameJsons[fname] = parsedOrError;
            }
            fnameStrings.Clear(); // don't need the strings anymore, only the JSON
            //if (reportProgress)
            //{
            //    progressBarForm.Close();
            //    progressBarForm.Dispose();
            //    progressBarForm = null;
            //    progressBar = null;
            //}
        }

        /// <summary>
        /// for each file that matches (any of the searchPatterns) in rootDir
        /// (and all subdirectories of rootDir if recursive is true)<br></br>
        /// attempt to parse that file as JSON using this JsonGrepper's JsonParser.<br></br>
        /// For each file that contains valid JSON (according to the parser)
        /// map that filename to the JNode produced by the parser in fnameJsons.
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="recursive"></param>
        /// <param name="searchPattern"></param>
        /// /// <exception cref="TooMuchTextToParseException">Thrown if the total length of files to parse would exceed <see cref="TooMuchTextToParseException.MAX_COMBINED_LENGTH_TEXT_TO_PARSE"/></exception>
        public void Grep(string rootDir, bool recursive = false, params string[] searchPatterns)
        {
            ReadJsonFiles(rootDir, recursive, searchPatterns);
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
    }
}
