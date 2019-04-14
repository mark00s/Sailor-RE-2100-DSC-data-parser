using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
