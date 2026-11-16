using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Marvi;

internal static class DesktopChecks
{
    private static string data;
    private static PetWindow pet;
    private static DateTime now = DateTime.UtcNow;
    private static Application app;
    private static bool measuring;

    [STAThread]
    private static int Main(string[] args)
    {
        data = Path.Combine(Path.GetTempPath(), "Marvi-check-" + Guid.NewGuid());
        Directory.CreateDirectory(data);
        try
        {
            app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            measuring = args.Contains("measure");
            if (measuring)
            {
                using (var budget = new MemoryBudget(256 * 1024 * 1024)) Measure();
                return 0;
            }
            CheckData();
            OpenPet();
            Pomodoro();
            Practice();
            Flowtime();
            Console.WriteLine("Correcto: todos los recorridos de escritorio.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            if (pet != null && pet.IsLoaded) pet.Close();
            if (app != null) app.Shutdown();
            // Solo se borra el directorio temporal único creado por esta ejecución.
            if (Path.GetDirectoryName(data) == Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar) &&
                Path.GetFileName(data).StartsWith("Marvi-check-")) Directory.Delete(data, true);
        }
    }

    private static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); }
    private static void Pump(int ms)
    {
        var frame = new DispatcherFrame();
        var stop = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        stop.Tick += (s,e) => { stop.Stop(); frame.Continue = false; };
        stop.Start(); Dispatcher.PushFrame(frame);
    }
    private static void OpenPet()
    {
        pet = new PetWindow(data, () => measuring ? DateTime.UtcNow : now);
        app.MainWindow = pet;
        pet.Show(); Pump(120);
    }
    private static T WindowOf<T>() where T : Window { return app.Windows.OfType<T>().Single(w => w.IsVisible); }
    private static IEnumerable<T> Children<T>(DependencyObject node) where T : DependencyObject
    {
        T match = node as T;
        if (match != null) yield return match;
        foreach (object child in LogicalTreeHelper.GetChildren(node))
            if (child is DependencyObject)
                foreach (T descendant in Children<T>((DependencyObject)child)) yield return descendant;
    }
    private static void Click(Window w, string title)
    {
        w.UpdateLayout(); Pump(35);
        var button = Children<Button>(w).FirstOrDefault(b => b.IsVisible && b.IsEnabled && (string)b.Content == title);
        Assert(button != null, "Botón ausente: " + title + " en " + w.Title);
        button.BringIntoView();
        ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)).Invoke();
        Pump(75);
    }
    private static TextBox Input(Window w, string label)
    {
        return Children<TextBox>(w).Single(b => AutomationProperties.GetName(b) == label);
    }
    private static bool HasText(Window w, string value) { return Children<TextBlock>(w).Any(t => t.IsVisible && t.Text.Contains(value)); }
    private static void Menu()
    {
        var dog = Children<Image>(pet).First(i => i.IsVisible && i.Source != null && i.IsHitTestVisible);
        dog.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) {
            RoutedEvent = UIElement.MouseRightButtonUpEvent });
        Pump(70);
    }
    private static StudyWindow Study()
    {
        Menu(); Click(WindowOf<PetMenu>(), "Estudiá conmigo");
        return WindowOf<StudyWindow>();
    }
    private static StudySession Session() { return StudySession.Load(Path.Combine(data, "estudio.json")); }
    private static void CheckData()
    {
        var old = Path.Combine(data, "sesion-antigua.json");
        File.WriteAllText(old, "{\"Mode\":0,\"Stage\":1,\"DueUtc\":12345}");
        var migrated = StudySession.Load(old);
        Assert(migrated.DueUtc == 12345 && migrated.FocusMinutes == 25 && migrated.BreakMinutes == 5 && migrated.ExtensionMinutes == 5, "Migración de Pomodoro");
        for (int seed = 0; seed < 100; seed++)
        {
            var topics = MixedMath.Examples.Select(p => p.Topic).Distinct().Take(2 + seed % 2).ToList();
            var mix = MixedMath.Mix(MixedMath.Examples, topics, new Random(seed));
            Assert(mix.Count == topics.Count * 3 && mix.Distinct().Count() == mix.Count, "Repetición en matemática");
            Assert(!mix.Zip(mix.Skip(1), (a,b) => a.Topic == b.Topic).Any(same => same), "Tipos consecutivos");
        }
        var library = new LetterLibrary(data);
        File.WriteAllText(Path.Combine(data, "Cartas", "propia.txt"), "Prueba local");
        Assert(library.Choose(new Random()) == "Prueba local", "Prioridad de las cartas personales");
        var poses = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../src/Marvi/Assets"), "dog-*.png");
        foreach (string file in poses)
        {
            int pose = Int32.Parse(Path.GetFileNameWithoutExtension(file).Substring(4));
            for (int coat = 0; coat < PetProfile.CoatNames.Length; coat++)
                Assert(PetProfile.Sprite(coat, pose).PixelWidth > 0, "Sprite faltante");
        }
        Console.WriteLine("Correcto: perfiles anteriores, cartas locales, pelajes y 100 mezclas.");
    }

    private static void Pomodoro()
    {
        var w = Study();
        var settings = Children<Expander>(w).Single(); settings.IsExpanded = true;
        Input(w, "Estudio (min)").Text = "0"; Click(w, "Empezar");
        Assert(HasText(w, "entre 1 y 1440"), "Permite una duración inválida");
        Input(w, "Estudio (min)").Text = "2"; Input(w, "Descanso (min)").Text = "3"; Input(w, "Extensión (min)").Text = "4";
        Input(w, "¿Qué querés hacer en esta sesión?").Text = "Resolver química";
        Click(w, "Empezar"); now = now.AddMinutes(1);
        pet.Close(); OpenPet(); w = Study();
        Assert(HasText(w, "Tiempo de estudio") && Session().FocusMinutes == 2, "Reapertura antes de vencer");
        pet.Close(); now = now.AddMinutes(3); OpenPet();
        w = WindowOf<StudyWindow>();
        Assert(Session().NoticeShown && Session().FocusTicks == TimeSpan.FromMinutes(2).Ticks, "Vencimiento durante cierre");
        pet.Close(); OpenPet();
        Assert(!app.Windows.OfType<StudyWindow>().Any(x => x.IsVisible), "Aviso duplicado al reabrir");
        w = Study(); Click(w, "Seguir 4 min");
        Assert(Session().DueUtc == now.AddMinutes(4).Ticks, "Duración de extensión");
        now = now.AddMinutes(5); Pump(1150);
        Click(w, "Descansar 3 min");
        Assert(Session().DueUtc == now.AddMinutes(3).Ticks, "Duración de descanso");
        Children<Expander>(w).Single().IsExpanded = true;
        Input(w, "Estudio (min)").Text = "7"; Click(w, "Guardar tiempos");
        Assert(Session().DueUtc == now.AddMinutes(3).Ticks, "Cambió el plazo en curso");
        now = now.AddMinutes(4); Pump(1150); Click(w, "Volver a estudiar");
        Assert(Session().DueUtc == now.AddMinutes(7).Ticks, "Vuelta con tiempo personalizado");
        Click(w, "Finalizar sesión");
        Assert(HasText(w, "Estudio · 6 min") && HasText(w, "Descanso · 3 min"), "Totales de Pomodoro");
        Click(w, "Dejá una pregunta para otro día");
        var p = WindowOf<PracticeWindow>();
        Input(p, "¿Qué querés preguntarte?").Text = "¿Por qué balancear?";
        Input(p, "Respuesta o referencia al apunte (opcional)").Text = "Masa · pág. 12";
        Children<DatePicker>(p).Single().SelectedDate = DateTime.Today;
        Click(p, "Guardar pregunta"); Click(p, "Cerrar"); Click(w, "Sí");
        Assert(!Session().Active && Session().Outcome == "Sí", "Cierre con meta");
        Console.WriteLine("Correcto: Pomodoro editable, extensión, cambios, totales, aviso único y reapertura.");
    }

    private static void Practice()
    {
        pet.Close(); OpenPet();
        var w = Study();
        Click(w, "Repasá con Marvi · 1 pregunta para hoy");
        var p = WindowOf<PracticeWindow>(); Click(p, "Empezar repaso");
        Assert(HasText(p, "¿Por qué balancear?") && !HasText(p, "Masa · pág. 12"), "La respuesta se adelantó");
        Click(p, "Ver respuesta o pista"); Assert(HasText(p, "pág. 12"), "Falta la referencia");
        Click(p, "Me salió"); Click(p, "Cerrar");
        Click(w, "Explicáselo a Marvi"); p = WindowOf<PracticeWindow>();
        Click(p, "Me quedó una duda"); Input(p, "¿Qué parte querés entender mejor?").Text = "¿Qué cambia en el segundo paso?";
        Click(p, "Guardar pregunta"); Click(p, "Cerrar");
        Click(w, "Repasá con Marvi"); p = WindowOf<PracticeWindow>(); Click(p, "Mis preguntas");
        var cards = Children<ListBox>(p).Single(); Assert(cards.Items.Count == 2, "Duda sin guardar");
        cards.SelectedIndex = 0; Click(p, "Repasar ahora"); Click(p, "Hoy paso"); Click(p, "Cerrar");
        Click(w, "Explicáselo a Marvi"); Click(WindowOf<PracticeWindow>(), "Lo pude explicar");
        Click(w, "Mezclá ejercicios"); p = WindowOf<PracticeWindow>(); Click(p, "Agregar ejercicio propio");
        Input(p, "Tipo de ejercicio").Text = "Fracciones"; Input(p, "Enunciado").Text = "¿Cuánto es la mitad de 3/4?";
        Input(p, "Solución o referencia al apunte (opcional)").Text = "3/8";
        Click(p, "Guardar ejercicio");
        foreach (var choice in Children<CheckBox>(p)) choice.IsChecked = (string)choice.Content != "Proporcionalidad";
        Click(p, "Empezar mezcla");
        for (int i = 1; i <= 3; i++)
        {
            Assert(HasText(p, "Ejercicio " + i + " de 3"), "Mezcla incompleta");
            Click(p, "Ver solución o pista"); Click(p, i == 3 ? "Terminar mezcla" : "Siguiente ejercicio");
        }
        Click(p, "Cerrar"); w.Close();
        var saved = PracticeBook.Load(Path.Combine(data, "practica.json"));
        Assert(saved.Problems.Single().Topic == "Fracciones" && saved.Cards.Count == 2, "Práctica sin persistir");
        Console.WriteLine("Correcto: repaso diferido, pista, duda, autoexplicación y mezcla con ejercicio propio.");
    }
    private static void Flowtime()
    {
        var w = Study();
        Children<RadioButton>(w).Single(r => ((string)r.Content).StartsWith("Flowtime")).IsChecked = true;
        Click(w, "Empezar"); now = now.AddMinutes(10); Click(w, "Tomar un descanso");
        Assert(HasText(w, "Pausa sugerida · 2 min"), "Proporción de Flowtime");
        Click(w, "Sin límite");
        Input(w, "¿Por dónde querés seguir?").Text = "Ejercicio 4";
        pet.Close(); OpenPet(); w = Study();
        Assert(Input(w, "¿Por dónde querés seguir?").Text == "Ejercicio 4", "Se perdió la última edición al cerrar");
        now = now.AddMinutes(2); Click(w, "Volver a estudiar");
        Assert(HasText(w, "Ejercicio 4"), "Falta la nota al retomar");
        now = now.AddMinutes(5); Click(w, "Tomar un descanso"); Click(w, "Empezar descanso"); Click(w, "Finalizar sesión");
        Assert(HasText(w, "Estudio · 15 min") && HasText(w, "Descanso · 2 min"), "Totales de Flowtime");
        Click(w, "Listo");
        Console.WriteLine("Correcto: Flowtime, nota al reabrir, pausa libre, dos tramos y salida durante el descanso.");
    }

    private static void Sample(string name, int seconds)
    {
        using (var process = Process.GetCurrentProcess())
        {
            var wall = Stopwatch.StartNew();
            TimeSpan start = process.TotalProcessorTime;
            // Un único despertar evita imputarle a Marvi el coste de muestrear la memoria continuamente.
            Pump(seconds * 1000);
            process.Refresh();
            double cpu = (process.TotalProcessorTime - start).TotalSeconds / wall.Elapsed.TotalSeconds * 100;
            Console.WriteLine("{0}: {1:F1} MiB residentes | {2:F1} MiB privados | {3:F2}% de un núcleo | {4:F1} s",
                name, process.WorkingSet64 / 1048576.0, process.PrivateMemorySize64 / 1048576.0, cpu, wall.Elapsed.TotalSeconds);
        }
    }
    private static void Measure()
    {
        using (var process = Process.GetCurrentProcess()) process.ProcessorAffinity = new IntPtr(1);
        OpenPet(); Pump(1600); Sample("Mascota visible", 15);
        Menu(); Click(WindowOf<PetMenu>(), "Rodar"); Sample("Rodar", 2);
        var w = Study(); Click(w, "Empezar"); Sample("Pomodoro con reloj visible", 12);
        Click(w, "Mezclá ejercicios"); var p = WindowOf<PracticeWindow>(); Click(p, "Empezar mezcla");
        Sample("Ejercicio y Pomodoro", 10); Click(p, "Ver solución o pista"); p.Close();
        Click(w, "Finalizar sesión"); Click(w, "Listo");
        Menu(); Click(WindowOf<PetMenu>(), "Ir a la casita"); Sample("Casita", 15);
    }
}
