using System;
using Mochi.ServiceLocator;
using NUnit.Framework;

namespace Mochi.Tests
{
    public class ServiceProviderTests
    {
        private class KeyboardService
        {
        }

        private class AudioService
        {
        }

        private class UnregisteredService
        {
        }

        // 验收条件 AC1A:注册某类型服务后再获取该类型，返回注册时的同一实例。
        [Test]
        public void ServiceProvider_Register_ThenGet_ReturnsRegisteredInstance()
        {
            ServiceProvider provider = new ServiceProvider();
            KeyboardService service = new KeyboardService();
            provider.Register(service);

            Assert.That(provider.Get<KeyboardService>(), Is.SameAs(service));
        }

        // 验收条件 AC2A:获取未注册的类型，抛出包含该类型名的异常，不返回 null。
        [Test]
        public void ServiceProvider_Get_UnregisteredType_ThrowsWithName()
        {
            ServiceProvider provider = new ServiceProvider();

            Assert.That(
                () => provider.Get<UnregisteredService>(),
                Throws.Exception.With.Message.Contains(
                    typeof(UnregisteredService).FullName));
        }

        // 验收条件 AC3A:同一类型被重复注册，抛出异常拒绝覆盖。
        [Test]
        public void ServiceProvider_Register_DuplicateType_Throws()
        {
            ServiceProvider provider = new ServiceProvider();
            KeyboardService first = new KeyboardService();
            provider.Register(first);

            Assert.That(
                () => provider.Register(new KeyboardService()),
                Throws.Exception.With.Message.Contains(
                    typeof(KeyboardService).FullName));
            Assert.That(provider.Get<KeyboardService>(), Is.SameAs(first));
        }

        // 验收条件 AC4A:两个容器实例各自注册不同服务时，任一实例获取不到对方注册的服务。
        [Test]
        public void ServiceProvider_TwoContainers_DoNotShareServices()
        {
            ServiceProvider first = new ServiceProvider();
            ServiceProvider second = new ServiceProvider();
            first.Register(new KeyboardService());
            second.Register(new AudioService());

            Assert.That(() => first.Get<AudioService>(), Throws.Exception);
            Assert.That(() => second.Get<KeyboardService>(), Throws.Exception);
        }
    }
}
