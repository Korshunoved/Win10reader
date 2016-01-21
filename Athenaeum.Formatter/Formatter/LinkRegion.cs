using System;
using System.Collections;
using System.ComponentModel;

namespace Athenaeum.Formatter
{
    public class LinkRegion : Bounded
    {
        public string LinkTarget { get; set; }
        public LinkRegion(XRect bounds, string linkTarget)
        {
            Bounds = bounds;
            LinkTarget = linkTarget;
        }
    }
}