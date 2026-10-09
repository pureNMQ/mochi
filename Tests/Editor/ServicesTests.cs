using System;
using Mochi.ServiceLocator;
using NUnit.Framework;

namespace Mochi.Tests
{
    public class ServicesTests
    {
        private class KeyboardService
        {
        }

        private class UnregisteredService
        {
        }

        // 验收条件 AC5A:Services.Root 指向某容器后经其取服务，结果与直接从该容器实例获取一致。
        [Test]
        public void Services_Root_Get_ReturnsSameAsDirectContainerGet()
        {
            ServiceProvider provider = new ServiceProvider();
            KeyboardService service = new KeyboardService();
            provider.Register(service);

            try
            {
                Services.Root = provider;

                Assert.That(Services.Root.Get<KeyboardService>(), Is.SameAs(service));
                Assert.That(
                    Services.Root.Get<KeyboardService>(),
                    Is.SameAs(provider.Get<KeyboardService>()));
            }
            finally
            {
                Services.Root = null;
            }
        }

        // 验收条件 AC6A:Services.Root 未被赋值时，容器实例自身的注册与获取行为不受影响。
        [Test]
        public void Services_RootUnassigned_ContainerRegisterAndGet_Unaffected()
        {
            ServiceProvider previousRoot = Services.Root;
            Services.Root = null;

            try
            {
                ServiceProvider provider = new ServiceProvider();
                KeyboardService service = new KeyboardService();
                provider.Register(service);

                Assert.That(Services.Root, Is.Null);
                Assert.That(provider.Get<KeyboardService>(), Is.SameAs(service));
                Assert.That(
                    () => provider.Get<UnregisteredService>(),
                    Throws.Exception.With.Message.Contains(
                        typeof(UnregisteredService).FullName));
            }
            finally
            {
                Services.Root = previousRoot;
            }
        }
    }
}
