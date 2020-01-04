using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using System;
using Xamarin.Forms;
using Esri.ArcGISRuntime.Data;
using System.Threading.Tasks;
using System.Linq;
using Colors = System.Drawing.Color;
using System.Collections.Generic;
using Esri.ArcGISRuntime.Xamarin.Forms;

namespace XamArcGIS
{
    public partial class MapPage : ContentPage
    {
        private GraphicsOverlay _sketchOverlay;
        public MapPage()
        {
            InitializeComponent();

            Initialize();
        }

        private void Initialize()
        {
            Map myMap = new Map(Basemap.CreateLightGrayCanvas());

            _sketchOverlay = new GraphicsOverlay();

            WorldMapView.GraphicsOverlays.Add(_sketchOverlay);

            WorldMapView.Map = myMap;

            Array sketchModes = System.Enum.GetValues(typeof(SketchCreationMode));

            foreach (var mode in sketchModes)
            {
                if (mode.ToString().ToUpper() == "MULTIPOINT")
                {
                    SketchModePicker.Items.Add(mode.ToString());
                }
            }

            if (Device.RuntimePlatform == Device.Android)
            {
                DrawToolsGrid.BackgroundColor = Color.Gray;
            }

            CompleteButton.Command = WorldMapView.SketchEditor.CompleteCommand;
        }

        #region Graphic and symbol helpers
        private Graphic CreateGraphic(Esri.ArcGISRuntime.Geometry.Geometry geometry)
        {
            Symbol symbol = null;
            switch (geometry.GeometryType)
            {
                case GeometryType.Multipoint:
                    {
                        symbol = new SimpleMarkerSymbol()
                        {
                            Color = Colors.Purple,
                            Style = SimpleMarkerSymbolStyle.Circle,
                            Size = 15d
                        };
                        break;
                    }
            }

            return new Graphic(geometry, symbol);
        }

        private async Task<Graphic> GetGraphicAsync()
        {
            MapPoint mapPoint = (MapPoint)await WorldMapView.SketchEditor.StartAsync(SketchCreationMode.Point, false);

            var screenCoordinate = WorldMapView.LocationToScreen(mapPoint);

            IReadOnlyList<IdentifyGraphicsOverlayResult> results = await WorldMapView.IdentifyGraphicsOverlaysAsync(screenCoordinate, 2, false);

            Graphic graphic = null;
            IdentifyGraphicsOverlayResult idResult = results.FirstOrDefault();
            if (idResult != null && idResult.Graphics.Count > 0)
            {
                graphic = idResult.Graphics.FirstOrDefault();
            }

            return graphic;
        }
        #endregion

        private async void DrawButtonClick(object sender, EventArgs e)
        {
            try
            {
                DrawToolsGrid.IsVisible = false;

                SketchCreationMode creationMode = SketchCreationMode.Multipoint;
                Esri.ArcGISRuntime.Geometry.Geometry geometry = await WorldMapView.SketchEditor.StartAsync(creationMode, true);

                Graphic graphic = CreateGraphic(geometry);
                _sketchOverlay.Graphics.Add(graphic);

                ClearButton.IsEnabled = _sketchOverlay.Graphics.Count > 0;
                EditButton.IsEnabled = _sketchOverlay.Graphics.Count > 0;
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Error drawing graphic shape: " + ex.Message, "OK");
            }
        }

        private void ClearButtonClick(object sender, EventArgs e)
        {
            _sketchOverlay.Graphics.Clear();

            if (WorldMapView.SketchEditor.CancelCommand.CanExecute(null))
            {
                WorldMapView.SketchEditor.CancelCommand.Execute(null);
            }

            ClearButton.IsEnabled = false;
            EditButton.IsEnabled = false;
        }

        private async void EditButtonClick(object sender, EventArgs e)
        {
            try
            {
                DrawToolsGrid.IsVisible = false;

                Graphic editGraphic = await GetGraphicAsync();
                if (editGraphic == null) { return; }

                Geometry newGeometry = await WorldMapView.SketchEditor.StartAsync(editGraphic.Geometry);

                editGraphic.Geometry = newGeometry;
            }
            catch (TaskCanceledException)
            {

            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Error editing shape: " + ex.Message, "OK");
            }
        }

        private void ShowDrawTools(object sender, EventArgs e)
        {
            DrawToolsGrid.IsVisible = true;
        }
    }
}
