using ECMA2Yaml;
using ECMA2Yaml.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MSDNUrlPatch
{
    public class UrlRepairHelper
    {
        bool _isLogVerbose = false;
        bool _isFixPreVersions = false;
        bool _isFixFixedVersions = false;
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
        Dictionary<string, string> _MockTestData = new Dictionary<string, string>();   // Mock data: <msdn url,docs url>
        List<string> logMessages = new List<string>();

        public UrlRepairHelper(CommandLineOptions option)
        {
            _client.DefaultRequestHeaders.Add("User-Agent", _userAgent);
            _sourceFolder = option.SourceFolder;
            _logPath = option.LogFilePath;
            _isLogVerbose = option.LogVerbose;
            _baseUrl = option.BaseUrl;
            _fileExtension = option.FileExtension;
            _isFixPreVersions = option.PreVersions;
            _isFixFixedVersions = option.FixedVersions;

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
            string message = $"Repair done, Total file:{allXmlFileList.Count()}, updated file:{modifyFileCounter}";
            WriteLine(message);
            logMessages.Add(message);
        }

        public bool RepairFile(string filePath)
        {
            if (filePath.ToLower().EndsWith(".xml"))
            {
                return RepairXmlFile(filePath);
            }
            else
            {
                return RepairNonXmlFile(filePath);
            }
        }

        public bool RepairNonXmlFile(string filePath)
        {
            bool isChange = false;

            string[] lines = File.ReadAllLines(filePath);
            if (lines != null && lines.Length > 0)
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    string l = lines[i];
                    if (!string.IsNullOrEmpty(l))
                    {
                        string newContent = RepairString(l, filePath);
                        if (!l.Equals(newContent))
                        {
                            lines[i] = newContent;
                            if (!isChange)
                            {
                                isChange = true;
                            }
                        }
                    }
                }
            }

            if (isChange)
            {
                File.WriteAllLines(filePath, lines);
                Console.WriteLine($"{filePath} update done.");
                return true;
            }

            return false;
        }

        public bool RepairXmlFile(string filePath)
        {
            bool isChange = false;
            XDocument xmlDoc = XDocument.Load(filePath);
            var child = xmlDoc.Nodes();
            if (child != null && child.Count() > 0)
            {
                foreach (var e in child)
                {
                    if (RepairXElement((e as XElement), filePath))
                    {
                        isChange = true;
                    }
                }
            }
            if (isChange)
            {
                SaveXmlFile(xmlDoc, filePath);
                Console.WriteLine($"{filePath} update done.");
                return true;
            }

            return false;
        }

        private bool RepairXElement(XElement ele, string filePath)
        {
            bool isChange = false;

            if (ele != null)
            {
                var child = ele.Nodes();
                if (child != null && child.Count() > 0)
                {
                    foreach (var e in child)
                    {
                        if (e.NodeType == System.Xml.XmlNodeType.Text)
                        {
                            if (RepairXText((e as XText), filePath) && isChange == false)
                            {
                                isChange = true;
                            }
                        }
                        else if (e.NodeType == System.Xml.XmlNodeType.Element)
                        {
                            if (RepairXElement((e as XElement), filePath) && isChange == false)
                            {
                                isChange = true;
                            }
                        }
                    }
                }

                var attributes = ele.Attributes();
                if (attributes != null && attributes.Count() > 0)
                {
                    foreach (var attr in attributes)
                    {
                        if (RepairXAttribute(attr, filePath) && isChange == false)
                        {
                            isChange = true;
                        }
                    }
                }
            }

            return isChange;
        }

        public bool RepairXText(XText xText, string filePath)
        {
            string content = xText.Value;
            string newContent = RepairString(content, filePath);

            if (!content.Equals(newContent))
            {
                xText.Value = newContent;
                return true;
            }

            return false;
        }

        public bool RepairXAttribute(XAttribute attr, string filePath)
        {
            string content = attr.Value;
            string newContent = RepairString(content, filePath);

            if (!content.Equals(newContent))
            {
                attr.Value = newContent;
                return true;
            }

            return false;
        }

        public string RepairString(string inputText, string filePath)
        {
            if (inputText.Length < 18)
            {
                return inputText;
            }

            // Fix issue 2 in bug https://dev.azure.com/ceapex/Engineering/_workitems/edit/267076
            if (inputText.Contains("ms:mtpsurl:"))
            {
                return inputText;
            }

            // 1. Get Link markdown url like
            // [Global Assembly Cache Tool (Gacutil.exe)](https://msdn.microsoft.com/library/ex0ss12c(VS.80).aspx)
            var matches = _linkRegex.Matches(inputText);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups != null)
                    {
                        var groups = match.Groups;
                        string linkString = groups[0].Value.ToString();
                        for (int i = 1; i < groups.Count; i++)
                        {
                            string partialChar = string.Empty;
                            string oldUrl = groups[i].Value.TrimEnd('.');
                            if (IsMSDNUrl(oldUrl))
                            {
                                oldUrl = PreProcessMSDNUrl(oldUrl, out partialChar);
                                string docsUrl = GetNewUrl(oldUrl);
                                if (string.IsNullOrEmpty(docsUrl))
                                {
                                    logMessages.Add(string.Format("Can't repair msdn url {0} into file {1}", oldUrl, filePath));
                                }
                                else
                                {
                                    if (docsUrl != _noNeedRepairKeyWord)
                                    {
                                        // inputText = _linkRegex.Replace(inputText, docsUrl);
                                        // Fix issue 1 in bug https://dev.azure.com/ceapex/Engineering/_workitems/edit/267076

                                        if (!string.IsNullOrEmpty(_baseUrl) && !docsUrl.StartsWith("http"))
                                        {
                                            inputText = inputText.Replace(linkString, $"<{_baseUrl}{docsUrl}>{partialChar}");
                                        }
                                        else
                                        {
                                            inputText = inputText.Replace(linkString, $"<{docsUrl}>{partialChar}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 2. Get link like
            // <a href="https://msdn.microsoft.com/library/windows/desktop/bb968803(v=vs.85).aspx">Event Tracing</a>
            matches = _link1Regex.Matches(inputText);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups != null)
                    {
                        var groups = match.Groups;
                        for (int i = 1; i < groups.Count; i++)
                        {
                            string oldUrl = groups[i].Value.TrimEnd('.');
                            if (IsMSDNUrl(oldUrl))
                            {
                                string docsUrl = GetNewUrl(oldUrl);
                                if (string.IsNullOrEmpty(docsUrl))
                                {
                                    logMessages.Add(string.Format("Can't repair msdn url {0} into file {1}", oldUrl, filePath));
                                }
                                else
                                {
                                    if (docsUrl != _noNeedRepairKeyWord)
                                    {
                                        inputText = _link1Regex.Replace(inputText, docsUrl);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 3. Get msdn url for left part
            matches = _msdnUrlRegex.Matches(inputText);
            if (matches != null && matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    if (match.Groups != null)
                    {
                        var groups = match.Groups;
                        for (int i = 1; i < groups.Count; i++)
                        {
                            string oldUrl = groups[i].Value.TrimEnd('.');
                            if (IsMSDNUrl(oldUrl))
                            {
                                string docsUrl = GetNewUrl(oldUrl);
                                if (string.IsNullOrEmpty(docsUrl))
                                {
                                    logMessages.Add(string.Format("Can't repair msdn url {0} into file {1}", oldUrl, filePath));
                                }
                                else
                                {
                                    if (docsUrl != _noNeedRepairKeyWord)
                                    {
                                        inputText = _msdnUrlRegex.Replace(inputText, docsUrl);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return inputText;
        }

        public string GetNewUrl(string oldUrl)
        {
            string docsUrl = string.Empty;
            if (!UrlDic.ContainsKey(oldUrl))
            {
                docsUrl = GetDocsUrl(oldUrl);
                UrlDic[oldUrl] = docsUrl;
            }
            else
            {
                docsUrl = UrlDic[oldUrl];
            }

            return docsUrl;
        }

        public string GetDocsUrl(string msdnUrl)
        {
            if (IsMSDNUrl(msdnUrl))
            {
                // fix issue 1 in bug https://dev.azure.com/ceapex/Engineering/_workitems/edit/245894
                if (msdnUrl.EndsWith("msdn.microsoft.com"))
                {
                    return "https://docs.microsoft.com";
                }

                string url = DecodeUrl(msdnUrl);
                string redirectUrl = GetRedirectUrl(url).Result;
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

            if (_msdnUrlRegex.IsMatch(url))
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
            if (url.Contains("/previous-versions/") && !_isFixPreVersions)
            {
                string message = string.Format("'{0}' contains '/previous-versions/', don't need be processed.", url);
                logMessages.Add(message);

                return false;
            }

            if ((url.Contains("?view=") || url.Contains("&view=")) && !_isFixFixedVersions)
            {
                string message = string.Format("'{0}' contains 'view=', don't need be processed.", url);
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
            // Only for unit test
            if (_MockTestData != null && _MockTestData.ContainsKey(msdnUrl))
            {
                return _MockTestData[msdnUrl];
            }

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

        public void SaveXmlFile(XDocument xmlDoc, string xmlFilePath)
        {
            var fileEncoding = GetEncoding(xmlFilePath);
            XmlWriterSettings xws = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                Encoding = fileEncoding
            };
            using (XmlWriter xw = XmlWriter.Create(xmlFilePath, xws))
            {
                xmlDoc.Save(xw);
            }
        }

        private Encoding GetEncoding(string filename)
        {
            // This is a direct quote from MSDN:  
            // The CurrentEncoding value can be different after the first
            // call to any Read method of StreamReader, since encoding
            // autodetection is not done until the first call to a Read method.

            using (var reader = new StreamReader(filename, Encoding.Default, true))
            {
                if (reader.Peek() >= 0) // you need this!
                    reader.Read();

                return reader.CurrentEncoding;
            }
        }

        private string PreProcessMSDNUrl(string url, out string partialChar)
        {
            if (string.IsNullOrEmpty(url))
            {
                partialChar = "";
                return url;
            }

            // for _linkRegex, can't handle multiple ')' chars in one line, need to trim end it.
            // like: (see docs at [https://msdn.microsoft.com/library/1d3t3c61.aspx](https://msdn.microsoft.com/library/1d3t3c61.aspx))
            // => https://msdn.microsoft.com/library/1d3t3c61.aspx)
            if (url.EndsWith(")") && !url.Contains("("))
            {
                partialChar = ")";
                url = url.TrimEnd(')');
                return url;
            }

            partialChar = "";
            return url;
        }

        private string DecodeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            // Fix bug: https://dev.azure.com/ceapex/Engineering/_workitems/edit/287551
            return url.Replace(@"\(","(").Replace(@"\)", ")");
        }

        public void MockTestData(string msdnUrl, string redirectUrl)
        {
            if (!_MockTestData.ContainsKey(msdnUrl))
            {
                _MockTestData[msdnUrl] = redirectUrl;
            }
        }
    }
}
