using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

// Build-time artwork only. All positions refer to the approved 144 px seated cel.
internal sealed class CareArtwork : IDisposable
{
    private readonly Bitmap reaching, stroking;

    public CareArtwork(string directory)
    {
        using(var sheet=new Bitmap(Path.Combine(directory,"caricias.png")))
        {
            reaching=Prop(sheet,new Rectangle(130,16,580,464),38);
            stroking=Prop(sheet,new Rectangle(786,49,630,460),40);
        }
    }

    // Sample whole source pixels, including the outline. Keep a transparent
    // margin around each completed prop throughout its approach and withdrawal.
    private static Bitmap Prop(Bitmap sheet, Rectangle bounds, int width)
    {
        int height=(int)Math.Round(bounds.Height*width/(double)bounds.Width);
        var result=new Bitmap(width,height);
        for(int y=0;y<height;y++)
        for(int x=0;x<width;x++)
        {
            int sx=bounds.X+(int)((x+0.5)*bounds.Width/width);
            int sy=bounds.Y+(int)((y+0.5)*bounds.Height/height);
            var ink=sheet.GetPixel(sx,sy);
            if(ink.A>=100) result.SetPixel(x,y,Color.FromArgb(255,ink.R,ink.G,ink.B));
        }
        return result;
    }

    public static Color Eyelid(Bitmap idle,int x,int y,bool half)
    {
        var ink=idle.GetPixel(x,y);
        if(x<52 || x>62 || y<36 || y>43) return ink;
        int edge=half ? 39 : 41;
        if(x<=53 || x>=61) edge--;
        if(y==edge && x>=53 && x<=61) return idle.GetPixel(55,41);
        if(y<edge || !half)
        {
            // Preserve the gold cheek marking below the eye.
            if(y>=42 && ink.R>165 && ink.G>120) return ink;
            return idle.GetPixel(x,35);
        }
        return ink;
    }

    // Six copper and three cream landmarks sampled from corresponding lit,
    // middle and shaded areas of the seated cel and the rest model sheet.
    private static readonly int[,] Rest = {
        {202,120,73},{186,105,66},{170,91,60},{150,78,54},{110,58,46},{88,51,47},
        {243,212,189},{200,164,142},{189,153,133},{41,19,27},{250,224,202}
    };
    private static readonly int[,] Seated = {
        {202,127,70},{183,106,60},{171,94,56},{146,76,48},{108,56,42},{81,45,40},
        {242,213,187},{203,172,146},{191,157,131},{38,16,27},{250,225,199}
    };

    public static void MatchRestPalette(Bitmap cel)
    {
        for(int y=0;y<cel.Height;y++)
        for(int x=0;x<cel.Width;x++)
        {
            Color c=cel.GetPixel(x,y);
            if(c.A==0) continue;
            double dr=0,dg=0,db=0,total=0;
            for(int i=0;i<Rest.GetLength(0);i++)
            {
                int r=c.R-Rest[i,0],g=c.G-Rest[i,1],b=c.B-Rest[i,2];
                double distance=1+r*r+g*g+b*b;
                double weight=1/(distance*distance);
                dr+=weight*(Seated[i,0]-Rest[i,0]);
                dg+=weight*(Seated[i,1]-Rest[i,1]);
                db+=weight*(Seated[i,2]-Rest[i,2]);
                total+=weight;
            }
            cel.SetPixel(x,y,Color.FromArgb(c.A,Channel(c.R+dr/total),
                Channel(c.G+dg/total),Channel(c.B+db/total)));
        }
    }

    private static int Channel(double n) { return Math.Max(0,Math.Min(255,(int)Math.Round(n))); }

    private static void Layer(Bitmap target,Bitmap sprite,int x,int y,double opacity)
    {
        if(x<0 || y<0 || x+sprite.Width>target.Width || y+sprite.Height>target.Height)
            throw new InvalidDataException("Un accesorio sale del lienzo.");
        using(var g=Graphics.FromImage(target))
        using(var attributes=new ImageAttributes())
        {
            var color=new ColorMatrix();
            color.Matrix33=(float)opacity;
            attributes.SetColorMatrix(color);
            g.DrawImage(sprite,new Rectangle(x,y,sprite.Width,sprite.Height),0,0,
                sprite.Width,sprite.Height,GraphicsUnit.Pixel,attributes);
        }
    }

    private static readonly Point[] Stroke = {
        new Point(95,3),new Point(82,2),new Point(67,2),new Point(53,2),new Point(43,2),
        new Point(46,2),new Point(50,2),new Point(54,3),new Point(58,4),new Point(61,3)
    };
    public void Draw(Bitmap cel,int pose)
    {
        int frame=pose-36;
        Point p=Stroke[frame];
        Layer(cel,frame>=4 ? stroking : reaching,p.X,p.Y,
            frame==0 ? 0.30 : frame==1 ? 0.70 : 1);
    }
    public void Dispose()
    {
        reaching.Dispose(); stroking.Dispose();
    }
}
