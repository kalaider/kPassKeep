using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using kPassKeep.Model;

namespace kPassKeep.Service
{
    public abstract class SecurityProvider
    {
        public static SecurityProvider Latest => new SP();
        public static SecurityProvider V_1_1 => new SP_V_1_1();
        public static SecurityProvider V_1_0 => new SP_V_1_0();

        public static SecurityProvider ForVersion(Version version)
        {
            if (version < new Version(1, 0, 0))
            {
                throw new ArgumentException($"First version is 1.0.0, but {version} given");
            }

            if (version == new Version(1, 0, 0))
            {
                return V_1_0;
            }

            if (version == new Version(1, 1, 0))
            {
                return V_1_1;
            }

            return Latest;
        }

        public static bool Unlock(AccountGroup g, string password)
        {
            return ForVersion(g.RawAccountGroup.FormatVersion).UnlockGroup(g, password);
        }

        public abstract byte[] Hash(string password, byte[] salt);

        public abstract byte[] GetDataSalt(byte[] salt);

        public bool Match(string password, byte[] hash, byte[] salt)
        {
            if (hash == null && password == null)
            {
                return true;
            }
            if (hash == null || password == null)
            {
                return false;
            }
            return Enumerable.SequenceEqual(Hash(password, salt), hash);
        }

        public bool UnlockGroup(AccountGroup g, string password)
        {
            if (!g.IsLocked)
            {
                return true;
            }
            if (!Match(password, g.RawAccountGroup.PasswordHash, g.RawAccountGroup.Salt))
            {
                return false;
            }
            PersistenceProvider.LoadUnlocked(g,
                DecryptAll(g.RawAccountGroup.RawMembers.Values, password, g.RawAccountGroup.DataSalt));
            g.IsLocked = false;
            g.Password = password;
            g.ModificationTracker.ModifiedElements.Clear();
            return true;
        }

        public IEnumerable<RawSimpleEntity> EncryptAll(
            IEnumerable<RawSimpleEntity> entities, string password, byte[] salt)
        {
            if (String.IsNullOrEmpty(password))
            {
                foreach (var entity in entities)
                {
                    yield return entity;
                }
            }

            var key = Hash(password, GetDataSalt(salt));

            foreach (var entity in entities)
            {
                RawSimpleEntity e = new RawSimpleEntity();
                e.Guid = entity.Guid;
                using (var ms = new MemoryStream())
                {
                    Rijndael rijndael = Rijndael.Create();
                    rijndael.Key = key;
                    rijndael.GenerateIV();
                    using (var cs = new CryptoStream(ms, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(entity.Data, 0, entity.Data.Length);
                    }
                    var data = ms.ToArray();
                    e.Data = new byte[data.Length + 16];
                    Array.Copy(rijndael.IV, e.Data, 16);
                    Array.Copy(data, 0, e.Data, 16, data.Length);
                }
                yield return e;
            }
        }

        public IEnumerable<RawSimpleEntity> DecryptAll(
            IEnumerable<RawSimpleEntity> entities, string password, byte[] salt)
        {
            if (String.IsNullOrEmpty(password))
            {
                foreach (var entity in entities)
                {
                    yield return entity;
                }
            }

            var key = Hash(password, GetDataSalt(salt));

            foreach (var entity in entities)
            {
                RawSimpleEntity e = new RawSimpleEntity();
                e.Guid = entity.Guid;
                using (var ms = new MemoryStream(entity.Data))
                {
                    Rijndael rijndael = Rijndael.Create();
                    rijndael.Key = key;
                    byte[] iv = new byte[16];
                    ms.Read(iv, 0, 16);
                    rijndael.IV = iv;
                    var buffer = new byte[entity.Data.Length - 16];
                    using (var cs = new CryptoStream(ms, rijndael.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        var read = cs.Read(buffer, 0, buffer.Length);
                        e.Data = new byte[read];
                        Array.Copy(buffer, e.Data, read);
                    }
                }
                yield return e;
            }
        }

        private class SP_V_1_0 : SecurityProvider
        {
            private static readonly byte[] SALT = new byte[] { 0x16, 0xdd, 0xfc, 0x30, 0xaf, 0x1d, 0x7b, 0xec, 0x00, 0xfe, 0x07, 0x14, 0x4d, 0xc8, 0x52, 0xfa };
            public override byte[] Hash(string password, byte[] salt)
            {
                return SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
            }

            public override byte[] GetDataSalt(byte[] salt) => SALT;
        }

        private class SP_V_1_1 : SP_V_1_0
        {
            public override byte[] Hash(string password, byte[] salt)
            {
                return new Rfc2898DeriveBytes(password, salt).GetBytes(32);
            }
        }

        private class SP : SP_V_1_1
        {
            private static readonly int ITERATION_COUNT = 500_000;
            public override byte[] Hash(string password, byte[] salt)
            {
                return new Rfc2898DeriveBytes(password, salt, ITERATION_COUNT).GetBytes(32);
            }

            public override byte[] GetDataSalt(byte[] salt) => salt;
        }
    }
}
