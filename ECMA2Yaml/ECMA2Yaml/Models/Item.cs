using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public abstract class ReflectionItem
    {
        private string _uid;
        public string Name { get; set; }
        public string Id { get; set; }
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
        public ReflectionItem Parent { get; set; }

        public abstract void BuildId(ECMAStore store);
    }
}
