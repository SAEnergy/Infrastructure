using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.Util.Test
{
    [TestClass]
    public class TypeLocatorTest
    {
        [TestMethod]
        public void TypeLocatorGenericBaseTest()
        {
            List<Type> types = TypeLocator.SearchAssembly(Assembly.GetExecutingAssembly(), typeof(TestInheritInterface<>));

            Assert.AreEqual(2, types.Count);
            Assert.IsTrue(types.Contains(typeof(TestConcreteInheritClass)));
            Assert.IsTrue(types.Contains(typeof(TestConcreteGenericClass)));
        }

        [TestMethod]
        public void TypeLocatorInheritedInterfaceTest()
        {
            List<Type> types = TypeLocator.SearchAssembly(Assembly.GetExecutingAssembly(), typeof(TestBaseInterface));

            Assert.AreEqual(4, types.Count);
            Assert.IsTrue(types.Contains(typeof(TestConcreteClass)));
            Assert.IsTrue(types.Contains(typeof(TestConcreterClass)));
            Assert.IsTrue(types.Contains(typeof(TestConcreteInheritClass)));
            Assert.IsTrue(types.Contains(typeof(TestConcreteGenericClass)));
        }

        [TestMethod]
        public void TypeLocatorBaseClassTest()
        {
            List<Type> types = TypeLocator.SearchAssembly(Assembly.GetExecutingAssembly(), typeof(TestConcreteClass));

            Assert.AreEqual(1, types.Count);
            Assert.IsTrue(types.Contains(typeof(TestConcreterClass)));
        }
    }


    public interface TestBaseInterface
    {
        string Hello { get; set; }
    }

    public interface TestInheritInterface<T> : TestBaseInterface
    {
        T HI2U { get; set; }
    }

    public abstract class TestBaseClass : TestBaseInterface
    {
        public virtual string Hello { get; set; }
    }

    public class TestConcreteClass : TestBaseClass { }

    public class TestConcreterClass : TestConcreteClass { }

    public abstract class TestInheritClass<T> : TestBaseClass, TestInheritInterface<T>
    {
        public virtual T HI2U { get; set; }
    }

    public class TestConcreteInheritClass : TestInheritClass<int> { }

    public class TestConcreteGenericClass : TestInheritInterface<int>
    {
        public string Hello { get; set; }

        public int HI2U { get; set; }
    }
}
