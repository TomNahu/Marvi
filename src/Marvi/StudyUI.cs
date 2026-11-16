using System;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Marvi
{
    internal sealed partial class PetWindow
    {
        private readonly Func<DateTime> utcNow;
        private StudySession session=new StudySession();
        private StudyWindow studyWindow;
        private readonly Button studyBadge=new Button { FontFamily=new FontFamily("Consolas"),
            FontSize=12, Padding=new Thickness(6,2,6,2), Margin=new Thickness(0,2,0,0),
            Background=new SolidColorBrush(Color.FromRgb(255,242,217)), Foreground=Brushes.SaddleBrown,
            HorizontalAlignment=HorizontalAlignment.Center, VerticalAlignment=VerticalAlignment.Top,
            Visibility=Visibility.Collapsed, ToolTip="Abrí Estudiá conmigo" };
        private bool readFailed, studyWriteError;
        private string StudyFile { get { return Path.Combine(dataDirectory,"estudio.json"); } }
        private bool StudyingNow { get { return session.Active; } }

        private void InitializeStudy(Grid viewport)
        {
            viewport.Children.Add(studyBadge);
            studyBadge.Click+=(s,e)=>OpenStudy();
            LoadStudy();
            study.Interval=TimeSpan.FromSeconds(1);
            study.Tick+=(s,e)=>StudyTick();
            Loaded+=(s,e)=>
            {
                if(session.Active) { SetStudyPose(); study.Start(); StudyTick(); }
                else if(session.Stage==StudyStage.Summary) ShowStudy(false);
            };
        }
        private void LoadStudy()
        {
            try { session=StudySession.Load(StudyFile); readFailed=false; }
            catch(IOException) { readFailed=true; }
            catch(SerializationException) { readFailed=true; }
            catch(UnauthorizedAccessException) { readFailed=true; }
        }

        private bool SaveStudy(StudySession next)
        {
            try { LocalData.Write(StudyFile,next); session=next; studyWriteError=false; return true; }
            catch(IOException) { }
            catch(UnauthorizedAccessException) { }
            if(!studyWriteError)
            {
                studyWriteError=true;
                MessageBox.Show("No se pudo guardar el cambio. Revisá el acceso a la carpeta de Marvi y volvé a intentarlo.","Estudiá conmigo");
            }
            return false;
        }
        private void OpenStudy()
        {
            if(readFailed)
            {
                LoadStudy();
                if(readFailed)
                {
                    MessageBox.Show("No pude leer tu sesión guardada. Revisá el archivo estudio.json en la carpeta de Marvi.","Estudiá conmigo");
                    return;
                }
            }
            StudyTick();
            ShowStudy(true);
        }
        private void ShowStudy(bool activate)
        {
            if(studyWindow==null)
            {
                studyWindow=new StudyWindow(this);
                var area=MonitorArea();
                studyWindow.MaxHeight=Math.Max(120,area.Height-16);
                studyWindow.Width=Math.Min(365,area.Width);
                studyWindow.Left=Math.Max(area.Left,Math.Min(area.Right-studyWindow.Width,
                    Left-area.Left>=studyWindow.Width+8 ? Left-studyWindow.Width-8 : Left+Width+8));
                studyWindow.Top=area.Top+Math.Max(0,(area.Height-450)/2);
                studyWindow.SizeChanged+=(s,e)=>{
                    if(studyWindow!=null) studyWindow.Top=Math.Max(area.Top,Math.Min(studyWindow.Top,area.Bottom-e.NewSize.Height));
                };
                studyWindow.Closed+=(s,e)=>{ studyWindow=null; if(!StudyingNow && IsLoaded && IsVisible) ScheduleIdle(); };
                studyWindow.ShowActivated=activate;
                studyWindow.Render(session);
                studyWindow.Show();
            }
            if(studyWindow.WindowState==WindowState.Minimized) studyWindow.WindowState=WindowState.Normal;
            if(activate) studyWindow.Activate();
            PaintStudyClock(utcNow().Ticks);
        }
        private void SetStudyPose()
        {
            returnHomeAfterGift=false;
            ClearMeal(); HideParcel(); foundObject.Visibility=Visibility.Collapsed;
            gesture=null;
            target=session.Stage==StudyStage.Focus ? 6 : 0;
            if(frame!=target || target==6) StartMotion();
            else { dog.Source=Pose(frame); motion.Stop(); }
        }
        private void StudyTick()
        {
            if(!session.Active) return;
            long now=utcNow().Ticks;
            if(session.DueUtc!=0 && now>=session.DueUtc)
            {
                var next=session.Copy(); next.Expire(now);
                if(!SaveStudy(next)) return;
                SetStudyPose();
                if(studyWindow!=null) studyWindow.Render(session);
            }
            if(session.Pending && !session.NoticeShown)
            {
                ShowStudy(false);
                var seen=session.Copy(); seen.NoticeShown=true;
                SaveStudy(seen);
            }
            PaintStudyClock(now);
        }
        private void PaintStudyClock(long now)
        {
            studyBadge.Visibility=session.Active ? Visibility.Visible : Visibility.Collapsed;
            if(!session.Active) { studyBadge.Content=null; return; }
            bool ticking=session.Stage==StudyStage.Focus || session.Stage==StudyStage.Break;
            long ticks=ticking ? session.ClockTicks(now) : 0;
            long seconds=session.DueUtc==0 ? ticks/TimeSpan.TicksPerSecond :
                (long)Math.Ceiling(ticks/(double)TimeSpan.TicksPerSecond);
            string time=String.Format("{0:00}:{1:00}",seconds/60,seconds%60);
            string label=session.Stage==StudyStage.Focus ? "Estudio " : "Pausa ";
            string caption=ticking ? label+time : (session.Stage==StudyStage.FocusDue ? "Bloque terminado" :
                session.Stage==StudyStage.BreakDue ? "Pausa terminada" : "Elegí la pausa");
            studyBadge.Content=caption;
            tray.Text=caption+" · Marvi";
            if(studyWindow!=null) studyWindow.UpdateClock(time);
        }

        internal static string TotalText(long ticks)
        {
            long seconds=ticks/TimeSpan.TicksPerSecond;
            return String.Format("{0} min {1:00} s",seconds/60,seconds%60);
        }
        private void ChangeStudy(Action<StudySession,long> change)
        {
            long now=utcNow().Ticks;
            var next=session.Copy();
            next.Expire(now);
            change(next,now);
            if(!SaveStudy(next)) return;
            if(session.Active)
            {
                if(!IsVisible) Show();
                SetStudyPose(); study.Start();
            }
            else
            {
                study.Stop(); studyBadge.Visibility=Visibility.Collapsed;
                tray.Text=profile.Name+" · Marvi"; target=0; gesture=null;
                if(frame!=0) StartMotion(); else ScheduleIdle();
            }
            if(studyWindow!=null) studyWindow.Render(session);
            PaintStudyClock(now);
        }
        internal void StartStudy(StudyMode mode,string goal) { ChangeStudy((s,t)=>s.Start(mode,goal,t)); }
        internal void PauseStudy() { ChangeStudy((s,t)=>s.Pause(t)); }
        internal void BeginStudyBreak(int minutes) { ChangeStudy((s,t)=>s.StartBreak(minutes,t)); }
        internal void ExtendStudy() { ChangeStudy((s,t)=>s.Extend(t)); }
        internal void ResumeStudy() { ChangeStudy((s,t)=>s.Resume(t)); }
        internal void EndStudy() { ChangeStudy((s,t)=>s.Finish(t)); }
        internal void CompleteStudy(string outcome)
        {
            var next=session.Copy(); next.Complete(outcome,utcNow().Ticks);
            if(SaveStudy(next) && studyWindow!=null) studyWindow.Close();
        }
        internal void EditStudyNote(string note)
        {
            if(session.Note==note) return;
            var next=session.Copy(); next.Note=note; SaveStudy(next);
        }
        private void CloseStudyUI() { study.Stop(); if(studyWindow!=null) studyWindow.Close(); }
    }
}
