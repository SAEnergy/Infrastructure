using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Util.Test
{
    [TestClass]
    public class TypeLoaderTest
    {
        [TestMethod]
        [Ignore]
        public void TypeLoaderGenericBaseTest()
        {
            // test generic class that inherits from generic
            // test concrete class that inhertis from generic
            // verity base class not returned
        }

        [TestMethod]
        [Ignore]
        public void TypeLoaderInheritedInterfaceTest()
        {
            // test interface that inherits from interface
            // test class that inerhits from interface
            // verify search interface is not returned
        }

        [TestMethod]
        [Ignore]
        public void TypeLoaderBaseClassTest()
        {
            // basic smoke test
            // verify searched class is not included in results
        }

        [TestMethod]
        [Ignore]
        public void TypeLoaderAbstractClassTest()
        {
            // verify abstract classes are not included in previous tests
        }


    }
}
