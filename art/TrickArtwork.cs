using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static partial class SpriteArtist
{
    // Crop cells by their ink, then anchor them at floor level. Cropping changes
    // the offset only. Each entire action has a single isotropic drawing scale.
    private static Rectangle InkBounds(Bitmap sheet,Rectangle tile)
    {
        int l=tile.Right,r=tile.Left,t=tile.Bottom,b=tile.Top;
        for(int y=tile.Top;y<tile.Bottom;y++)
        for(int x=tile.Left;x<tile.Right;x++)
        {
            if(sheet.GetPixel(x,y).A<90) continue;
            l=Math.Min(l,x); r=Math.Max(r,x); t=Math.Min(t,y); b=Math.Max(b,y);
        }
        if(r<l || b<t) throw new InvalidDataException("Empty sprite cell");
        var bounds=Rectangle.FromLTRB(l-1,t-1,r+2,b+2);
        return Rectangle.Intersect(bounds,tile);
    }

    private static Bitmap Grounded(Bitmap sheet,int index,int rowSplit,double scale,int canvasHeight,int anchorX,
        int furRed,int furGreen,int furBlue)
    {
        int top=index<3 ? 0 : rowSplit, bottom=index<3 ? rowSplit : sheet.Height;
        int col=index%3;
        // The leaning ear of the second roll inbetween crosses the nominal grid
        // by a few pixels. Preserve the inter-cell gutter explicitly.
        int left=col*512, right=left+512;
        if(index<3 && rowSplit==496) { if(col==0) right=503; if(col==1) left=503; }
        Rectangle bounds=InkBounds(sheet,Rectangle.FromLTRB(left,top,right,bottom));
        int w=(int)Math.Round(bounds.Width*scale),h=(int)Math.Round(bounds.Height*scale);
        var dest=new Rectangle(anchorX-w/2,canvasHeight-13-h,w,h);
        if(dest.Left<1 || dest.Right>Grid-1 || dest.Top<1)
            throw new InvalidDataException("Trick sprite exceeds its canvas: "+index);
        var result=new Bitmap(Grid,canvasHeight);
        using(var g=Graphics.FromImage(result))
        {
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode=PixelOffsetMode.Half;
            g.DrawImage(sheet,dest,bounds,GraphicsUnit.Pixel);
        }
        // Balance the copper planes to the seated master's highlight; retain
        // the nose, eyes, white blaze, cream chest, and the transparent edge.
        for(int y=dest.Top;y<dest.Bottom;y++)
        for(int x=dest.Left;x<dest.Right;x++)
        {
            var c=result.GetPixel(x,y);
            if(c.A==0 || c.R<70) continue;
            double mix=Ease((c.R-c.G-25)/30.0)*Ease((c.G-c.B-10)/18.0);
            result.SetPixel(x,y,Color.FromArgb(c.A,Math.Max(0,c.R-(int)Math.Round(furRed*mix)),
                Math.Min(255,c.G+(int)Math.Round(furGreen*mix)), Math.Max(0,c.B-(int)Math.Round(furBlue*mix))));
        }
        return result;
    }

    private static void BuildTricks(string art,string output)
    {
        using(var roll=new Bitmap(Path.Combine(art,"rodar.png")))
        using(var between=new Bitmap(Path.Combine(art,"rodar-intermedios.png")))
        using(var stand=new Bitmap(Path.Combine(art,"dos-patas.png")))
        using(var dig=new Bitmap(Path.Combine(art,"excavar.png")))
        {
            int[] principal={50,52,55,57,59,61}, inbetweens={51,53,54,56,58,60};
            // The centre of the hind feet, rather than the shoulder, is the
            // stable pivot when the forepaws leave the ground.
            int[] standingCenters={70,75,78,73,74,78};
            for(int i=0;i<6;i++)
            {
                using(var cel=Grounded(roll,i,512,i==5 ? 0.298 : 0.273,144,72,8,1,3))
                    SaveCoats(cel,output,principal[i]);
                using(var cel=Grounded(between,i,496,0.273,144,72,13,0,7))
                    SaveCoats(cel,output,inbetweens[i]);
                using(var cel=Grounded(stand,i,460,i>=3 ? 0.324 : 0.307,184,standingCenters[i],8,0,6))
                    SaveCoats(cel,output,62+i);
                using(var cel=Grounded(dig,i,512,0.274,144,i==0?72:77,3,4,4))
                    SaveCoats(cel,output,68+i);
            }
        }
        using(var objects=new Bitmap(Path.Combine(art,"hallazgos.png")))
        {
            string[] names={"hueso","zapato","pelota","juguete"};
            for(int i=0;i<names.Length;i++)
            {
                int col=i%2,row=i/2;
                var cell=Rectangle.FromLTRB(col*objects.Width/2,row*objects.Height/2,
                    (col+1)*objects.Width/2,(row+1)*objects.Height/2);
                var crop=InkBounds(objects,cell);
                double scale=26.0/Math.Max(crop.Width,crop.Height);
                int w=(int)Math.Round(crop.Width*scale),h=(int)Math.Round(crop.Height*scale);
                using(var icon=new Bitmap(28,28))
                using(var g=Graphics.FromImage(icon))
                {
                    g.InterpolationMode=InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode=PixelOffsetMode.Half;
                    g.DrawImage(objects,new Rectangle((28-w)/2,27-h,w,h),crop,GraphicsUnit.Pixel);
                    icon.Save(Path.Combine(output,names[i]+".png"),ImageFormat.Png);
                }
            }
        }
    }

    private static void BuildMeals(string art,string output)
    {
        using(var dog=new Bitmap(Path.Combine(art,"comer-beber.png")))
        using(var props=new Bitmap(Path.Combine(art,"platos.png")))
        for(int i=0;i<6;i++)
        {
            // The six drawings share one rear-paw anchor even when only the
            // tongue moves. Never trim or rescale the torso between bites.
            using(var cel=Cel(dog,i,new Point(481,437),new Point(137,131),0.267f))
            {
                for(int y=0;y<cel.Height;y++)
                for(int x=0;x<cel.Width;x++)
                {
                    var ink=cel.GetPixel(x,y);
                    cel.SetPixel(x,y,ink.A<145 ? Color.Transparent : Color.FromArgb(255,ink.R,ink.G,ink.B));
                }
                CareArtwork.MatchRestPalette(cel);
                SaveCoats(cel,output,18+i);
            }
            string[] names={"comida-lleno","comida-medio","comida-migas","agua-quieta","agua-ondas","mano-plato"};
            var bounds=InkBounds(props,new Rectangle(i%3*512,i/3*512,512,512));
            int width=i==5 ? 24 : 34;
            int height=(int)Math.Round(bounds.Height*width/(double)bounds.Width);
            int targetW=i==5 ? 24 : 36, targetH=i==5 ? 16 : 26;
            using(var bit=new Bitmap(targetW,targetH))
            using(var g=Graphics.FromImage(bit))
            {
                g.InterpolationMode=InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode=PixelOffsetMode.Half;
                g.DrawImage(props,new Rectangle((targetW-width)/2,targetH-height,width,height),bounds,GraphicsUnit.Pixel);
                bit.Save(Path.Combine(output,names[i]+".png"),ImageFormat.Png);
            }
        }
    }

    private static void BuildDelivery(string art,string output)
    {
        BuildFetchTurns(art,output);
        using(var walk=new Bitmap(Path.Combine(art,"caminar.png")))
        for(int i=0;i<6;i++)
        using(var cel=Cel(walk,i,new Point(110,435),new Point(27,131),0.316f))
        {
            for(int y=0;y<cel.Height;y++)
            for(int x=0;x<cel.Width;x++)
            {
                var c=cel.GetPixel(x,y);
                if(c.A<237 || (c.A<247 && c.G>85 && c.R-c.G<18 && c.G-c.B<13))
                    cel.SetPixel(x,y,Color.Transparent);
                else
                    cel.SetPixel(x,y,Color.FromArgb(255,Math.Max(0,c.R-6),Math.Max(0,c.G-5),Math.Max(0,c.B-5)));
            }
            CareArtwork.MatchRestPalette(cel);
            SaveCoats(cel,output,81+i);
        }
        using(var voice=new Bitmap(Path.Combine(art,"voice.png")))
        for(int i=0;i<2;i++)
        using(var cel=Cel(voice,i,new Point(i==0 ? 454 : 422,436),new Point(115,131),0.287f))
            SaveCoats(cel,output,87+i);
        using(var bubble=new Bitmap(65,34))
        {
            SpeechBubble(bubble,"¡guau!",true);
            bubble.Save(Path.Combine(output,"aviso-guau.png"),ImageFormat.Png);
        }
        using(var bubble=new Bitmap(65,34))
        {
            SpeechBubble(bubble,"¡guau!",true,true);
            bubble.Save(Path.Combine(output,"aviso-guau-inverso.png"),ImageFormat.Png);
        }
        BuildGiftGlow(output);
        // The looking-down pose bridges the standing height and the low grip.
        // Use the original painted cel before the digging overlay is added.
        using(var sheet=new Bitmap(Path.Combine(art,"excavar.png")))
        using(var cel=Grounded(sheet,5,512,0.267,144,78,3,4,4))
            SaveCoats(cel,output,80);
        using(var sheet=new Bitmap(Path.Combine(art,"entrega.png")))
        {
            int[] left={0,500,980,0,505,1005}, right={490,970,1536,500,980,1536};
            for(int i=0;i<6;i++)
            {
                if(i==3) continue; // The walking cycle supplies the moving stride.
                var bounds=InkBounds(sheet,Rectangle.FromLTRB(left[i],i<3?0:495,right[i],i<3?490:1024));
                int w=(int)Math.Round(bounds.Width*0.267),h=(int)Math.Round(bounds.Height*0.267);
                using(var cel=new Bitmap(Grid,Grid))
                using(var g=Graphics.FromImage(cel))
                {
                    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode=PixelOffsetMode.Half;
                    g.DrawImage(sheet,new Rectangle(78-w/2,131-h,w,h),bounds,GraphicsUnit.Pixel);
                    CareArtwork.MatchRestPalette(cel);
                    SaveCoats(cel,output,74+i);
                }
            }
        }
        using(var sheet=new Bitmap(Path.Combine(art,"ramos.png")))
        for(int i=0;i<6;i++)
        {
            int left=i%3==0?0:i%3==1?503:1005;
            int right=i%3==0?500:i%3==1?995:1536;
            var bounds=InkBounds(sheet,Rectangle.FromLTRB(left,i<3?0:512,right,i<3?512:1024));
            double scale=68.0/Math.Max(bounds.Width,bounds.Height);
            int w=(int)Math.Round(bounds.Width*scale),h=(int)Math.Round(bounds.Height*scale);
            using(var icon=new Bitmap(72,72))
            using(var g=Graphics.FromImage(icon))
            {
                g.InterpolationMode=InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode=PixelOffsetMode.Half;
                g.DrawImage(sheet,new Rectangle((72-w)/2,(72-h)/2,w,h),bounds,GraphicsUnit.Pixel);
                icon.Save(Path.Combine(output,"ramo-"+i+".png"),ImageFormat.Png);
            }
        }
        using(var letter=new Bitmap(32,24))
        using(var g=Graphics.FromImage(letter))
        using(var outline=new Pen(Color.FromArgb(81,49,43),2))
        using(var paper=new SolidBrush(Color.FromArgb(255,231,187)))
        using(var wax=new SolidBrush(Color.FromArgb(164,57,65)))
        {
            g.FillRectangle(paper,2,3,27,18);
            g.DrawRectangle(outline,2,3,27,18);
            g.DrawLines(outline,new[]{new Point(3,5),new Point(15,13),new Point(28,5)});
            g.FillRectangle(wax,12,11,7,4);
            g.FillRectangle(wax,14,15,3,2);
            letter.Save(Path.Combine(output,"sobre.png"),ImageFormat.Png);
        }
    }

    private static void BuildFetchTurns(string art,string output)
    {
        string[] sheets={"giro-ramo.png","buscar-ramo.png"};
        for(int sheetIndex=0;sheetIndex<sheets.Length;sheetIndex++)
        using(var sheet=new Bitmap(Path.Combine(art,sheets[sheetIndex])))
        for(int i=0;i<6;i++)
        {
            // The ground line and scale match the forward walking cycle.
            int floor=sheetIndex==0 ? (i==5 ? 439 : 435) : 431;
            using(var cel=Cel(sheet,i,new Point(256,floor),new Point(72,131),0.316f))
            {
                for(int y=0;y<cel.Height;y++)
                for(int x=0;x<cel.Width;x++)
                {
                    Color ink=cel.GetPixel(x,y);
                    if(ink.A<160) cel.SetPixel(x,y,Color.Transparent);
                    else cel.SetPixel(x,y,Color.FromArgb(255,Math.Max(0,ink.R-6),
                        Math.Max(0,ink.G-5),Math.Max(0,ink.B-5)));
                }
                CareArtwork.MatchRestPalette(cel);
                SaveCoats(cel,output,89+6*sheetIndex+i);
            }
        }
    }

    private static void BuildGiftGlow(string output)
    {
        // A small indexed-looking atlas effect: amber halo, tapered rays and
        // four-point glints. Work on a half-resolution pixel grid.
        using(var glow=new Bitmap(64,52))
        {
            for(int y=0;y<52;y++)
            for(int x=0;x<64;x++)
            {
                double dx=x-31.5,dy=(y-25.5)*1.22,r=Math.Sqrt(dx*dx+dy*dy);
                if(r>31) continue;
                double angle=Math.Atan2(dy,dx);
                double beam=Math.Pow(Math.Max(0,Math.Cos(13*angle)),5);
                int alpha=(int)((1-r/32)*(115+120*beam));
                if(r>15 && beam>0.72 && r<27) alpha+=64;
                glow.SetPixel(x,y,Color.FromArgb(Math.Min(245,alpha),255,205+(int)(28*beam),101));
            }
            using(var g=Graphics.FromImage(glow))
            using(var gold=new SolidBrush(Color.FromArgb(248,186,80)))
            using(var light=new SolidBrush(Color.FromArgb(255,249,199)))
            {
                foreach(var star in new[]{new Point(10,18),new Point(50,9),new Point(53,36),new Point(19,44)})
                {
                    g.FillRectangle(gold,star.X-3,star.Y,7,1);
                    g.FillRectangle(gold,star.X,star.Y-4,1,9);
                    g.FillRectangle(light,star.X-1,star.Y-1,3,3);
                }
            }
            glow.Save(Path.Combine(output,"brillo.png"),ImageFormat.Png);
        }
    }

    private static void DiggingSoil(Bitmap image,int pose)
    {
        using(var g=Graphics.FromImage(image))
        using(var earth=new SolidBrush(Color.FromArgb(133,86,55)))
        using(var dark=new SolidBrush(Color.FromArgb(76,48,43)))
        using(var light=new SolidBrush(Color.FromArgb(192,141,88)))
        {
            int width=pose==69 ? 10 : pose==70 ? 19 : 25;
            g.FillEllipse(earth,8,129,width+3,8);
            g.FillEllipse(dark,10,129,width-2,5);
            g.FillRectangle(light,13,134,5,1);
            if(pose>=71)
            {
                g.FillRectangle(earth,35,131,6,3);
                g.FillRectangle(light,37,130,2,2);
                g.FillRectangle(earth,43,132,2,2);
            }
            if(pose==71 || pose==72)
            {
                int dx=pose==71 ? 0 : 8;
                g.FillRectangle(earth,39+dx,120-dx/2,3,2);
                g.FillRectangle(light,43+dx,123,2,2);
                g.FillRectangle(dark,36+dx,126-dx/2,3,2);
            }
        }
    }
}


