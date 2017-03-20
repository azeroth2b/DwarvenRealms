namespace DwarvenRealms
{
    public struct internalData
    {
        public int riverType;
        public Structures.Type structureType;
        public int waterLevel;
        public int riverLevel;
        public int biomeIndex;
        public int height;

        public internalData(int height, int biome, int riverType, int riverLevel, int waterLevel, Structures.Type structureType)
        {
            this.riverLevel = riverLevel;
            this.structureType = structureType;
            this.waterLevel = waterLevel;
            this.height = height;
            this.biomeIndex = biome;
            this.riverType = riverType;
        }

    }

    class mapData
    {
        internalData[,] data;
        int offsetX;
        int offsetY;

        public mapData(int x, int y, int offsetX, int offsetY)
        {
            this.data = new internalData[x, y];
            this.offsetX = offsetX;
            this.offsetY = offsetY;
        }

        public static internalData defaultData = new internalData(-1, -1, -1, -1, -1, Structures.Type.Unknown);

        public void setAll(internalData id, int x, int y)
        {
            this.data[x-offsetX, y-offsetY] = id;
        }

        public void setStructure(Structures.Type v, int x, int y)
        {
            this.data[x-offsetX, y-offsetY].structureType = v;
        }
        public void setHeight(int v, int x, int y)
        {
            this.data[x-offsetX, y-offsetY].height = v;
        }
        public void setBiome(int v, int x, int y)
        {
            this.data[x-offsetX, y-offsetY].biomeIndex = v;
        }
        public void setWaterLevel(int v, int x, int y)
        {
            this.data[x-offsetX, y-offsetY].waterLevel = v;
        }
        public void setRiverLevel(int v, int x, int y)
        {
            this.data[x-offsetX, y-offsetY].riverLevel = v;
        }
        public void setRiverType(int v, int x, int y)
        {
            this.data[x-offsetX, y-offsetY].riverType = v;
        }

        public internalData getInternalData(int x, int y)
        {
            if (x-offsetX > this.data.GetUpperBound(0) || x - offsetX < this.data.GetLowerBound(0) ||
                y - offsetY> this.data.GetUpperBound(1) || y - offsetY < this.data.GetLowerBound(1))
            {
                return mapData.defaultData;
            }

            return this.data[x-offsetX, y-offsetY];
        }

    }
}
