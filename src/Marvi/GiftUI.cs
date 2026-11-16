using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Marvi
{
    internal sealed partial class PetWindow
    {
        private readonly GiftInbox inbox;
        private readonly LetterLibrary letters;
        private readonly GiftClock giftClock = new GiftClock(GiftClock.Awake());
        private readonly DispatcherTimer giftTimer = new DispatcherTimer();
        private readonly Dictionary<Key,Button> shortcuts = new Dictionary<Key,Button>();
        private readonly List<KeyValuePair<Key,Button>> actions = new List<KeyValuePair<Key,Button>>();
        private Key flowerKey, letterKey, rereadKey;
        private Button recallItem;
        private Window letterWindow;
        private bool returnHomeAfterGift;
        private Gift presentation;
        private bool dirtyInbox;
        private bool reportedSaveError;
        private int secretProgress;

        private void OpenMenu()
        {
            AssignShortcuts();
            recallItem.Visibility=IsVisible ? Visibility.Collapsed : Visibility.Visible;
            menu.Open();
        }

        private Button AddItem(string label, Key key, Action action)
        {
            var item=new Button { Content=label, Tag="Ctrl + Shift + "+key };
            item.Click+=(s,e)=>{ if(menu.IsVisible) menu.Hide(); action(); };
            actions.Add(new KeyValuePair<Key,Button>(key,item));
            menu.Items.Add(item);
            return item;
        }

        private void AssignShortcuts()
        {
            shortcuts.Clear();
            var used=new HashSet<Key>();
            flowerKey=ChooseKey(Key.F8,used);
            letterKey=ChooseKey(Key.F9,used);
            rereadKey=ChooseKey(Key.F7,used);
            // Retain the original reread chord on desktops where it is free.
            if(PetMenu.KeyAvailable(Key.M)) used.Add(Key.M);
            foreach(var pair in actions)
                if(!used.Contains(pair.Key) && PetMenu.KeyAvailable(pair.Key))
                {
                    used.Add(pair.Key);
                    shortcuts.Add(pair.Key,pair.Value);
                }
            foreach(var pair in actions)
            {
                Key actual=pair.Key;
                if(!shortcuts.ContainsValue(pair.Value))
                {
                    actual=ChooseKey(Key.None,used);
                    if(actual!=Key.None) shortcuts.Add(actual,pair.Value);
                }
                string label=actual>=Key.D0 && actual<=Key.D9 ? ((int)actual-(int)Key.D0).ToString() : actual.ToString();
                pair.Value.Tag=actual==Key.None ? "" : "Ctrl + Shift + "+label;
            }
        }

        private static Key ChooseKey(Key preferred,HashSet<Key> used)
        {
            if(preferred!=Key.None && !used.Contains(preferred) && PetMenu.KeyAvailable(preferred))
            { used.Add(preferred); return preferred; }
            Key[] alternatives={Key.D1,Key.D2,Key.D3,Key.D4,Key.D5,Key.D6,Key.D7,Key.D8,Key.D9,Key.D0,
                Key.F1,Key.F2,Key.F3,Key.F4,Key.F5,Key.F6,Key.F10,Key.F11,Key.D,Key.G,Key.K,Key.Y};
            foreach(Key key in alternatives)
                if(!used.Contains(key) && PetMenu.KeyAvailable(key)) { used.Add(key); return key; }
            return Key.None;
        }

        private Button AddPetItem(string label,Key key,Action action)
        {
            return AddItem(label,key,()=>{ if(!IsVisible) Show(); action(); });
        }

        private void MenuKey(object sender, KeyEventArgs e)
        {
            // Reconoce el acorde simultáneo y la secuencia manteniendo Ctrl dentro de Marvi.
            if(e.Handled || e.IsRepeat) return;
            if(Keyboard.Modifiers==ModifierKeys.Control)
            {
                if(e.Key==Key.LeftCtrl || e.Key==Key.RightCtrl) return;
                bool fullChord=Keyboard.IsKeyDown(Key.M) && Keyboard.IsKeyDown(Key.A) && Keyboard.IsKeyDown(Key.R);
                if(fullChord && (e.Key==Key.M || e.Key==Key.A || e.Key==Key.R))
                {
                    secretProgress=0;
                    ToggleSpecials();
                }
                else if(e.Key==Key.M) secretProgress=1;
                else if(secretProgress==1 && e.Key==Key.A) secretProgress=2;
                else if(secretProgress==2 && e.Key==Key.R)
                {
                    secretProgress=0;
                    ToggleSpecials();
                }
                else { secretProgress=0; return; }
                e.Handled=true;
                return;
            }
            secretProgress=0;
            if(HandleShortcut(e.Key,Keyboard.Modifiers)) e.Handled=true;
        }

        private void ToggleSpecials()
        {
            inbox.SpecialsUnlocked=!inbox.SpecialsUnlocked;
            dirtyInbox=true;
            PersistInbox();
            if(menu.IsVisible) menu.Hide();
            returnHomeAfterGift=false;
            if(letterWindow!=null) letterWindow.Close();
            if(!IsVisible) Show();
            BeginGesture(Howl);
        }

        private bool HandleShortcut(Key key, ModifierKeys modifiers)
        {
            if((!menu.IsActive && !IsActive) || modifiers!=(ModifierKeys.Control|ModifierKeys.Shift)) return false;
            if(key==Key.None) return false;
            if(key==flowerKey || key==letterKey)
            {
                if(!inbox.SpecialsUnlocked) return true;
                if(menu.IsVisible) menu.Hide();
                QueueGifts(1,key==letterKey);
                if(!IsVisible) Recall();
                TryStartGift();
                return true;
            }
            if(inbox.SpecialsUnlocked && (key==rereadKey || key==Key.M) && inbox.Latest!=null && presentation==null)
            {
                if(menu.IsVisible) menu.Hide();
                ReadGift(inbox.Latest);
                return true;
            }
            Button item;
            if(!shortcuts.TryGetValue(key,out item)) return false;
            if(!item.IsEnabled || item.Visibility!=Visibility.Visible) return true;
            item.RaiseEvent(new RoutedEventArgs(Button.ClickEvent,item));
            return true;
        }

        private void InitializeGifts()
        {
            KeyEventHandler release=(s,e)=>{ if(e.Key==Key.LeftCtrl || e.Key==Key.RightCtrl) secretProgress=0; };
            menu.PreviewKeyUp+=release;
            PreviewKeyUp+=release;
            Deactivated+=(s,e)=>secretProgress=0;
            menu.Deactivated+=(s,e)=>secretProgress=0;
            giftTimer.Tick+=(s,e)=>CheckGiftClock();
            Loaded+=(s,e)=>CheckGiftClock();
            tray.BalloonTipClicked+=(s,e)=>{ if(inbox.SpecialsUnlocked && inbox.Pending.Count>0) Recall(); };
        }

        private bool IsSitting()
        {
            return frame==0 && target==0 && (gesture==null || gesture==Blink || gesture==Wag ||
                gesture==Cuddle || gesture==Woof || gesture==Howl ||
                ((gesture==Eat || gesture==Drink) && (step==0 || gesture[step-1]==0)));
        }

        private void CheckGiftClock()
        {
            DateTime now=utcNow().ToLocalTime();
            TimeSpan awake=GiftClock.Awake();
            bool newSlot;
            int gifts=giftClock.Poll(now,awake,inbox.SpecialsUnlocked && (!IsVisible || IsSitting()),inbox,random.Next,out newSlot);
            if(newSlot) dirtyInbox=true;
            if(gifts>0 && inbox.SpecialsUnlocked) QueueGifts(gifts,false);
            else PersistInbox();
            PresentScheduledGifts();
            giftTimer.Interval=giftClock.Delay(now,awake);
            giftTimer.Start();
        }

        private void PresentScheduledGifts()
        {
            if(!inbox.SpecialsUnlocked || inbox.Pending.Count==0 || StudyingNow || practiceWindow!=null || menu.IsVisible || OwnedWindows.Count>0) return;
            if(!IsVisible)
            {
                returnHomeAfterGift=true;
                Recall();
            }
            else TryStartGift();
        }

        private void QueueGifts(int count,bool letterOnly)
        {
            for(int i=0;i<count;i++)
                inbox.Pending.Add(new Gift { Flower=random.Next(Gift.Flowers.Length),
                    Letter=letters.Choose(random), LetterOnly=letterOnly });
            dirtyInbox=true; PersistInbox();
        }

        private void PersistInbox()
        {
            if(!dirtyInbox) return;
            try { inbox.Save(); dirtyInbox=false; reportedSaveError=false; }
            catch(IOException) { ReportSaveError(); }
            catch(UnauthorizedAccessException) { ReportSaveError(); }
        }

        private void ReportSaveError()
        {
            if(reportedSaveError) return;
            reportedSaveError=true;
            tray.ShowBalloonTip(5000,"Marvi","La entrega está en memoria. Marvi volverá a intentar guardarla.",Forms.ToolTipIcon.Warning);
        }

        private bool TryStartGift()
        {
            if(!inbox.SpecialsUnlocked || inbox.Pending.Count==0 || !IsVisible || StudyingNow || practiceWindow!=null || menu.IsVisible ||
                OwnedWindows.Count>0 || (gesture!=null && gesture!=Wag && gesture!=Blink)) return false;
            BeginGesture(Delivery);
            var gift=presentation=inbox.Pending[0];
            SetFetchPath();
            parcel.Source=GiftIcon(gift);
            return true;
        }

        private static BitmapImage GiftIcon(Gift gift)
        {
            return Decoration(gift.LetterOnly ? "sobre" : "ramo-"+gift.Flower);
        }

        private void ReadGift(Gift gift)
        {
            if(!inbox.SpecialsUnlocked || gift==null || letterWindow!=null) return;
            var dialog=new Window { Title="Carta de "+profile.Name, Width=400,
                MinWidth=280,MinHeight=220, ShowActivated=false, Topmost=true, ShowInTaskbar=false,
                Background=new SolidColorBrush(Color.FromRgb(255,243,219)) };
            dialog.Owner=this;
            var area=MonitorArea();
            dialog.Width=Math.Min(dialog.Width,area.Width);
            double next=Left>=area.Left+dialog.Width+8 ? Left-dialog.Width-8 : Left+Width+8;
            dialog.Left=Math.Max(area.Left,Math.Min(area.Right-dialog.Width,next));
            letterWindow=dialog;
            var panel=new DockPanel { Margin=new Thickness(21) };
            var heading=new StackPanel { Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,14) };
            DockPanel.SetDock(heading,Dock.Top);
            var icon=new Image { Source=GiftIcon(gift),Width=62,Height=62 };
            RenderOptions.SetBitmapScalingMode(icon,BitmapScalingMode.NearestNeighbor);
            heading.Children.Add(icon);
            heading.Children.Add(new TextBlock { Text=gift.Label,Margin=new Thickness(12,0,0,0),
                VerticalAlignment=VerticalAlignment.Center,FontFamily=new FontFamily("Consolas"),FontSize=19 });
            panel.Children.Add(heading);
            var done=new Button {Content="Cerrar",Padding=new Thickness(12,4,12,4),
                HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,12,0,0)};
            DockPanel.SetDock(done,Dock.Bottom); panel.Children.Add(done);
            var note=new TextBox { Text=gift.Letter,IsReadOnly=true,TextWrapping=TextWrapping.Wrap,
                FontFamily=new FontFamily("Georgia"),FontSize=17,Foreground=Brushes.SaddleBrown,
                VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Background=Brushes.Transparent,
                BorderThickness=new Thickness(0),Padding=new Thickness(3) };
            panel.Children.Add(note);
            note.Measure(new Size(dialog.Width-60,280));
            dialog.Height=Math.Min(area.Height,Math.Max(dialog.MinHeight,190+note.DesiredSize.Height));
            dialog.Top=Math.Max(area.Top,Math.Min(area.Bottom-dialog.Height,Top+Height-dialog.Height));
            done.Click+=(s,e)=>dialog.Close();
            dialog.PreviewKeyDown+=(s,e)=>{ if(e.Key==Key.Escape) { e.Handled=true; dialog.Close(); } };
            dialog.Closed+=(s,e)=>
            {
                letterWindow=null;
                if(!IsLoaded || !IsVisible) return;
                if(returnHomeAfterGift && inbox.Pending.Count==0) GoHome();
                else if(gesture==null) ScheduleIdle();
            };
            dialog.Content=panel;
            dialog.Show();
        }
    }
}
