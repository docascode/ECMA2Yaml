using ECMA2Yaml.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MSDNUrlPatch
{
    public class UrlRepaireHelper
    {
        bool _isLogVerbose = false;
        string _sourceFolder = string.Empty;
        string _logPath = string.Empty;
        string _repoRootFolder = string.Empty;
        FileAccessor _fileAccessor;
        Regex _msdnUrlRegex = new Regex(@"(https?://msdn.microsoft.com[\w-./?%&=]*)", RegexOptions.Compiled);
        Regex _redirectedFromRegex = new Regex(@"(redirectedfrom=\w*)", RegexOptions.Compiled);
        const string _redirectedKey = "redirectedfrom";
        const string _msdnUrlDomain = "msdn.microsoft.com";
        static HttpClient _client = new HttpClient();

        Dictionary<string, string> UrlDic = new Dictionary<string, string>();   // <msdn url,docs url>
        List<string> logMessages = new List<string>();

        public void Start(string sourceFolder, string logPath, bool isLogVerbose)
        {
            try
            {
                _sourceFolder = sourceFolder;
                _logPath = logPath;
                _isLogVerbose = isLogVerbose;

                _fileAccessor = new FileAccessor(_sourceFolder, null);
                RepaireAll();
            }
            catch (Exception ex)
            {
                logMessages.Add(ex.Message);
            }
            finally
            {
                if (logMessages.Count() > 0)
                {
                    File.AppendAllLines(_logPath, logMessages);
                }
            }
        }

        public void RepaireAll()
        {
            var allXmlFileList = _fileAccessor.ListFiles("*.xml", _sourceFolder, allDirectories: true);
            List<string> needRepaireXmlFileList = new List<string>();
            List<string> msdnUrlList = new List<string>();

            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

            // 1. Load msdn urls
            Parallel.ForEach(allXmlFileList, opt, xmlFile =>
            {
                var msdnUrls = LoadMSDNUrls(xmlFile.AbsolutePath);
                if (msdnUrls.Count() > 0)
                {
                    needRepaireXmlFileList.Add(xmlFile.AbsolutePath);
                    msdnUrlList.AddRange(msdnUrls);
                }
            });

            var allNeedRepaireUrls = msdnUrlList.Distinct().Where(p => !string.IsNullOrEmpty(p)).ToList();
            string message = string.Format("Have {0} msdn urls in {1} xml files need to be repaired", allNeedRepaireUrls.Count(), needRepaireXmlFileList.Count());
            WriteLine(message);
            logMessages.Add(message);

            // 2. Set msdn urls dic with initial value
            allNeedRepaireUrls.ForEach(url =>
            {
                UrlDic.Add(url, null);
            });

            // 3. Get docs url of msdn url
            Parallel.ForEach(allNeedRepaireUrls, opt, msdnUrl =>
            {
                string docsUrl = GetDocsUrl(msdnUrl);
                if (!string.IsNullOrEmpty(docsUrl))
                {
                    UrlDic[msdnUrl] = docsUrl;
                }
            });

            // 4. Replace msdn url with docs url in xml file
            Parallel.ForEach(needRepaireXmlFileList, opt, xmlFile =>
            {
                RepairFile(xmlFile);
            });

            WriteLine("Repair done.");
        }

        public List<string> LoadMSDNUrls(string xmlFile)
        {
            List<string> msdnUrlList = new List<string>();

            string fileText = File.ReadAllText(xmlFile);
            var matches = _msdnUrlRegex.Matches(fileText);
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
                            if (!UrlDic.ContainsKey(url))
                            {
                                msdnUrlList.Add(url);
                            }
                        }
                    }
                }
            }

            return msdnUrlList;
        }

        public void RepairFile(string xmlFile)
        {
            string fileText = File.ReadAllText(xmlFile);
            bool isChange = false;

            List<string> oldUrlList = new List<string>();
            var matches = _msdnUrlRegex.Matches(fileText);
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

            if (oldUrlList != null && oldUrlList.Count > 0)
            {
                foreach (string url in oldUrlList)
                {
                    string docsUrl = UrlDic[url];
                    if (string.IsNullOrEmpty(docsUrl))
                    {
                        logMessages.Add(string.Format("Can't repair msdn url {0} into file {1}", url, xmlFile));
                    }
                    else
                    {
                        fileText = fileText.Replace(url, docsUrl);
                        isChange = true;
                    }
                }
            }

            if (isChange)
            {
                File.WriteAllText(xmlFile, fileText);
            }
        }

        public string GetDocsUrl(string msdnUrl)
        {
            if (IsMSDNUrl(msdnUrl))
            {
                string redirectUrl = GetRedirectUrl(msdnUrl).Result;
                if (IsDocsUrl(redirectUrl))
                {
                    return RemoveUnusePartFromRedirectUrl(redirectUrl);
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

            if (url.ToLower().IndexOf(_msdnUrlDomain) > 0)
            {
                return true;
            }

            return false;
        }

        public bool IsDocsUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            // Not all msdn urls are redirect to docs url
            // Demo: https://msdn.microsoft.com/library/ms533714.aspx --> https://developer.mozilla.org/en-US/docs/Web/API/Document
            //if (url.ToLower().IndexOf("docs.microsoft.com") > 0)
            //{
            //    return true;
            //}

            return true;
        }

        public string RemoveUnusePartFromRedirectUrl(string redirectUrl)
        {
            string newUrl = redirectUrl.ToLower();
            if (redirectUrl.IndexOf(_redirectedKey) > 0)
            {
                newUrl = _redirectedFromRegex.Replace(newUrl, "");
                newUrl = newUrl.Replace("&&","&").Replace("?&","?").TrimEnd('&').TrimEnd('?');

                if (_isLogVerbose)
                {
                    logMessages.Add(string.Format("Redirect url: {0}, new url:{1}", redirectUrl, newUrl));
                }
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

                        return response.RequestMessage.RequestUri.OriginalString;
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
