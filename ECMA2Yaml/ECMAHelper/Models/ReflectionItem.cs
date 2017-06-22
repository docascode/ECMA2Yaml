using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public enum ItemType
    {
        Default = 0,
        Toc = 1,
        Assembly = 2,
        Namespace = 3,
        Class = 4,
        Interface = 5,
        Struct = 6,
        Delegate = 7,
        Enum = 8,
        Field = 9,
        Property = 10,
        Event = 11,
        Constructor = 12,
        Method = 13,
        Operator = 14
    }

    public abstract class ReflectionItem
    {
        private string _uid;
        public string Name { get; set; }
        public string Id { get; set; }
        public ItemType ItemType { get; set; }
        public string Uid
        {
            get
            {
                if (string.IsNullOrEmpty(_uid))
                {
                    _uid = Parent == null ? Id : (Parent.Uid + "." + Id);
                }
                return _uid;
            }
        }
        public string DocId { get; set; }
        public string CommentId {
            get
            {
                if (string.IsNullOrEmpty(Uid))
                {
                    return null;
                }
                var cid = Uid;
                if (cid.EndsWith("*"))
                {
                    return "Overload:" + cid.Trim('*');
                }
                if (!string.IsNullOrEmpty(DocId) && DocId.Contains(':'))
                {
                    cid = DocId.Substring(0, DocId.IndexOf(':')) + ":" + cid;
                }
                return cid;
            }
        }
        public ReflectionItem Parent { get; set; }

        public string SourceFileLocalPath { get; set; }

        public Dictionary<string, object> Metadata { get; set; }
        public SortedList<string, List<string>> Modifiers { get; set; }
        public List<AssemblyInfo> AssemblyInfo { get; set; }

        public ReflectionItem()
        {
            Metadata = new Dictionary<string, object>();
        }

        public abstract void Build(ECMAStore store);
    }
}
