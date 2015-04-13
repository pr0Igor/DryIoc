﻿using NUnit.Framework;

namespace DryIoc.UnitTests
{
    [TestFixture]
    public class ConstructionTests
    {
        [Test]
        public void Can_use_static_method_for_service_creation()
        {
            var container = new Container();
            container.Register<SomeService>(made: Made.Of(
                r => FactoryMethod.Of(r.ImplementationType.GetDeclaredMethodOrNull("Create"))));

            var service = container.Resolve<SomeService>();

            Assert.That(service.Message, Is.EqualTo("static"));
        }

        [Test]
        public void Can_use_any_type_static_method_for_service_creation()
        {
            var container = new Container();
            container.Register<IService>(made: typeof(ServiceFactory).GetDeclaredMethodOrNull("CreateService"));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("static"));
        }

        [Test]
        public void Can_use_any_type_static_method_for_service_creation_Refactoring_friendly()
        {
            var container = new Container();
            container.Register<IService>(made: Made.Of(() => ServiceFactory.CreateService()));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("static"));
        }

        [Test]
        public void Can_use_instance_method_for_service_creation()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.Register<IService>(made: FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"), 
                ServiceInfo.Of<ServiceFactory>()));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("instance"));
        }

        [Test]
        public void Can_use_instance_method_with_resolved_parameter()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.RegisterInstance("parameter");
            container.Register<IService>(made: FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create", typeof(string)), 
                ServiceInfo.Of<ServiceFactory>()));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("parameter"));
        }

        [Test]
        public void Can_specify_instance_method_without_strings()
        {
            var container = new Container();
            container.Register<ServiceFactory>();
            container.RegisterInstance("parameter");
            container.Register<IService>(made: Made.Of(
                r => ServiceInfo.Of<ServiceFactory>(), 
                f => f.Create(default(string))));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("parameter"));
        }

        [Test]
        public void Can_get_factory_registered_with_key()
        {
            var container = new Container();

            container.Register<ServiceFactory>(serviceKey: "factory");
            container.RegisterInstance("parameter");

            container.Register<IService>(made: Made.Of(
                r => ServiceInfo.Of<ServiceFactory>(serviceKey: "factory"),
                f => f.Create(default(string))));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("parameter"));
        }

        [Test]
        public void Can_get_factory_registered_with_key_and_specify_factory_method_parameter_with_key()
        {
            var container = new Container();

            container.Register<ServiceFactory>(serviceKey: "factory");
            container.RegisterInstance("XXX", serviceKey: "myKey");

            container.Register<IService>(made: Made.Of(
                r => ServiceInfo.Of<ServiceFactory>(serviceKey: "factory"),
                f => f.Create(Arg.Of<string>("myKey"))));

            var service = container.Resolve<IService>();

            Assert.That(service.Message, Is.EqualTo("XXX"));
        }

        [Test]
        public void Should_throw_if_instance_factory_unresolved()
        {
            var container = new Container();

            container.Register<IService, SomeService>(made: Made.Of(FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"), 
                ServiceInfo.Of<ServiceFactory>())));

            var ex = Assert.Throws<ContainerException>(() =>
                container.Resolve<IService>());

            Assert.AreEqual(ex.Error, Error.UNABLE_TO_RESOLVE_SERVICE);
            Assert.That(ex.Message, Is.StringContaining("Unable to resolve"));
        }

        [Test]
        public void Should_throw_for_instance_method_without_factory()
        {
            var container = new Container();
            container.Register<IService>(made: typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"));

            var ex = Assert.Throws<ContainerException>(() => 
                container.Resolve<IService>());

            Assert.AreEqual(ex.Error, Error.FACTORY_OBJ_IS_NULL_IN_FACTORY_METHOD);
            Assert.That(ex.Message, Is.StringContaining("Unable to use null factory object with factory method"));
        }

        [Test]
        public void Should_return_null_if_instance_factory_is_not_resolved_on_TryResolve()
        {
            var container = new Container();
            container.Register<IService>(made: FactoryMethod.Of(
                typeof(ServiceFactory).GetDeclaredMethodOrNull("Create"), 
                ServiceInfo.Of<ServiceFactory>()));

            var service = container.Resolve<IService>(IfUnresolved.ReturnDefault);

            Assert.That(service, Is.Null);
        }

        [Test]
        public void What_if_factory_method_returned_incompatible_type()
        {
            var container = new Container();
            container.Register<SomeService>(made: typeof(BadFactory).GetDeclaredMethodOrNull("Create"));

            var ex = Assert.Throws<ContainerException>(() =>
                container.Resolve<SomeService>());

            Assert.AreEqual(ex.Error, Error.SERVICE_IS_NOT_ASSIGNABLE_FROM_FACTORY_METHOD);
            Assert.That(ex.Message, Is.StringContaining("SomeService is not assignable from factory method"));
        }

        [Test]
        public void It_is_fine_to_have_static_ctor()
        {
            var container = new Container();
            container.Register<WithStaticCtor>();

            var service = container.Resolve<WithStaticCtor>();

            Assert.NotNull(service);
        }

        #region CUT

        internal class WithStaticCtor
        {
            public static int Hey;
            static WithStaticCtor()
            {
                ++Hey;
            }

            public WithStaticCtor()
            {
                --Hey;
            }
        }

        internal interface IService 
        {
            string Message { get; }
        }

        internal class SomeService : IService
        {
            public string Message { get; private set; }

            internal SomeService(string message)
            {
                Message = message;
            }

            public static SomeService Create()
            {
                return new SomeService("static");
            }
        }

        internal class ServiceFactory
        {
            public static IService CreateService()
            {
                return new SomeService("static");
            }

            public IService Create()
            {
                return new SomeService("instance");
            }

            public IService Create(string parameter)
            {
                return new SomeService(parameter);
            }
        }

        internal class PropertyBasedFactory
        {
            public string Message { get; set; }

            public IService Create()
            {
                return new SomeService(Message);
            }
        }

        internal class BadFactory
        {
            public static string Create()
            {
                return "bad";
            }
        }

        internal class Generic<T>
        {
            public T X { get; private set; }

            public Generic(T x)
            {
                X = x;
            }
        }

        #endregion
    }
}
