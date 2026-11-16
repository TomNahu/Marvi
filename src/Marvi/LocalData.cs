using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Marvi
{
    internal static class LocalData
    {
        public static string DirectoryPath()
        {
            // La edición portátil lleva sus datos al lado del ejecutable, incluso en un pendrive.
            string home = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(home, "Marvi.portable")))
                return Path.Combine(home, "Datos");
            return Migrate(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        }

        internal static string Migrate(string parent)
        {
            string destination=Path.Combine(parent,"Marvi"), legacy=Path.Combine(parent,"VPet");
            if(Directory.Exists(destination) || !Directory.Exists(legacy)) return destination;
            // Publish the whole first-run copy together. The old program's
            // profile and manually supplied letters remain recoverable.
            string staging=Path.Combine(parent,"Marvi-"+Guid.NewGuid().ToString("N")+".tmp");
            Directory.CreateDirectory(staging);
            try
            {
                foreach(string name in new[]{"profile.json","entregas.json"})
                {
                    string file=Path.Combine(legacy,name);
                    if(File.Exists(file)) File.Copy(file,Path.Combine(staging,name));
                }
                string cards=Path.Combine(legacy,"Cartas");
                if(Directory.Exists(cards))
                {
                    string copy=Path.Combine(staging,"Cartas");
                    Directory.CreateDirectory(copy);
                    foreach(string file in Directory.GetFiles(cards,"*.txt"))
                        File.Copy(file,Path.Combine(copy,Path.GetFileName(file)));
                }
                Directory.Move(staging,destination);
            }
            finally
            {
                if(Directory.Exists(staging)) Directory.Delete(staging,true);
            }
            return destination;
        }

        public static T Read<T>(string file)
        {
            using (var input = File.OpenRead(file))
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(input);
        }

        // Primero completamos el archivo temporal. Un cierre durante la escritura conserva la copia anterior.
        public static void Write<T>(string file,T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=File.Create(temp))
                    new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value);
                if(File.Exists(file)) File.Replace(temp,file,null);
                else File.Move(temp,file);
            }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }
}
