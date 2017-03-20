using Substrate;
using Substrate.Core;
using System;
using System.Diagnostics;
using DwarvenRealms.Properties;

namespace DwarvenRealms
{
    class MapCrafter
    {
        int shift = -35; // ??? base world greyscale?

        NbtWorld currentWorld;
        DwarfWorldMap currentDwarfMap;
        CaveThing currentCaveMap;

        int maxHeight = -9999;
        int minHeight = 9999;

        mapData internalMap;

        

        public static int getChunkStartX() { return ((Settings.Default.borderWest - Settings.Default.mapCenterX) * Settings.Default.blocksPerEmbarkTile) / 16; }
        public static int getChunkStartY() { return ((Settings.Default.borderNorth - Settings.Default.mapCenterY) * Settings.Default.blocksPerEmbarkTile) / 16; }
        public static int getChunkFinishX() { return ((Settings.Default.borderEast - Settings.Default.mapCenterX) * Settings.Default.blocksPerEmbarkTile) / 16; }
        public static int getChunkFinishY() { return ((Settings.Default.borderSouth - Settings.Default.mapCenterY) * Settings.Default.blocksPerEmbarkTile) / 16; }

        public void loadDwarfMaps()
        {
            currentDwarfMap = new DwarfWorldMap();
            currentDwarfMap.loadElevationMap(Settings.Default.elevationMapPath);
            currentDwarfMap.loadWaterMap(Settings.Default.elevationWaterMapPath);
            currentDwarfMap.loadBiomeMap(Settings.Default.biomeMapPath);
            currentDwarfMap.loadStructureMap(Settings.Default.structureMapPath);
            currentCaveMap = new CaveThing(currentDwarfMap);
           /* internalMap = new mapData(((Settings.Default.borderEast - Settings.Default.borderWest + 2) * Settings.Default.blocksPerEmbarkTile) * 16,
                ((Settings.Default.borderSouth - Settings.Default.borderNorth + 2) * Settings.Default.blocksPerEmbarkTile) * 16, Settings.Default.mapCenterX * 16, Settings.Default.mapCenterY * 16);
                */
        }

        public void initializeMinecraftWorld()
        {
            currentWorld = AnvilWorld.Create(Settings.Default.outputPath);

            // We can set different world parameters
            currentWorld.Level.LevelName = Settings.Default.levelName;
            currentWorld.Level.Spawn = new SpawnPoint(0, 255, 0);
            if (Settings.Default.gameMode == 0)
            {
                currentWorld.Level.GameType = GameType.SURVIVAL;
            }
            else
            {
                currentWorld.Level.GameType = GameType.CREATIVE;
                currentWorld.Level.AllowCommands = true;
            }
        }

        public void saveMinecraftWorld()
        {
            // Save all remaining data (including a default level.dat)
            // If we didn't save chunks earlier, they would be saved here
            currentWorld.Save();
        }

        /* public void simpleWriteTest()
        {
            initializeMinecraftWorld();
            IChunkManager cm = currentWorld.GetChunkManager();

            loadDwarfMaps();

            int cropWidth = 40;
            int cropHeight = 40;

            Settings.Default.borderWest = 40;
            Settings.Default.borderEast = Settings.Default.borderWest + cropWidth;

            Settings.Default.borderNorth = 40;
            Settings.Default.borderSouth = Settings.Default.borderNorth + cropHeight;

            //FIXME get rid of this junk
            Settings.Default.mapCenterX = (Settings.Default.borderWest + Settings.Default.borderEast) / 2;
            Settings.Default.mapCenterY = (Settings.Default.borderNorth + Settings.Default.borderSouth) / 2;


            Console.WriteLine("Starting conversion now.");
            Stopwatch watch = Stopwatch.StartNew();
            for (int xi = getChunkStartX(); xi < getChunkFinishX(); xi++)
            {
                for (int zi = getChunkStartY(); zi < getChunkFinishY(); zi++)
                {
                    // This line will create a default empty chunk, and create a
                    // backing region file if necessary (which will immediately be
                    // written to disk)
                    ChunkRef chunk = cm.CreateChunk(xi, zi);

                    // This will make sure all the necessary things like trees and
                    // ores are generated for us.
                    chunk.IsTerrainPopulated = false;

                    // Auto light recalculation is horrifically bad for creating
                    // chunks from scratch, because we're placing thousands
                    // of blocks.  Turn it off.
                    chunk.Blocks.AutoLight = false;

                    double xMin = ((xi * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterX);
                    double xMax = (((xi + 1) * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterX);
                    double yMin = ((zi * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterY);
                    double yMax = (((zi + 1) * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterY);


                    // Make the terrain
                    HeightMapChunk(chunk, xMin, xMax, yMin, yMax);

                    // Reset and rebuild the lighting for the entire chunk at once
                    chunk.Blocks.RebuildHeightMap();
                    if (Settings.Default.lightGenerationEnable)
                    {
                        chunk.Blocks.RebuildBlockLight();
                        chunk.Blocks.RebuildSkyLight();
                    }

                    // Save the chunk to disk so it doesn't hang around in RAM
                    cm.Save();
                }
                //TimeSpan elapsedTime = watch.Elapsed;
                //int finished = xi - chunkStartX + 1;
                //int left = chunkFinishX - xi - 1;
                //TimeSpan remainingTime = TimeSpan.FromTicks(elapsedTime.Ticks / finished * left);
                //Console.WriteLine("Built Chunk Row {0} of {1}. {2}:{3}:{4} elapsed, {5}:{6}:{7} remaining.",
                //    finished, chunkFinishX - chunkStartX,
                //    elapsedTime.Hours, elapsedTime.Minutes, elapsedTime.Seconds,
                //    remainingTime.Hours, remainingTime.Minutes, remainingTime.Seconds);
                maxHeight = int.MinValue;
                minHeight = int.MaxValue;
            }

            saveMinecraftWorld();
        }
        */

        public internalData generateInternalMapChunk(bounds bound)
        {
            return new internalData(
                 (int)currentDwarfMap.getElevation(bound.mux, bound.muy) + shift,
                 currentDwarfMap.getBiome(bound.MuxInt(), bound.MuyInt()),
                 currentDwarfMap.getRiverType(bound.MuxInt(), bound.MuyInt()),
                 currentDwarfMap.getRiverLevel(bound.MuxInt(), bound.MuyInt()) + shift,
                 currentDwarfMap.getWaterbodyLevel(bound.MuxInt(), bound.MuyInt()) + shift,
                 currentDwarfMap.getStructureMap(bound.MuxInt(), bound.MuyInt())
                 );
        }

        public class bounds
        {
            public double xMin;
            public double xMid;
            public double xMax;
            public double yMin;
            public double yMid;
            public double yMax;
            public int x, xt;
            public int z, zt;
            public double mux;
            public double muy;

            // This converts from MC chunk location back to DF Map reference
            //double mux = (mapXMax - mapXMin) * x / 16.0 + mapXMin;  //  mux = width / 16 + xOffset
            //double muy = (mapYMax - mapYMin) * z / 16.0 + mapYMin;  //  muy = height / 16 + yOffset

            /*
                   int height = (int)currentDwarfMap.getElevation(mux, muy) + shift;
                   int waterLevel = currentDwarfMap.getWaterbodyLevel((int)Math.Floor(mux + 0.5), (int)Math.Floor(muy + 0.5)) + shift;
                   int riverLevel = currentDwarfMap.getRiverLevel((int)Math.Floor(mux + 0.5), (int)Math.Floor(muy + 0.5)) + shift;
                   int depth = currentDwarfMap.getRiverType((int)Math.Floor(mux + 0.5), (int)Math.Floor(muy + 0.5));
                   int structure = currentDwarfMap.getStructureMap((int)Math.Floor(mux + 0.5), (int)Math.Floor(muy + 0.5));
                   */
            //int xt = ((int)Math.Floor(mux + 0.5)*16) + x;
            //int zt = ((int)Math.Floor(muy + 0.5)*16) + z;
            /* int biomeIndex = currentDwarfMap.getBiome((int)Math.Floor(mux + 0.5), (int)Math.Floor(muy + 0.5));
                     */

            public bounds(int xi, int zi)
            {
                x = xi;
                z = zi;
                xMin = ((xi * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterX);
                xMax = (((xi + 1) * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterX);
                xMid = (xMax - xMin) / 2;
                yMin = ((zi * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterY);
                yMax = (((zi + 1) * 16.0 / (double)Settings.Default.blocksPerEmbarkTile) + Settings.Default.mapCenterY);
                yMid = (yMax - yMin) / 2;

                updateBound(0, 0);
            }

            // This converts from MC chunk location back to DF Map reference
            public void updateBound(int x, int z)
            {
                mux = (xMax - xMin) * x / 16.0 + xMin;
                muy = (yMax - yMin) * z / 16.0 + yMin;
                xt = ((int)Math.Floor(mux + 0.5) * 16) + x;
                zt = ((int)Math.Floor(muy + 0.5) * 16) + z;
            }

            public int MuxInt()
            {
                return (int)Math.Floor(mux + 0.5);
            }
            public int MuyInt()
            {
                return (int)Math.Floor(muy + 0.5);
            }

        }

        /// <summary>
        /// Generates and saves a single minecraft chunk using current settings.
        /// </summary>
        /// <param name="xi">X coordinate of the chunk.</param>
        /// <param name="zi">Y coordinate of the chunk.</param>
        public void generateSingleChunk(int xi, int zi)
        {
            // This line will create a default empty chunk, and create a
            // backing region file if necessary (which will immediately be
            // written to disk)
            ChunkRef chunk = currentWorld.GetChunkManager().CreateChunk(xi, zi);

            // This will make sure all the necessary things like trees and
            // ores are generated for us.
            chunk.IsTerrainPopulated = false;

            // Auto light recalculation is horrifically bad for creating
            // chunks from scratch, because we're placing thousands
            // of blocks.  Turn it off.
            chunk.Blocks.AutoLight = false;

            // This scales the dimensions based off the blocksPerEmbarkTile [1..8]
            bounds bound = new bounds(xi, zi);

            // Make the terrain
            HeightMapChunk(chunk, bound);

            // Reset and rebuild the lighting for the entire chunk at once
            chunk.Blocks.RebuildHeightMap();
            chunk.Blocks.RebuildBlockLight();
            chunk.Blocks.RebuildSkyLight();

            // Save the chunk to disk so it doesn't hang around in RAM
            currentWorld.GetChunkManager().Save();
        }

        void HeightMapChunk(ChunkRef chunk, bounds bound)
        {
            double mapXMin = bound.xMin;
            double mapXMax = bound.xMax;
            double mapYMin = bound.yMin;
            double mapYMax = bound.yMax;

            int size = 16;
            internalData[,] mapData = new internalData[3*size+2,  3*size+2];
            for (int x = -size-1; x <= 2*size; x++)
            {
                for (int z = -size-1; z <= 2 * size; z++)
                {
                    bound.updateBound(x, z);
                    // build 48x48 chunk of source maps for use below
                    mapData[x+size+1, z+size+1] = generateInternalMapChunk(bound);
                }
            }
            
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    bound.updateBound(x, z);


                   

                    internalData id = mapData[x+1, z+1]; // .getInternalData(xt, zt);
                    int height      = id.height;
                    int waterLevel  = id.waterLevel;
                    int riverLevel  = id.riverLevel;
                    int depth       = id.riverType;
                    Structures.Type structure   = id.structureType;
                    int biomeIndex  = id.biomeIndex < 0 ? 0 : id.biomeIndex;



                    if (height > maxHeight) maxHeight = height;
                    if (height < minHeight) minHeight = height;

                    
                    chunk.Biomes.SetBiome(x, z, BiomeList.biomes[biomeIndex].mineCraftBiome);
                    //create bedrock
                    for (int y = 0; y < 2; y++)
                    {
                        chunk.Blocks.SetID(x, y, z, BlockType.BEDROCK);
                    }
                    ////deal with rivers
                    // BezierRivers.MakeRivers mr = new BezierRivers.MakeRivers();
                    // mr.makeRivers();

                    if (riverLevel >= 0)
                    {
                        chunk.Biomes.SetBiome(x, z, BiomeType.River);

                        height = riverLevel;

                        // All DF rivers are 1 px wide.  
                        // That scales into embarkWidth regardless of type of river
                        // This metric allows us to fine-tune river width beyond that of embarkWidth
                        double oX = (mapXMax - mapXMin) * x % 16 - 8;
                        double oY = (mapYMax - mapYMin) * z % 16 - 8;
                        // Console.WriteLine("[{3},{4},({2}): {0},{1}  (({5},{6})) [{7},{8}]", oX, oY, depth,x,z, mux, muy, mux-Math.Floor(mux + 0.5), muy-Math.Floor(muy+0.5));
                        int scale = Settings.Default.blocksPerEmbarkTile;

                        // can account for depth of river here 
                        for (int y = 0; y < height - depth - 2; y++)
                        {
                            chunk.Blocks.SetID(x, y, z, BlockType.STONE);
                        }
                        for (int y = height - depth - 2; y < height - depth - 1; y++)
                        {
                            chunk.Blocks.SetID(x, y, z, BlockType.GRAVEL);
                        }
                        for (int y = height - depth - 1; y < height; y++)
                        {
                            chunk.Blocks.SetID(x, y, z, BlockType.WATER);
                        }
                        for (int y = height; y < height + 1; y++)//Just to blockupdate for flowing water(falls).
                        {
                            chunk.Blocks.SetID(x, y, z, BlockType.AIR);
                        }
                    }
                    else if (BiomeList.biomes[biomeIndex].mineCraftBiome == BiomeID.DeepOcean && waterLevel <= height)
                    {
                        //make beaches
                        chunk.Biomes.SetBiome(x, z, BiomeType.Beach);
                        height = 98 + shift;
                        for (int y = 0; y < height - 4; y++)
                        {
                            chunk.Blocks.SetID(x, y, z, BlockType.STONE);
                        }
                        for (int y = height - 4; y < height - 3; y++)
                        {
                            chunk.Blocks.SetID(x, y, z, BlockType.SANDSTONE);
                        }
                        for (int y = height - 3; y < height; y++)
                        {
                            chunk.Blocks.SetID(x, y, z, BlockType.SAND);
                        }

                    }
                    else
                    {
                        // Create the rest, according to biome
                        for (int y = 2; y < height; y++)
                        {
                            if (y >= chunk.Blocks.YDim) break;
                            chunk.Blocks.SetID(x, y, z, BiomeList.biomes[biomeIndex].getBlockID(height - y, x + (chunk.X * 16), z + (chunk.Z * 16)));
                        }
                    }
                    //// Create Oceans and lakes
                    for (int y = height; y < waterLevel; y++)
                    {
                        if (y < 2) continue;
                        if (y >= chunk.Blocks.YDim) break;
                        chunk.Blocks.SetID(x, y, z, BlockType.STATIONARY_WATER);
                    }

                    // Fill caves
                    for (int y = 2; y < height; y++)
                    {
                        if (y >= chunk.Blocks.YDim) break;
                        int caveID = currentCaveMap.getCaveBlock(bound.mux, y - shift, bound.muy);
                        if (caveID == -2)
                            break;
                        if (caveID >= 0)
                            chunk.Blocks.SetID(x, y, z, caveID);
                    }

                    // Populate Structures
                    int tunnelHeight = 5;
                    int tunnelDepth = 10;
                    // structureNeighbor:
                    // 1 2 3
                    // 4 X 5
                    // 6 7 8

                    // 

                   switch (structure)
                    {
                        case Structures.Type.Underground_Road:
                            chunk.Blocks.SetID(x, height - tunnelDepth - tunnelHeight, z, BlockType.COBBLESTONE);
                            for (int y = height - tunnelHeight - tunnelDepth; y < height - tunnelDepth; y++)
                            {
                                chunk.Blocks.SetID(x, y, z, BlockType.AIR);
                            }
                            chunk.Blocks.SetID(x, height - tunnelHeight, z, BlockType.COBBLESTONE);
                            break;
                        case Structures.Type.Road: // road
                            for (int y = height - 2; y < height; y++)
                            {
                                chunk.Blocks.SetID(x, y, z, BlockType.STONE_BRICK);
                                if (mapData[x + 1 - 1, z +1 ].structureType == Structures.Type.Bridge)
                                {
                                    chunk.Blocks.SetID(x, height + 1, z, BlockType.COBBLESTONE_STAIRS);
                                    chunk.Blocks.GetBlock(x, height + 1, z).Data = (int)StairOrientation.ASCEND_WEST;
                                }
                                if (mapData[x + 1 + 1, z + 1 ].structureType == Structures.Type.Bridge)
                                {
                                    chunk.Blocks.SetID(x, height + 1, z, BlockType.COBBLESTONE_STAIRS);
                                    chunk.Blocks.GetBlock(x, height + 1, z).Data = (int)StairOrientation.ASCEND_EAST;
                                }

                            }
                            break;
                        case Structures.Type.Bridge: // bridge
                            chunk.Blocks.SetID(x, height + 1, z, BlockType.HARDENED_CLAY);
                            break;
                        case Structures.Type.HumanCity1: // wheat?
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.GOLD_BLOCK);
                            break;
                        case Structures.Type.HumanCity2: // potato
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.HAY_BLOCK);
                            break;
                        case Structures.Type.HumanCity3: // carrot
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.OBSIDIAN);
                            break;
                        case Structures.Type.HumanCity4: // carrot
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.BIRCH_WOOD_STAIRS);
                            break;
                        case Structures.Type.Fortress: // Fort?
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.IRON_BLOCK);
                            break;
                        case Structures.Type.ElvenCity1: // Farm?
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.FARMLAND);
                            break;
                        case Structures.Type.ElvenCity2: // Farm?
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.MELON);
                            break;
                        case Structures.Type.ElvenCity3: // Farm?
                            chunk.Blocks.SetID(x, height - 1, z, BlockType.PUMPKIN);
                            break;
                        default:
                            directions neighbor = getRoadEdge(mapData, x + 1, z + 1);  // +1 for edge shift

                            switch (neighbor)
                            {
                                case directions.north:
                                case directions.south:
                                    if (x % 4 < 3)
                                    {
                                        if (x % 8 == 1)
                                        {
                                            chunk.Blocks.SetID(x, height + 1, z, BlockType.TORCH);
                                            currentWorld.
                                            chunk.Blocks.GetBlock(x, height + 1, z).SetTileEntity(                                        }
                                        chunk.Blocks.SetID(x, height, z, BlockType.FENCE);
                                    }
                                    break;
                                case directions.west:
                                case directions.east:
                                    if (z % 4 < 3)
                                    {
                                        chunk.Blocks.SetID(x, height, z, BlockType.FENCE);
                                        if (z % 8 == 1)
                                        {
                                            chunk.Blocks.SetID(x, height + 1, z, BlockType.TORCH);
                                        }
                                    }
                                    break;
                            }

                            break;
                    }
                }
            }
        }

        // North: 0;
        // East: 1;
        // South: 2;
        // West: 3;

        enum directions
        {
            none = -1,
            north,
            south,
            west,
            east,
            northwest,
            northeast,
            southwest,
            southeast
        }

        private directions getRoadEdge(internalData[,] map, int x, int y)
        {
            Structures.Type nw = map[x - 1, y + 1].structureType;
            Structures.Type no = map[x - 0, y + 1].structureType;
            Structures.Type ne = map[x + 1, y + 1].structureType;
            Structures.Type ea = map[x - 1, y - 0].structureType;
            Structures.Type we = map[x + 1, y - 0].structureType;
            Structures.Type sw = map[x - 1, y - 1].structureType;
            Structures.Type so = map[x - 0, y - 1].structureType;
            Structures.Type se = map[x + 1, y - 1].structureType;

            if (nw == Structures.Type.Road && 
                no == Structures.Type.Road &&
                ne == Structures.Type.Road) return directions.north;
            if (sw == Structures.Type.Road &&
                so == Structures.Type.Road &&
                se == Structures.Type.Road) return directions.south;
            if (nw == Structures.Type.Road &&
                we == Structures.Type.Road &&
                sw == Structures.Type.Road) return directions.west;
            if (ne == Structures.Type.Road &&
                ea == Structures.Type.Road &&
                se == Structures.Type.Road) return directions.east;

            return directions.none;

        }


        static void FlatChunk(ChunkRef chunk, int height)
        {
            // Create bedrock
            for (int y = 0; y < 2; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.BEDROCK);
                    }
                }
            }

            // Create stone
            for (int y = 2; y < height - 5; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.STONE);
                    }
                }
            }

            // Create dirt
            for (int y = height - 5; y < height - 1; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.DIRT);
                    }
                }
            }

            // Create grass
            for (int y = height - 1; y < height; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        chunk.Blocks.SetID(x, y, z, (int)BlockType.GRASS);
                    }
                }
            }
        }
    }
}
