using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;//if this throws error just reinstall it

namespace BadApple
{
    class Program
    {
        //some video things to show them correctly in terminal
        const double WIDTH_OFFSET= 1;
        const int MAX_WIDTH = 200;
        //if video is quicker then terminal do some magic with numbers down there
        const int FRAME_RATIO = 5555;//how many frames should be drawn evenly
        const double FRAMES_TOT = 10000;//from a certain number of frames 
        //(if the video is faster, we draw fewer frames)


        /// <summary>
        /// viewer discretion: this cancerous piece of code probably will crash. 
        /// 
        /// on the other hand what did you expect
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //create temp folder to store frames
            if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "tmp")))
                Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "tmp"), true);
            else Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "tmp"));
            //specifying the path to ffmpeg
            FFmpeg.SetExecutablesPath(Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg-4.4-full_build", "bin"));
            //the task for getting frames
            MainAsync().Wait();
            //the task for drawing in console
            SecondTask().Wait();
        }

        public static async Task MainAsync()
        {
            Console.WriteLine("Start");
            //if console not in fully scrolled mode(dunno how to name that) we need to scroll it down to the depths of hell
            for (var i = 1; i < 35000; i++)
            {
                Console.WriteLine();
            }
            //get video
            IMediaInfo info = await FFmpeg.GetMediaInfo(Path.Combine(Directory.GetCurrentDirectory(), "badapple.mp4")).ConfigureAwait(false);

            //function for frame name generation
            Func<string, string> outputFileNameBuilder = (number) => { return Path.Combine(Directory.GetCurrentDirectory(), "tmp",  number + ".png"); };
            //set frame output to png
            IVideoStream videoStream = info.VideoStreams.First()?.SetCodec(VideoCodec.png);
            //get every frame of video
            IConversionResult conversionResult = await FFmpeg.Conversions.New()
                                                .AddStream(videoStream)
                                                .ExtractEveryNthFrame(1, outputFileNameBuilder)
                                                .Start();
        }

        public static async Task SecondTask()
        {
            //we use StringBuilder to create the illusion of optimized code
            var sb = new StringBuilder();
            //converter class(the strangest thing out there)
            var conv = new BitmapConverter();
            var c = 1;
            //this is how we get our frame files
            var frames = Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), "tmp")).OrderBy(x => File.GetCreationTime(x)).ToList();
            var first = DateTime.Now;
            var list = new List<int>();
            //lines of code down there are trying to slow (or speed) video down (or up). be gentle with them
            for(int i = 0; i < FRAME_RATIO; i++)
            {
                list.Add((int)Math.Round(1 + i * (FRAMES_TOT / FRAME_RATIO)));
            }
            //we need some music to light this party up
            Process.Start(Path.Combine(Directory.GetCurrentDirectory(),"badapple_sound.mp3"));
            foreach (var frame in frames)
            {
                if (list.Contains(c))
                {   
                    await Write(conv, sb, frame);  
                }
                c++;
                if (c == FRAMES_TOT)
                {
                    c = 1;
                }
            }
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "tmp"), true);
        }
        
        //this is how we draw frames
        public static async Task Write(BitmapConverter conv,StringBuilder sb,string frame)
        {
            var bitmap = new Bitmap(frame);
            bitmap = Resize(bitmap);
            var rows = conv.Convert(bitmap, sb);
            Console.WriteLine(rows);
        }

        //try and fit our bitmap into our tiny terminal window(probably won`t work lol, try combinating this with lowest possible scale of terminal font)
        static Bitmap Resize(Bitmap bitmap)
        {
            var newHeight = bitmap.Height / WIDTH_OFFSET * MAX_WIDTH / bitmap.Width;
            if (bitmap.Width > MAX_WIDTH || bitmap.Height > newHeight)
                bitmap = new Bitmap(bitmap, new Size(MAX_WIDTH, (int)newHeight));
            return bitmap;
        }
    }

    public class BitmapConverter
    {
        //symbols in ascending order of " brightness"
        readonly char[] _asciiTable = { '.', ',', ':', '+', '*', '?', '%', 'S', '#', '@' };
        //we bring the color to some kind grayscale
        public string Convert(Bitmap bitmap,StringBuilder sb)
        {
            sb.Clear();

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    int avg = (pixel.R + pixel.G + pixel.B) / 3;
                    bitmap.SetPixel(x, y, Color.FromArgb(pixel.A, avg, avg, avg));
                    int mapIndex = (int)Map(bitmap.GetPixel(x, y).R, 255, _asciiTable.Length - 1);
                    sb.Append(_asciiTable[mapIndex]);
                }
                sb.Append("\n");
            }
            return sb.ToString();
        }

        //here the magic happens(i dunno what it does, i was too drunk, it just works)
        float Map(float vtm,float stop1,float stop2)
        {
            return (vtm) / (stop1) * (stop2);
        }
    }
}
