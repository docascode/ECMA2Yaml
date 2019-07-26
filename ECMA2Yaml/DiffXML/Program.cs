using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DiffXML
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<OrderToolOptions>(args).WithParsed<OrderToolOptions>(option =>
            {
                OrderXML(option.InFolder, option.OutFolder);
            });
        }

        static void OrderXML(string inFolder, string outPutFolder)
        {
            var needOrderFiles = GetFiles(inFolder, "*.xml");
            ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };

            if (needOrderFiles != null)
            {
                Parallel.ForEach(needOrderFiles, opt, xfile => {
                    XDocument fxDoc = XDocument.Load(xfile.FullName);
                    var membersEle = fxDoc.Root.Element("members");
                    var memberEles = membersEle?.Elements("member");
                    if (memberEles != null)
                    {
                        var orderedList = memberEles.OrderBy(member => member.Attribute("name").Value).ToList();
                        orderedList.ForEach(m =>
                        {
                            var child = m.Elements().ToList();
                            if (child != null && child.Count() > 1)
                            {
                                m.RemoveNodes();
                                m.Add(child.OrderBy(c => c.Attribute("name")?.Value).OrderBy(c => c.Name.LocalName));
                            }
                        });
                        membersEle.RemoveAll();
                        membersEle.Add(orderedList);

                        orderedList.ToList().ForEach(p => {
                            //if (p.Attribute("name").Value == "M:System.Globalization.CultureAndRegionInfoBuilder.#ctor(System.String,System.Globalization.CultureAndRegionModifiers)")
                            //{
                            SpecialProcessElement(p);
                            //}
                        });

                        string directoryName = xfile.DirectoryName.Replace(inFolder, outPutFolder);
                        if (!Directory.Exists(directoryName))
                        {
                            Directory.CreateDirectory(directoryName);
                        }
                        fxDoc.Save(xfile.FullName.Replace(inFolder, outPutFolder));
                    }
                });
            }
            WriteLine(outPutFolder);
            WriteLine("done.");
        }

        private static void SpecialProcessElement(XElement ele)
        {
            if (ele != null)
            {
                var child = ele.Nodes();
                if (child != null && child.Count() > 0)
                {
                    List<XNode> toBeAddEles = new List<XNode>();
                    foreach (var e in child)
                    {
                        if (e.NodeType == System.Xml.XmlNodeType.Text)
                        {
                            SpecialProcessText((e as XText));
                        }
                        else if (e.NodeType == System.Xml.XmlNodeType.Element)
                        {
                            SpecialProcessElement(e as XElement);
                        }
                    }
                }
            }
        }

        public static void SpecialProcessText(XText xText)
        {
            string content = xText.Value;
            if (Regex.IsMatch(content, @"\n"))
            {
                // remove blank line
                content = Regex.Replace(content, @"^\s+|\s+$", string.Empty,RegexOptions.Multiline);
                xText.Value = content;
            }
        }

        static FileInfo[] GetFiles(string path, string pattern)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            return di.GetFiles(pattern, SearchOption.AllDirectories).OrderBy(f => f.Name).ToArray();
        }

        static void WriteLine(string format, params object[] args)
        {
            string timestamp = string.Format("[{0}]", DateTime.Now.ToString());
            Console.WriteLine(timestamp + string.Format(format, args));
        }
    }

    class OrderToolOptions
    {
        [Option('i', "inFolder", Required = true, HelpText = "the in file folder.")]
        public string InFolder { get; set; }

        [Option('o', "outFolder", Required = false, Default = "", HelpText = "The output file folder.")]
        public string OutFolder { get; set; }
    }
}
