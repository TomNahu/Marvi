using System;
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
        private Window letterWindow;
        private bool returnHomeAfterGift;
        private Gift presentation;
        private bool dirtyInbox;
        private bool reportedSaveError;

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

        private void InitializeGifts()
        {
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
