using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using LitRes;
using LitRes.Models;
using RestSharp;

namespace LitResReadW10.Helpers
{
    public class TileImageHelper
    {
        public const string LiveTilePicsDirectoryName = "Shared\\ShellContent\\LiveTilePics";

        public static async Task<string> CreateNormalTileImage(string cover, UIElement tile)
        {
            var fileName = MD5.GetMd5String(cover);
            var filePath = Path.Combine(LiveTilePicsDirectoryName, fileName);

            if (FileExists(filePath)) return filePath;
            if (cover.Contains("http"))
            {
                var target = new RenderTargetBitmap();
                tile.Opacity = 1;
                await target.RenderAsync(tile);
                tile.Opacity = 0;
                var pixels = await target.GetPixelsAsync();
                
            }
            else
            {
            }

            return filePath;
        }

        private static bool FileExists(string path)
        {
            using (var isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return isf.FileExists(path);
            }
        }
    }
}
