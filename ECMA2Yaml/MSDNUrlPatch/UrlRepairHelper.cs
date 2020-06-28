using ECMA2Yaml.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MSDNUrlPatch
{
    public class UrlRepairHelper
    {
        bool _isLogVerbose = false;
        string _sourceFolder = string.Empty;
        string _logPath = string.Empty;
        string _repoRootFolder = string.Empty;
        string _baseUrl = "https://docs.microsoft.com/en-us";
        string _fileExtension = "";
        FileAccessor _fileAccessor;
        Regex _msdnUrlRegex = new Regex(@"(https?://msdn.microsoft.com[\w-./?%&=]*)", RegexOptions.Compiled);
        Regex _linkRegex = new Regex(@"\[.*?\]\(([^\s,]*)\)", RegexOptions.Compiled);
        Regex _link1Regex = new Regex("\"(https?://msdn.microsoft.com.*)\"", RegexOptions.Compiled);
        Regex _redirectedFromRegex = new Regex(@"(redirectedfrom=\w*)", RegexOptions.Compiled);
        Regex _versionUrlRegex = new Regex(@"\\\(v=.*\).aspx", RegexOptions.Compiled);
        const string _redirectedKey = "redirectedfrom";
        const string _noNeedRepairKeyWord = "NoNeed";
        const string _userAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.61 Safari/537.36";
        static HttpClient _client = new HttpClient();
        static int _batchSize = 100;

        Dictionary<string, string> UrlDic = new Dictionary<string, string>();   // <msdn url,docs url>
        List<string> logMessages = new List<string>();

        public UrlRepairHelper(CommandLineOptions option)
        {
            _client.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            _sourceFolder = option.SourceFolder;
            _logPath = option.LogFilePath;
            _isLogVerbose = option.LogVerbose;
            _baseUrl = option.BaseUrl;
            _fileExtension = option.FileExtension;
            if (option.BatchSize > 0)
            {
                _batchSize = option.BatchSize;
            }
        }

        public void Start()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                RepairAll();
            }
            catch (Exception ex)
            {
                logMessages.Add(ex.Message);
            }
            finally
            {
                sw.Stop();
                string message = string.Format("Total run {0:F1} mins", sw.ElapsedMilliseconds / (1000 * 60));
                logMessages.Add(message);

                WriteLine(message);
                File.AppendAllLines(_logPath, logMessages);
            }
        }

        public void RepairAll()
        {
            _fileAccessor = new FileAccessor(_sourceFolder, null);
            var allXmlFileList = _fileAccessor.ListFiles("*." + _fileExtension, _sourceFolder, allDirectories: true).ToList();
            List<string> needRepairXmlFileList = new List<string>();
            List<string> msdnUrlList = new List<string>();

            int modifyFileCounter = 0;
            for (int i = 0; i < allXmlFileList.Count(); i++)
            {
                var xmlFile = allXmlFileList[i];
                if (RepairFile(xmlFile.AbsolutePath))
                {
                    modifyFileCounter++;
                }
                if (modifyFileCounter >= _batchSize)
                {
                    break;
                }
            }

            WriteLine("Repair done.");
        }

        public bool RepairFile(string xmlFile)
        {
            string fileText = File.ReadAllText(xmlFile);
            bool isChange = false;

            List<string> oldUrlList = GetMSDNUrls(fileText);

            // TODO: How to process follow case
            // <NavigateUrl>https://msdn.microsoft.com/library/bfbb433f-7ab6-417a-90f0-71443d76bcb3<NavigateUrl/> <NavigateUrl>https://msdn.microsoft.com/library<NavigateUrl/> 

            if (oldUrlList != null && oldUrlList.Count > 0)
            {
                oldUrlList = oldUrlList.OrderByDescending(u => u.Length).ToList();
                foreach (string url in oldUrlList)
                {
                    string docsUrl = string.Empty;
                    if (!UrlDic.ContainsKey(url))
                    {
                        docsUrl = GetDocsUrl(url);
                        UrlDic[url] = docsUrl;
                    }
                    else
                    {
                        docsUrl = UrlDic[url];
                    }

                    if (string.IsNullOrEmpty(docsUrl))
                    {
                        logMessages.Add(string.Format("Can't repair msdn url {0} into file {1}", url, xmlFile));
                    }
                    else
                    {
                        if (docsUrl != _noNeedRepairKeyWord)
                        {
                            fileText = fileText.Replace(url, docsUrl);
                            isChange = true;
                        }
                    }
                }
            }

            if (isChange)
            {
                File.WriteAllText(xmlFile, fileText);
                Console.WriteLine($"{xmlFile} update done.");
                return true;
            }

            return false;
        }

        public List<string> GetMSDNUrls(string fileText)
        {
            List<string> oldUrlList = new List<string>();

            // 1. Get Link markdown url like
            // [Global Assembly Cache Tool (Gacutil.exe)](https://msdn.microsoft.com/library/ex0ss12c(VS.80).aspx)
            var matches = _linkRegex.Matches(fileText);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups != null)
                    {
                        var groups = match.Groups;
                        for (int i = 1; i < groups.Count; i++)
                        {
                            string url = groups[i].Value.TrimEnd('.');
                            if (oldUrlList.IndexOf(url) < 0 && IsMSDNUrl(url))
                            {
                                oldUrlList.Add(url);
                                // Replace it with "" in case it been found twice time in following step
                                fileText = fileText.Replace(url, "");
                            }
                        }
                    }
                }
            }

            // 2. Get link like
            // <a href="https://msdn.microsoft.com/library/windows/desktop/bb968803(v=vs.85).aspx">Event Tracing</a>
            matches = _link1Regex.Matches(fileText);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups != null)
                    {
                        var groups = match.Groups;
                        for (int i = 1; i < groups.Count; i++)
                        {
                            string url = groups[i].Value.TrimEnd('.');
                            if (oldUrlList.IndexOf(url) < 0)
                            {
                                oldUrlList.Add(url);
                                // Replace it with "" in case it been found twice time in following step
                                fileText = fileText.Replace(url, "");
                            }
                        }
                    }
                }
            }

            // 3. Get msdn url for left part
            matches = _msdnUrlRegex.Matches(fileText);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups != null)
                    {
                        var groups = match.Groups;
                        for (int i = 1; i < groups.Count; i++)
                        {
                            string url = groups[i].Value.TrimEnd('.');
                            if (oldUrlList.IndexOf(url) < 0)
                            {
                                oldUrlList.Add(url);
                            }
                        }
                    }
                }
            }

            return oldUrlList;
        }

        public string GetDocsUrl(string msdnUrl)
        {
            if (IsMSDNUrl(msdnUrl))
            {
                string redirectUrl = GetRedirectUrl(msdnUrl).Result;
                if (IsNeedRepair(redirectUrl))
                {
                    return RemoveUnusePartFromRedirectUrl(redirectUrl);
                }
                else
                {
                    return _noNeedRepairKeyWord;
                }
            }

            return string.Empty;
        }

        public bool IsMSDNUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            if(_msdnUrlRegex.IsMatch(url))
            {
                return true;
            }

            return false;
        }

        public bool IsNeedRepair(string redirectUrl)
        {
            if (string.IsNullOrEmpty(redirectUrl))
            {
                return false;
            }

            // Don't replace msdn links with different msdn links (the F# content still hasn't migrated)
            if (IsMSDNUrl(redirectUrl))
            {
                return false;
            }

            return IsNeedRepair_DotnetApiDocs(redirectUrl);
        }

        // If the redirected link starts with "/previous-versions/" or contains "view=xxx", we won't replace them.
        public bool IsNeedRepair_DotnetApiDocs(string url)
        {
            if (url.Contains("/previous-versions/") || url.Contains("?view=") || url.Contains("&view="))
            {
                string message = string.Format("'{0}' contains '/previous-versions/' or 'view=', don't need be processed.", url);
                logMessages.Add(message);

                return false;
            }

            return true;
        }

        public string RemoveUnusePartFromRedirectUrl(string redirectUrl)
        {
            string newUrl = redirectUrl.ToLower();
            // remove redirectedfrom parameter(redirectedfrom=MSDN) from url
            if (redirectUrl.IndexOf(_redirectedKey) > 0)
            {
                newUrl = _redirectedFromRegex.Replace(newUrl, "");
                newUrl = newUrl.Replace("&&", "&").Replace("?&", "?").TrimEnd('&').TrimEnd('?');

                if (_isLogVerbose)
                {
                    logMessages.Add(string.Format("Redirect url: {0}, new url:{1}", redirectUrl, newUrl));
                }
            }

            // If url is internal url, need remove base url, just keep relative part
            // But if url is equal base url, just keep this url, no need keep relative path, just like: https://docs.microsoft.com/en-us/
            if (!string.IsNullOrEmpty(_baseUrl) && newUrl.StartsWith(_baseUrl))
            {
                if (!newUrl.TrimEnd('/').Equals(_baseUrl))
                {
                    newUrl = newUrl.Replace(_baseUrl, "");
                }
            }

            //  There are a couple of links that have a version extension that is being kept when the link is updated to docs, 
            //  The version should be removed.The extension looks like this for example: \(v=vs.85\).aspx
            if (newUrl.Contains(@"\).aspx"))
            {
                newUrl = _versionUrlRegex.Replace(newUrl, "");
            }

            return newUrl;
        }

        public async Task<string> GetRedirectUrl(string msdnUrl)
        {
            try
            {
                var response = await _client.GetAsync(msdnUrl);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (msdnUrl.Equals(response.RequestMessage.RequestUri.OriginalString, StringComparison.OrdinalIgnoreCase))
                    {
                        string message = string.Format("Success but no redirect to request msdn url: {0}", msdnUrl);
                        logMessages.Add(message);
                        return string.Empty;
                    }
                    else
                    {
                        if (_isLogVerbose)
                        {
                            string message = string.Format("Success to request msdn url: {0}, redirect url: {1}", msdnUrl, response.RequestMessage.RequestUri.OriginalString);
                            logMessages.Add(message);
                        }

                        return response.RequestMessage.RequestUri.OriginalString.ToLower();
                    }
                }
                else
                {
                    logMessages.Add(string.Format("Failed to request msdn url: {0} .", msdnUrl));
                    logMessages.Add(string.Format(" Details: {0} {1}", response.StatusCode, response.RequestMessage));
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                logMessages.Add(string.Format("Failed to request msdn url: {0} .", msdnUrl));
                logMessages.Add(string.Format(" Exception: {0}", ex.Message));
                return string.Empty;
            }
        }

        static void WriteLine(string format, params object[] args)
        {
            string timestamp = string.Format("[{0}]", DateTime.Now.ToString());
            Console.WriteLine(timestamp + string.Format(format, args));
        }
    }
}
