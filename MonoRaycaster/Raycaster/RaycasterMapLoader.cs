using System;
using System.Xml;
using System.Xml.Schema;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OpenGLTest
{
	/*
	 * RaycasterMap
	 * 
	 * Class for storage of the texture indexes for the Ceilings, Walls, and Floors.
	 * Additionally, stores the Bitmap data for the textures and skybox.
	 */
	public class RaycasterMap
	{
		public int[,] Map;
		public int[,] Floors;
		public int[,] Ceiling;
		public int Height;
		public int Width;
		public int StartX = 0;
		public int StartY = 0;
		public int[] skyInts;
		public int[][] textureInts;
		public BitmapData[] textures;
		public BitmapData skyTexture;
		public IntPtr skyScan0;
		public IntPtr[] textureScan0;

		public RaycasterMap () { }

		public void LoadMapFromFile(string filePath, string fileName)
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (filePath + "\\" + fileName);

			XmlNode textureNode = doc.GetElementsByTagName ("Texture")[0];
			fileName = textureNode.Attributes ["Filename"].Value;
			using (Bitmap textureBitmap = new Bitmap(fileName)) 
			{
				int textureCount = textureBitmap.Width / RaycasterConstants.TextureSize;
				textures = new BitmapData[textureCount];
				textureScan0 = new IntPtr[textureCount];
				textureInts = new int[textureCount][];

				for (int i = 0; i < textureCount; i++) 
				{
					using (Bitmap tileBitmap = textureBitmap.Clone (new Rectangle (i * RaycasterConstants.TextureSize, 0, RaycasterConstants.TextureSize, RaycasterConstants.TextureSize), RaycasterConstants.RaycasterPixelFormat)) 
					{
						textures[i] = tileBitmap.LockBits(new Rectangle(0, 0, RaycasterConstants.TextureSize, RaycasterConstants.TextureSize), ImageLockMode.ReadOnly, RaycasterConstants.RaycasterPixelFormat);
						textureScan0[i] = textures[i].Scan0;
						textureInts[i] = new int[Math.Abs(textures[i].Stride/4) * RaycasterConstants.TextureSize];
						Marshal.Copy(textureScan0[i], textureInts[i], 0, textureInts[i].Length);
						tileBitmap.UnlockBits (textures [i]);
					}
				}
			}

			XmlNode skyboxNode = doc.GetElementsByTagName ("Skybox")[0];
			fileName = skyboxNode.Attributes ["Filename"].Value;
			using (Bitmap skyboxBitmap = new Bitmap(fileName)) 
			{
				skyTexture = skyboxBitmap.LockBits(new Rectangle(0, 0, skyboxBitmap.Width, skyboxBitmap.Height), ImageLockMode.ReadOnly, RaycasterConstants.RaycasterPixelFormat);
				skyInts = new int[Math.Abs(skyTexture.Stride/4) * 240];
				skyScan0 = skyTexture.Scan0;
				Marshal.Copy(skyScan0, skyInts, 0, skyInts.Length);
			}

			XmlNode mapNode = doc.GetElementsByTagName ("Layout")[0];
			string intString = mapNode.Attributes ["Width"].Value;
			int.TryParse (intString, out Width);
			intString = mapNode.Attributes ["Height"].Value;
			int.TryParse (intString, out Height);

			Ceiling = new int[Height, Width];
			Map = new int[Height, Width];
			Floors = new int[Height, Width];

			MapSegmentLoader (doc.GetElementsByTagName ("Ceiling")[0], Ceiling);
			MapSegmentLoader (doc.GetElementsByTagName ("Wall")[0], Map);
			MapSegmentLoader (doc.GetElementsByTagName ("Floor")[0], Floors);

			XmlNode startNode = doc.GetElementsByTagName ("StartPosition")[0];
			intString = startNode.Attributes ["X"].Value;
			int.TryParse (intString, out StartX);
			intString = startNode.Attributes ["Y"].Value;
			int.TryParse (intString, out StartY);
		}

		private void MapSegmentLoader(XmlNode mapNode, int[,] map)
		{
			int i = 0;
			int j = 0;
			int convertedInt = 0;
			string intString = string.Empty;

			XmlNodeList rowList = mapNode.SelectNodes ("Row");
			foreach (XmlNode rowNode in rowList) 
			{
				XmlNodeList colList = rowNode.SelectNodes ("Column");
				foreach (XmlNode colNode in colList) 
				{	
					intString = colNode.Attributes ["TextureIndex"].Value;
					int.TryParse (intString, out convertedInt);
					map [i, j] = convertedInt;
					j++;
				}
				i++;
				j = 0;
			}
		}
	}
}

