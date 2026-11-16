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
            tray.Icon = System.Drawing.Icon.ExtractAssociatedIcon(typeof(Program).Assembly.Location);
            tray.Text = profile.Name + " · Marvi";
            tray.DoubleClick += (s, e) => Recall();
            tray.Visible = true;

            InitializeStudy(viewport);
            InitializeShortcuts();
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
                var trayIcon = tray.Icon;
                tray.Dispose();
                if (trayIcon != null) trayIcon.Dispose();
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
            // Solo se decodifican los fotogramas usados del pelaje activo.
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
