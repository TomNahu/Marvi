using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;

namespace Marvi
{
    internal sealed partial class PetWindow
    {
        private PracticeWindow practiceWindow;
        private bool explaining;
        internal PracticeBook Notebook { get; private set; }
        private string NotebookFile { get { return Path.Combine(dataDirectory, "practica.json"); } }
        internal string PracticeDirectory { get { return dataDirectory; } }

        private bool LoadNotebook(bool reportError)
        {
            if (Notebook != null) return true;
            try { Notebook = PracticeBook.Load(NotebookFile); return true; }
            catch (Exception ex)
            {
                if (!LocalData.IsReadError(ex)) throw;
                if (reportError) MessageBox.Show("No se pudo abrir practica.json. Revisá el archivo en tu carpeta de datos.", "Práctica · Marvi");
                return false;
            }
        }

        internal int DueReviewCount()
        {
            return LoadNotebook(false) ? Notebook.Due(DateTime.Today).Count() : 0;
        }

        internal bool EditBook(Action<PracticeBook> edit)
        {
            var next = Notebook.Copy();
            edit(next);
            try { LocalData.Write(NotebookFile, next); }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException)) throw;
                MessageBox.Show("No se pudo guardar. Revisá el espacio y el permiso de escritura y volvé a intentarlo.", "Práctica · Marvi");
                return false;
            }
            Notebook = next;
            if (studyWindow != null) studyWindow.RefreshReviewCount();
            return true;
        }

        internal void OpenPractice(PracticePage page)
        {
            if (!LoadNotebook(true)) return;
            if (practiceWindow == null)
            {
                practiceWindow = new PracticeWindow(this) { Owner = studyWindow };
                var area = MonitorArea();
                practiceWindow.MaxHeight = area.Height - 12;
                practiceWindow.Width = Math.Min(380, area.Width);
                practiceWindow.Left = Math.Max(area.Left, Math.Min(area.Right - practiceWindow.Width, studyWindow.Left));
                practiceWindow.Top = Math.Max(area.Top, Math.Min(area.Bottom - 420, studyWindow.Top));
                practiceWindow.SizeChanged += (s,e) => {
                    if (practiceWindow != null)
                        practiceWindow.Top = Math.Max(area.Top, Math.Min(practiceWindow.Top, area.Bottom - e.NewSize.Height));
                };
                practiceWindow.Closed += (s,e) => {
                    practiceWindow = null;
                    ListenToExplanation(false);
                    if (IsLoaded && IsVisible && !StudyingNow && gesture == null) ScheduleIdle();
                };
            }
            practiceWindow.ShowPage(page);
            if (!practiceWindow.IsVisible) practiceWindow.Show();
            practiceWindow.WindowState = WindowState.Normal;
            practiceWindow.Activate();
        }

        // La autoexplicación usa la pose sentada. Solo la controla mientras esté abierta esa actividad.
        internal void ListenToExplanation(bool active)
        {
            if (explaining == active) return;
            explaining = active;
            if (active)
            {
                if (!IsVisible) Show();
                ClearMeal(); HideParcel();
                foundObject.Visibility = Visibility.Collapsed;
                gesture = null; target = 0;
                if (frame != 0) StartMotion();
                else { dog.Source = Pose(0); ScheduleIdle(); }
            }
            else if (StudyingNow) SetStudyPose();
            else ScheduleIdle();
        }
    }
}
