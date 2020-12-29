using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GMap.NET;
using GMap.NET.WindowsPresentation;

namespace BSc_Thesis
{
    /// <summary>
    /// Interaction logic for gMapView.xaml
    /// </summary>
    public partial class gMapView : UserControl
    {
        private readonly SynchronizationContext synchronizationContext = SynchronizationContext.Current;

        public gMapView()
        {
            InitializeComponent();
        }

        private void MapView_Loaded(object sender, RoutedEventArgs e)
        {
            mapView.MapProvider = GMap.NET.MapProviders.OpenSeaMapHybridProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            mapView.IgnoreMarkerOnMouseWheel = true;
            mapView.ShowCenter = false;
            mapView.MinZoom = 2;
            mapView.MaxZoom = 24;
            mapView.Zoom = 11;
            mapView.Position = new PointLatLng(53.4285, 14.5637);
            mapView.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
            mapView.CanDragMap = true;
            mapView.DragButton = MouseButton.Left;
        }

        private void addMapMarker(double lat, double lng)
        {
            GMapMarker x = new GMapMarker(new PointLatLng(lat, lng));
            x.Shape = new PinControl();
            mapView.Markers.Add(x);
        }
    }
}
