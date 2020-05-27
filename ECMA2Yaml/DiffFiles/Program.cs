using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DiffFiles
{
    class Program
    {
        static string _logFileFullName = "";
        static void Main(string[] args)
        {
            var opt = new CommandLineOptions();

            if (opt.Parse(args))
            {
                _logFileFullName = Path.Combine(opt.LogPath, string.Format("compare_result_{0:yyyyMMddHHmmss}.log", DateTime.Now));

                ConsoleLog(string.Format("Path1: '{0}'", opt.OldPath));
                ConsoleLog(string.Format("Path2: '{0}'", opt.NewPath));
                ConsoleLog(string.Format("Log: '{0}'", opt.LogPath));

                // Diff paths
                if (opt.IsDiffPath)
                {
                    ConsoleLog("Start diff paths...");
                    ComparePaths(opt.OldPath, opt.NewPath);
                }
                // Diff files
                else
                {
                    ConsoleLog("Start diff files...");
                    string diffMessage = string.Empty;
                    if (!CompareFiles(opt.OldPath, opt.NewPath, out diffMessage))
                    {
                        LogMessage(2, string.Format("================{0} have diff as following===========\r\n", opt.OldPath));
                        LogMessage(2, diffMessage);
                    }
                }

                if (File.Exists(_logFileFullName))
                {
                    string compareLog = File.ReadAllText(_logFileFullName);
                    if (!string.IsNullOrEmpty(compareLog))
                    {
                        ConsoleLog("======================Compare log======================.");
                        ConsoleLog(compareLog);
                        Environment.Exit(-1);
                    }
                }
                else
                {
                    ConsoleLog("No different.");
                    Environment.Exit(0);
                }
            }
            else
            {
                Environment.Exit(-1);
            }
        }

        #region Private
        private static void ComparePaths(string oldPath, string newPath)
        {
            if (!Directory.Exists(oldPath))
            {
                LogMessage(1, string.Format("'{0}' not exist", oldPath));
            }
            if (!Directory.Exists(newPath))
            {
                LogMessage(1, string.Format("'{0}' not exist", newPath));
            }

            var oldFileList = GetFiles(oldPath);
            var newFileList = GetFiles(newPath);

            int yaml1FileCount = 0;
            int yaml2FileCount = 0;

            yaml1FileCount = oldFileList == null ? 0 : oldFileList.Length;
            yaml2FileCount = newFileList == null ? 0 : newFileList.Length;

            // Different count
            if (yaml1FileCount != yaml2FileCount)
            {
                StringBuilder errorMessage = new StringBuilder();
                errorMessage.AppendLine(string.Format("{0} files under {1}", yaml1FileCount, oldPath));
                errorMessage.AppendLine(string.Format("{0} files under {1}", yaml2FileCount, newPath));

                if (oldFileList != null)
                {
                    oldFileList.ToList().ForEach(f =>
                    {
                        errorMessage.AppendLine(f.FullName);
                    });
                }
                if (newFileList != null)
                {
                    newFileList.ToList().ForEach(f =>
                    {
                        errorMessage.AppendLine(f.FullName);
                    });
                }

                LogMessage(1, errorMessage.ToString());
            }
            // newFileCount = oldFileCount = 0
            else if (yaml1FileCount == 0)
            {
                LogMessage(1, string.Format("no file under two paths\r\n{0}\r\n{1}", oldPath, newPath));
            }
            // newFileCount = oldFileCount != 0
            else
            {
                ConsoleLog(string.Format("Path1 have {0} files", oldFileList.Count()));
                ConsoleLog(string.Format("Path2 have {0} files", newFileList.Count()));

                //List<string> shortYaml1FileNameList = oldFileList.Select(f => f.FullName.Replace(oldPath, "")).ToList();
                //List<string> shortYaml2FileNameList = newFileList.Select(f => f.FullName.Replace(newPath, "")).ToList();
                List<string> shortYaml1FileNameList = oldFileList.Select(f => f.FullName.Replace(oldPath, "")).ToList();
                List<string> shortYaml2FileNameList = newFileList.Select(f => f.FullName.Replace(newPath, "")).ToList();

                var except = shortYaml2FileNameList.Except(shortYaml1FileNameList);
                var except1 = shortYaml1FileNameList.Except(shortYaml2FileNameList);
                // Have missing or additional file
                if (except != null && except.Count() != 0
                    || except1 != null && except1.Count() != 0)
                {
                    StringBuilder errorMessage = new StringBuilder();
                    errorMessage.AppendLine(string.Format("'{0}' Compare with '{1}'", newPath, oldPath));

                    if (except != null && except.Count() != 0)
                    {
                        errorMessage.AppendLine("Following files are additional");
                        except.ToList().ForEach(file =>
                        {
                            errorMessage.AppendLine(file);
                        });
                    }

                    if (except1 != null && except1.Count() != 0)
                    {
                        errorMessage.AppendLine("Following files are missing");
                        except1.ToList().ForEach(file =>
                        {
                            errorMessage.AppendLine(file);
                        });
                    }

                    LogMessage(1, errorMessage.ToString());
                }


                oldFileList.ToList().ForEach(file =>
                {
                    string file1FullPath = file.FullName;
                    string file2FullPath = newFileList.Where(p => p.FullName.Replace(newPath, oldPath) == file1FullPath).FirstOrDefault()?.FullName;

                    string diffMessage = string.Empty;
                    if (!CompareFiles(file1FullPath, file2FullPath, out diffMessage))
                    {
                        LogMessage(2, string.Format("================{0} have diff as following===========\r\n", file.FullName.Replace(oldPath, "")));
                        LogMessage(2, diffMessage);
                    }
                });
            }
        }

        /// <summary>
        /// Insert line, mark with "++++"
        /// delete line, mark with "----"
        /// </summary>
        /// <param name="oldFileFullPath"></param>
        /// <param name="newFileFullPath"></param>
        /// <param name="diffMessage"></param>
        /// <returns></returns>
        private static bool CompareFiles(string oldFileFullPath, string newFileFullPath, out string diffMessage)
        {
            StringBuilder diffSb = new StringBuilder();

            var diffBuilder = new InlineDiffBuilder(new Differ());
            string oldText = File.ReadAllText(oldFileFullPath);
            string newText = File.ReadAllText(newFileFullPath);

            var diff = diffBuilder.BuildDiffModel(oldText, newText);

            int lineCount = 1;
            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        if (line.Position.HasValue)
                        {
                            diffSb.AppendLine(string.Format("Line:{0} ++++", line.Position));
                        }
                        else
                        {
                            diffSb.AppendLine("++++");
                        }

                        diffSb.AppendLine(line.Text);
                        break;
                    case ChangeType.Deleted:
                        if (line.Position.HasValue)
                        {
                            diffSb.AppendLine(string.Format("Line:{0} ----", line.Position));
                        }
                        else
                        {
                            diffSb.AppendLine("----");
                        }
                        diffSb.AppendLine(line.Text);
                        break;
                    default:
                        break;
                }
                lineCount++;
            }
            diffMessage = diffSb.ToString();

            if (string.IsNullOrEmpty(diffMessage))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static FileInfo[] GetFiles(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            return di.GetFiles("*.*", SearchOption.AllDirectories);
        }

        private static void LogMessage(int errorType, string message)
        {
            File.AppendAllText(_logFileFullName, message);
            //ConsoleLog(message);
            if (errorType == 1)
            {
                Environment.Exit(-1);
            }
        }

        private static void ConsoleLog(string message)
        {
            Console.WriteLine(string.Format("[{0}]{1}", DateTime.Now, message));
        }

        private static string GetTestDataDir()
        {
            return Path.Combine(new System.IO.DirectoryInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Parent.Parent.FullName.TrimEnd('\\'), "test");
        }
        #endregion
    }
}
