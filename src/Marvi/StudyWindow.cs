using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Marvi
{
    internal sealed class StudyWindow : StudySurface
    {
        private readonly PetWindow pet;
        private TextBlock clock;
        private Button reviewButton;

        internal StudyWindow(PetWindow pet) : base("Estudiá conmigo · Marvi")
        {
            this.pet = pet;
            Activated += (s,e) => RefreshReviewCount();
        }
        private void Goal(StudySession s)
        {
            if(!String.IsNullOrEmpty(s.Goal)) Text(s.Goal,16);
        }
        private void Note(StudySession s)
        {
            var input=Entry("¿Por dónde querés seguir?",s.Note,true);
            input.TextChanged+=(sender,e)=>pet.EditStudyNote(input.Text);
        }
        private void Clock()
        {
            clock=Text("",28); clock.FontFamily=new FontFamily("Consolas"); clock.Foreground=Leaf;
        }
        public void UpdateClock(string value) { if(clock!=null) clock.Text=value; }

        // Los atajos de práctica solo viven en esta ventana y no interceptan la escritura de otras apps.
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (reviewButton == null || Keyboard.Modifiers != (ModifierKeys.Control | ModifierKeys.Shift)) return;
            PracticePage page;
            switch (e.Key)
            {
                case Key.F1: page = PracticePage.Review; break;
                case Key.F2: page = PracticePage.Explain; break;
                case Key.F3: page = PracticePage.Math; break;
                default: return;
            }
            e.Handled = true;
            if (!e.IsRepeat) pet.OpenPractice(page);
        }

        private void PracticeActions()
        {
            reviewButton = ActionButton("Repasá con Marvi", () => pet.OpenPractice(PracticePage.Review));
            reviewButton.ToolTip = "Ctrl + Shift + F1";
            ActionButton("Explicáselo a Marvi", () => pet.OpenPractice(PracticePage.Explain)).ToolTip = "Ctrl + Shift + F2";
            ActionButton("Mezclá ejercicios", () => pet.OpenPractice(PracticePage.Math)).ToolTip = "Ctrl + Shift + F3";
            RefreshReviewCount();
        }

        internal void RefreshReviewCount()
        {
            if (reviewButton == null) return;
            int count = pet.DueReviewCount();
            reviewButton.Content = count == 0 ? "Repasá con Marvi" :
                String.Format("Repasá con Marvi · {0} {1} para hoy", count, count == 1 ? "pregunta" : "preguntas");
        }

        private sealed class Durations : Expander
        {
            private readonly TextBox focus, pause, extension;
            private readonly TextBlock message = new TextBlock { TextWrapping = TextWrapping.Wrap };

            public Durations(StudySession s, Action<int,int,int> update)
            {
                Header = "Ajustar tiempos del Pomodoro";
                Margin = new Thickness(0, 4, 0, 5);
                var panel = new StackPanel { Margin = new Thickness(12, 4, 0, 4) };
                Content = panel;
                focus = MinutesField(panel, "Estudio", s.FocusMinutes);
                pause = MinutesField(panel, "Descanso", s.BreakMinutes);
                extension = MinutesField(panel, "Extensión", s.ExtensionMinutes);
                panel.Children.Add(message);
                if (update != null)
                {
                    panel.Children.Add(new TextBlock { Text = "Se aplican a los próximos tramos.", TextWrapping = TextWrapping.Wrap });
                    panel.Children.Add(MakeButton("Guardar tiempos", () => Apply(update), false));
                }
            }

            private static TextBox MinutesField(Panel panel, string name, int value)
            {
                var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
                var input = new TextBox { Text = value.ToString(), Width = 56, MaxLength = 4, Padding = new Thickness(4) };
                string label = name + " (min)";
                AutomationProperties.SetName(input, label);
                DockPanel.SetDock(input, Dock.Right);
                row.Children.Add(input);
                row.Children.Add(new Label { Content = label, Target = input });
                panel.Children.Add(row);
                return input;
            }

            public void Apply(Action<int,int,int> continuation)
            {
                int f, p, e;
                if (Int32.TryParse(focus.Text, out f) && StudySession.ValidMinutes(f) &&
                    Int32.TryParse(pause.Text, out p) && StudySession.ValidMinutes(p) &&
                    Int32.TryParse(extension.Text, out e) && StudySession.ValidMinutes(e))
                    continuation(f, p, e);
                else
                {
                    IsExpanded = true;
                    message.Text = "Escribí entre 1 y 1440 minutos enteros en cada campo.";
                }
            }
        }

        public void Render(StudySession s)
        {
            // Reconstruimos los controles al cambiar de etapa. Cada segundo solo cambia el texto del reloj.
            ClearPage(); clock=null; reviewButton=null;
            if(s.Stage==StudyStage.Idle)
            {
                Heading("Estudiá conmigo");
                var goal=Entry("¿Qué querés hacer en esta sesión?","",false);
                Text("Podés dejar la meta en blanco.",12);
                var pom=new RadioButton {Content="Pomodoro · Avisame cuándo descansar",IsChecked=true,Margin=new Thickness(0,8,0,5)};
                var flow=new RadioButton {Content="Flowtime · Yo elijo cuándo parar",Margin=new Thickness(0,4,0,10)};
                body.Children.Add(pom); body.Children.Add(flow);
                var durations = new Durations(s, null);
                body.Children.Add(durations);
                flow.Checked += (sender,e) => durations.Visibility = Visibility.Collapsed;
                pom.Checked += (sender,e) => durations.Visibility = Visibility.Visible;
                ActionButton("Empezar", () => {
                    if (flow.IsChecked == true) pet.StartStudy(StudyMode.Flowtime, goal.Text, s.FocusMinutes, s.BreakMinutes, s.ExtensionMinutes);
                    else durations.Apply((f,p,e) => pet.StartStudy(StudyMode.Pomodoro, goal.Text, f, p, e));
                }, true);
                PracticeActions();
                return;
            }
            if(s.Stage==StudyStage.Summary)
            {
                Heading("Tu sesión de estudio");
                Text("Estudio · "+PetWindow.TotalText(s.FocusTicks),16);
                Text("Descanso · "+PetWindow.TotalText(s.BreakTicks),16);
                ActionButton("Dejá una pregunta para otro día", () => pet.OpenPractice(PracticePage.NewCard));
                if(String.IsNullOrEmpty(s.Goal)) ActionButton("Listo",()=>pet.CompleteStudy(""),true);
                else
                {
                    Goal(s); Text("¿Pudiste completarla?",16).FontWeight=FontWeights.SemiBold;
                    foreach(string result in new[]{"Sí","En parte","Lo dejo para después"})
                    {
                        string answer=result;
                        ActionButton(answer,()=>pet.CompleteStudy(answer),result=="Sí");
                    }
                }
                return;
            }
            Text(s.Mode==StudyMode.Pomodoro ? "Pomodoro" : "Flowtime",13).Foreground=Leaf;
            switch(s.Stage)
            {
                case StudyStage.Focus:
                    Heading("Tiempo de estudio"); Clock(); Goal(s);
                    if(!String.IsNullOrEmpty(s.Note)) { Text("Para retomar",13); Text(s.Note,16); }
                    ActionButton("Tomar un descanso",pet.PauseStudy,true);
                    break;
                case StudyStage.FocusDue:
                    Heading("Terminó tu bloque"); Goal(s);
                    ActionButton("Descansar "+s.BreakMinutes+" min",pet.PauseStudy,true);
                    ActionButton("Seguir "+s.ExtensionMinutes+" min",pet.ExtendStudy);
                    break;
                case StudyStage.ChooseBreak:
                    Heading("Elegí tu descanso");
                    Text("Pausa sugerida · "+s.SuggestedMinutes+" min",15);
                    var minutes=Entry("Duración en minutos",s.SuggestedMinutes.ToString(),false);
                    var invalid=Text("",12); invalid.Visibility=Visibility.Collapsed;
                    ActionButton("Empezar descanso",()=>{
                        int n;
                        if(Int32.TryParse(minutes.Text,NumberStyles.None,CultureInfo.CurrentCulture,out n) && n>0)
                            pet.BeginStudyBreak(n);
                        else { invalid.Text="Escribí una cantidad entera de minutos mayor que cero."; invalid.Visibility=Visibility.Visible; }
                    },true);
                    ActionButton("Sin límite",()=>pet.BeginStudyBreak(0));
                    ActionButton("Volver a estudiar",pet.ResumeStudy);
                    break;
                case StudyStage.Break:
                    Heading(s.DueUtc==0 ? "Descanso sin límite" : "Tiempo de descanso"); Clock(); Note(s);
                    ActionButton("Volver a estudiar",pet.ResumeStudy,true);
                    break;
                case StudyStage.BreakDue:
                    Heading("Terminó tu descanso"); Note(s);
                    ActionButton("Volver a estudiar",pet.ResumeStudy,true);
                    break;
            }
            ActionButton("Finalizar sesión",pet.EndStudy);
            if (s.Mode == StudyMode.Pomodoro) body.Children.Add(new Durations(s, pet.SetPomodoroTimes));
            if (s.Stage == StudyStage.Focus) PracticeActions();
        }
    }
}
