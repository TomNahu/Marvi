using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static partial class SpriteArtist
{
    private const int Grid = 144;

    // Fit the approved full-resolution artwork once, with one scale on both axes.
    // These source-space anchors put its paws on the original desktop baseline.
    private static Bitmap FitOriginal(Bitmap source)
    {
        var result = new Bitmap(Grid, Grid, PixelFormat.Format32bppArgb);
        const float scale = 0.108f;
        using (var graphics = Graphics.FromImage(result))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(source, new RectangleF(5, -4, source.Width * scale, source.Height * scale),
                new RectangleF(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
        }
        return result;
    }

    private static double Ease(double value)
    {
        value = Math.Max(0, Math.Min(1, value));
        return value * value * (3 - 2 * value);
    }

    private static PointF Joint(double x, double y, int pose)
    {
        double tail = Ease((x - 99) / 11) * (1 - Ease((y - 78) / 23));
        double wag = pose == 9 || pose == 42 ? 0.5 : pose == 10 || pose == 43 ? 1 : 0;
        return new PointF((float)(x + 7 * tail * wag), (float)(y + 9 * tail * wag));
    }
    private static Color Pigment(Bitmap master, int x, int y, int skin, int pose)
    {
        Color ink = master.GetPixel(x, y);
        if (ink.A == 0) return ink;
        if(pose==11 || pose==46 || (pose>=40 && pose<=45))
            ink=CareArtwork.Eyelid(master,x,y,pose==46);
        if (skin >= 3) return SolidCoat(ink,skin);
        // The original three coats keep the master's cream markings.
        bool fur = ink.R > 65 && ink.R - ink.G > 33 && ink.G - ink.B > 13;
        if (skin == 0 || !fur) return ink;
        int light = (ink.R * 3 + ink.G * 6 + ink.B) / 10;
        if (skin == 1) return Color.FromArgb(ink.A, light - 13, light, Math.Min(255, light + 14));
        return Color.FromArgb(ink.A, light * 87 / 100, light * 61 / 100, light * 55 / 100);
    }

    private static readonly Color[] SolidBases = {
        Color.FromArgb(153,95,57), Color.FromArgb(58,56,61),
        Color.FromArgb(153,151,151), Color.FromArgb(244,241,235)
    };

    private static Color SolidCoat(Color c,int skin)
    {
        // Eye reflections, mouth, tongue and the dark contour are separate inks.
        if(c.R<62 || (c.R>228 && c.R-c.B<14) || (c.R>100 && c.B>c.G+3)) return c;
        // Copper and cream in the master have different local base values.
        // Map both to a common light plane before applying the solid pigment.
        double cream=Ease((71-(c.R-c.G))/35.0);
        double level=(c.R*0.3+c.G*0.6+c.B*0.1)/(145+74*cream);
        double shade=Math.Max(0.23,Math.Min(1.06,level));
        Color basis=SolidBases[skin-3];
        return Color.FromArgb(c.A, Math.Min(255,(int)Math.Round(basis.R*shade)),
            Math.Min(255,(int)Math.Round(basis.G*shade)),Math.Min(255,(int)Math.Round(basis.B*shade)));
    }
    private static double Edge(PointF a,PointF b,double x,double y)
    { return (b.X-a.X)*(y-a.Y)-(b.Y-a.Y)*(x-a.X); }

    private static void Cell(Bitmap canvas,PointF a,PointF b,PointF c,PointF d,Color color)
    {
        int l=(int)Math.Floor(Math.Min(Math.Min(a.X,b.X),Math.Min(c.X,d.X)));
        int r=(int)Math.Ceiling(Math.Max(Math.Max(a.X,b.X),Math.Max(c.X,d.X)));
        int t=(int)Math.Floor(Math.Min(Math.Min(a.Y,b.Y),Math.Min(c.Y,d.Y)));
        int h=(int)Math.Ceiling(Math.Max(Math.Max(a.Y,b.Y),Math.Max(c.Y,d.Y)));
        for(int y=Math.Max(t,0);y<Math.Min(h,Grid);y++)
        for(int x=Math.Max(l,0);x<Math.Min(r,Grid);x++)
        {
            double u=x+0.5, v=y+0.5;
            if(Edge(a,b,u,v)>=0 && Edge(b,c,u,v)>=0 && Edge(c,d,u,v)>=0 && Edge(d,a,u,v)>=0)
                canvas.SetPixel(x,y,color);
        }
    }

    private static void SpeechBubble(Bitmap image, string word, bool standalone=false, bool tailRight=false)
    {
        using (var g = Graphics.FromImage(image))
        using (var ink = new SolidBrush(Color.FromArgb(61,40,43)))
        using (var cream = new SolidBrush(Color.FromArgb(255,241,217)))
        {
            Point[] cloud = {
                new Point(88,5),new Point(94,5),new Point(94,2),new Point(106,2),
                new Point(106,4),new Point(115,4),new Point(115,2),new Point(128,2),
                new Point(128,5),new Point(135,5),new Point(135,9),new Point(140,9),
                new Point(140,21),new Point(137,21),new Point(137,25),new Point(128,25),
                new Point(128,27),new Point(101,27),new Point(101,25),new Point(96,25),
                new Point(86,31),new Point(88,23),new Point(83,23),new Point(83,19),
                new Point(81,19),new Point(81,10),new Point(85,10),new Point(85,7),new Point(88,7)
            };
            if(standalone) g.TranslateTransform(-79,0);
            if(tailRight)
                for(int i=0;i<cloud.Length;i++) cloud[i].X=222-cloud[i].X;
            g.FillPolygon(cream, cloud);
            using (var line = new Pen(ink,2)) g.DrawPolygon(line, cloud);
            int left = 111 - (word.Length * 10 - 2) / 2;
            foreach (char letter in word)
            {
                string[] glyph;
                switch(letter)
                {
                    case 'A': glyph=new[]{"0110","1001","1111","1001","1001"};break;
                    case 'G': glyph=new[]{"0111","1000","1011","1001","0111"};break;
                    case 'g': glyph=new[]{"0000","0111","1001","0111","0001","0110"};break;
                    case '¡': glyph=new[]{"0010","0000","0010","0010","0010"};break;
                    case '!': glyph=new[]{"0100","0100","0100","0000","0100"};break;
                    case 'a': glyph=new[]{"0000","0110","0001","0111","0111"};break;
                    default: glyph=new[]{"0000","1001","1001","1001","0111"};break;
                }
                for(int row=0; row<glyph.Length; row++)
                    for(int col=0; col<4; col++)
                        if(glyph[row][col]=='1') g.FillRectangle(ink,left+2*col,10+2*row,2,2);
                left+=10;
            }
        }
    }
    // Each source anchor is measured in its own 512px cell. Anchor paws for
    // rest and voice, the centre of the bowl for care. Use one scale per sheet.
    private static Bitmap Cel(Bitmap sheet, int index, Point anchor, Point destination, float scale)
    {
        var tile = new Rectangle(index % 3 * 512, index / 3 * 512, 512, 512);
        var image = new Bitmap(Grid, Grid);
        using (var g = Graphics.FromImage(image))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            var target = new RectangleF(destination.X-anchor.X*scale, destination.Y-anchor.Y*scale,
                512*scale, 512*scale);
            g.DrawImage(sheet, target, tile, GraphicsUnit.Pixel);
        }
        return image;
    }

    private static void SaveCoats(Bitmap drawing, string output, int pose, CareArtwork care = null)
    {
        for (int skin = 0; skin < 7; skin++)
        using (var image = new Bitmap(drawing.Width,drawing.Height))
        {
            for (int y=0; y<drawing.Height; y++)
            for (int x=0; x<drawing.Width; x++)
            {
                Color ink = Pigment(drawing,x,y,skin,pose);
                if (ink.A==0) continue;
                if (pose==9 || pose==10 || pose==42 || pose==43)
                    Cell(image,Joint(x,y,pose),Joint(x+1,y,pose),Joint(x+1,y+1,pose),Joint(x,y+1,pose),ink);
                else image.SetPixel(x,y,ink);
            }
            if (pose>=12 && pose<=17) SpeechBubble(image,pose>=15 ? "Auuuu" : "Guau");
            if (pose>=69 && pose<=73) DiggingSoil(image,pose);
            if (care!=null) care.Draw(image,pose);
            string prefix = skin==0 ? "dog" : "coat"+skin;
            image.Save(Path.Combine(output,prefix+"-"+pose+".png"),ImageFormat.Png);
        }
    }

    public static void Main(string[] args)
    {
        string art=args[0];
        string output=Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        using(var original=new Bitmap(Path.Combine(art,"pitbull-master.png")))
        using(var idle=FitOriginal(original))
        using(var rest=new Bitmap(Path.Combine(art,"rest.png")))
        using(var voice=new Bitmap(Path.Combine(art,"voice.png")))
        using(var care=new CareArtwork(art))
        {
            foreach(int pose in new[]{0,9,10,11,46}) SaveCoats(idle,output,pose);
            int[] restRight={456,435,424,474,461,442};
            int[] restFloor={483,483,486,449,449,449};
            int[] voiceRight={454,422,422,454,422,422};
            int[] voiceFloor={436,436,436,436,436,436};
            for(int i=0;i<6;i++)
            {
                using(var pose=Cel(rest,i,new Point(restRight[i],restFloor[i]),new Point(119,131),0.27f))
                {
                    CareArtwork.MatchRestPalette(pose);
                    SaveCoats(pose,output,1+i);
                    if(i==5)
                    {
                        SaveCoats(pose,output,7);
                        using(var breath=new Bitmap(Grid,Grid))
                        using(var g=Graphics.FromImage(breath))
                        {
                            // One-pixel rise at the withers. The paws stay on the baseline.
                            g.InterpolationMode=InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode=PixelOffsetMode.Half;
                            g.DrawImage(pose,new Rectangle(0,-2,144,146));
                            SaveCoats(breath,output,8);
                        }
                    }
                }
                using(var pose=Cel(voice,i,new Point(voiceRight[i],voiceFloor[i]),new Point(115,131),0.287f))
                    SaveCoats(pose,output,12+i);
            }
            for(int pose=36;pose<=45;pose++) SaveCoats(idle,output,pose,care);
        }
        BuildMeals(art,output);
        BuildTricks(art,output);
        BuildDelivery(art,output);
    }
}
