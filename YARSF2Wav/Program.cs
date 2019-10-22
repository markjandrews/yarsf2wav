using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YARSF2Wav
{
    class Program
    {
        static void Main(string[] args)
        {
            var root_dir = args[0];
            var out_dir = args[1];

            out_dir = Path.GetFullPath(out_dir);
            Directory.CreateDirectory(out_dir);

            string[] files = Directory.GetFiles(root_dir, "*.rsf", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var rel_file = file.Replace(root_dir, "");
                rel_file = String.Join("-", rel_file.Split(new char[] { Path.DirectorySeparatorChar })).TrimStart(new char[] { '-' }).Replace(" ", "_");
                var rsf = new ResourceSoundFile(file);
                var wf = new WaveFile(1, rsf.SoundSampleRate, rsf.SoundData);

                var s = wf.Save<MemoryStream>();

                rel_file = Path.ChangeExtension(rel_file, ".wav");
                var out_file = Path.Combine(new string[] { out_dir, rel_file });
                Console.WriteLine(out_file);
                s.WriteTo(new FileStream(out_file, FileMode.Create));
                s.Close();

                
                //var soundPlayer = new System.Media.SoundPlayer(s);
                //soundPlayer.Play();
                //Thread.Sleep(1000);
            }
        }
    }
}
