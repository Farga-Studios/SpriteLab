using System.Text;

namespace SpriteLab.Core.Export;

public sealed record GifOptions(int Scale=1, int SpeedPercent=100, int Repetitions=0, bool Transparent=true, int BackgroundRgb=0x202630)
{
    public void Validate()
    {
        if (Scale is not (1 or 2 or 3 or 4 or 8) || SpeedPercent is <25 or >400 ||
            Repetitions is <0 or >65535 || BackgroundRgb is <0 or >0xffffff)
            throw new ArgumentException("Invalid GIF options.");
    }
}
public sealed record GifFrame(RasterFrame Raster,double DurationMs);

public static class AnimatedGif
{
    public static void Write(Stream output,int width,int height,IEnumerable<GifFrame> frames,GifOptions options,CancellationToken token=default)
    {
        options.Validate();
        if(width<1 || height<1 || (long)width*options.Scale>4096 || (long)height*options.Scale>4096 ||
            (long)width*height*options.Scale*options.Scale>4_194_304) throw new ArgumentException("GIF dimensions exceed limit.");
        token.ThrowIfCancellationRequested();
        int w=width*options.Scale,h=height*options.Scale;
        using var writer=new BinaryWriter(output,Encoding.ASCII,true);
        writer.Write("GIF89a"u8); writer.Write((ushort)w);writer.Write((ushort)h);
        writer.Write((byte)0xf7);writer.Write((byte)0);writer.Write((byte)0);
        writer.Write(new byte[768]); // global background index zero; each frame has a local palette
        writer.Write(new byte[]{0x21,0xff,11});writer.Write("NETSCAPE2.0"u8);
        writer.Write(new byte[]{3,1});writer.Write((ushort)options.Repetitions);writer.Write((byte)0);
        long totalPixels=0;int count=0;double elapsed=0;long emitted=0;
        foreach(var frame in frames)
        {
            token.ThrowIfCancellationRequested();
            if(++count>4096 || (totalPixels+=(long)w*h)>100_000_000)throw new ArgumentException("GIF animation exceeds work limit.");
            if(frame.Raster.Width!=width || frame.Raster.Height!=height || frame.Raster.Rgba.LongLength!=(long)width*height*4 ||
                !double.IsFinite(frame.DurationMs)||frame.DurationMs<=0)throw new ArgumentException("Invalid GIF frame.");
            elapsed+=frame.DurationMs*10/options.SpeedPercent; // centiseconds
            if(elapsed>360000)throw new ArgumentException("GIF duration exceeds one hour.");
            var delay=(int)Math.Clamp((long)Math.Round(elapsed)-emitted,1,65535);emitted+=delay;
            var indexed=Index(frame.Raster.Rgba,options,token);
            writer.Write(new byte[]{0x21,0xf9,4,(byte)(options.Transparent?9:8)});
            writer.Write((ushort)delay);writer.Write((byte)0);writer.Write((byte)0);
            writer.Write((byte)0x2c);writer.Write((ushort)0);writer.Write((ushort)0);
            writer.Write((ushort)w);writer.Write((ushort)h);writer.Write((byte)0x87);writer.Write(indexed.Palette);
            writer.Write((byte)8);
            // Legal bounded LZW stream: clear before dictionary reaches 512 entries.
            var block=new byte[255];int used=0,bits=0,acc=0,since=0;
            void Byte(byte b){block[used++]=b;if(used==255){writer.Write((byte)used);writer.Write(block);used=0;}}
            void Code(int code){acc|=code<<bits;bits+=9;while(bits>=8){Byte((byte)acc);acc>>=8;bits-=8;}}
            Code(256);
            for(int y=0;y<h;y++)
            {
                token.ThrowIfCancellationRequested();
                for(int x=0;x<w;x++)
                {
                    if(since==250){Code(256);since=0;}
                    Code(indexed.Indices[(y/options.Scale)*width+x/options.Scale]);since++;
                }
            }
            Code(257);if(bits>0)Byte((byte)acc);
            if(used>0){writer.Write((byte)used);writer.Write(block,0,used);}writer.Write((byte)0);
        }
        if(count==0)throw new ArgumentException("GIF requires frames.");
        writer.Write((byte)0x3b);
    }

    private static (byte[] Palette,byte[] Indices) Index(byte[] rgba,GifOptions o,CancellationToken token)
    {
        var palette=new byte[768];var indices=new byte[rgba.Length/4];var colors=new Dictionary<int,byte>();
        int next=1;bool overflow=false;
        int Color(int p)
        {
            int a=rgba[p+3];
            if(o.Transparent && a<128)return -1;
            int Blend(int value,int bg)=>o.Transparent?value:(value*a+bg*(255-a)+127)/255;
            return (Blend(rgba[p],o.BackgroundRgb>>16)<<16)|(Blend(rgba[p+1],(o.BackgroundRgb>>8)&255)<<8)|Blend(rgba[p+2],o.BackgroundRgb&255);
        }
        for(int i=0;i<indices.Length;i++)
        {
            if((i&4095)==0)token.ThrowIfCancellationRequested();
            int c=Color(i*4);if(c<0)continue;
            if(!colors.TryGetValue(c,out byte index))
            {
                if(next==256){overflow=true;break;}
                index=(byte)next++;colors.Add(c,index);
                palette[index*3]=(byte)(c>>16);palette[index*3+1]=(byte)(c>>8);palette[index*3+2]=(byte)c;
            }
            indices[i]=index;
        }
        if(!overflow)return(palette,indices);
        for(int r=0;r<6;r++)for(int g=0;g<7;g++)for(int b=0;b<6;b++)
        {int i=1+(r*7+g)*6+b;palette[i*3]=(byte)(r*51);palette[i*3+1]=(byte)((g*255+3)/6);palette[i*3+2]=(byte)(b*51);}
        for(int i=0;i<indices.Length;i++)
        {
            if((i&4095)==0)token.ThrowIfCancellationRequested();
            int c=Color(i*4);
            indices[i]=c<0?(byte)0:(byte)(1+(((c>>16)*5+127)/255*7+(((c>>8)&255)*6+127)/255)*6+((c&255)*5+127)/255);
        }
        return(palette,indices);
    }
}
