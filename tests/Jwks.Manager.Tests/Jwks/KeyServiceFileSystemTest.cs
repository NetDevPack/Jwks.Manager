using FluentAssertions;
using Jwks.Manager.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Jwks.Manager.Tests.Jwks
{
    public class KeyServiceFileSystemTest : IClassFixture<WarmupFileStore>
    {
        private readonly IJsonWebKeySetService _keyService;
        private readonly IJsonWebKeyStore _jsonWebKeyStore;
        public WarmupFileStore FileStoreWarmupData { get; }
        public KeyServiceFileSystemTest(WarmupFileStore fileStoreWarmup)
        {
            FileStoreWarmupData = fileStoreWarmup;
            _keyService = FileStoreWarmupData.Services.GetRequiredService<IJsonWebKeySetService>();
            _jsonWebKeyStore = FileStoreWarmupData.Services.GetRequiredService<IJsonWebKeyStore>();

        }

        [Fact]
        public void ShouldSaveCryptoInDatabase()
        {
            _keyService.GetCurrent();

            _keyService.GetLastKeysCredentials(5).Count.Should().BePositive();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(2)]
        [InlineData(6)]
        public void ShouldGenerateManyRsa(int qty)
        {
            _jsonWebKeyStore.Clear();
            var keysGenerated = new List<SigningCredentials>();
            for (int i = 0; i < qty; i++)
            {
                var sign = _keyService.Generate();
                keysGenerated.Add(sign);
            }

            var current = _keyService.GetLastKeysCredentials(qty * 2);
            foreach (var securityKey in current)
            {
                keysGenerated.Select(s => s.Key.KeyId).Should().Contain(securityKey.KeyId);
            }
        }


        [Fact]
        public void ShouldSaveCryptoAndRecover()
        {
            FileStoreWarmupData.Services.GetRequiredService<IOptions<JwksOptions>>();
            var newKey = _keyService.GetCurrent();

            _keyService.GetLastKeysCredentials(5).Count.Should().BePositive();

            var currentKey = _keyService.GetCurrent();
            newKey.Kid.Should().Be(currentKey.Kid);
        }

    }
}