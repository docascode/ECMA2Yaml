using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml
{
    public class FolderDiff
    {
        const string OP_Add = "Created";
        const string OP_Update = "Updated";
        const string OP_Delete = "Deleted";

        public static void Run(CommandLineOptions opt)
        {
            if (!Directory.Exists(opt.FolderToDiff) || !File.Exists(opt.ChangeListPath))
            {
                return;
            }

            Dictionary<string, string> md5Cache = LoadMD5Cache(opt.CacheFilePath) ?? new Dictionary<string, string>();
            Dictionary<string, string> newMd5Cache = new Dictionary<string, string>();
            Dictionary<string, string> diff = new Dictionary<string, string>();

            //add repo root to md5Cache key so that we can compare
            if (!string.IsNullOrEmpty(opt.RepoRootPath))
            {
                md5Cache = md5Cache.ToDictionary(p => Path.Combine(opt.RepoRootPath, p.Key.TrimStart('\\')), p => p.Value);
            }
            
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                foreach (var file in Directory.EnumerateFiles(opt.FolderToDiff, "*.*", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllBytes(file);
                    if (content != null)
                    {
                        byte[] hashBytes = md5.ComputeHash(content);
                        var hash = BitConverter.ToString(hashBytes);
                        if (!md5Cache.ContainsKey(file))
                        {
                            diff.Add(file, OP_Add);
                        }
                        else if (hash != md5Cache[file])
                        {
                            diff.Add(file, OP_Update);
                        }
                        newMd5Cache.Add(file, hash);
                    }
                }
            }

            foreach(var oldPair in md5Cache)
            {
                if (!newMd5Cache.ContainsKey(oldPair.Key))
                {
                    diff.Add(oldPair.Key, OP_Delete);
                }
            }

            //remove repo root from new md5Cache key to save to disk
            if (!string.IsNullOrEmpty(opt.RepoRootPath))
            {
                newMd5Cache = newMd5Cache.ToDictionary(p => p.Key.Replace(opt.RepoRootPath, ""), p => p.Value);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(opt.CacheFilePath));
            File.WriteAllText(opt.CacheFilePath, JsonConvert.SerializeObject(newMd5Cache, Formatting.Indented));

            SaveToChangeList(opt.ChangeListPath, diff);
        }

        private static Dictionary<string, string> LoadMD5Cache(string path)
        {
            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            }
            return null;
        }

        private static void SaveToChangeList(string changeListFile, Dictionary<string, string> diff)
        {
            if (diff != null && diff.Count > 0)
            {
                var lines = File.ReadAllLines(changeListFile);
                Dictionary<string, string> changeList = new Dictionary<string, string>();
                foreach(var l in lines)
                {
                    if (!string.IsNullOrWhiteSpace(l))
                    {
                        var parts = l.Split('\t');
                        var file = parts[0].Replace("/", "\\");
                        changeList.Add(file, parts[1]);
                    }
                }
                foreach(var fileDiff in diff)
                {
                    if (!changeList.ContainsKey(fileDiff.Key))
                    {
                        changeList.Add(fileDiff.Key, fileDiff.Value);
                    }
                }
                StringBuilder sb = new StringBuilder();
                foreach(var change in changeList)
                {
                    sb.AppendLine(string.Format("{0}\t{1}", change.Key, change.Value));
                }
                File.WriteAllText(changeListFile, sb.ToString());
            }
        }
    }
}
