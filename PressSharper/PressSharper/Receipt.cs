using System.Collections.Generic;
using System.Xml.Linq;

namespace PressSharper
{
    public class Receipt
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Infusion { get; set; }
        public double Pg { get; set; }
        public double Vg { get; set; }
        public string Ratio { get; set; }
        public string ImageName { get; set; }
        public Dictionary<Aroma,double> Aromas { get; set; }
    }

    public class ImageLink
    {
        public int ReceiptId { get; set; }
        public string Name { get; set; }
    }
}