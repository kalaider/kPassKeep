using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using kPassKeep.Model;

namespace kPassKeep.Service
{

    public static class PersistenceProvider
    {

        public static readonly Version LatestFormat = new Version(1, 1, 0);

        public static AccountGroups Load()
        {
            var groups = new AccountGroups();
            if (!Directory.Exists("data"))
            {
                return groups;
            }

            foreach (var path in Directory.EnumerateFiles("data"))
            {
                AccountGroup g = Load(path);
                groups.Groups.Add(g);
            }

            return groups;
        }

        public static void Persist(AccountGroups groups)
        {
            foreach (var group in groups.Groups)
            {
                Persist(group);
            }

            var guids = groups.Groups.Select(g => g.Guid);
            var files = Directory.EnumerateFiles("data");
            var toDelete = files
                .Where(f => !guids.Contains(Guid.Parse(Path.GetFileName(f))));
            foreach (var f in toDelete)
            {
                File.Delete(f);
            }
        }

        private static void Persist(AccountGroup group)
        {
            if (group.IsLocked)
            {
                return;
            }
            var modifiedMembers = group.ModificationTracker.ModifiedElements;
            var path = "data\\" + group.Guid.ToString();
            var rawMembers = group.RawAccountGroup.RawMembers;
            var rawMemberGuids = group.RawAccountGroup.RawMembers.Keys;
            var matchAll = !SecurityProvider.Match(group.Password, group.RawAccountGroup.Data);
            var serializedMembers =
                group.Logins
                     .Where(l => matchAll
                              || modifiedMembers.Contains(l.Guid)
                              || !rawMembers.ContainsKey(l.Guid))
                     .Select(Persist)
                     .Concat
            (
                group.Accounts
                     .Select(a => a.Target)
                     .Where(t => t != null)
                     .Distinct()
                     .Where(t => matchAll
                              || modifiedMembers.Contains(t.Guid)
                              || !rawMembers.ContainsKey(t.Guid))
                     .Select(Persist)
            )
                    .Concat
            (
                group.Accounts
                     .Where(a => matchAll
                              || modifiedMembers.Contains(a.Guid)
                              || !rawMembers.ContainsKey(a.Guid))
                     .Select(Persist)
            )
                    .Select(e => SecurityProvider.Encrypt(e, group.Password));

            group.RawAccountGroup.FormatVersion = LatestFormat;

            if (!String.IsNullOrEmpty(group.Password))
            {
                group.RawAccountGroup.Data = SecurityProvider.Hash(group.Password);
            }
            else
            {
                group.RawAccountGroup.Data = null;
            }
            foreach (var e in serializedMembers)
            {
                group.RawAccountGroup.RawMembers[e.Guid] = e;
            }
            
            var guids =
                group.Logins
                     .Select(l => l.Guid)
                     .Concat
            (
                group.Accounts
                     .Select(a => a.Target)
                     .Where(t => t != null)
                     .Select(t => t.Guid)
            )
                    .Concat
            (
                group.Accounts
                     .Select(a => a.Guid)
            ).ToList();

            var remove = group.RawAccountGroup.RawMembers.Keys.Except(guids).ToArray();

            foreach (var e in remove)
            {
                group.RawAccountGroup.RawMembers.Remove(e);
            }

            SerializableGroup ht = new SerializableGroup();
            ht.name = group.Name;
            ht.descr = group.Description;
            ht.pass = group.RawAccountGroup.Data;
            ht.version = group.RawAccountGroup.FormatVersion.ToString();
            ht.members = group.RawAccountGroup.RawMembers.Values.ToArray();

            var json = JsonConvert.SerializeObject(ht, Formatting.Indented);

            System.IO.Directory.CreateDirectory("data");
            File.WriteAllText(path, json, Encoding.UTF8);

            group.ModificationTracker.ModifiedElements.Clear();
        }

        private static RawSimpleEntity Persist(Target target)
        {
            var ht = new SerializableEntity();
            ht.type = "target";
            ht.title = target.Title;
            ht.descr = target.Description;
            ht.uri = target.Uri;
            if (target.Icon != null)
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(CreateResizedImage(target.Icon, 16, 16));
                using (var s = new System.IO.MemoryStream())
                {
                    enc.Save(s);
                    s.Flush();
                    ht.icon = s.ToArray();
                }
            }
            RawSimpleEntity e = new RawSimpleEntity();
            e.Guid = target.Guid;
            var json = JsonConvert.SerializeObject(ht);
            e.Data = Encoding.UTF8.GetBytes(json);
            return e;
        }

        private static RawSimpleEntity Persist(Login target)
        {
            var ht = new SerializableEntity();
            ht.type = "login";
            ht.user = target.Username;
            ht.descr = target.Description;
            RawSimpleEntity e = new RawSimpleEntity();
            e.Guid = target.Guid;
            var json = JsonConvert.SerializeObject(ht);
            e.Data = Encoding.UTF8.GetBytes(json);
            return e;
        }

        private static RawSimpleEntity Persist(Account target)
        {
            var ht = new SerializableEntity();
            ht.type = "account";
            ht.descr = target.Description;
            ht.pass = target.Password;
            if (target.Target != null)
            {
                ht.target = target.Target.Guid;
            }
            if (target.Login != null)
            {
                ht.login = target.Login.Guid;
            }
            RawSimpleEntity e = new RawSimpleEntity();
            e.Guid = target.Guid;
            var json = JsonConvert.SerializeObject(ht);
            e.Data = Encoding.UTF8.GetBytes(json);
            return e;
        }

        private static BitmapFrame CreateResizedImage(ImageSource source, int width, int height)
        {
            var rect = new Rect(0, 0, width, height);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(source, rect));

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
                drawingContext.DrawDrawing(group);

            var resizedImage = new RenderTargetBitmap(
                width, height,         // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resizedImage.Render(drawingVisual);

            return BitmapFrame.Create(resizedImage);
        }

        private static AccountGroup Load(string path)
        {
            AccountGroup g = new AccountGroup();
            g.Guid = Guid.Parse(Path.GetFileName(path));
            g.RawAccountGroup.Guid = g.Guid;
            var ht = (SerializableGroup) JsonConvert.DeserializeObject(File.ReadAllText(path), typeof(SerializableGroup));
            g.Name = ht.name;
            g.Description = ht.descr;
            if (ht.version != null)
            {
                g.RawAccountGroup.FormatVersion = Version.Parse(ht.version);
            }
            else
            {
                g.RawAccountGroup.FormatVersion = new Version(1, 0, 0);
            }
            foreach (var e in ht.members)
            {
                g.RawAccountGroup.RawMembers[e.Guid] = e;
            }
            if (ht.pass != null)
            {
                g.IsLocked = true;
                g.RawAccountGroup.Data = ht.pass;
            }
            else
            {
                g.IsLocked = false;
                LoadUnlocked(g, g.RawAccountGroup.RawMembers.Values);
            }
            return g;
        }

        public static void LoadUnlocked(AccountGroup g, IEnumerable<RawSimpleEntity> members)
        {
            var entities = new Dictionary<Guid, Entity>();
            var deserializedRawEntities = members
                .Select(m => new KeyValuePair<RawSimpleEntity, SerializableEntity>(m, LoadEntity(m)))
                .ToArray();
            foreach (var e in deserializedRawEntities)
            {
                var login = LoadLogin(e.Key, e.Value);
                if (login != null)
                {
                    g.Logins.Add(login);
                    entities[login.Guid] = login;
                }
                var target = LoadTarget(e.Key, e.Value);
                if (target != null)
                {
                    entities[target.Guid] = target;
                }
            }
            foreach (var e in deserializedRawEntities)
            {
                var account = LoadAccount(e.Key, e.Value, entities);
                if (account != null)
                {
                    g.Accounts.Add(account);
                }
            }
        }

        private static SerializableEntity LoadEntity(RawSimpleEntity e)
        {
            return (SerializableEntity)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(e.Data), typeof(SerializableEntity));
        }

        private static Login LoadLogin(RawSimpleEntity e, SerializableEntity login)
        {
            if (!"login".Equals(login.type))
            {
                return null;
            }
            return new Login {
                Guid = e.Guid,
                Username = login.user,
                Description = login.descr
            };
        }

        private static Target LoadTarget(RawSimpleEntity e, SerializableEntity target)
        {
            if (!"target".Equals(target.type))
            {
                return null;
            }
            BitmapImage icon = null;
            if (target.icon != null)
            {
                using (var s = new MemoryStream(target.icon))
                {
                    icon = new BitmapImage();
                    icon.BeginInit();
                    icon.CacheOption = BitmapCacheOption.OnLoad;
                    icon.StreamSource = s;
                    icon.EndInit();
                }
            }
            return new Target
            {
                Guid = e.Guid,
                Title = target.title,
                Uri = target.uri,
                Icon = icon,
                Description = target.descr
            };
        }

        private static Account LoadAccount(RawSimpleEntity e, SerializableEntity account, IDictionary<Guid, Entity> entities)
        {
            if (!"account".Equals(account.type))
            {
                return null;
            }
            Login login = null;
            if (account.login != null)
            {
                login = (Login)entities[(Guid)account.login];
            }
            Target target = null;
            if (account.target != null)
            {
                target = (Target)entities[(Guid)account.target];
            }
            return new Account
            {
                Guid = e.Guid,
                Password = account.pass,
                Login = login,
                Target = target,
                Description = account.descr
            };
        }
    }

    class SerializableGroup
    {
        public string version;
        public string name;
        public string descr;
        public byte[] pass;
        public byte[] salt;
        public RawSimpleEntity[] members;
    }
    class SerializableEntity
    {
        // common
        public string type;
        public string descr;
        // login
        public string user;
        // target
        public string title;
        public string uri;
        public byte[] icon;
        // account
        public string pass;
        public Guid?  login = null;
        public Guid?  target = null;
    }
}
