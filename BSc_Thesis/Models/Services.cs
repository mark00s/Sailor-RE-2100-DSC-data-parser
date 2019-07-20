using TinyMessenger;

namespace BSc_Thesis.Models
{
    static class Services
    {
        private static TinyMessengerHub messengerHub;

        public static TinyMessengerHub MessengerHub {
            get {
                if (messengerHub == null)
                    messengerHub = new TinyMessengerHub();
                return messengerHub;
            }
        }
    }
}
