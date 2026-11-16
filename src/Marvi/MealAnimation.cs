using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Marvi
{
    internal sealed partial class PetWindow
    {
        private sealed class MealFrame
        {
            public int Pose, Contents, Placement, Duration;
            public double DishOpacity;
        }

        private static readonly MealFrame[] FoodFrames=MakeMeal(false), WaterFrames=MakeMeal(true);
        private static readonly int[] Eat=Array.ConvertAll(FoodFrames,f=>f.Pose);
        private static readonly int[] Drink=Array.ConvertAll(WaterFrames,f=>f.Pose);
        private static readonly string[] MealArt={"comida-lleno","comida-medio","comida-migas","agua-quieta","agua-ondas","mano-plato"};
        private readonly BitmapImage[] mealArt=new BitmapImage[MealArt.Length];
        private readonly Image dish=Layer(36), frontRim=Layer(36), servingHand=Layer(24);

        // The bowl touches the floor at stop 5. Only the wrist moves after that.
        private static readonly Point[] DishPath={new Point(19,72),new Point(20,78),new Point(20,87),
            new Point(18,96),new Point(15,104),new Point(12,109)};
        private static readonly Point[] WristRetreat={new Point(-2,-2),new Point(-3,-6),new Point(-3,-12)};
        private const int HandGone=9;

        private static MealFrame[] MakeMeal(bool water)
        {
            var frames=new List<MealFrame>();
            int contents=water ? 3 : 0;
            Action<int,int,int,int,double> add=(pose,fill,place,ms,opacity)=>frames.Add(new MealFrame {
                Pose=pose,Contents=fill,Placement=place,Duration=ms,DishOpacity=opacity });
            for(int i=0;i<HandGone;i++) add(0,contents,i,i==5 ? 190 : 95,i==0 ? .48 : 1);
            foreach(int pose in new[]{0,75,80,18,19}) add(pose,contents,HandGone,130,1);
            for(int bite=0;bite<4;bite++)
            {
                if(water)
                {
                    add(22,3,HandGone,90,1); add(23,4,HandGone,135,1);
                    add(22,4,HandGone,90,1); add(19,3,HandGone,145,1);
                }
                else
                {
                    add(20,contents,HandGone,145,1);
                    if(bite==1) contents=1;
                    if(bite==3) contents=2;
                    add(21,contents,HandGone,165,1);
                    add(20,contents,HandGone,90,1); add(19,contents,HandGone,125,1);
                }
            }
            foreach(int pose in new[]{18,80,75,74,0}) add(pose,contents,HandGone,125,1);
            add(0,contents,HandGone,100,.65); add(0,contents,HandGone,100,.3);
            add(0,contents,HandGone,90,0);
            return frames.ToArray();
        }

        private void AddMealLayers()
        {
            var back=new Canvas { Height=144, VerticalAlignment=VerticalAlignment.Bottom,IsHitTestVisible=false };
            var front=new Canvas { Height=144, VerticalAlignment=VerticalAlignment.Bottom,IsHitTestVisible=false };
            dish.Height=frontRim.Height=26;
            servingHand.Height=16;
            frontRim.Clip=new RectangleGeometry(new Rect(0,18,36,8));
            back.Children.Add(dish);
            front.Children.Add(frontRim);
            front.Children.Add(servingHand);
            Panel.SetZIndex(front,2);
            actor.Children.Insert(0,back);
            actor.Children.Add(front);
        }

        private BitmapImage MealImage(int index)
        {
            return mealArt[index] ?? (mealArt[index]=Decoration(MealArt[index]));
        }

        private int DrawMeal()
        {
            var beat=(gesture==Eat ? FoodFrames : WaterFrames)[step-1];
            Point position=DishPath[Math.Min(beat.Placement,DishPath.Length-1)];
            // At rest the saucer is in front of the paws. Once the muzzle dips
            // into it only the near rim occludes the tongue or lower jaw.
            bool headInBowl=beat.Pose>=18 && beat.Pose<=23;
            Panel.SetZIndex((UIElement)dish.Parent,headInBowl ? -1 : 1);
            dish.Source=frontRim.Source=MealImage(beat.Contents);
            Canvas.SetLeft(dish,position.X); Canvas.SetLeft(frontRim,position.X);
            Canvas.SetTop(dish,position.Y); Canvas.SetTop(frontRim,position.Y);
            dish.Opacity=frontRim.Opacity=beat.DishOpacity;
            dish.Visibility=frontRim.Visibility=Visibility.Visible;
            if(beat.Placement<HandGone)
            {
                int retreat=beat.Placement-DishPath.Length;
                Point offset=retreat>=0 ? WristRetreat[retreat] : new Point();
                servingHand.Source=MealImage(5);
                Canvas.SetLeft(servingHand,Math.Max(0,position.X-15+offset.X));
                Canvas.SetTop(servingHand,position.Y+4+offset.Y);
                servingHand.Opacity=retreat<0 ? beat.DishOpacity : 1-retreat*.34;
                servingHand.Visibility=Visibility.Visible;
            }
            else servingHand.Visibility=Visibility.Collapsed;
            return beat.Duration;
        }

        private void ClearMeal()
        {
            dish.Visibility=frontRim.Visibility=servingHand.Visibility=Visibility.Collapsed;
        }
    }
}
