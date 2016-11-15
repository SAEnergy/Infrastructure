using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces.Components.Logging;

namespace Core.Settings.Test
{
    public class ArgumentsMock : ArgumentsBase
    {
        [Argument]
        public bool BoolTest { get; set; }

        [Argument(Optional = true, Name = "SuperAwesomeBool")]
        public bool BoolNameOverrideTest { get; set; }

        [Argument(Optional = true, DefaultValue = true)]
        public bool BoolDefaultOverrideTest { get; set; }

        [Argument]
        public string StringTest { get; set; }

        [Argument(Optional = true, Name = "SuperAwesomeString")]
        public string StringNameOverrideTest { get; set; }

        [Argument(Optional = true, Delimiter = '#')]
        public string StringDelimiterOverrideTest { get; set; }

        [Argument(Optional = true, DefaultValue = "SuperAwesomeStringString")]
        public string StringDefaultOverrrideTest { get; set; }

        [Argument]
        public int IntTest { get; set; }

        [Argument(Optional = true, Name = "SuperAwesomeInt")]
        public int IntNameOverrideTest { get; set; }

        [Argument(Optional = true, Delimiter = '-')]
        public int IntDelimiterOverrrideTest { get; set; }

        [Argument(Optional = true, DefaultValue = 42)]
        public int IntDefaultOverrideTest { get; set; }

        // do not care about malformed arguments, and do not load automatically, test will inject argument string via Load method
        public ArgumentsMock(ILogger logger) : base(logger, false, false)
        {
        }
    }
}
