using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Marvi
{
    // Tiempo despierto para el sorteo horario y hora civil local para las visitas de las 11:11.
    internal sealed class GiftClock
    {
        [DllImport("kernel32.dll")]
        private static extern bool QueryUnbiasedInterruptTime(out ulong time);
        public static TimeSpan Awake()
        {
            ulong ticks;
            if (!QueryUnbiasedInterruptTime(out ticks)) throw new System.ComponentModel.Win32Exception();
            return TimeSpan.FromTicks((long)ticks);
        }

        private TimeSpan nextHour;
        public GiftClock(TimeSpan start) { nextHour = start + TimeSpan.FromHours(1); }

        public int Poll(DateTime local, TimeSpan awake, bool eligible, GiftInbox inbox, Func<int,int> draw, out bool newSlot)
        {
            int count = 0;
            while (awake >= nextHour)
            {
                nextHour += TimeSpan.FromHours(1);
                if (draw(100) < 5) count++;
            }
            newSlot = false;
            if (local.Minute != 11 || (local.Hour != 11 && local.Hour != 23)) return count;
            int date = local.Year * 10000 + local.Month * 100 + local.Day;
            bool morning = local.Hour == 11;
            if (date <= (morning ? inbox.Morning : inbox.Evening)) return count;
            if (morning) inbox.Morning = date;
            else inbox.Evening = date;
            newSlot = true;
            return eligible ? Math.Max(1,count) : count;
        }

        public TimeSpan Delay(DateTime local, TimeSpan awake)
        {
            DateTime slot = local.Date.AddHours(11).AddMinutes(11);
            if (slot <= local) slot = slot.AddHours(12);
            if (slot <= local) slot = slot.AddHours(12);
            return TimeSpan.FromMilliseconds(Math.Max(100, Math.Min(60000,
                Math.Min((slot-local).TotalMilliseconds,(nextHour-awake).TotalMilliseconds))));
        }
    }

    [DataContract]
    internal sealed class Gift
    {
        [DataMember] public int Flower;
        [DataMember] public string Letter;
        [DataMember] public bool LetterOnly;
        public static readonly string[] Flowers = { "Rosas rojas", "Tulipanes rojos", "Tulipanes lilas",
            "Orquídeas violetas", "Gerberas blancas", "Girasoles" };
        public string Label { get { return LetterOnly ? "Una carta" : Flowers[Flower]; } }
    }

    [DataContract]
    internal sealed class GiftInbox
    {
        [DataMember] public int Morning, Evening;
        [DataMember] public List<Gift> Pending = new List<Gift>();
        [DataMember] public Gift Latest;
        [DataMember] public bool SpecialsUnlocked;
        [DataMember] private bool currentLetters;
        private string directory;
        private string StatePath { get { return Path.Combine(directory,"entregas.json"); } }

        public static GiftInbox Load(string directory)
        {
            try
            {
                var state = LocalData.Read<GiftInbox>(Path.Combine(directory,"entregas.json"));
                if (state != null)
                {
                    state.directory=directory;
                    if (state.Pending == null) state.Pending = new List<Gift>();
                    state.Pending.RemoveAll(g => !Valid(g));
                    if (!Valid(state.Latest)) state.Latest = null;
                    return state;
                }
            }
            catch (IOException) { }
            catch (SerializationException) { }
            catch (UnauthorizedAccessException) { }
            return new GiftInbox { directory=directory };
        }

        private static bool Valid(Gift g)
        {
            return g != null && g.Flower >= 0 && g.Flower < Gift.Flowers.Length && !String.IsNullOrWhiteSpace(g.Letter);
        }

        public bool UpgradeLetters(LetterLibrary library,Random random)
        {
            if(currentLetters) return false;
            currentLetters=true;
            // Solo retiramos mensajes antiguos de fábrica. Los poemas elegidos por el usuario se conservan.
            if(Latest!=null && LetterLibrary.Retired(Latest.Letter)) Latest=null;
            foreach(var gift in Pending)
                if(LetterLibrary.Retired(gift.Letter)) gift.Letter=library.Choose(random);
            return File.Exists(StatePath);
        }

        public void Save()
        {
            LocalData.Write(StatePath,this);
        }
    }

    internal sealed class LetterLibrary
    {
        private readonly string folder;
        public LetterLibrary(string directory)
        {
            folder=Path.Combine(directory,"Cartas");
            try { Directory.CreateDirectory(folder); }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
        }
        // Las cartas personalizadas viven exclusivamente en la carpeta de datos del estudiante.
        private const string WelcomeNote = "Te traje estas flores para acompañarte un ratito. Marvi";
        private const int MaxBytes = 128 * 1024;
        private static readonly HashSet<string> RetiredNotes=new HashSet<string> {
            "KlsYfj0ZE3wEJClHUSWHZgOPdig/zwK8xHZHjTIMqWo=",
            "VtjQZibLXsDdbH67a2kdpohC17HcGQn1rLcT3uwv25s=",
            "Y9Ng0Cf6a/WvfbNqZhB7xtBIyuEYJaJtYUyg6gQ+/HM=",
            "LfbnkTaoVqa473pIB0CfP4p5yZqG1ltGYpvqHokX2K8=",
            "N1PMmwOp6KvxDGwoaUl60kGg/dbYd51VG2LqW67EuBI=",
            "rciA5b6bgGh6+S9VS4oIPp+A4ljA6gsR6rlaA5l4gjM=",
            "bAkOC/PWOfR2BLhN64uaQ2HLReY5cUtz6kvEmCRFVBE=",
            "4ZvXSSMSlLnZZ1q862xnMF+ijJmA8a/yW49+/s0REaY=",
            "psVBinPMfyAfwmniSOlEQcQuyYqzOvjJRvKGZxGE29I="
        };
        public static bool Retired(string letter)
        {
            using(var sha=System.Security.Cryptography.SHA256.Create())
                return RetiredNotes.Contains(Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(letter))));
        }

        private static string Read(string path)
        {
            if (!String.Equals(Path.GetExtension(path),".txt",StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Elegí un archivo .txt.");
            using (var input = File.OpenRead(path))
            {
                if (input.Length > MaxBytes) throw new InvalidDataException("La carta supera los 128 KB.");
                using (var reader = new StreamReader(input,new UTF8Encoding(false,true),true))
                {
                    string letter = reader.ReadToEnd();
                    if (String.IsNullOrWhiteSpace(letter) || letter.IndexOf('\0') >= 0)
                        throw new InvalidDataException("La carta debe contener texto.");
                    return letter;
                }
            }
        }

        public string Choose(Random random)
        {
            try
            {
                var files = Directory.GetFiles(folder,"*.txt").ToList();
                while(files.Count>0)
                {
                    int i=random.Next(files.Count);
                    string file=files[i];files.RemoveAt(i);
                    try { return Read(file); }
                    catch(IOException) { }
                    catch(InvalidDataException) { }
                    catch(UnauthorizedAccessException) { }
                    catch(DecoderFallbackException) { }
                }
            }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
            return WelcomeNote;
        }
    }
}
