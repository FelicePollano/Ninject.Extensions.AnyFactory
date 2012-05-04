//Copyright (C) 2012 Felice Pollano (felice@felicepollano.com)
//All rights reserved.

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this product except in compliance with the License.
//You may obtain a copy of the License at

//<http://www.apache.org/licenses/LICENSE-2.0>

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Ninject;
using Moq;
using Ninject.Parameters;
using NInject.Extensions.AnyFactory;

namespace Ninject.Extensions.AnyFactory.Tests
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

    public class DependsOnISomeFactory
    {
        ICustomFormatter created;
        public DependsOnISomeFactory(ISomeFactory dep)
        {
            created = dep.CreateOne();
        }
        public bool IsInitialized()
        {
            return created != null;
        }
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
        public void Check_ADependency_On_Any_Factory()
        {
            var kernel = new StandardKernel();
            kernel.Bind<ICustomFormatter>().ToConstant(new Mock<ICustomFormatter>().Object).Named("One");
            var obj = kernel.Get<DependsOnISomeFactory>();
            Assert.IsTrue(obj.IsInitialized());
        }
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
