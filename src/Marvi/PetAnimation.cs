using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace Marvi
{
    internal sealed partial class PetWindow
    {
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
            foundObject.Source = Decoration(name);
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
            // Cada dibujo conserva su propia duración. Al terminar el gesto se detiene el reloj rápido.
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
    }
}
