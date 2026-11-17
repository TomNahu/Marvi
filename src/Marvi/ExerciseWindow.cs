using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Marvi
{
    internal sealed partial class PracticeWindow
    {
        internal const string Workshop = "Taller de ejercicios";
        private string subject = ExerciseCatalog.Examples[0].Topic;

        private ComboBox Choice(string label, System.Collections.IEnumerable items, string display)
        {
            Text(label, 14).FontWeight = FontWeights.SemiBold;
            var box = new ComboBox { ItemsSource = items, DisplayMemberPath = display, SelectedIndex = 0,
                Margin = new Thickness(0, 0, 0, 6), MinHeight = 27 };
            AutomationProperties.SetName(box, label);
            body.Children.Add(box);
            return box;
        }

        private void ExerciseHome(MathProblem selected = null, PdfGuide imported = null)
        {
            ClearPage();
            Heading(Workshop);
            var all = ExerciseCatalog.Examples.Concat(Book.Problems).ToList();
            var subjects = all.Select(p => p.Topic).Concat(Book.Guides.Select(g => g.Subject))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var selector = Choice("Materia", subjects, "");
            selector.SelectedItem = subject;
            selector.SelectionChanged += (s,e) => {
                if (selector.SelectedItem == null) return;
                subject = (string)selector.SelectedItem;
                ExerciseHome();
            };
            var exercises = all.Where(p => SameSubject(p.Topic)).ToList();
            if (exercises.Count > 0)
            {
                var list = Choice("Ejercicio", exercises, "Title");
                if (selected != null) list.SelectedItem = selected;
                ActionButton("Resolver ejercicio", () => Solve((MathProblem)list.SelectedItem), true);
            }
            ActionButton("Agregar ejercicio propio", () => AddProblem(null));
            var guides = Book.Guides.Where(g => SameSubject(g.Subject)).ToList();
            if (guides.Count > 0)
            {
                var list = Choice("Guías de trabajos prácticos", guides, "Title");
                if (imported != null) list.SelectedItem = imported;
                ActionButton("Abrir guía PDF", () => OpenGuide((PdfGuide)list.SelectedItem));
                ActionButton("Anotar ejercicio de la guía", () => AddProblem((PdfGuide)list.SelectedItem));
            }
            ActionButton("Importar guía PDF", ImportGuide);
            ActionButton("Cerrar", Close);
        }

        private bool SameSubject(string value) { return String.Equals(subject, value, StringComparison.OrdinalIgnoreCase); }

        private async void ImportGuide()
        {
            var picker = new OpenFileDialog { Title = "Elegí una guía de trabajos prácticos", Filter = "Guías PDF (*.pdf)|*.pdf", CheckFileExists = true };
            if (picker.ShowDialog(this) != true) return;
            await ImportGuideFile(picker.FileName);
        }

        internal async Task ImportGuideFile(string source)
        {
            string data = pet.PracticeDirectory, selectedSubject = subject;
            IsEnabled = false;
            try
            {
                // La copia se hace fuera del hilo que anima la mascota y mantiene el reloj de estudio.
                var guide = await Task.Run(() => PdfGuide.Import(source, selectedSubject, data));
                if (pet.EditBook(copy => copy.Guides.Add(guide)))
                {
                    if (IsLoaded) ExerciseHome(null, guide);
                }
                else File.Delete(guide.LocalPath(data));
            }
            catch (Exception ex)
            {
                if (!LocalData.IsReadError(ex)) throw;
                MessageBox.Show("No se pudo importar la guía. " + ex.Message, Workshop);
            }
            finally { IsEnabled = true; }
        }

        private void OpenGuide(PdfGuide guide)
        {
            try { guide.Open(pet.PracticeDirectory); }
            catch (Exception ex)
            {
                if (!LocalData.IsReadError(ex) && !(ex is Win32Exception)) throw;
                MessageBox.Show(this, "No se pudo abrir la guía en el lector de PDF de Windows. " + ex.Message, Workshop);
            }
        }

        private void AddProblem(PdfGuide guide)
        {
            ClearPage();
            Heading("Agregá un ejercicio");
            Text(subject, 16);
            if (guide != null)
            {
                Text(guide.Title, 14);
                ActionButton("Consultar guía PDF", () => OpenGuide(guide));
            }
            var title = Entry("Tema o título (opcional)", "", false);
            var prompt = Entry(guide == null ? "Enunciado" : "Consigna o número de ejercicio y página", "", true);
            var solution = Entry("Solución o referencia al apunte (opcional)", "", true);
            var error = Text("", 13);
            ActionButton("Guardar ejercicio", () => {
                if (String.IsNullOrWhiteSpace(prompt.Text)) { error.Text = "Escribí la consigna o la referencia al ejercicio."; return; }
                var problem = new MathProblem(subject, prompt.Text.Trim(), solution.Text.Trim()) {
                    Unit = title.Text.Trim(), GuideId = guide == null ? null : guide.Id };
                if (pet.EditBook(copy => copy.Problems.Add(problem))) ExerciseHome(problem);
            }, true);
            ActionButton("Volver", () => ExerciseHome());
        }

        private void Solve(MathProblem problem)
        {
            ClearPage();
            Heading(String.IsNullOrWhiteSpace(problem.Unit) ? subject : problem.Unit);
            Text(problem.Prompt, 17);
            var guide = Book.Guides.FirstOrDefault(g => g.Id == problem.GuideId);
            if (guide != null) ActionButton("Consultar guía PDF", () => OpenGuide(guide));
            Entry("Tu desarrollo (opcional, solo durante este intento)", "", true);
            var answer = Text(String.IsNullOrWhiteSpace(problem.Solution) ?
                "Contrastá el resultado con tu guía o apunte." : problem.Solution, 15);
            answer.Visibility = Visibility.Collapsed;
            var reveal = ActionButton("Ver solución o pista", () => answer.Visibility = Visibility.Visible);
            reveal.Click += (s,e) => reveal.Visibility = Visibility.Collapsed;
            ActionButton("Elegir otro ejercicio", () => ExerciseHome(problem), true);
        }
    }
}
