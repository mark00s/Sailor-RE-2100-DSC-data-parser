using BSc_Thesis.Models;
using GMap.NET;
using GMap.NET.ObjectModel;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BSc_Thesis.ViewModels
{
    class gMapViewModel : ViewModelBase
    {
        private ObservableCollection<GMapMarker> markersValue;
        public ObservableCollection<GMapMarker> Markers {
            get {
                if (markersValue == null) {
                    markersValue = new ObservableCollection<GMapMarker>();
                }

                return markersValue;
            }
        }
        public DelegateCommand AddPointCommand { get; }

        //addMapMarker(53, 14);
        public gMapViewModel() {
            Services.MessengerHub.Subscribe<GeoMessage>(addPoint);
        }

        public void addPoint(GeoMessage gm)
        {
            double la;
            double lo;

            if (gm.Lat.Split('N').Length == 2) {
                // N => +
                string[] lat = gm.Lat.Split('N');
                la = Double.Parse(lat[0]);
                la = Double.Parse(lat[0]) + (Double.Parse(lat[1]) / 60.0);

            } else {
                // S => -
                string[] lat = gm.Lat.Split('S');
                la = (-1.0) * (Double.Parse(lat[0]) + (Double.Parse(lat[1]) / 60.0));
            }

            if (gm.Long.Split('E').Length == 2) {
                // E => +
                string[] lon = gm.Long.Split('E');
                lo = Double.Parse(lon[0]);
                lo = Double.Parse(lon[0]) + (Double.Parse(lon[1]) / 60.0);

            } else {
                // W => -
                string[] lon = gm.Long.Split('W');
                lo = (-1.0) * (Double.Parse(lon[0]) + (Double.Parse(lon[1]) / 60.0));
            }

            if (markersValue.Count == 0) {
                markersValue = new ObservableCollection<GMapMarker>();
            }
            Application.Current.Dispatcher.Invoke((Action) delegate {
                GMapMarker gmm = new GMapMarker(new PointLatLng(la, lo));
                gmm.Shape = new PinControl();
                Markers.Add(gmm);
                OnPropertyChanged("Markers");
            });

        }
    }
}
