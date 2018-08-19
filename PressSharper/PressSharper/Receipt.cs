namespace PressSharper
{
    public class Receipt
    {
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Description { get; set; }
        public int Infusion { get; set; }
        public Flavor[] Flavors { get; set; }
    }
}