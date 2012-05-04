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
using Ninject.Modules;
using Ninject.Planning.Bindings.Resolvers;
using Ninject.Planning.Bindings;
using Ninject;
using Ninject.Activation.Providers;
using System.Reflection.Emit;
using System.Reflection;
using Ninject.Parameters;
using Ninject.Activation;

namespace NInject.Extensions.AnyFactory
{
    public class AnyFactoryModule:NinjectModule
    {
        public override void Load()
        {
            Kernel.Components.Add<IMissingBindingResolver,AnyFactory>();
        }
        
    }
    class AnyFactory : IMissingBindingResolver
    {
        #region IMissingBindingResolver Members

        public IEnumerable<IBinding> Resolve(Ninject.Infrastructure.Multimap<Type, Ninject.Planning.Bindings.IBinding> bindings, Ninject.Activation.IRequest request)
        {
            if (CanActAsAFactory(request.Service))
            {
                var binding = new Binding(request.Service)
                {
                    ProviderCallback = (k) => GetProvider(k)
                };

                
                return new[] { binding};
            }
            else
                return Enumerable.Empty<IBinding>();
        }

        internal bool CanActAsAFactory(Type type)
        {
            return type.IsInterface
                && AllMethodsReturns(type)
                && AllMethodsBeginsWith(type,"Create")
                && type.GetProperties().Length == 0;
                ;
        }

        private bool AllMethodsBeginsWith(Type t,string p)
        {
            return t.GetMethods().All(m => m.Name.StartsWith(p));
        }

        private bool AllMethodsReturns(Type type)
        {
            return type.GetMethods().All(m => m.ReturnType!=typeof(void));
        }

        private IProvider GetProvider(Ninject.Activation.IContext k)
        {
            return new InternalFactoryProvider(k.Kernel,k.Request.Service);
        }

        #endregion

        #region INinjectComponent Members

        public INinjectSettings Settings
        {
            get;
            set;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            
        }

        #endregion
    }
    class InternalFactoryProvider : IProvider
    {
        readonly IKernel kernel;
        readonly Type svc;
        public InternalFactoryProvider(IKernel kernel,Type svc)
        {
            this.kernel = kernel;
            this.svc = svc;
        }
        #region IProvider Members

        public object Create(IContext context)
        {
            return ProxyFactory.CreateAnyFactory(context.Request.Service, kernel);
        }

        public Type Type
        {
            get { return svc; }
        }

        #endregion
    }
    public interface IInternalFactory
    {
        object Create(Type ret,string name, params KeyValuePair<string,object>[] args);
    }
    
    class InternalFactory : IInternalFactory
    {
        IKernel kernel;
        public InternalFactory(IKernel k)
        {
            this.kernel = k;
        }
        public object Create(Type ret,string name,params KeyValuePair<string,object>[] args)
        {
            return kernel.Get(ret,GetKey(name), ToParameters(args));
        }

        private string GetKey(string name)
        {
            if (name.StartsWith("Create"))
            {
                var res = name.Substring(6);
                if (res == string.Empty)
                    return null;
                else
                    return res;
            }
            else
                return null;
        }

        private IParameter[] ToParameters(KeyValuePair<string, object>[] args)
        {
            return args.Select(a => new ConstructorArgument(a.Key, a.Value)).ToArray();
        }
    }
    class ProxyFactory
    {
        static AssemblyBuilder assemblyBuilder;
        static ModuleBuilder moduleBuilder;
        static Dictionary<Type, Type> cache = new Dictionary<Type, Type>();
        static ProxyFactory()
        {
            AssemblyName name = new AssemblyName("anyfactory");
            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("main");
        }


        public static object CreateAnyFactory(Type t,IKernel kernel)
        {
            var tFact = CreateProxyType(t);
            return Activator.CreateInstance(tFact, new object[] { new InternalFactory(kernel) });
        }

        static Type CreateProxyType<T>()
        {
            Type parent = typeof(T);
            return CreateProxyType(parent);
        }

        static Type CreateProxyType(Type parent)
        {
            lock (cache)
            {
                if (cache.ContainsKey(parent))
                {
                    return cache[parent];
                }

                var tbuilder = moduleBuilder.DefineType(parent.FullName + "__proxy", TypeAttributes.Class | TypeAttributes.Public,typeof(object),new[]{ parent} );
                //backing fields
                var factoryField = tbuilder.DefineField("factory", typeof(IInternalFactory), FieldAttributes.Private);


                MethodAttributes interfaceImpl = MethodAttributes.Public |
                    MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;

                foreach (var method in parent.GetMethods())
                {
                    var impl = tbuilder.DefineMethod(method.Name, interfaceImpl
                                                    ,method.ReturnType
                                                    ,method.GetParameters().Select(t=>t.ParameterType).ToArray());
                    var parameters = method.GetParameters();
                    var mgen = impl.GetILGenerator();
                    
                   
                    mgen.DeclareLocal(typeof(KeyValuePair<string,object>[]));
                    mgen.Emit(OpCodes.Ldarg_0);
                    mgen.Emit(OpCodes.Ldfld, factoryField);
                    mgen.Emit(OpCodes.Ldtoken, method.ReturnType);
                    
                    mgen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                    mgen.Emit(OpCodes.Ldstr, method.Name);
                    mgen.Emit(OpCodes.Ldc_I4,parameters.Length);
                    mgen.Emit(OpCodes.Newarr, typeof(KeyValuePair<string, object>));
                    mgen.Emit(OpCodes.Stloc_0);

                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        EmitAddInArray(mgen,parameters[i],i);
                    }


                    mgen.Emit(OpCodes.Ldloc_0);
                    mgen.Emit(OpCodes.Callvirt, typeof(IInternalFactory).GetMethod("Create"));
                    mgen.Emit(OpCodes.Castclass, method.ReturnType);
                    mgen.Emit(OpCodes.Ret);
                }

                //Ctor
                var ctor = tbuilder.DefineConstructor(MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(IInternalFactory) });
                var gen = ctor.GetILGenerator();
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Stfld, factoryField);
                gen.Emit(OpCodes.Ret);

                var tt = tbuilder.CreateType();
                cache[parent] = tt;
            }
            return cache[parent];
        }

        private static void EmitAddInArray(ILGenerator mgen,ParameterInfo p,int ordinal)
        {
            mgen.Emit(OpCodes.Ldloc_0); // load back array
            mgen.Emit(OpCodes.Ldc_I4, ordinal); // at address ordinal
            mgen.Emit(OpCodes.Ldelema, typeof(KeyValuePair<string, object>));
            mgen.Emit(OpCodes.Ldstr, p.Name);
            mgen.Emit(OpCodes.Ldarg, ordinal+1);

            if (p.ParameterType.IsValueType)
                mgen.Emit(OpCodes.Box, p.ParameterType);

            mgen.Emit(OpCodes.Newobj, typeof(KeyValuePair<string, object>).GetConstructor(new []{typeof(string),typeof(object)}));
            mgen.Emit(OpCodes.Stobj, typeof(KeyValuePair<string, object>));// put obj into array
        }

    }

}
