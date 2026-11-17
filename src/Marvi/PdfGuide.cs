using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace Marvi
{
    [DataContract]
    internal sealed class PdfGuide
    {
        [DataMember] public string Id;
        [DataMember] public string Subject;
        [DataMember] public string Title { get; set; }

        public bool Valid
        {
            get
            {
                Guid guid;
                return Guid.TryParseExact(Id, "N", out guid) && !String.IsNullOrWhiteSpace(Subject) && !String.IsNullOrWhiteSpace(Title);
            }
        }

        public string LocalPath(string data)
        {
            if (!Valid) throw new InvalidDataException("La referencia a la guía no es válida.");
            return Path.Combine(data, "Guias", Id + ".pdf");
        }

        // Solo guardamos copias locales con una clave propia. El nombre original es texto para la interfaz.
        public static PdfGuide Import(string source, string subject, string data)
        {
            if (!String.Equals(Path.GetExtension(source), ".pdf", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Elegí un archivo PDF.");
            var guide = new PdfGuide { Id = Guid.NewGuid().ToString("N"), Subject = subject, Title = Path.GetFileName(source) };
            string destination = guide.LocalPath(data);
            using (var input = File.OpenRead(source))
            {
                if (input.ReadByte() != '%' || input.ReadByte() != 'P' || input.ReadByte() != 'D' ||
                    input.ReadByte() != 'F' || input.ReadByte() != '-')
                    throw new InvalidDataException("El archivo no tiene una cabecera PDF válida.");
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                string temp = destination + ".tmp";
                try
                {
                    // La copia de Windows conserva los metadatos de procedencia en volúmenes compatibles.
                    // El descriptor de lectura impide modificar el original entre la validación y la copia.
                    File.Copy(source, temp);
                    File.SetAttributes(temp, File.GetAttributes(temp) & ~FileAttributes.ReadOnly);
                    File.Move(temp, destination);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            return guide;
        }

        public void Open(string data)
        {
            string path = LocalPath(data);
            if (!File.Exists(path)) throw new FileNotFoundException("La copia de la guía ya no está en la carpeta de datos.");
            using (Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })) { }
        }
    }
}
