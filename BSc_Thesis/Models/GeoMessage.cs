using TinyMessenger;

namespace BSc_Thesis.Models
{
    class GeoMessage : TinyMessageBase
    {
        public string Lat { get; set; } 
        public string Long { get; set; }
        public GeoMessage(object sender, string Lat, string Long) : base(sender)
        {
            this.Lat = Lat;
            this.Long = Long;
        }
    }
}
