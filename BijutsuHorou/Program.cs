using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using BooruSharp.Booru;
using BooruSharp.Search.Post;
using System;

namespace BijutsuHorou
{
    class Program
    {
        static DanbooruDonmai _danbooru = new DanbooruDonmai();
        static HttpClient _http = new HttpClient();
        static string darkDir;
        static string lightDir;

        static void Main()
        {
            var fileContent = File.ReadAllLines("danbooru.txt");
            var imageCount = 0;

            darkDir = fileContent[0];
            lightDir = fileContent[1];
            if (!Directory.Exists(darkDir))
                Directory.CreateDirectory(darkDir);
            if (!Directory.Exists(lightDir))
                Directory.CreateDirectory(lightDir);
            foreach (var line in fileContent.Skip(2))
            {
                imageCount++;
                Console.Write("\r" + imageCount + "/" + (fileContent.Length - 2));
                downloadImage(line).GetAwaiter().GetResult();
            }
        }

        private static async Task downloadImage(string line)
        {
            Bitmap bmp;
            var match = Regex.Match(line, "danbooru\\.donmai\\.us\\/posts\\/([0-9]+)");
            SearchResult result;

            if (!match.Success)
            {
                var md5 = Regex.Match(line, "danbooru\\.donmai\\.us\\/data\\/__[a-z0-9_]+__([a-z0-9]+)").Groups[1].Value;
                result = await _danbooru.GetImageByMd5Async(md5);
                bmp = new Bitmap(await (await _http.GetAsync(line)).Content.ReadAsStreamAsync());
            }
            else
            {
                var postId = int.Parse(match.Groups[1].Value);
                result = await _danbooru.GetImageByIdAsync(postId);
                bmp = new Bitmap(await (await _http.GetAsync((result.fileUrl.AbsoluteUri))).Content.ReadAsStreamAsync());
            }
            int pxNb = 0;
            int darkPxNb = 0;

            for (var xPos = 0; xPos < (bmp.Width - 1); xPos++)
                for (var yPos = 0; yPos < (bmp.Height - 1); yPos++)
                {
                    var tmp = bmp.GetPixel(xPos, yPos);
                    if ((tmp.R/255f) + (tmp.B / 255f) + (tmp.G / 255f) < 1.5)
                        ++darkPxNb;
                    ++pxNb;
                }
            bmp.Save((darkPxNb / (float)pxNb > 0.45f ? darkDir : lightDir) + "/" + result.md5 + ".png");
        }
    }
}
