using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Shapes;

namespace ProjektV
{
    internal class ShapeProperties
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public int ShapeTop { get; set; }
        public int ShapeLeft { get; set; }    

        public string Kind { get; set; }

        public ShapeProperties() { }

        public ShapeProperties(int width, int height, int shapeTop, int shapeLeft, string shape)
        {
            Width = width;
            Height = height;
            ShapeTop = shapeTop;
            ShapeLeft = shapeLeft;
            Kind = shape;
        }
    }
}
