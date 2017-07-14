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
    public static class SecurityProvider
    {
        private static readonly byte[] SALT = new byte[] { 0x16, 0xdd, 0xfc, 0x30, 0xaf, 0x1d, 0x7b, 0xec, 0x00, 0xfe, 0x07, 0x14, 0x4d, 0xc8, 0x52, 0xfa };

        public static byte[] Hash(string password)
        {
            return SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
        }
        public static bool Match(string password, byte[] hash)
        {
            if (hash == null && password == null)
            {
                return true;
            }
            if (hash == null || password == null)
            {
                return false;
            }
            return Enumerable.SequenceEqual(Hash(password), hash);
        }

        public static bool Unlock(AccountGroup g, string password)
        {
            if (!g.IsLocked)
            {
                return true;
            }
            if (!Match(password, g.RawAccountGroup.Data))
            {
                return false;
            }
            PersistenceProvider.LoadUnlocked(g,
                g.RawAccountGroup.RawMembers.Select(e => Decrypt(e.Value, password)));
            g.IsLocked = false;
            g.Password = password;
            g.ModificationTracker.ModifiedElements.Clear();
            return true;
        }

        public static RawSimpleEntity Encrypt(RawSimpleEntity entity, string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                return entity;
            }
            using (var ms = new MemoryStream())
            {
                Rijndael rijndael = Rijndael.Create();
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, SALT);
                rijndael.Key = pdb.GetBytes(32);
                rijndael.GenerateIV();
                using (var cs = new CryptoStream(ms, rijndael.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(entity.Data, 0, entity.Data.Length);
                }
                var data = ms.ToArray();
                entity.Data = new byte[data.Length + 16];
                Array.Copy(rijndael.IV, entity.Data, 16);
                Array.Copy(data, 0, entity.Data, 16, data.Length);
            }
            return entity;
        }

        public static RawSimpleEntity Decrypt(RawSimpleEntity entity, string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                return entity;
            }
            RawSimpleEntity e = new RawSimpleEntity();
            e.Guid = entity.Guid;
            using (var ms = new MemoryStream(entity.Data))
            {
                Rijndael rijndael = Rijndael.Create();
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, SALT);
                rijndael.Key = pdb.GetBytes(32);
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
            return e;
        }
    }
}
