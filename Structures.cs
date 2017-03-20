using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarvenRealms
{
    public class Structures
    {
        private static short ColorSpacing = 5;

        public enum Type
        {
            Unknown = 0,
            Underground_Road,
            Road,
            Bridge,
            HumanCity1,
            HumanCity2,
            HumanCity3,
            HumanCity4,
            Fortress,
            ElvenCity1,
            ElvenCity2,
            ElvenCity3,
            
        };

        static Dictionary<Structures.Type, Color> structureDefinitions = new Dictionary<Structures.Type, Color>()
        {
            { Structures.Type.Underground_Road, Color.FromArgb(18, 18, 18) },
            { Structures.Type.Road, Color.FromArgb(150,127,18) },
            { Structures.Type.Bridge, Color.FromArgb(179,165,21) },
            { Structures.Type.HumanCity1, Color.FromArgb(0, 255, 0) },
            { Structures.Type.HumanCity2, Color.FromArgb(0, 160, 0) },
            { Structures.Type.HumanCity3, Color.FromArgb(0, 128, 0) },
            { Structures.Type.HumanCity4, Color.FromArgb(68, 255, 0) },
            { Structures.Type.Fortress, Color.FromArgb(255, 255, 255) },
            { Structures.Type.ElvenCity1, Color.FromArgb(255,192,0) },
            { Structures.Type.ElvenCity2, Color.FromArgb(255,160,0) },
            { Structures.Type.ElvenCity3, Color.FromArgb(255,128,0) }
        };

        public static Structures.Type getStructureType(Color color)
        {
            Structures.Type structure = Structures.Type.Unknown;
            foreach (KeyValuePair<Structures.Type, Color> def in structureDefinitions)
            {
                if (match(def.Value, color))
                {
                    structure = def.Key;
                    break;
                }
            }
            return structure;
        }

        private static bool match(Color a, Color b)
        {
            return (Math.Pow(a.R - b.R, 2) + Math.Pow(a.G - b.G, 2) + Math.Pow(a.B - b.B, 2)) < Math.Pow(ColorSpacing, 2);
        }
    }

}
