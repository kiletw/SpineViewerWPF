
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpineViewerWPF;

 public	static class Util {
#if WINDOWS_STOREAPP
		private static async Task<Texture2D> LoadFile(GraphicsDevice device, String path) {
			var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
			var file = await folder.GetFileAsync(path).AsTask().ConfigureAwait(false);
			try {
				return Util.LoadTexture(device, await file.OpenStreamForReadAsync().ConfigureAwait(false));
			} catch (Exception ex) {
				throw new Exception("Error reading texture file: " + path, ex);
			}
		}

		static public Texture2D LoadTexture (GraphicsDevice device, String path) {
			return LoadFile(device, path).Result;
		}
#else
		static public Texture2D LoadTexture (GraphicsDevice device, String path) {

#if WINDOWS_PHONE
            Stream stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(path);
            using (Stream input = stream)
            {
#else
            using (Stream input = new FileStream(path, FileMode.Open, FileAccess.Read)) {
#endif
				try {
					return Util.LoadTexture(device, input);
				} catch (Exception ex) {
					throw new Exception("Error reading texture file: " + path, ex);
				}
			}
		}
#endif

		static public Texture2D LoadTexture (GraphicsDevice device, Stream input) {
            NewTextureLoader ntl = new NewTextureLoader(device);
            return ntl.FromStreamFast(input, App.GV.PreMultiplyAlpha);
        }
	}

