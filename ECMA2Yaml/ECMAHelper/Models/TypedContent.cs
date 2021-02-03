﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECMA2Yaml.Models
{
    [Serializable]
    public class TypedContent
    {
        public string CommentId { get; set; }
        public string Description { get; set; }
        public string Uid { get; set; }
    }
}
