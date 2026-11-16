using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace Marvi
{
    internal sealed class StudyWindow : Window
    {
        private readonly PetWindow pet;
        private readonly StackPanel body=new StackPanel { Margin=new Thickness(14,9,14,12) };
        private TextBlock clock;
        private static readonly Brush Ink=new SolidColorBrush(Color.FromRgb(61,43,34));
        private static readonly Brush Leaf=new SolidColorBrush(Color.FromRgb(41,91,76));

        internal StudyWindow(PetWindow pet)
        {
            this.pet=pet;
            Title="Estudiá conmigo · Marvi";
            Width=365; SizeToContent=SizeToContent.Height; ResizeMode=ResizeMode.CanMinimize;
            ShowInTaskbar=true; Topmost=true;
            FontFamily=new FontFamily("Segoe UI"); FontSize=14; Foreground=Ink;
            var theme=new ResourceDictionary { Source=new Uri("pack://application:,,,/Marvi;component/MenuTheme.xaml") };
            var scroll=new ScrollViewer { Content=body, VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
            var paper=new Border { Style=(Style)theme["Paper"], Child=scroll };
            Content=new Border { Style=(Style)theme["Frame"],Child=paper };
        }

        private TextBlock Text(string value,double size)
        {
            var label=new TextBlock { Text=value, FontSize=size, TextWrapping=TextWrapping.Wrap,
                Margin=new Thickness(0,4,0,5) };
            body.Children.Add(label);
            return label;
        }
        private void Heading(string value) { Text(value,20).FontWeight=FontWeights.SemiBold; }

        private TextBox Entry(string label,string value,bool multiline)
        {
            Text(label,14).FontWeight=FontWeights.SemiBold;
            var input=new TextBox { Text=value ?? "", Padding=new Thickness(6), Margin=new Thickness(0,0,0,8),
                MaxLength=multiline ? 2000 : 400, TextWrapping=TextWrapping.Wrap,
                AcceptsReturn=multiline, MinHeight=multiline ? 48 : 28, MaxHeight=90,
                VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
            AutomationProperties.SetName(input,label);
            body.Children.Add(input);
            return input;
        }
        private Button ActionButton(string label,Action action,bool primary=false)
        {
            var button=new Button { Content=label, Foreground=primary ? Brushes.White : Ink,
                Background=primary ? Leaf : new SolidColorBrush(Color.FromRgb(249,228,189)),
                HorizontalContentAlignment=HorizontalAlignment.Center, Padding=new Thickness(8,5,8,5),
                Margin=new Thickness(0,4,0,0), BorderBrush=new SolidColorBrush(Color.FromRgb(130,97,66)) };
            button.Click+=(s,e)=>action();
            body.Children.Add(button);
            return button;
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

        public void Render(StudySession s)
        {
            body.Children.Clear(); clock=null;
            if(s.Stage==StudyStage.Idle)
            {
                Heading("Estudiá conmigo");
                var goal=Entry("¿Qué querés hacer en esta sesión?","",false);
                Text("Podés dejar la meta en blanco.",12);
                var pom=new RadioButton {Content="Pomodoro · Avisame cuándo descansar",IsChecked=true,Margin=new Thickness(0,8,0,5)};
                var flow=new RadioButton {Content="Flowtime · Yo elijo cuándo parar",Margin=new Thickness(0,4,0,10)};
                body.Children.Add(pom); body.Children.Add(flow);
                ActionButton("Empezar",()=>pet.StartStudy(flow.IsChecked==true ? StudyMode.Flowtime : StudyMode.Pomodoro,goal.Text),true);
                return;
            }
            if(s.Stage==StudyStage.Summary)
            {
                Heading("Tu sesión de estudio");
                Text("Estudio · "+PetWindow.TotalText(s.FocusTicks),16);
                Text("Descanso · "+PetWindow.TotalText(s.BreakTicks),16);
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
                    ActionButton("Descansar 5 min",pet.PauseStudy,true);
                    ActionButton("Seguir 5 min",pet.ExtendStudy);
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
        }
    }
}
