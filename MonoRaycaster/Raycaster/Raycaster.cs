using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGLTest
{
	/*
	 * Ported and Forked from: http://lodev.org/cgtutor/raycasting.html
	 * 
	 * A Textured Raycaster written in managed C#.  Lode's method for
	 * floorcasting was discarded, and instead I opted to port Amarillion's
	 * "MODE7" example instead (http://www.helixsoft.nl/articles/circle/sincos.htm).
	 * 
	 * A pretty lame/simple fog effect is implimented.  The farther away 
	 * something is, the darker it is.  This gives the impression of
	 * minimal light.
	 * 
	 * The end result is a fairly speedy C# based raycaster.  
	 * 
	 * Culling is not implimented, but may provide significant performance
	 * improvements.  
	 * 
	 * Textures for Walls and Flats are assumed to be 256x256, and they should
	 * have a 32bpp (ARGB).  You can get a slight speed bump by dropping the
	 * textures to 64x64.
	 * 
	 * The Skybox is 1024x240, and is also 32bpp (ARGB).
	 */
	public class Raycaster
	{
		public Ray[] rays;

		RaycasterMap worldMap = new RaycasterMap();

		public bool k_fw;
		public bool k_bk;
		public bool k_l;
		public bool k_r;
		public bool k_rot_l;
		public bool k_rot_r;

		double posX;
		double posY;  //x and y start position

		double dirX = -1.0;
		double dirY = -0.0; //initial direction vector

		double planeX = 0.0;
		double planeY = 0.66; //the 2d raycaster version of camera plane

		double directionDegrees = 0;

		int RenderWidth;
		int RenderHeight;
		int depthConst = 1000;

		byte[] depthTable;

		public Bitmap buffer;	
		int[] bufferInts;

		public Raycaster(int inputWidth, int inputHeight)
		{
			RenderWidth = inputWidth;
			RenderHeight = inputHeight;
			rays = new Ray[RenderWidth];
			for (int i = 0; i < RenderWidth; i++) { rays [i] = new Ray (); }

			buffer = new Bitmap(RenderWidth, RenderHeight);

			depthTable = new byte[depthConst];
			int count = 0;
			byte depthDivisor = 1;
			for (int i = 0; i < depthConst; i++, count++)
			{
				if (count == 64)
				{
					depthDivisor++;
					count = 0;
				}
				depthTable[i] = depthDivisor;
			}
		}

		public void LoadLevel(string filePath, string fileName)
		{
			worldMap.LoadMapFromFile(filePath, fileName);

			posX = worldMap.StartX;
			posY = worldMap.StartY;

			//Build BufferInt table
			BitmapData bufferData = buffer.LockBits(new Rectangle(0, 0, RenderWidth, RenderHeight), ImageLockMode.WriteOnly, buffer.PixelFormat);
			bufferInts = new int[Math.Abs(bufferData.Stride/4) * buffer.Height];
			buffer.UnlockBits (bufferData);		
		}

		void BuildRaysForWalls()
		{ 
			for(int x = 0; x < RenderWidth; x++)
			{
				double cameraX = 2 * x / (double)RenderWidth - 1; //x-coordinate in camera space
				double rayPosX = posX;
				double rayPosY = posY;
				double rayDirX = dirX + planeX * cameraX;
				double rayDirY = dirY + planeY * cameraX;
				//which box of the map we're in  
				int mapX = (int)rayPosX;
				int mapY = (int)rayPosY;

				//length of ray from current position to next x or y-side
				double sideDistX;
				double sideDistY;

				//length of ray from one x or y-side to next x or y-side
				double deltaDistX = Math.Sqrt (1 + (rayDirY * rayDirY) / (rayDirX * rayDirX));
				double deltaDistY = Math.Sqrt (1 + (rayDirX * rayDirX) / (rayDirY * rayDirY));
				double perpWallDist;

				//what direction to step in x or y-direction (either +1 or -1)
				int stepX;
				int stepY;

				int hit = 0; //was there a wall hit?
				int side = 0; //was a NS or a EW wall hit?
				//calculate step and initial sideDist
				if (rayDirX < 0) {
					stepX = -1;
					sideDistX = (rayPosX - mapX) * deltaDistX;
				} else {
					stepX = 1;
					sideDistX = (mapX + 1.0 - rayPosX) * deltaDistX;
				}
				if (rayDirY < 0) {
					stepY = -1;
					sideDistY = (rayPosY - mapY) * deltaDistY;
				} else {
					stepY = 1;
					sideDistY = (mapY + 1.0 - rayPosY) * deltaDistY;
				}
				//perform DDA
				while (hit == 0) {
					//jump to next map square, OR in x-direction, OR in y-direction
					if (sideDistX < sideDistY) {
						sideDistX += deltaDistX;
						mapX += stepX;
						side = 0;
					} else {
						sideDistY += deltaDistY;
						mapY += stepY;
						side = 1;
					}
					//Check if ray has hit a wall
					if (worldMap.Map [mapX, mapY] >= 0)
						hit = 1;
				}

				//Calculate distance projected on camera direction (oblique distance will give fisheye effect!)
				if (side == 0)
					perpWallDist = (mapX - rayPosX + (1 - stepX) / 2) / rayDirX;
				else
					perpWallDist = (mapY - rayPosY + (1 - stepY) / 2) / rayDirY;

				int lineHeight;
				lineHeight = (int)(RenderHeight / perpWallDist);

				//calculate lowest and highest pixel to fill in current stripe
				int drawStart = -lineHeight / 2 + RenderHeight / 2;
				if (drawStart < 0)
					drawStart = 0;
				int drawEnd = lineHeight / 2 + RenderHeight / 2;
				if (drawEnd >= RenderHeight)
					drawEnd = RenderHeight - 1;

				double wallX; //where exactly the wall was hit
				if (side == 1)
					wallX = rayPosX + ((mapY - rayPosY + (1 - stepY) / 2) / rayDirY) * rayDirX;
				else
					wallX = rayPosY + ((mapX - rayPosX + (1 - stepX) / 2) / rayDirX) * rayDirY;
				wallX -= Math.Floor ((wallX));

				//x coordinate on the texture
				int texX = (int)(wallX * (double)RaycasterConstants.TextureSize);
				if (side == 0 && rayDirX > 0)
					texX = RaycasterConstants.TextureSize - texX - 1;
				if (side == 1 && rayDirY < 0)
					texX = RaycasterConstants.TextureSize - texX - 1;

				//FLOOR CASTING
				double floorXWall, floorYWall; //x, y position of the floor texel at the bottom of the wall

				//4 different wall directions possible
				if (side == 0 && rayDirX > 0) {
					floorXWall = mapX;
					floorYWall = mapY + wallX;
				} else if (side == 0 && rayDirX < 0) {
					floorXWall = mapX + 1.0;
					floorYWall = mapY + wallX;
				} else if (side == 1 && rayDirY > 0) {
					floorXWall = mapX + wallX;
					floorYWall = mapY;
				} else {
					floorXWall = mapX + wallX;
					floorYWall = mapY + 1.0;
				}

				rays [x].Use (p => {
					p.X = x;
					p.drawStart = drawStart;
					p.drawEnd = drawEnd;
					p.TextureX = texX;
					p.TextureIndex = worldMap.Map [mapX, mapY];
					p.OffSide = side == 1;
					p.Depth = perpWallDist;
					p.FloorX = floorXWall;
					p.FloorY = floorYWall;
					p.lineHeight = lineHeight;
				});					
			}
		}

		void DrawWalls()
		{
			int lineHeight;
			long d;
			long texY;

			int texturePointerIndex;
			int textureStripeIndex;

			int start;
			int end;

			int depth;
			Pixel pixel;
			byte shiftValue;

			for (int i = 0; i < rays.Length; i++)
			{
				depth = (int)(rays[i].Depth * 100);

				//Avoiding array access improves overall seek time
				lineHeight = rays[i].lineHeight;
				textureStripeIndex = rays[i].TextureX;
				texturePointerIndex = rays[i].TextureIndex;

				if (rays[i].TextureIndex >= 0)
				{

					start = rays[i].drawStart;
					end = rays[i].drawEnd;

					if (depth < depthConst)
						shiftValue = depthTable[depth];
					else
						shiftValue = 255;

					for (int lineY = start; lineY < end; lineY++)
					{
						d = (lineY << 8) - (RenderHeight << 7) + (lineHeight << 7);
						texY = ((d * RaycasterConstants.TextureSize) / lineHeight) / RaycasterConstants.TextureSize;

						pixel = new Pixel(worldMap.textureInts[texturePointerIndex][textureStripeIndex + texY * RaycasterConstants.TextureSize]);

						pixel.Red /= shiftValue;
						pixel.Green /= shiftValue;
						pixel.Blue /= shiftValue;

						bufferInts[i + lineY * RenderWidth] = pixel.Int32;
					}
				}
			}
		}

		void DrawSky(double directionDegrees)
		{
			int degrees = (int)((directionDegrees) * (180 / Math.PI));
			if (degrees < 0) degrees = 360 + (degrees);
			double shift = 1024 / 360.0;
			int startPosition = (int)(degrees * shift);

			int mask = (1024 - 1);
			int texX;

			for (int y = 0; y < RenderHeight / 2; y++)
			{
				for (int x = 0; x < RenderWidth; x++)
				{
					texX = (int)(x + startPosition) & mask;
					bufferInts[x + y * RenderWidth] = worldMap.skyInts[texX + y * 1024];
				}
			}

			//Blank the rest
			for (int y =  RenderHeight / 2; y < RenderHeight; y++)
			{
				for (int x = 0; x < RenderWidth; x++)
				{
					bufferInts[x + y * RenderWidth] = 0;
				}
			}
		}

		void DrawFlats(double angle, double cx, double cy)
		{
			// current screen position
			int screen_x, screen_y;

			// texture pixels
			int texX, texY;

			// the distance and horizontal scale of the line we are drawing
			double distance, horizontal_scale;

			// masks to make sure we don't read pixels outside the tile
			int mask_x = (RaycasterConstants.TextureSize - 1);
			int mask_y = (RaycasterConstants.TextureSize - 1);

			// step for points in space between two pixels on a horizontal line
			double line_dx, line_dy;

			// current space position
			double space_x, space_y;

			int mapX, mapY;

			int depth;
			Pixel pixel;
			byte shiftValue = 1;

			for (screen_y = RenderHeight; screen_y > (RenderHeight / 2); screen_y--)
			{
				// first calculate the distance of the line we are drawing
				distance = RenderHeight / (2.0f * screen_y - RenderHeight); //you could make a small lookup table for this instead
				depth = (int)(distance * 100);
				distance *= RaycasterConstants.TextureSize;

				// then calculate the horizontal scale, or the distance between
				// space points on this horizontal line
				horizontal_scale = (distance / RenderHeight);

				// calculate the dx and dy of points in space when we step
				// through all points on this line
				line_dx = (-1 * Math.Sin(angle) * horizontal_scale);
				line_dy = (Math.Cos(angle) * horizontal_scale);

				// calculate the starting position
				space_x = -cx * RaycasterConstants.TextureSize + (distance * Math.Cos(angle)) - RenderWidth / 2 * line_dx;
				space_y = cy * RaycasterConstants.TextureSize + (distance * Math.Sin(angle)) - RenderWidth / 2 * line_dy;

				if (depth < depthConst && depth > 0)
					shiftValue = depthTable[depth];
				else
					shiftValue = 255;

				// go through all points in this screen line
				for (screen_x = 0; screen_x < RenderWidth; screen_x++)
				{
					// get a pixel from the tile and put it on the screen
					texX = (int)space_x & mask_x;
					texY = (int)space_y & mask_y;

					mapX = (int)Math.Abs(space_x / RaycasterConstants.TextureSize);
					mapY = (int)Math.Abs(space_y / RaycasterConstants.TextureSize);

					if (mapX < worldMap.Width && mapY < worldMap.Height)
					{
						pixel = new Pixel(worldMap.textureInts[worldMap.Floors[mapX, mapY]][texX + texY * RaycasterConstants.TextureSize]);

						pixel.Red /= shiftValue;
						pixel.Green /= shiftValue;
						pixel.Blue /= shiftValue;

						bufferInts[screen_x + (screen_y-1) * RenderWidth] = pixel.Int32;
					}

					//Only Ceelings which exist
					if (mapX < worldMap.Width && mapY < worldMap.Height)
					{
						if (worldMap.Ceiling[mapX, mapY] >= 0)
						{
							pixel = new Pixel(worldMap.textureInts[worldMap.Ceiling[mapX, mapY]][texX + texY * RaycasterConstants.TextureSize]);

							pixel.Red /= shiftValue;
							pixel.Green /= shiftValue;
							pixel.Blue /= shiftValue;						

							bufferInts[screen_x + (RenderHeight - screen_y) * RenderWidth] = pixel.Int32;
						}
					}

					// advance to the next position in space
					space_x += line_dx;
					space_y += line_dy;
				}
			}
		}

		public void UpdateInput()
		{
			double frameTime = 0.05;

			//speed modifiers
			double moveSpeed = frameTime * 3.0; //the constant value is in squares/second
			double rotSpeed = frameTime * 1.0; //the constant value is in radians/second

			//move forward if no wall in front of you
			if (k_fw)
			{
				if (worldMap.Map[(int)(posX + dirX * moveSpeed), (int)(posY)] < 0) posX += dirX * moveSpeed;
				if (worldMap.Map[(int)(posX), (int)(posY + dirY * moveSpeed)] < 0) posY += dirY * moveSpeed;
			}
			//move backwards if no wall behind you
			if (k_bk)
			{
				if (worldMap.Map[(int)(posX - dirX * moveSpeed), (int)(posY)] < 0) posX -= dirX * moveSpeed;
				if (worldMap.Map[(int)(posX), (int)(posY - dirY * moveSpeed)] < 0) posY -= dirY * moveSpeed;
			}
			//rotate to the right
			if (k_rot_r)
			{
				//both camera direction and camera plane must be rotated
				double oldDirX = dirX;
				dirX = dirX * Math.Cos(-rotSpeed) - dirY * Math.Sin(-rotSpeed);
				dirY = oldDirX * Math.Sin(-rotSpeed) + dirY * Math.Cos(-rotSpeed);
				double oldPlaneX = planeX;
				planeX = planeX * Math.Cos(-rotSpeed) - planeY * Math.Sin(-rotSpeed);
				planeY = oldPlaneX * Math.Sin(-rotSpeed) + planeY * Math.Cos(-rotSpeed);
			}
			//rotate to the left
			if (k_rot_l)
			{
				//both camera direction and camera plane must be rotated
				double oldDirX = dirX;
				dirX = dirX * Math.Cos(rotSpeed) - dirY * Math.Sin(rotSpeed);
				dirY = oldDirX * Math.Sin(rotSpeed) + dirY * Math.Cos(rotSpeed);
				double oldPlaneX = planeX;
				planeX = planeX * Math.Cos(rotSpeed) - planeY * Math.Sin(rotSpeed);
				planeY = oldPlaneX * Math.Sin(rotSpeed) + planeY * Math.Cos(rotSpeed);
			}

			directionDegrees = Math.Atan2(dirY, -dirX);

			BuildRaysForWalls();
		}

		public void PaintFrame()
		{
			BitmapData bufferData = buffer.LockBits(new Rectangle(0, 0, RenderWidth, RenderHeight), ImageLockMode.WriteOnly, RaycasterConstants.RaycasterPixelFormat);

			IntPtr bufferScan0 = bufferData.Scan0;
			Marshal.Copy(bufferScan0, bufferInts, 0, bufferInts.Length);

			DrawSky(directionDegrees);
			DrawFlats(directionDegrees, posX, posY);
			DrawWalls();

			Marshal.Copy(bufferInts, 0, bufferScan0, bufferInts.Length);
			buffer.UnlockBits(bufferData);
		}
	}
}
