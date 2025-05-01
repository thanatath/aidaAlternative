using System.Drawing;

namespace aidaAlternative.Models
{
    public class StatItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public float? Percentage { get; set; }
        public Rectangle Bounds { get; set; }
    }
}