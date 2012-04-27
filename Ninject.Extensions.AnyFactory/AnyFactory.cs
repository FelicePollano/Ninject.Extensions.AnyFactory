using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Ors.TimeSeries.Engine.Impl;
using Ninject;
using Moq;
using Ninject.Parameters;
using NInject.Extensions.AnyFactory;

namespace Ors.TimeSeries.Engine.Tests
{
    public interface ISomeFactory
    {
        ICustomFormatter Create();
        ICustomFormatter CreateOne();
        ICustomFormatter CreateAnother();
        IMyService Create(string s);
        IMyService Create(string s,double d); 
    }
    public interface ISomeNonProperFactory1
    {
        ICustomFormatter Create();
        ICustomFormatter Foo();
        ICustomFormatter CreateAnother();
        IMyService Create(string s);
        IMyService Create(string s, double d);
    }
    public interface ISomeNonProperFactory2
    {
        ICustomFormatter Create();
        string Name { get; set; }
        ICustomFormatter CreateAnother();
        IMyService Create(string s);
        IMyService Create(string s, double d);
    }
    public interface ISomeNonProperFactory3
    {
        void Create();
        ICustomFormatter CreateAnother();
        IMyService Create(string s);
        IMyService Create(string s, double d);
    }
   

    public interface IMyService
    {
 
    }

    class MyServiceImpl : IMyService
    {
        public string ConstructionString { get; set; }
        public double ConstructionDouble { get; set; }
        public MyServiceImpl(string s)
        {
            this.ConstructionString = s;
        }
        public MyServiceImpl(string s,double d)
        {
            this.ConstructionString = s;
            this.ConstructionDouble = d;
        }
    }
    [TestFixture]
    public class AnyFactory
    {
        [Test]
        public void Check_Any_Factory_With_Single_Method_No_Args()
        {
            var kernel = new StandardKernel();
            var service = new Mock<ICustomFormatter>().Object;
            kernel.Bind<ICustomFormatter>().ToConstant(service);
            var factory = ProxyFactory.CreateAnyFactory(typeof(ISomeFactory),kernel);
            Assert.AreSame(service,(factory as ISomeFactory).Create());
        }
        [Test]
        public void Check_Any_Factory_With_Named_Instances()
        {
            var kernel = new StandardKernel();
            var one = new Mock<ICustomFormatter>().Object;
            var another = new Mock<ICustomFormatter>().Object;
            kernel.Bind<ICustomFormatter>().ToConstant(one).Named("One");
            kernel.Bind<ICustomFormatter>().ToConstant(another).Named("Another");
            var factory = ProxyFactory.CreateAnyFactory(typeof(ISomeFactory), kernel);
            Assert.AreSame(one, (factory as ISomeFactory).CreateOne());
            Assert.AreSame(another, (factory as ISomeFactory).CreateAnother());
        }
        [Test]
        public void Check_Any_Factory_With_AnArgument_Reference()
        {
            var kernel = new StandardKernel();
            
            kernel.Bind<IMyService>().To<MyServiceImpl>();
            var factory = ProxyFactory.CreateAnyFactory(typeof(ISomeFactory), kernel);
            var obj = (factory as ISomeFactory).Create("Test") as MyServiceImpl;
            Assert.AreEqual("Test", obj.ConstructionString);
        }
        [Test]
        public void Check_Any_Factory_With_AnArgument_Reference_And_ValueType()
        {
            var kernel = new StandardKernel();

            kernel.Bind<IMyService>().To<MyServiceImpl>();
            var factory = ProxyFactory.CreateAnyFactory(typeof(ISomeFactory), kernel);
            var obj = (factory as ISomeFactory).Create("Test",0.56) as MyServiceImpl;
            Assert.AreEqual("Test", obj.ConstructionString);
            Assert.AreEqual(0.56, obj.ConstructionDouble);
        }
        [Test]
        public void Check_AnyFactory_Resolves_Proper_Candidate()
        {
            var f = new NInject.Extensions.AnyFactory.AnyFactory();
            Assert.IsTrue(f.CanActAsAFactory(typeof(ISomeFactory)));
        }
        [Test]
        public void Check_Any_Factory_DoesNot_Solve_AWrong_Candidate_1()
        {
            var f = new NInject.Extensions.AnyFactory.AnyFactory();
            Assert.IsFalse(f.CanActAsAFactory(typeof(ISomeNonProperFactory1)));
        }
        [Test]
        public void Check_Any_Factory_DoesNot_Solve_AWrong_Candidate_2()
        {
            var f = new NInject.Extensions.AnyFactory.AnyFactory();
            Assert.IsFalse(f.CanActAsAFactory(typeof(ISomeNonProperFactory2)));
        }
        [Test]
        public void Check_Any_Factory_DoesNot_Solve_AWrong_Candidate_3()
        {
            var f = new NInject.Extensions.AnyFactory.AnyFactory();
            Assert.IsFalse(f.CanActAsAFactory(typeof(ISomeNonProperFactory3)));
        }
    }
}
