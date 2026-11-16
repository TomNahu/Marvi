using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Marvi
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool first;
            using (var instance = new System.Threading.Mutex(true, @"Local\Marvi.Desktop", out first))
            {
                if (!first) return;
                var app = new Application();
                app.Run(new PetWindow());
            }
        }
    }

    internal sealed partial class PetWindow : Window
    {
        private readonly Image dog = new Image();
        private readonly BitmapImage[] poses = new BitmapImage[101];
        private readonly DispatcherTimer motion = new DispatcherTimer();
        private readonly DispatcherTimer study = new DispatcherTimer();
        private readonly Forms.NotifyIcon tray = new Forms.NotifyIcon();
        private readonly Forms.ContextMenuStrip trayMenu = new Forms.ContextMenuStrip();
        private readonly PetMenu menu = new PetMenu();

        private readonly string dataDirectory;
        private int frame;
        private int target;
        private bool breathing;
        private Point press;
        private PetProfile profile;
        private readonly Random random = new Random();
        private static readonly int[] Wag = { 9, 10, 9, 0, 9, 10, 9, 0 };
        private static readonly int[] Blink = { 46, 11, 46, 0 };
        private bool blinkNext = true;
        private static readonly int[] Cuddle = { 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 44, 43, 42, 41, 40, 41, 42, 43, 44, 45, 38, 37, 36, 0 };
        private static readonly int[] Woof = { 12, 13, 13, 12, 14, 12, 13, 12, 14, 0 };
        private static readonly int[] Howl = { 15, 16, 17, 17, 17, 17, 16, 15, 0 };
        private static readonly int[] RollRight = { 1, 2, 3, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 0 };
        private static readonly int[] RollLeft = { 1, 2, 3, 59, 58, 57, 56, 55, 54, 53, 52, 51, 50, 3, 2, 1, 0 };
        private static readonly int[] Stand = { 62, 63, 64, 65, 66, 67, 66, 67, 66, 65, 64, 63, 62, 0 };
        private static readonly int[] Dig = { 68, 69, 70, 71, 72, 70, 71, 72, 70, 71, 72, 73, 73, 73, 73, 68, 0 };
        private static readonly string[] Finds = { "hueso", "zapato", "pelota", "juguete" };
        private readonly Image foundObject = new Image {
            Width = 28, Height = 28, IsHitTestVisible = false, Visibility = Visibility.Collapsed,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom
        };
        private double rollStart;
        private double rollDistance;
        private int[] gesture;
        private int step;

        public PetWindow() : this(LocalData.DirectoryPath()) { }

        internal PetWindow(string dataDirectory) : this(dataDirectory,()=>DateTime.UtcNow) { }

        internal PetWindow(string dataDirectory,Func<DateTime> utcNow)
        {
            this.dataDirectory=dataDirectory;
            this.utcNow=utcNow;
            profile=PetProfile.Load(dataDirectory);
            inbox = GiftInbox.Load(dataDirectory);
            letters = new LetterLibrary(dataDirectory);
            dirtyInbox=inbox.UpgradeLetters(letters,random);
            Title = "Marvi";
            Width = 144;
            Height = 184;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = null;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            Left = SystemParameters.WorkArea.Right - Width - 24;
            Top = SystemParameters.WorkArea.Bottom - Height - 12;

            LoadProfile();
            RenderOptions.SetBitmapScalingMode(dog, BitmapScalingMode.NearestNeighbor);
            dog.Source = Pose(0);

            var stage = new Grid { Width=144, HorizontalAlignment=HorizontalAlignment.Center };
            dog.VerticalAlignment = VerticalAlignment.Bottom;
            dog.Stretch = Stretch.Uniform;
            RenderOptions.SetBitmapScalingMode(foundObject, BitmapScalingMode.NearestNeighbor);
            actor.Children.Add(dog);
            actor.Children.Add(foundObject);
            var carriedObject = new Canvas { Height=144, VerticalAlignment=VerticalAlignment.Bottom,
                IsHitTestVisible=false };
            carriedObject.Children.Add(parcel);
            actor.Children.Add(carriedObject);
            AddMealLayers();
            stage.Children.Add(actor);
            stage.Children.Add(barkBubble);
            stage.Children.Add(glow);
            stage.Children.Add(prize);
            var viewport = new Grid();
            viewport.Children.Add(stage);
            Content = viewport;
            dog.MouseLeftButtonDown += (s, e) => { press = e.GetPosition(this); dog.CaptureMouse(); };
            dog.MouseMove += OnDrag;
            dog.MouseLeftButtonUp += (s, e) =>
            {
                if (!dog.IsMouseCaptured) return;
                dog.ReleaseMouseCapture();
                ChangePose();
            };

            recallItem = AddItem("Llamar", Key.V, Recall);
            AddPetItem("Sentarse / acostarse", Key.S, ChangePose);
            AddPetItem("Acariciar", Key.A, () => BeginGesture(Cuddle));
            AddPetItem("Dar comida", Key.C, () => BeginGesture(Eat));
            AddPetItem("Dar agua", Key.B, () => BeginGesture(Drink));
            menu.Items.Add(new Separator());
            AddPetItem("Ladrar", Key.L, () => BeginGesture(Woof));
            AddPetItem("Aullar", Key.U, () => BeginGesture(Howl));
            AddPetItem("Rodar", Key.R, BeginRoll);
            AddPetItem("Pararse en dos patas", Key.P, () => BeginGesture(Stand));
            AddPetItem("Excavar", Key.E, BeginDig);
            menu.Items.Add(new Separator());
            AddItem("Nombre y pelaje…", Key.N, Customize);
            AddItem("Estudiá conmigo", Key.F, OpenStudy);
            AddItem("Ir a la casita", Key.H, GoHome);
            AddItem("Salir", Key.X, Close);
            AssignShortcuts();
            dog.MouseRightButtonUp += (s,e)=>{ e.Handled=true; OpenMenu(); };
            menu.IsVisibleChanged += (s,e) =>
            {
                if (!(bool)e.NewValue)
                    Dispatcher.BeginInvoke(new Action(() => { if (IsLoaded) TryStartGift(); }));
            };
            menu.AddHandler(Keyboard.PreviewKeyDownEvent,new KeyEventHandler(MenuKey),true);
            AddHandler(Keyboard.PreviewKeyDownEvent,new KeyEventHandler(MenuKey),true);
            trayMenu.Items.Add("Marvi");
            trayMenu.Opening += (s,e) =>
            {
                e.Cancel = true;
                Dispatcher.BeginInvoke(new Action(OpenMenu));
            };
            tray.ContextMenuStrip = trayMenu;
            tray.Icon = System.Drawing.SystemIcons.Application;
            tray.Text = profile.Name + " · Marvi";
            tray.DoubleClick += (s, e) => Recall();
            tray.Visible = true;

            InitializeStudy(viewport);
            InitializeGifts();
            motion.Tick += (s, e) => Animate();
            Loaded += (s, e) => { if (gesture == null && !StudyingNow) ScheduleIdle(); };
            Closed += (s, e) =>
            {
                motion.Stop();
                CloseStudyUI();
                giftTimer.Stop();
                PersistInbox();
                tray.Visible = false;
                tray.Dispose();
                trayMenu.Dispose();
                menu.Close();
            };
        }

        private void OnDrag(object sender, MouseEventArgs e)
        {
            if (!dog.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;
            var offset = e.GetPosition(this) - press;
            if (Math.Abs(offset.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(offset.Y) < SystemParameters.MinimumVerticalDragDistance) return;
            dog.ReleaseMouseCapture();
            if (gesture == RollRight || gesture == RollLeft)
            {
                gesture = null;
                dog.Source = Pose(frame);
                ScheduleIdle();
            }
            if(gesture==Delivery)
            {
                HideParcel();
                gesture=null;
                dog.Source=Pose(frame);
                returnHomeAfterGift=false;
                motion.Stop();
            }
            DragMove();
            if(gesture==null && !motion.IsEnabled) ScheduleIdle();
        }

        private BitmapImage Pose(int index)
        {
            if (poses[index] == null) poses[index] = PetProfile.Sprite(profile.Coat, index);
            return poses[index];
        }

        private void LoadProfile()
        {
            Array.Clear(poses, 0, poses.Length);
            dog.Source = Pose(frame);
            tray.Text = profile.Name + " · Marvi";
            dog.ToolTip = profile.Name + " · Clic para cambiar de postura · Clic derecho para opciones";
        }

        private void Customize()
        {
            var dialog = new ProfileDialog(profile,dataDirectory);
            if (IsVisible) dialog.Owner = this;
            if (dialog.ShowDialog() != true) return;
            profile = dialog.Result;
            LoadProfile();
        }

        private void ScheduleIdle()
        {
            motion.Stop();
            if (TryStartGift() || !IsVisible || StudyingNow) return;
            motion.Interval = TimeSpan.FromSeconds(random.Next(5, 12));
            motion.Start();
        }

        private void BeginRoll()
        {
            var screen = Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var bounds = screen.WorkingArea;
            // Screen reports device pixels. Convert through this window so the
            // travel limits follow the current monitor and its DPI.
            double roomLeft = Math.Max(0, -PointFromScreen(new Point(bounds.Left, bounds.Top)).X);
            double roomRight = Math.Max(0, PointFromScreen(new Point(bounds.Right, bounds.Top)).X - Width);
            bool right = random.Next(2) == 0;
            const double travel = 88;
            if ((right ? roomRight : roomLeft) < travel && (right ? roomLeft : roomRight) >= travel)
                right = !right;
            rollStart = Left;
            rollDistance = right ? Math.Min(roomRight, travel) : -Math.Min(roomLeft, travel);
            BeginGesture(right ? RollRight : RollLeft);
        }

        private void BeginDig()
        {
            var name = Finds[random.Next(Finds.Length)];
            foundObject.Source = new BitmapImage(new Uri("pack://application:,,,/Marvi;component/Assets/" + name + ".png"));
            BeginGesture(Dig);
        }

        private void BeginGesture(int[] sequence)
        {
            ClearMeal();
            if (sequence != Delivery) returnHomeAfterGift = false;
            HideParcel();
            foundObject.Visibility = Visibility.Collapsed;
            gesture = sequence;
            step = 0;
            target = 0;
            StartMotion();
        }

        private void ChangePose()
        {
            ClearMeal();
            returnHomeAfterGift = false;
            HideParcel();
            foundObject.Visibility = Visibility.Collapsed;
            gesture = null;
            target = target == 0 ? 6 : 0;
            StartMotion();
        }

        private void StartMotion()
        {
            motion.Interval = TimeSpan.FromMilliseconds(95);
            if (IsVisible) motion.Start();
        }

        private void DisplayGestureFrame()
        {
            int pose = gesture[step++];
            dog.Source = Pose(pose);
            double duration = 150;
            if (gesture == RollRight || gesture == RollLeft)
            {
                duration = 85;
                if (pose >= 50 && pose <= 59)
                {
                    double fraction = (pose - 50) / 9.0;
                    if (gesture == RollLeft) fraction = 1 - fraction;
                    double eased = fraction * fraction * (3 - 2 * fraction);
                    Left = rollStart + Math.Round(rollDistance * eased);
                }
            }
            else if (gesture == Delivery) duration = DrawDelivery(pose);
            else if (gesture == Eat || gesture == Drink) duration = DrawMeal();
            else if (gesture == Stand) duration = pose == 67 ? 310 : 125;
            else if (gesture == Dig)
            {
                duration = 115;
                if (pose == 73)
                {
                    int reveal = step - Array.IndexOf(Dig, 73);
                    int visible = Math.Min(28, reveal * 9);
                    foundObject.Margin = new Thickness(6, 0, 0, 10 - (28 - visible));
                    foundObject.Clip = new RectangleGeometry(new Rect(0, 0, 28, visible));
                    foundObject.Visibility = Visibility.Visible;
                    if (visible == 28) duration = 1500;
                }
                else foundObject.Visibility = Visibility.Collapsed;
            }
            else if (gesture == Blink) duration = 65;
            else if (gesture == Cuddle) duration = 95;
            motion.Interval = TimeSpan.FromMilliseconds(duration);
        }

        private void Animate()
        {
            if (frame != target)
            {
                frame += Math.Sign(target - frame);
                dog.Source = Pose(frame);
                return;
            }
            if (target == 0)
            {
                if (gesture == null)
                {
                    if (StudyingNow) { motion.Stop(); return; }
                    gesture = blinkNext ? Blink : Wag;
                    blinkNext = !blinkNext;
                    step = 0;
                }
                if (gesture == Delivery && step == Delivery.Length)
                {
                    FinishDelivery();
                    return;
                }
                DisplayGestureFrame();
                if (gesture != Delivery && step == gesture.Length)
                {
                    if(gesture==Eat || gesture==Drink) ClearMeal();
                    gesture = null;
                    ScheduleIdle();
                }
                return;
            }
            breathing = !breathing;
            dog.Source = Pose(breathing ? 7 : 8);
            motion.Interval = TimeSpan.FromSeconds(1.6);
        }

        private void GoHome()
        {
            returnHomeAfterGift = false;
            if(gesture==Delivery) { HideParcel(); gesture=null; dog.Source=Pose(frame); }
            motion.Stop();
            Hide();
        }

        private void Recall()
        {
            if (IsVisible) return;
            Show();
            if (TryStartGift()) return;
            if (frame != target || target != 0 || gesture != null) StartMotion();
            else ScheduleIdle();
        }

    }
}
