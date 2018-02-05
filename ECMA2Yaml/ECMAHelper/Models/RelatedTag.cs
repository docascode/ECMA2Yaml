using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    public enum RelatedType
    {
        Sample,
        Specification,
        ExternalDocumentation,
        Article,
        Recipe
    }

    public class RelatedTag
    {
        public RelatedType Type { get; set; }

        public string Uri { get; set; }

        public string Text { get; set; }
    }
}
