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
    public class TooMuchTextToParseException : Exception
    {
        public static readonly int MAX_COMBINED_LENGTH_TEXT_TO_PARSE = IntPtr.Size == 4 ? int.MaxValue / 10 : int.MaxValue / 5;

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
        /// Do not report progress while reading files unless there are at least this many files
        /// </summary>
        private const int PROGRESS_REPORT_FILE_READING_MIN_COUNT = 64;
        /// <summary>
        /// do not report progress while parsing unless there are at least this many files
        /// </summary>
        private const int PROGRESS_REPORT_FILE_MIN_COUNT = 16;
        /// <summary>
        /// do not report progress (while parsing or reading files) unless the combined length of all files to be parsed is at least this great
        /// </summary>
        private const int PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH = 8_000_000;
        /// <summary>
        /// If the total combined length of all files to be parsed is at least this great,<br></br>
        /// show a progress bar <i>even if there are fewer than <see cref="PROGRESS_REPORT_FILE_MIN_COUNT"/> files.</i>
        /// </summary>
        private const int PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH_IF_LT_MINCOUNT_FILES = 50_000_000;
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
        private int totalLengthToParse;
        /// <summary>
        /// Called before beginning progress reporting for file reading or parsing
        /// </summary>
        /// <param name="totalNumber">When reporting progress for parsing, the combined number of characters in all files to parse.<br></br>
        /// Otherwise, the total number of all files to search (before determining whether their names match)</param>
        /// <param name="totalLengthOnHardDrive">-1 if this is called when reporting progress for parsing.<br></br>
        /// Otherwise, this is the combined size (on hard drive) of all files to search (before determining whether their names match)</param>
        public delegate void ProgressReportSetup(int totalNumber, long totalLengthOnHardDrive);
        public delegate void ProgressReportCallback(int numberSoFar, int totalNumber);
        // constructor fields related to progress reporting
        private bool reportProgress;
        private ProgressReportSetup progressReportSetup;
        private ProgressReportCallback progressReportCallback;
        private Action progressReportTeardown;
        // derived fields related to progress reporting
        private int totLengthAlreadyParsed;
        private int progressReportCheckpoints;
        private int checkPointLength;
        private int nextCheckPoint;

        /// <summary></summary>
        /// <param name="jsonParser">the parser to use to parse API responses and grepped files</param>
        /// <param name="reportProgress">whether to report progress</param>
        /// <param name="progressReportCheckpoints">How many times to report progress</param>
        /// <param name="progressReportSetup">Anything that must be done before progress reporting starts</param>
        /// <param name="progressReportCallback">A function that is called at each progress report checkpoint</param>
        /// <param name="progressReportTeardown">Anything that must be done after progress reporting is complete</param>
		public JsonGrepper(JsonParser jsonParser = null, bool reportProgress = false, int progressReportCheckpoints = -1, ProgressReportSetup progressReportSetup = null, ProgressReportCallback progressReportCallback = null, Action progressReportTeardown = null)
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
            // configure progress reporting
            this.reportProgress = reportProgress;
            this.progressReportCheckpoints = progressReportCheckpoints;
            this.progressReportSetup = progressReportSetup;
            this.progressReportCallback = progressReportCallback;
            this.progressReportTeardown = progressReportTeardown;
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
            totalLengthToParse = 0;
            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            FileInfo[] allFiles = dirInfo.GetFiles("*.*", searchOption);
            long totalLengthToRead = allFiles.Sum(x => x.Length);
            int nFiles = allFiles.Length;
            bool shouldReportProgress = reportProgress
                && (totalLengthToRead >= PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH_IF_LT_MINCOUNT_FILES
                    || (totalLengthToRead >= PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH && nFiles >= PROGRESS_REPORT_FILE_READING_MIN_COUNT));
            int checkPointLength = nFiles / progressReportCheckpoints;
            int nextCheckPoint = checkPointLength;
            Func<string, bool> globFunc = new Glob().ParseLinesSimple(string.Join("\n", searchPatterns));
            if (shouldReportProgress)
            {
                progressReportSetup?.Invoke(nFiles, totalLengthToRead);
                progressReportCallback(0, nFiles);
            }
            for (int filesReadSoFar = 0; filesReadSoFar < nFiles; filesReadSoFar++)
			{
                FileInfo fileInfo = allFiles[filesReadSoFar];
				string fname = fileInfo.FullName;
                if (globFunc(fname))
                {
                    using (var fp = fileInfo.OpenText())
                    {
                        string text = fp.ReadToEnd();
                        totalLengthToParse += text.Length;
                        TooMuchTextToParseException.ThrowIfTooMuchText(totalLengthToParse);
                        fnameStrings[fname] = text;
                    }
                }
                if (shouldReportProgress && filesReadSoFar == nextCheckPoint)
                {
                    nextCheckPoint = nextCheckPoint + checkPointLength > nFiles ? nFiles : nextCheckPoint + checkPointLength;
                    progressReportCallback(filesReadSoFar, nFiles);
                }
            }
            if (shouldReportProgress)
            {
                progressReportCallback(nFiles, nFiles);
                progressReportTeardown?.Invoke();
            }
        }

        private (string fname, JNode parsedOrError, bool error) ParseOrGetError(string fname, string jsonStr, JsonParser jsonParserTemplate, bool shouldReportProgress)
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
            if (shouldReportProgress && !(progressReportCallback is null))
            {
                // Update the progress bar if the next checkpoint has been reached, then advance the checkpoint.
                // Otherwise, keep moving toward the next checkpoint.
                int alreadyParsed = Interlocked.Add(ref totLengthAlreadyParsed, jsonStr.Length);
                if (alreadyParsed >= nextCheckPoint)
                {
                    progressReportCallback(alreadyParsed, totalLengthToParse);
                    int newNextCheckPoint = alreadyParsed + checkPointLength;
                    newNextCheckPoint = newNextCheckPoint > totalLengthToParse ? totalLengthToParse : newNextCheckPoint;
                    Interlocked.Exchange(ref nextCheckPoint, newNextCheckPoint);
                }
            }
            return (fname, parsedOrError, error);
        }

        /// <summary>
        /// Takes a map of filenames/urls to strings,
        /// and attempts to parse each string and add the fname-JNode pair to fnameJsons.
        /// </summary>
        private void ParseJsonStringsThreaded()
        {
            bool shouldReportProgress = reportProgress
                && (totalLengthToParse >= PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH_IF_LT_MINCOUNT_FILES
                    || (totalLengthToParse >= PROGRESS_REPORT_TEXT_MIN_TOT_LENGTH && fnameStrings.Count >= PROGRESS_REPORT_FILE_MIN_COUNT));
            totLengthAlreadyParsed = 0;
            checkPointLength = totalLengthToParse / progressReportCheckpoints;
		    string[] fnames = fnameStrings.Keys.ToArray();
            if (shouldReportProgress)
            {
                progressReportSetup?.Invoke(totalLengthToParse, -1);
                progressReportCallback(0, totalLengthToParse);
            }
            var results = fnames
                .Select(fname => ParseOrGetError(fname, fnameStrings[fname], jsonParser, shouldReportProgress))
                .AsParallel()
                .OrderBy(x => x.fname)
                .ToArray();
            if (shouldReportProgress)
            {
                progressReportCallback(totalLengthToParse, totalLengthToParse);
                progressReportTeardown?.Invoke();
            }
            foreach ((string fname, JNode parsedOrError, bool error) in results)
            {
                if (error)
                    exceptions[fname] = parsedOrError;
                else
                    fnameJsons[fname] = parsedOrError;
            }
            fnameStrings.Clear(); // don't need the strings anymore, only the JSON
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
