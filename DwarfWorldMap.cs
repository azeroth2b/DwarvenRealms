using SimplexNoise;
using System;
using System.Drawing;
using DwarvenRealms.Properties;

namespace DwarvenRealms
{
    class DwarfWorldMap
    {
        int[,] biomeMap;
        int[,] elevationMap;
        int[,] smoothedElevationMap;
        int[,] waterMap;
        int[,] riverHeightMap;
        Structures.Type[,] structureMap;
        int[,] riverFlow;
        int[,] riverType;

        // unsafe is needed to use raw pointers.
        // This implementation is not ideal, because before and after calling this function, the user has to manually lock/unlock the bitmap. But there seems to be no other, better, cleaner, more elegant way.
        unsafe Color fetchColor(int x, int y, int stride, int colorsize, System.Drawing.Imaging.BitmapData bmpdata)
        {
            byte* p = (byte*)(void*)bmpdata.Scan0.ToPointer(); // A raw pointer to bytes! This is a rarity in C#.
            int cs = colorsize;
            byte* row = &p[y * stride];
            byte b = row[x*cs];
            byte g = row[x*cs + 1];
            byte r = row[x*cs + 2];
            return Color.FromArgb(r, g, b);
        }

        public void loadElevationMap(string path)
        {
            Bitmap elevationBitmap = (Bitmap)Bitmap.FromFile(path);
            // For speed improvement, the bitmap must be locked and accessed unsafely.
            //                                          v lock entire bitmap      v readonly                                   v use pixel format of bitmap
            var bmpdata = elevationBitmap.LockBits(new Rectangle(0, 0, elevationBitmap.Width, elevationBitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, elevationBitmap.PixelFormat);
            int stride = bmpdata.Stride;
            int colorsize = System.Drawing.Bitmap.GetPixelFormatSize(bmpdata.PixelFormat) / 8; // Divide by 8 because 1 byte is 8 bits and FormatSize returns bits.

            elevationMap = new int[elevationBitmap.Width, elevationBitmap.Height];
            for (int y = 0; y < elevationBitmap.Height; y++)
            {
                for (int x = 0; x < elevationBitmap.Width; x++)
                {
                    Color point = fetchColor(x, y, stride, colorsize, bmpdata);
                    int height;
                    if (point.R == 0)
                        height = point.B;
                    else
                        height = point.B + 25;
                    elevationMap[x, y] = height;
                }
            }
            // Once we are done, immediately unlock the bitmap
            elevationBitmap.UnlockBits(bmpdata);

            smoothedElevationMap = Blur.gaussianBlur(elevationMap, 8);
            Console.WriteLine("Loaded elevation map sized {0}x{1}", elevationMap.GetUpperBound(0), elevationMap.GetUpperBound(1));
        }

        public bool river(Color point)
        {
            return point.R == 0 && point.G != 0;
        }

        public void loadWaterMap(string path)
        {
            Bitmap waterBitMap = (Bitmap)Bitmap.FromFile(path);
            // BezierRivers.MakeRivers mr = new BezierRivers.MakeRivers(path);
            // mr.makeRivers();
            
            // locking bitmap...
            var bmpdata = waterBitMap.LockBits(new Rectangle(0, 0, waterBitMap.Width, waterBitMap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, waterBitMap.PixelFormat);
            int stride = bmpdata.Stride;
            int colorsize = System.Drawing.Bitmap.GetPixelFormatSize(bmpdata.PixelFormat) / 8;

            waterMap = new int[waterBitMap.Width, waterBitMap.Height];
            riverHeightMap = new int[waterBitMap.Width, waterBitMap.Height];
            riverFlow = new int[waterBitMap.Width, waterBitMap.Height];
            riverType = new int[waterBitMap.Width, waterBitMap.Height];

            for (int y = 0; y < waterBitMap.Height; y++)
            {
                for (int x = 0; x < waterBitMap.Width; x++)
                {
                    Color point = fetchColor(x, y, stride, colorsize, bmpdata);
                    Color pointNorth = fetchColor(x, (y < bmpdata.Height ? y + 1 : y), stride, colorsize, bmpdata);
                    Color pointEast = fetchColor((x < bmpdata.Width ? x + 1 : x), y, stride, colorsize, bmpdata);
                    Color pointSouth = fetchColor(x, (y > 0 ? y - 1 : y), stride, colorsize, bmpdata);
                    Color pointWest = fetchColor((x > 0 ? x - 1 : y), y, stride, colorsize, bmpdata);

                    bool east = river(pointEast);
                    bool west = river(pointWest);
                    bool north = river(pointNorth);
                    bool south = river(pointSouth);

                    waterMap[x, y] = -1;
                    riverHeightMap[x, y] = -1;
                    riverFlow[x, y] = -1;

                    // Ocean
                    if (point.R == 0 && point.G == 0)
                    {
                        waterMap[x, y] = point.B + 25;
                    }
                    // River
                    else if (point.R == 0)
                    {
                        // River
                        waterMap[x, y] = -1; // already processed
                                             // this defines the altitude of the river.  

                        // detect a neighbor river segments
                        riverFlow[x, y] = (north ? 1 : 0) + (south ? 2 : 0) + (west ? 4 : 0) + (east ? 8 : 0);

                        if (point.B >= 187) { riverType[x, y] = 0; }
                        else if (point.B >= 169) { riverType[x, y] = 1; }
                        else if (point.B >= 154) { riverType[x, y] = 1; }
                        else if (point.B >= 139) { riverType[x, y] = 2; }
                        else if (point.B >= 120) { riverType[x, y] = 3; }
                        else if (point.B >= 99) { riverType[x, y] = 4; }
                        else { riverType[x, y] = 5; }

                        // Make a better waterfall
                        if (north && pointNorth.B < point.B)
                            riverHeightMap[x, y] = point.B - (point.B - pointNorth.B) / 4;
                        else if (east && pointEast.B < point.B)
                            riverHeightMap[x, y] = point.B - (point.B - pointEast.B) / 4;
                        else if (south && pointSouth.B < point.B)
                            riverHeightMap[x, y] = point.B - (point.B - pointSouth.B) / 4;
                        else if (west && pointWest.B < point.B)
                            riverHeightMap[x, y] = point.B - (point.B - pointWest.B) / 4;
                        else
                            riverHeightMap[x, y] = point.B;
                    }
                }
            }


            // expand large rivers
            /*
            for (int y = 0; y < waterBitMap.Height; y++)
            {
                for (int x = 0; x < waterBitMap.Width; x++)
                {
                    if (riverType[x,y]>=3) {
                        //Console.WriteLine("riverFlow[" + x + "," + y + "]: " + ((UInt32)riverFlow[x, y]) + " -- " + ((UInt32)riverFlow[x, y] & (UInt32)0x0001));
                        if (((UInt32) riverFlow[x,y] & (UInt32) 0x0011) > 0)  // North or South neighbor
                        {
                            riverHeightMap[x+1, y] = riverHeightMap[x, y];
                            if (riverType[x,y] > 3)
                                riverHeightMap[x-1, y] = riverHeightMap[x, y];
                        }
                        if (((UInt32)riverFlow[x, y] & (UInt32)0x1100) > 0) // West or East Neighbor
                        {
                            riverHeightMap[x, y + 1] = riverHeightMap[x, y];
                            if (riverType[x, y] > 3)
                                riverHeightMap[x, y - 1] = riverHeightMap[x, y];
                        }
                    }
                }
            }
            */

            waterBitMap.UnlockBits(bmpdata); // unlocked bitmap

            Console.WriteLine("Loaded ocean map sized {0}x{1}", waterMap.GetUpperBound(0), waterMap.GetUpperBound(1));
        }

        public void loadBiomeMap(string path)
        {
            Bitmap tempBiomeMap = (Bitmap)Bitmap.FromFile(path);
            // locking bitmap ...
            var bmpdata = tempBiomeMap.LockBits(new Rectangle(0, 0, tempBiomeMap.Width, tempBiomeMap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, tempBiomeMap.PixelFormat);
            int stride = bmpdata.Stride;
            int colorsize = System.Drawing.Bitmap.GetPixelFormatSize(bmpdata.PixelFormat) / 8;

            biomeMap = new int[tempBiomeMap.Width, tempBiomeMap.Height];
            for (int y = 0; y < tempBiomeMap.Height; y++)
            {
                for (int x = 0; x < tempBiomeMap.Width; x++)
                {
                    biomeMap[x, y] = BiomeList.GetBiomeIndex(fetchColor(x, y, stride, colorsize, bmpdata));
                }
            }
            tempBiomeMap.UnlockBits(bmpdata); // unlocked bitmap
            Console.WriteLine("Loaded biome map sized {0}x{1}", biomeMap.GetUpperBound(0), biomeMap.GetUpperBound(1));
        }

        public Boolean closeEnough(Color a, Color b, double range)
        {
            return (Math.Pow(a.R - b.R, 2) + Math.Pow(a.G - b.G, 2) + Math.Pow(a.B - b.B, 2)) < Math.Pow(range, 2);
        }

        public void loadStructureMap(string path)
        {
            Bitmap tempStructureMap = (Bitmap)Bitmap.FromFile(path);
            // locking bitmap ...
            var bmpdata = tempStructureMap.LockBits(new Rectangle(0, 0, tempStructureMap.Width, tempStructureMap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, tempStructureMap.PixelFormat);
            int stride = bmpdata.Stride;
            int colorsize = System.Drawing.Bitmap.GetPixelFormatSize(bmpdata.PixelFormat) / 8;

            structureMap = new Structures.Type[tempStructureMap.Width, tempStructureMap.Height];
            for (int y = 0; y < tempStructureMap.Height; y++)
            {
                for (int x = 0; x < tempStructureMap.Width; x++)
                {
                    structureMap[x,y] = Structures.getStructureType(fetchColor(x, y, stride, colorsize, bmpdata));
                }
            }
            tempStructureMap.UnlockBits(bmpdata); // unlocked bitmap
            Console.WriteLine("Loaded structure map sized {0}x{1}", structureMap.GetUpperBound(0), structureMap.GetUpperBound(1));
    }

        public enum InterpolationChoice
        {
            linear,
            cosine,
            cubic,
            catmulRom,
            hermite,
            fritschCarlson
        }
        InterpolationChoice interpolationChoice = InterpolationChoice.fritschCarlson;

        public int getElevation(int x, int y)
        {
            return getClampedCoord(elevationMap, x, y);
        }

        public double getElevation(double x, double y)
        {
            return getInterpolatedValue(elevationMap, interpolationChoice, x, y);
        }
        public double getSmoothedElevation(double x, double y)
        {
            return getInterpolatedValue(smoothedElevationMap, interpolationChoice, x, y);
        }

        

        public static double getInterpolatedValue(int[,] array, InterpolationChoice type, double x, double y)
        {
            int x1, y1;
            x1 = (int)Math.Floor(x);
            y1 = (int)Math.Floor(y);

            double z11 = getClampedCoord(array, x1, y1);
            double z12 = getClampedCoord(array, x1, y1 + 1);
            double z21 = getClampedCoord(array, x1 + 1, y1);
            double z22 = getClampedCoord(array, x1 + 1, y1 + 1);

            double z00 = getClampedCoord(array, x1 - 1, y1 - 1);
            double z01 = getClampedCoord(array, x1 - 1, y1);
            double z02 = getClampedCoord(array, x1 - 1, y1 + 1);
            double z03 = getClampedCoord(array, x1 - 1, y1 + 2);
            double z10 = getClampedCoord(array, x1, y1 - 1);
            double z13 = getClampedCoord(array, x1, y1 + 2);
            double z20 = getClampedCoord(array, x1 + 1, y1 - 1);
            double z23 = getClampedCoord(array, x1 + 1, y1 + 2);
            double z30 = getClampedCoord(array, x1 + 2, y1 - 1);
            double z31 = getClampedCoord(array, x1 + 2, y1);
            double z32 = getClampedCoord(array, x1 + 2, y1 + 1);
            double z33 = getClampedCoord(array, x1 + 2, y1 + 2);

            double muy = x - x1;
            double mux = y - y1;

            //flat land sometimes gives trouble, so a slight increase can help that.
            double roundingCorrection = 0.5;

            switch (type)
            {
                case InterpolationChoice.linear:
                    return Interpolate.BiLinearInterpolate(z11, z12, z21, z22, mux, muy) + roundingCorrection;
                case InterpolationChoice.cosine:
                    return Interpolate.BiCosineInterpolate(z11, z12, z21, z22, mux, muy) + roundingCorrection;
                case InterpolationChoice.cubic:
                    return Interpolate.BiCubicInterpolate(z00, z01, z02, z03, z10, z11, z12, z13, z20, z21, z22, z23, z30, z31, z32, z33, mux, muy) + roundingCorrection;
                case InterpolationChoice.catmulRom:
                    return Interpolate.BiCatmullRomInterpolate(z00, z01, z02, z03, z10, z11, z12, z13, z20, z21, z22, z23, z30, z31, z32, z33, mux, muy) + roundingCorrection;
                case InterpolationChoice.hermite:
                    return Interpolate.BiHermiteInterpolate(z00, z01, z02, z03, z10, z11, z12, z13, z20, z21, z22, z23, z30, z31, z32, z33, mux, muy, 0.75) + roundingCorrection;
                case InterpolationChoice.fritschCarlson:
                    return Interpolate.BiFritschCarlsonInterpolate(z00, z01, z02, z03, z10, z11, z12, z13, z20, z21, z22, z23, z30, z31, z32, z33, mux, muy) + roundingCorrection;

            }
            return -1.0;
        }

        /// <summary>
        /// Returns the water level at the specified location, excluding rivers.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Water level, if available, or -1</returns>
        public int getWaterbodyLevel(int x, int y)
        {
            return getClampedCoord(waterMap, x, y);
        }
        public int getRiverLevel(int x, int y)
        {
            return getClampedCoord(riverHeightMap, x, y);
        }
        public int getRiverFlow(int x, int y)
        {
            return getClampedCoord(riverFlow, x, y);
        }
        public int getRiverType(int x, int y)
        {
            return getClampedCoord(riverType, x, y);
        }
        public Structures.Type getStructureMap(int x, int y)
        {
            return getClampedCoord(structureMap, x, y);
        }
        public int getBiome(int x, int y)
        {
            return getClampedCoord(biomeMap, x, y);
        }
        public int getBiome(double x, double y)
        {
            return getBiome((int)Math.Floor(x), (int)Math.Floor(y));
        }

        /// <summary>
        /// Safely reads the value from an index grid.
        /// </summary>
        /// <param name="grid">A two dimensional array of ints to read from</param>
        /// <param name="x">X coord</param>
        /// <param name="y">Y coord</param>
        /// <returns>The value stored at the nearest valid grid cell, or MinValue if the grid is invalid entirely.</returns>
        public static int getClampedCoord(int[,] grid, int x, int y)
        {
            if (grid == null || grid.Length == 0)
                return int.MinValue;
            if (x < grid.GetLowerBound(0))
                x = grid.GetLowerBound(0);
            if (x > grid.GetUpperBound(0))
                x = grid.GetUpperBound(0);
            if (y < grid.GetLowerBound(1))
                y = grid.GetLowerBound(1);
            if (y > grid.GetUpperBound(1))
                y = grid.GetUpperBound(1);
            return grid[x, y];
        }
        public static double getClampedCoord(double[,] grid, int x, int y)
        {
            if (grid == null || grid.Length == 0)
                return double.MinValue;
            if (x < grid.GetLowerBound(0))
                x = grid.GetLowerBound(0);
            if (x > grid.GetUpperBound(0))
                x = grid.GetUpperBound(0);
            if (y < grid.GetLowerBound(1))
                y = grid.GetLowerBound(1);
            if (y > grid.GetUpperBound(1))
                y = grid.GetUpperBound(1);
            return grid[x, y];
        }
        public static Structures.Type getClampedCoord(Structures.Type[,] grid, int x, int y)
        {
            if (grid == null || grid.Length == 0)
                return Structures.Type.Unknown;
            if (x < grid.GetLowerBound(0))
                x = grid.GetLowerBound(0);
            if (x > grid.GetUpperBound(0))
                x = grid.GetUpperBound(0);
            if (y < grid.GetLowerBound(1))
                y = grid.GetLowerBound(1);
            if (y > grid.GetUpperBound(1))
                y = grid.GetUpperBound(1);
            return grid[x, y];
        }
        public static bool outsideBounds(mapData[,] grid, int x, int y)
        {
            return (x < grid.GetLowerBound(0) || x > grid.GetUpperBound(0) ||
                y < grid.GetLowerBound(1) || y > grid.GetUpperBound(1));
        }

        int getFuzzyCoords(int[,] grid, double x, double y)
        {

            return int.MinValue;
        }

    }
}
