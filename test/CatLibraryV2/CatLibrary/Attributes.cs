using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatLibrary
{
    [AttributeUsage(AttributeTargets.All)]
    public class DocAttribute : Attribute
    {
        /// <remarks><c>C:Mono.DocTest.DocAttribute(System.String)</c></remarks>
        public DocAttribute(string docs)
        {
            if (docs == null)
                throw new ArgumentNullException("docs");
        }
    }
}
