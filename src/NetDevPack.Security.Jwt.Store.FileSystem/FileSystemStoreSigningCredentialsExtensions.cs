using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetDevPack.Security.Jwt.Interfaces;
using System.IO;

namespace NetDevPack.Security.Jwt.Store.FileSystem
{
    /// <summary>
    /// Builder extension methods for registering crypto services
    /// </summary>
    public static class FileSystemStoreSigningCredentialsExtensions
    {
        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <returns></returns>
        public static IJwksBuilder PersistKeysToFileSystem(this IJwksBuilder builder, DirectoryInfo directory)
        {

            builder.Services.AddScoped<IJsonWebKeyStore, FileSystemStore>(provider => new FileSystemStore(directory, provider.GetService<IOptions<JwksOptions>>(), provider.GetService<IMemoryCache>()));

            return builder;
        }
    }
}