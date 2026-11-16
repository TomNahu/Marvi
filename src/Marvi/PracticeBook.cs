using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Marvi
{
    [DataContract]
    internal sealed class ReviewCard
    {
        [DataMember] public string Question { get; set; }
        [DataMember] public string Hint;
        // Día del calendario local. Un cambio de huso horario no desplaza el repaso a la víspera.
        [DataMember] public long DueDay;
    }

    [DataContract]
    internal sealed class MathProblem
    {
        [DataMember] public string Topic;
        [DataMember] public string Prompt;
        [DataMember] public string Solution;

        public MathProblem(string topic, string prompt, string solution)
        {
            Topic = topic; Prompt = prompt; Solution = solution;
        }
    }

    [DataContract]
    internal sealed class PracticeBook
    {
        [DataMember] public List<ReviewCard> Cards = new List<ReviewCard>();
        [DataMember] public List<MathProblem> Problems = new List<MathProblem>();

        public static PracticeBook Load(string path)
        {
            if (!File.Exists(path)) return new PracticeBook();
            var book = LocalData.Read<PracticeBook>(path);
            if (book == null || book.Cards == null || book.Problems == null ||
                book.Cards.Any(c => c == null || String.IsNullOrWhiteSpace(c.Question) ||
                    c.DueDay < 0 || c.DueDay > DateTime.MaxValue.Ticks) ||
                book.Problems.Any(p => p == null || String.IsNullOrWhiteSpace(p.Topic) ||
                    String.IsNullOrWhiteSpace(p.Prompt)))
                throw new InvalidDataException("El cuaderno de práctica está dañado.");
            return book;
        }

        public IEnumerable<ReviewCard> Due(DateTime today)
        {
            return Cards.Where(c => c.DueDay <= today.Date.Ticks).OrderBy(c => c.DueDay);
        }

        // Las vistas editan una copia. Solo reemplazamos el cuaderno activo después de guardarlo.
        public PracticeBook Copy()
        {
            return new PracticeBook { Cards = new List<ReviewCard>(Cards), Problems = new List<MathProblem>(Problems) };
        }
    }

    internal static class MixedMath
    {
        // Los enunciados omiten el nombre del procedimiento. La solución se revela a pedido.
        internal static readonly MathProblem[] Examples = {
            new MathProblem("Ecuaciones", "Un número más su doble da 36. ¿Cuál es el número?", "12. Si x + 2x = 36, entonces 3x = 36."),
            new MathProblem("Ecuaciones", "Pensé un número, le resté 4 y dupliqué el resultado. Obtuve 18. ¿Qué número pensé?", "13. Al deshacer los pasos, 18 ÷ 2 + 4 = 13."),
            new MathProblem("Ecuaciones", "Dos números consecutivos suman 41. ¿Cuáles son?", "20 y 21. Si el primero es x, la suma es 2x + 1 = 41."),
            new MathProblem("Proporcionalidad", "Para 4 porciones usás 300 g de arroz. ¿Cuánto necesitás para 6 porciones iguales?", "450 g. Cada porción lleva 75 g."),
            new MathProblem("Proporcionalidad", "Una bicicleta recorre 15 km en 45 minutos a velocidad constante. ¿Cuánto recorre en una hora?", "20 km. En 15 minutos recorre 5 km."),
            new MathProblem("Proporcionalidad", "Con 8 litros pintás 24 m². Con la misma pintura, ¿cuánto necesitás para 15 m²?", "5 litros. Cada litro cubre 3 m²."),
            new MathProblem("Geometría", "Un cantero triangular tiene 8 m de base y 5 m de altura. ¿Cuánta superficie ocupa?", "20 m². Multiplicá la base por la altura y dividí por dos."),
            new MathProblem("Geometría", "Un marco rectangular mide 7 cm por 4 cm. ¿Cuánta cinta hace falta para rodearlo?", "22 cm. Sumá sus cuatro lados, 7 + 4 + 7 + 4."),
            new MathProblem("Geometría", "Un patio cuadrado tiene 36 m² de superficie. ¿Cuánto mide cada lado?", "6 m. El área de un cuadrado es lado por lado.")
        };

        private static void Shuffle<T>(List<T> items, Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int other = random.Next(i + 1);
                T value = items[i]; items[i] = items[other]; items[other] = value;
            }
        }

        public static List<MathProblem> Mix(IEnumerable<MathProblem> problems, IList<string> topics, Random random)
        {
            if (topics.Count < 2 || topics.Count > 3 || topics.Distinct(StringComparer.OrdinalIgnoreCase).Count() != topics.Count)
                throw new ArgumentException("Elegí dos o tres tipos distintos.");
            var groups = topics.Select(t => problems.Where(p => String.Equals(p.Topic, t, StringComparison.OrdinalIgnoreCase)).ToList()).ToList();
            foreach (var group in groups) Shuffle(group, random);
            if (groups.Any(g => g.Count == 0)) throw new ArgumentException("Cada tipo necesita un ejercicio.");
            // Rondas equilibradas de hasta tres ejercicios por tipo, sin repetir ejercicios ni tipos contiguos.
            int rounds = Math.Min(3, groups.Min(g => g.Count));
            var result = new List<MathProblem>();
            for (int i = 0; i < rounds; i++)
            {
                Shuffle(groups, random);
                if (i > 0 && String.Equals(result[result.Count - 1].Topic, groups[0][i].Topic, StringComparison.OrdinalIgnoreCase))
                {
                    var first = groups[0]; groups[0] = groups[1]; groups[1] = first;
                }
                foreach (var group in groups) result.Add(group[i]);
            }
            return result;
        }
    }
}
