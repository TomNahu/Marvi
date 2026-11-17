using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Marvi
{
    internal enum PracticePage { Review, NewCard, Explain, Math }

    internal sealed partial class PracticeWindow : StudySurface
    {
        private readonly PetWindow pet;
        private PracticeBook Book { get { return pet.Notebook; } }

        internal PracticeWindow(PetWindow pet) : base("Práctica · Marvi") { this.pet = pet; }

        internal void ShowPage(PracticePage page)
        {
            pet.ListenToExplanation(page == PracticePage.Explain);
            switch (page)
            {
                case PracticePage.Review: ReviewHome(); break;
                case PracticePage.NewCard: EditCard(null, false); break;
                case PracticePage.Explain: Explain(); break;
                case PracticePage.Math: ExerciseHome(); break;
            }
        }

        private DatePicker ReviewDate(DateTime? value)
        {
            var picker = new DatePicker { SelectedDate = value, DisplayDateStart = DateTime.Today };
            AutomationProperties.SetName(picker, "Fecha de repaso");
            body.Children.Add(picker);
            return picker;
        }

        private void ReviewHome(string feedback = null)
        {
            ClearPage();
            Heading("Repasá con Marvi");
            if (feedback != null) Text(feedback, 14);
            var due = Book.Due(DateTime.Today).ToList();
            if (due.Count > 0)
            {
                Text(due.Count == 1 ? "Tenés una pregunta para hoy." : "Tenés " + due.Count + " preguntas para hoy.", 15);
                ActionButton("Empezar repaso", () => Review(due[0], false), true);
            }
            else if (Book.Cards.Count == 0) Text("Dejá una pregunta sobre lo que estudiaste para probarte otro día.", 15);
            else
            {
                var next = Book.Cards.OrderBy(c => c.DueDay).First();
                Text("Próximo repaso · " + new DateTime(next.DueDay).ToString("dd/MM/yyyy"), 15);
            }
            ActionButton("Guardar una pregunta", () => EditCard(null, false), Book.Cards.Count == 0);
            if (Book.Cards.Count > 0) ActionButton("Mis preguntas", CardList);
            ActionButton("Cerrar", Close);
        }

        private void EditCard(ReviewCard original, bool doubt)
        {
            ClearPage();
            Heading(doubt ? "Guardá tu duda" : original == null ? "Dejá una pregunta para otro día" : "Editá la pregunta");
            var question = Entry(doubt ? "¿Qué parte querés entender mejor?" : "¿Qué querés preguntarte?", original == null ? "" : original.Question, true);
            var hint = Entry("Respuesta o referencia al apunte (opcional)", original == null ? "" : original.Hint, true);
            Text("¿Cuándo querés repasarla?", 14);
            var day = ReviewDate(original == null ? DateTime.Today.AddDays(1) :
                new DateTime(Math.Max(DateTime.Today.Ticks, original.DueDay)));
            var error = Text("", 13);
            ActionButton("Guardar pregunta", () => {
                if (String.IsNullOrWhiteSpace(question.Text) || !day.SelectedDate.HasValue)
                {
                    error.Text = "Escribí una pregunta y elegí una fecha de repaso.";
                    return;
                }
                var card = new ReviewCard { Question = question.Text.Trim(), Hint = hint.Text.Trim(),
                    DueDay = day.SelectedDate.Value.Date.Ticks };
                if (pet.EditBook(copy => {
                    if (original != null) copy.Cards.Remove(original);
                    copy.Cards.Add(card);
                })) ReviewHome("Pregunta guardada para el " + day.SelectedDate.Value.ToString("dd/MM") + ".");
            }, true);
            ActionButton("Volver", () => ReviewHome());
        }

        private void CardList()
        {
            ClearPage();
            Heading("Mis preguntas");
            var cards = new ListBox { ItemsSource = Book.Cards.OrderBy(c => c.DueDay), DisplayMemberPath = "Question",
                MinHeight = 60, MaxHeight = 200 };
            AutomationProperties.SetName(cards, "Preguntas guardadas");
            ScrollViewer.SetHorizontalScrollBarVisibility(cards, ScrollBarVisibility.Disabled);
            body.Children.Add(cards);
            var review = ActionButton("Repasar ahora", () => Review((ReviewCard)cards.SelectedItem, false), true);
            var edit = ActionButton("Editar pregunta", () => EditCard((ReviewCard)cards.SelectedItem, false));
            review.IsEnabled = edit.IsEnabled = false;
            cards.SelectionChanged += (s,e) => review.IsEnabled = edit.IsEnabled = cards.SelectedItem != null;
            ActionButton("Volver", () => ReviewHome());
        }

        private void Review(ReviewCard card, bool revealed)
        {
            // Primero se recupera de memoria. La referencia y la próxima fecha aparecen después del intento.
            ClearPage();
            Heading("Repasá con Marvi");
            Text(card.Question, 18);
            if (!revealed)
            {
                Text("Probá responder de memoria, en voz alta o en papel.", 14);
                ActionButton("Ver respuesta o pista", () => Review(card, true), true);
            }
            else
            {
                Text(String.IsNullOrWhiteSpace(card.Hint) ? "Contrastá tu respuesta con el apunte." : card.Hint, 16);
                Text("Próxima fecha (opcional)", 14);
                var day = ReviewDate(null);
                ActionButton("Me salió", () => Schedule(card, day.SelectedDate ?? DateTime.Today.AddDays(7)), true);
                ActionButton("Quiero practicarla otra vez", () => Schedule(card, day.SelectedDate ?? DateTime.Today.AddDays(1)));
            }
            ActionButton("Hoy paso", () => Schedule(card, DateTime.Today.AddDays(1)));
            ActionButton("Volver", () => ReviewHome());
        }

        private void Schedule(ReviewCard card, DateTime day)
        {
            var updated = new ReviewCard { Question = card.Question, Hint = card.Hint, DueDay = day.Date.Ticks };
            if (pet.EditBook(copy => { copy.Cards.Remove(card); copy.Cards.Add(updated); }))
                ReviewHome("Próximo repaso de esta pregunta · " + day.ToString("dd/MM"));
        }

        private void Explain()
        {
            ClearPage();
            Heading("Explicáselo a Marvi");
            Text("Elegí un paso de un ejercicio. ¿Por qué se puede hacer así?", 17);
            Text("Si estás leyendo, contame con tus palabras qué ocurre y por qué. Podés hablar en voz alta o escribir en tu cuaderno.", 14);
            ActionButton("Lo pude explicar", Close, true);
            ActionButton("Me quedó una duda", () => { pet.ListenToExplanation(false); EditCard(null, true); });
        }
    }
}
