using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace Marvi
{
    internal sealed partial class PetWindow
    {
        private enum FetchPhase { Look, Empty, Grip, Floor, Bark, Reveal }

        private sealed class FetchFrame
        {
            public readonly int Pose, Milliseconds;
            public readonly double Distance;
            public readonly FetchPhase Phase;
            public FetchFrame(int pose, FetchPhase phase, double distance, int milliseconds)
            {
                Pose=pose; Phase=phase; Distance=distance; Milliseconds=milliseconds;
            }
        }

        private static readonly FetchFrame[] Fetch = MakeFetch();
        private static readonly int[] Delivery = Array.ConvertAll(Fetch, f=>f.Pose);
        private readonly Grid actor = new Grid { RenderTransformOrigin=new Point(.5,.5) };
        private readonly Image parcel=Layer(36);
        private readonly Image barkBubble=Layer(65);
        private readonly Image glow=Layer(128);
        private readonly Image prize=Layer(80);
        // Allow blooms to extend past the muzzle without scaling the dog.
        private const double GiftGutter=32;
        private double fetchOrigin, fetchTop, fetchDistance, fetchRise, extraHeight;
        private double roomAtDrop;
        private static readonly ScaleTransform FacingOtherWay=FrozenMirror();

        private static ScaleTransform FrozenMirror()
        {
            var mirror=new ScaleTransform(-1,1);
            mirror.Freeze();
            return mirror;
        }

        private static Image Layer(int width)
        {
            var image=new Image { Width=width, Stretch=Stretch.Uniform, Visibility=Visibility.Collapsed,
                IsHitTestVisible=false, HorizontalAlignment=HorizontalAlignment.Left,
                VerticalAlignment=VerticalAlignment.Bottom, SnapsToDevicePixels=true };
            RenderOptions.SetBitmapScalingMode(image,BitmapScalingMode.NearestNeighbor);
            return image;
        }

        private static FetchFrame[] MakeFetch()
        {
            // Una secuencia sincroniza pose, desplazamiento, ramo, dos ladridos y el brillo de siete segundos.
            var beats=new List<FetchFrame>();
            Action<int,FetchPhase,double,int> add=(pose,phase,d,t)=>beats.Add(new FetchFrame(pose,phase,d,t));
            foreach(int pose in new[]{74,75,78,89,90}) add(pose,FetchPhase.Look,0,130);
            foreach(int pose in new[]{91,92,93,94}) add(pose,FetchPhase.Empty,0,140);
            const int awaySteps=16, homeSteps=18;
            for(int i=0;i<awaySteps;i++) add(95+i%4,FetchPhase.Empty,(i+1.0)/awaySteps,100);
            foreach(int pose in new[]{99,100}) add(pose,FetchPhase.Empty,1,155);
            foreach(int pose in new[]{100,99,95,94,93,92,91,90,89,78}) add(pose,FetchPhase.Grip,1,145);
            for(int i=0;i<homeSteps;i++) add(81+i%6,FetchPhase.Grip,1-(i+1.0)/homeSteps,95);
            foreach(int pose in new[]{75,80,76}) add(pose,FetchPhase.Grip,0,145);
            foreach(int pose in new[]{76,80,75,79,74}) add(pose,FetchPhase.Floor,0,135);
            for(int bark=0;bark<2;bark++)
            {
                add(87,FetchPhase.Bark,0,90);
                add(88,FetchPhase.Bark,0,340);
                add(87,FetchPhase.Bark,0,90);
                add(0,FetchPhase.Floor,0,230);
            }
            add(0,FetchPhase.Reveal,0,7000);
            return beats.ToArray();
        }

        private Rect MonitorArea()
        {
            if(PresentationSource.FromVisual(this)==null) return SystemParameters.WorkArea;
            var screen=Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
            var area=screen.WorkingArea;
            var tl=PointFromScreen(new Point(area.Left,area.Top));
            var br=PointFromScreen(new Point(area.Right,area.Bottom));
            return new Rect(Left+tl.X,Top+tl.Y,br.X-tl.X,br.Y-tl.Y);
        }

        private void SetFetchPath()
        {
            var area=MonitorArea();
            fetchOrigin=Left;
            fetchTop=Top;
            fetchRise=Math.Min(24,Math.Max(0,Top-area.Top));
            double right=Math.Max(0,area.Right-Left-Width);
            double left=Math.Max(0,Left-area.Left);
            const double stride=126;
            bool headRight=right>=stride+GiftGutter || right>=left;
            double room=headRight ? right : left;
            fetchDistance=(headRight ? 1 : -1)*Math.Min(stride,Math.Max(0,room-GiftGutter));
            roomAtDrop=headRight ? left : right;
            Width+=2*GiftGutter;
            Left=fetchOrigin-GiftGutter;
            barkBubble.Source=Decoration(fetchDistance<0 ? "aviso-guau-inverso" : "aviso-guau");
        }

        private void HideParcel()
        {
            if(presentation!=null)
            {
                Width-=2*GiftGutter;
                Left=fetchOrigin;
                Top=fetchTop;
            }
            if(extraHeight>0) { Height-=extraHeight; extraHeight=0; }
            actor.RenderTransform=Transform.Identity;
            glow.BeginAnimation(UIElement.OpacityProperty,null);
            prize.RenderTransform=Transform.Identity;
            parcel.Visibility=barkBubble.Visibility=glow.Visibility=prize.Visibility=Visibility.Collapsed;
            presentation=null;
        }

        private void ShowPrize()
        {
            double space=Math.Max(0,Top-MonitorArea().Top);
            extraHeight=Math.Min(92,space);
            Top-=extraHeight;
            Height+=extraHeight;
            glow.Source=Decoration("brillo");
            prize.Source=parcel.Source;
            double center=Height-54;
            glow.Margin=new Thickness(8,0,0,center-52);
            prize.Margin=new Thickness(32,0,0,center-40);
            glow.Visibility=prize.Visibility=Visibility.Visible;
            // Three pixel-aligned sizes, then hold. The timing is owned by the
            // final frame so a queued letter cannot appear during the reveal.
            var scale=new ScaleTransform(1,1);
            prize.RenderTransformOrigin=new Point(.5,.5);
            prize.RenderTransform=scale;
            var grow=new DoubleAnimationUsingKeyFrames();
            grow.KeyFrames.Add(new DiscreteDoubleKeyFrame(.5,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            grow.KeyFrames.Add(new DiscreteDoubleKeyFrame(.75,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110))));
            grow.KeyFrames.Add(new DiscreteDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(230))));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty,grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,grow);
            var shimmer=new DoubleAnimationUsingKeyFrames { Duration=TimeSpan.FromMilliseconds(1200),
                RepeatBehavior=RepeatBehavior.Forever };
            shimmer.KeyFrames.Add(new DiscreteDoubleKeyFrame(1,KeyTime.FromTimeSpan(TimeSpan.Zero)));
            shimmer.KeyFrames.Add(new DiscreteDoubleKeyFrame(.77,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600))));
            glow.BeginAnimation(UIElement.OpacityProperty,shimmer);
        }

        private static readonly Dictionary<string, BitmapImage> decorations = new Dictionary<string, BitmapImage>();
        private static BitmapImage Decoration(string name)
        {
            // El conjunto de accesorios es fijo. Compartir las imágenes evita decodificarlas en cada entrega.
            BitmapImage image;
            if (decorations.TryGetValue(name, out image)) return image;
            image=new BitmapImage(new Uri("pack://application:,,,/Marvi;component/Assets/"+name+".png"));
            image.Freeze();
            decorations.Add(name, image);
            return image;
        }

        private double DrawDelivery(int pose)
        {
            var beat=Fetch[step-1];
            Left=fetchOrigin-GiftGutter+Math.Round(fetchDistance*beat.Distance);
            Top=fetchTop-Math.Round(fetchRise*beat.Distance);
            actor.RenderTransform=beat.Phase!=FetchPhase.Look && fetchDistance<0 ? FacingOtherWay : Transform.Identity;
            bool barking=beat.Phase==FetchPhase.Bark;
            barkBubble.Margin=new Thickness(fetchDistance<0 ? 3 : 78,0,0,113);
            barkBubble.Visibility=barking ? Visibility.Visible : Visibility.Collapsed;
            if(beat.Phase==FetchPhase.Reveal) ShowPrize();
            if(beat.Phase==FetchPhase.Look || beat.Phase==FetchPhase.Empty || beat.Phase==FetchPhase.Reveal)
                parcel.Visibility=Visibility.Collapsed;
            else
                PlaceParcel(pose,beat.Phase==FetchPhase.Grip);
            return beat.Milliseconds;
        }

        private void PlaceParcel(int pose,bool held)
        {
            // Register the tip of the stems to the lower lip. Let the blossoms
            // swing below it as the muzzle turns from profile to front view.
            // Lay the bouquet in front of the paws when possible. At a screen
            // edge a flatter grip, or an inward drop, keeps the petals visible.
            bool dropInward=roomAtDrop<22;
            bool flatDrop=!dropInward && roomAtDrop<GiftGutter;
            Point lip=new Point(38,flatDrop ? 122 : 115);
            double floorAngle=dropInward ? -244 : flatDrop ? -25 : -41;
            double angle=floorAngle;
            if(held)
            {
                switch(pose)
                {
                    case 100: lip=new Point(111,109); angle=-242; break;
                    case 99: lip=new Point(115,77); angle=-201; break;
                    case 95: lip=new Point(108,52); angle=-193; break;
                    case 94: lip=new Point(109,50); angle=-192; break;
                    case 93: lip=new Point(112,52); angle=-189; break;
                    case 92: lip=new Point(108,53); angle=-178; break;
                    case 91: lip=new Point(91,53); angle=-160; break;
                    case 90: lip=new Point(66,57); angle=-141; break;
                    case 89: lip=new Point(58,57); angle=-121; break;
                    case 78: lip=new Point(42,49); angle=-101; break;
                    case 75: lip=new Point(40,69); angle=dropInward ? -158 : -86; break;
                    case 80: lip=new Point(37,88); angle=dropInward ? -190 : flatDrop ? -80 : -69; break;
                    case 76: lip=new Point(34,111); angle=floorAngle; break;
                    default: lip=new Point(42,55); angle=dropInward ? -117 : -96; break;
                }
            }
            bool letter=presentation.LetterOnly;
            double size=letter ? 36 : 62;
            Point grip=letter ? new Point(30,19) : new Point(50,52);
            parcel.Width=parcel.Height=size;
            parcel.RenderTransform=new RotateTransform(angle+(letter ? 36 : 0),grip.X,grip.Y);
            Canvas.SetLeft(parcel,lip.X-grip.X);
            Canvas.SetTop(parcel,lip.Y-grip.Y);
            Panel.SetZIndex((UIElement)parcel.Parent,held && pose>=93 ? -1 : 1);
            parcel.Visibility=Visibility.Visible;
        }

        private void FinishDelivery()
        {
            motion.Stop();
            var gift=presentation;
            HideParcel();
            gesture=null;
            dog.Source=Pose(0);
            ReadGift(gift);
            inbox.Pending.Remove(gift);
            inbox.Latest=gift;
            dirtyInbox=true;
            PersistInbox();
            ScheduleIdle();
        }
    }
}
