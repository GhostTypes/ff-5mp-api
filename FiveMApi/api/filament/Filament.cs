namespace FiveMApi.api.filament
{
    public class Filament
    {
        public string Name { get; set; }
        public int PrintTemp { get; set; }
        public int LoadTemp { get; set; }

        public Filament(string name, int printTemp, int loadTemp)
        {
            Name = name;
            PrintTemp = printTemp;
            LoadTemp = loadTemp;
        }
    }
}