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
        [DataMember(EmitDefaultValue=false)] public string Unit;
        [DataMember(EmitDefaultValue=false)] public string GuideId;
        public string Title { get { return String.IsNullOrWhiteSpace(Unit) ? Prompt : Unit; } }

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

        [DataMember] public List<PdfGuide> Guides = new List<PdfGuide>();

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
            // Los cuadernos anteriores a las guías solo tienen preguntas y ejercicios.
            if (book.Guides == null) book.Guides = new List<PdfGuide>();
            if (book.Guides.Any(g => g == null || !g.Valid)) throw new InvalidDataException("Guía inválida.");
            return book;
        }

        public IEnumerable<ReviewCard> Due(DateTime today)
        {
            return Cards.Where(c => c.DueDay <= today.Date.Ticks).OrderBy(c => c.DueDay);
        }

        // Las vistas editan una copia. Solo reemplazamos el cuaderno activo después de guardarlo.
        public PracticeBook Copy()
        {
            return new PracticeBook { Cards = new List<ReviewCard>(Cards), Problems = new List<MathProblem>(Problems), Guides = new List<PdfGuide>(Guides) };
        }
    }
}
