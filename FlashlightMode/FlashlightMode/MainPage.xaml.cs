using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace FlashlightMode
{
    public partial class MainPage : ContentPage
    {
        private FeatureLayer _featureLayer;

        public MainPage()
        {
            InitializeComponent();
            InitializeMap();
            MapView.NavigationCompleted += MapView_NavigationCompleted;
        }

        private void MapView_NavigationCompleted(object sender, EventArgs e)
        {
            Preferences.Set("Viewpoint", MapView.GetCurrentViewpoint(ViewpointType.BoundingGeometry).ToJson());
        }

        private async void InitializeMap()
        {
            var portal = await ArcGISPortal.CreateAsync();
            var item = await PortalItem.CreateAsync(portal, "6c75b7143f48487790bde6f5390df1a5");
            var map = new Esri.ArcGISRuntime.Mapping.Map(item);
            var initialViewpointJson = Preferences.Get("Viewpoint", null);
            if (initialViewpointJson != null)
            {
                try
                {
                    map.InitialViewpoint = Viewpoint.FromJson(initialViewpointJson);
                }
                catch (Exception ex) { }
            }
            MapView.Map = map;
            await map.LoadAsync();
            _featureLayer = (FeatureLayer)MapView.Map.OperationalLayers[0];

            var result = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            while (MapView.LocationDisplay == null)
            {
                await Task.Delay(500);
            }

            MapView.LocationDisplay.IsEnabled = true;
        }

        private async void ZoomTo_Clicked(object sender, EventArgs e)
        {
            await MapView.SetViewpointAsync(new Viewpoint(MapView.LocationDisplay.Location.Position, 3000));
        }

        private void CreateFeatures_Clicked(object sender, EventArgs e)
        {
            MainToolbar.IsVisible = false;
            AddFeatuersToolbar.IsVisible = true;
            MapView.GeoViewTapped += MapView_GeoViewTapped;
        }

        private async void MapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            var feature = _featureLayer.FeatureTable.CreateFeature(new Dictionary<string, object>(), e.Location);
            await _featureLayer.FeatureTable.AddFeatureAsync(feature);
        }

        private async void Apply_Clicked(object sender, EventArgs e)
        {
            HideAddFeatureToolbar();
            var serviceTable = (ServiceFeatureTable)_featureLayer.FeatureTable;
            await serviceTable.ApplyEditsAsync();
        }

        private void HideAddFeatureToolbar()
        {
            MapView.GeoViewTapped -= MapView_GeoViewTapped;
            AddFeatuersToolbar.IsVisible = false;
            MainToolbar.IsVisible = true;
        }

        private void ClearCache_Clicked(object sender, EventArgs e)
        {
            var serviceTable = (ServiceFeatureTable)_featureLayer.FeatureTable;
            serviceTable.ClearCache(false);
        }

        private void FollowMe_Clicked(object sender, EventArgs e)
        {
            MapView.LocationDisplay.AutoPanMode = Esri.ArcGISRuntime.UI.LocationDisplayAutoPanMode.CompassNavigation;
        }

        private async void Cancel_Clicked(object sender, EventArgs e)
        {
            HideAddFeatureToolbar();
            var serviceTable = (ServiceFeatureTable)_featureLayer.FeatureTable;
            await serviceTable.UndoLocalEditsAsync();
        }
    }
}
