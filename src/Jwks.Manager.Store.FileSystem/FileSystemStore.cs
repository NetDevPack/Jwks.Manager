using Jwks.Manager.Interfaces;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Jwks.Manager.Store.FileSystem
{
    public class FileSystemStore : IJsonWebKeyStore
    {
        private readonly IOptions<JwksOptions> _options;
        public DirectoryInfo KeysPath { get; }

        public FileSystemStore(DirectoryInfo keysPath, IOptions<JwksOptions> options)
        {
            _options = options;
            KeysPath = keysPath;
        }

        private string GetCurrentFile()
        {
            return Path.Combine(KeysPath.FullName, $"{_options.Value.KeyPrefix}current.key");
        }

        public void Save(SecurityKeyWithPrivate securityParamteres)
        {
            if (!KeysPath.Exists)
                KeysPath.Create();

            // Datetime it's just to be easy searchable.
            if (File.Exists(GetCurrentFile()))
                File.Copy(GetCurrentFile(), Path.Combine(Path.GetDirectoryName(GetCurrentFile()), $"{_options.Value.KeyPrefix}old-{DateTime.Now:yyyy-MM-dd}-{Guid.NewGuid()}.key"));

            File.WriteAllText(GetCurrentFile(), JsonConvert.SerializeObject(securityParamteres));
        }

        public bool NeedsUpdate()
        {
            return !File.Exists(GetCurrentFile()) || File.GetCreationTimeUtc(GetCurrentFile()).AddDays(_options.Value.DaysUntilExpire) < DateTime.UtcNow.Date;
        }

        public SecurityKeyWithPrivate GetCurrentKey()
        {
            return GetKey(GetCurrentFile());
        }

        private SecurityKeyWithPrivate GetKey(string file)
        {
            if (!File.Exists(file)) throw new FileNotFoundException("Check configuration - cannot find auth key file: " + file);
            var keyParams = JsonConvert.DeserializeObject<SecurityKeyWithPrivate>(File.ReadAllText(file));
            return keyParams;

        }

        public IReadOnlyCollection<SecurityKeyWithPrivate> Get(int quantity = 5)
        {
            return
                KeysPath.GetFiles("*.key")
                    .OrderByDescending(s => s.CreationTime)
                    .Take(quantity)
                    .Select(s => s.FullName)
                    .Select(GetKey).ToList().AsReadOnly();
        }

        public void Clear()
        {
            if (KeysPath.Exists)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                foreach (var fileInfo in KeysPath.GetFiles($"*.key"))
                {
                    fileInfo.Delete();
                }
            }
        }
    }


    public class InMemoryStore : IJsonWebKeyStore
    {
        private readonly IOptions<JwksOptions> _options;
        private List<SecurityKeyWithPrivate> _store;
        private SecurityKeyWithPrivate _current;

        public InMemoryStore(IOptions<JwksOptions> options)
        {
            _options = options;
            _store = new List<SecurityKeyWithPrivate>();
        }

        public void Save(SecurityKeyWithPrivate securityParamteres)
        {
            if (_current != null)
                _store.Add(_current);

            _current = securityParamteres;
        }

        public bool NeedsUpdate()
        {
            return _current.CreationDate.AddDays(_options.Value.DaysUntilExpire) < DateTime.UtcNow.Date;
        }

        public SecurityKeyWithPrivate GetCurrentKey()
        {
            return _current;
        }

        public IReadOnlyCollection<SecurityKeyWithPrivate> Get(int quantity = 5)
        {
            return
                _store
                    .OrderByDescending(s => s.CreationDate)
                    .Take(quantity).ToList().AsReadOnly();
        }

        public void Clear()
        {
            _store.Clear();
        }
    }


}
