using AssetStudio;
using AssetStudioCore;
using System.Collections.ObjectModel;
using CoreGraphics;
using UIKit;

namespace AssetStudio.Maui;

public partial class MainPage : ContentPage
{
    private readonly ObservableCollection<AssetItem> displayedAssets = new();
    private AssetItem selectedAsset;
    private double listColumnStartWidth;

    private enum SortColumn { Name, Type }
    private SortColumn sortColumn = SortColumn.Name;
    private bool sortAscending = true;

    public MainPage()
    {
        InitializeComponent();
        AssetListView.ItemsSource = displayedAssets;

        Studio.StatusUpdate = msg => MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = msg);
        Studio.ErrorReported = msg => MainThread.BeginInvokeOnMainThread(() => DisplayAlert("Export Error", msg, "OK"));
        Progress.Default = new Progress<int>(value => MainThread.BeginInvokeOnMainThread(() => ExportProgressBar.Progress = value / 100.0));

        ImagePreviewScroll.HandlerChanged += (s, e) => AttachScrollWheelZoom();
    }

    private string cachedThreeJs;
    private string cachedOrbitControlsJs;

    private async Task<(string threeJs, string orbitControlsJs)> GetThreeJsAsync()
    {
        if (cachedThreeJs == null)
        {
            using var threeStream = await FileSystem.OpenAppPackageFileAsync("threejs/three.min.js");
            using var threeReader = new StreamReader(threeStream);
            cachedThreeJs = await threeReader.ReadToEndAsync();

            using var orbitStream = await FileSystem.OpenAppPackageFileAsync("threejs/OrbitControls.js");
            using var orbitReader = new StreamReader(orbitStream);
            cachedOrbitControlsJs = await orbitReader.ReadToEndAsync();
        }
        return (cachedThreeJs, cachedOrbitControlsJs);
    }

    private async Task ShowMeshInViewerAsync(string geometryJson)
    {
        var (threeJs, orbitJs) = await GetThreeJsAsync();
        var html = MeshViewerHtml.BuildMeshHtml(threeJs, orbitJs, geometryJson);
        MeshPreviewWebView.Source = new HtmlWebViewSource { Html = html };
    }

    private async Task ShowShaderPreviewInViewerAsync(ShaderPreviewHelper.Descriptor descriptor)
    {
        var (threeJs, orbitJs) = await GetThreeJsAsync();
        var html = MeshViewerHtml.BuildShaderHtml(threeJs, orbitJs, descriptor.R, descriptor.G, descriptor.B, descriptor.Metalness, descriptor.Roughness);
        MeshPreviewWebView.Source = new HtmlWebViewSource { Html = html };
    }

    private bool scrollZoomAttached;

    private void AttachScrollWheelZoom()
    {
        if (scrollZoomAttached)
            return;
        if (ImagePreviewScroll.Handler?.PlatformView is not UIScrollView nativeScrollView)
            return;

        scrollZoomAttached = true;

        // Disable the scroll view's own handling of indirect scroll input (trackpad/mouse
        // wheel) so wheel motion drives zoom instead of panning; direct touch-drag panning
        // (and dragging the scrollbar thumbs once zoomed beyond the viewport) is untouched.
        if (nativeScrollView.PanGestureRecognizer is UIPanGestureRecognizer nativePan)
        {
            nativePan.AllowedScrollTypesMask = (UIScrollTypeMask)0;
        }

        var pan = new UIPanGestureRecognizer(HandleScrollWheelZoom)
        {
            AllowedScrollTypesMask = UIScrollTypeMask.All
        };
        nativeScrollView.AddGestureRecognizer(pan);
    }

    private void HandleScrollWheelZoom(UIPanGestureRecognizer recognizer)
    {
        if (recognizer.State != UIGestureRecognizerState.Changed)
            return;
        if (!PreviewImage.IsVisible)
            return;

        var translation = recognizer.TranslationInView(recognizer.View);
        AdjustZoom(-translation.Y * 0.002);
        recognizer.SetTranslation(CGPoint.Empty, recognizer.View);
    }

    private static string FormatException(Exception ex)
    {
        var lines = new List<string>();
        var current = ex;
        while (current != null)
        {
            lines.Add($"{current.GetType().Name}: {current.Message}");
            current = current.InnerException;
        }
        return string.Join("\ncaused by ", lines);
    }

    private void OnSplitterPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                listColumnStartWidth = ListColumn.Width.Value;
                break;
            case GestureStatus.Running:
                var newWidth = listColumnStartWidth + e.TotalX;
                newWidth = Math.Clamp(newWidth, 150, 900);
                ListColumn.Width = new GridLength(newWidth);
                break;
        }
    }

    private async void OnOpenFilesClicked(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickMultipleAsync(new PickOptions { PickerTitle = "Select Unity asset files" });
        var files = result?.Select(f => f.FullPath).ToArray();
        if (files == null || files.Length == 0)
            return;

        StatusLabel.Text = "Loading...";
        ExportSelectedButton.IsEnabled = false;
        ExportAllButton.IsEnabled = false;

        await Task.Run(() => Studio.assetsManager.LoadFiles(files));
        var (productName, _) = await Task.Run(() => Studio.BuildAssetData());

        RefreshList();

        ExportSelectedButton.IsEnabled = true;
        ExportAllButton.IsEnabled = true;
        StatusLabel.Text = string.IsNullOrEmpty(productName)
            ? $"Loaded {displayedAssets.Count} assets."
            : $"Loaded {displayedAssets.Count} assets from {productName}.";
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshList();
    }

    private void RefreshList()
    {
        var query = AssetSearchBar.Text?.Trim();
        IEnumerable<AssetItem> assets = string.IsNullOrEmpty(query)
            ? Studio.visibleAssets
            : Studio.visibleAssets.Where(a => a.Text.Contains(query, StringComparison.OrdinalIgnoreCase));

        assets = sortColumn == SortColumn.Name
            ? (sortAscending ? assets.OrderBy(a => a.Text, StringComparer.OrdinalIgnoreCase) : assets.OrderByDescending(a => a.Text, StringComparer.OrdinalIgnoreCase))
            : (sortAscending ? assets.OrderBy(a => a.TypeString, StringComparer.OrdinalIgnoreCase) : assets.OrderByDescending(a => a.TypeString, StringComparer.OrdinalIgnoreCase));

        displayedAssets.Clear();
        foreach (var asset in assets)
        {
            displayedAssets.Add(asset);
        }

        NameHeaderLabel.Text = "Name" + (sortColumn == SortColumn.Name ? (sortAscending ? " ▲" : " ▼") : "");
        TypeHeaderLabel.Text = "Type" + (sortColumn == SortColumn.Type ? (sortAscending ? " ▲" : " ▼") : "");
    }

    private void OnNameHeaderTapped(object sender, TappedEventArgs e)
    {
        if (sortColumn == SortColumn.Name)
            sortAscending = !sortAscending;
        else
        {
            sortColumn = SortColumn.Name;
            sortAscending = true;
        }
        RefreshList();
    }

    private void OnTypeHeaderTapped(object sender, TappedEventArgs e)
    {
        if (sortColumn == SortColumn.Type)
            sortAscending = !sortAscending;
        else
        {
            sortColumn = SortColumn.Type;
            sortAscending = true;
        }
        RefreshList();
    }

    private const int MaxPreviewTextLength = 200_000;
    private int previewGeneration;

    private async void OnAssetSelected(object sender, SelectionChangedEventArgs e)
    {
        selectedAsset = e.CurrentSelection.FirstOrDefault() as AssetItem;
        await UpdatePreviewAsync();
    }

    private async Task UpdatePreviewAsync()
    {
        PreviewImage.IsVisible = false;
        PreviewText.IsVisible = false;
        MeshPreviewWebView.IsVisible = false;
        ZoomControls.IsVisible = false;
        ShaderPreviewToggleButton.IsVisible = false;
        showingShaderViewer = false;

        var asset = selectedAsset;
        if (asset == null)
            return;

        var generation = ++previewGeneration;
        StatusLabel.Text = "Loading preview...";

        if (asset.Asset is Mesh mesh)
        {
            string meshJson = null;
            string meshError = null;
            await Task.Run(() =>
            {
                try
                {
                    meshJson = MeshViewerHelper.BuildGeometryJson(mesh);
                }
                catch (Exception ex)
                {
                    meshError = FormatException(ex);
                }
            });

            if (generation != previewGeneration)
                return;

            StatusLabel.Text = "Ready";

            if (meshError != null)
            {
                await DisplayAlert("Preview Error", meshError, "OK");
                return;
            }

            MeshPreviewWebView.IsVisible = true;
            try
            {
                await ShowMeshInViewerAsync(meshJson);
            }
            catch (Exception ex)
            {
                await DisplayAlert("3D Preview Error", FormatException(ex), "OK");
            }
            return;
        }

        byte[] png = null;
        string dump = null;
        string error = null;

        await Task.Run(() =>
        {
            try
            {
                png = PreviewHelper.GetPreviewPng(asset);
                if (png == null)
                {
                    dump = Studio.DumpAsset(asset.Asset);
                    if (dump != null && dump.Length > MaxPreviewTextLength)
                    {
                        dump = dump.Substring(0, MaxPreviewTextLength) + "\n... [truncated for preview - export this asset to see the full content]";
                    }
                }
            }
            catch (Exception ex)
            {
                error = FormatException(ex);
            }
        });

        if (generation != previewGeneration)
            return; // a newer selection was made while this one was loading; discard this result

        StatusLabel.Text = "Ready";

        if (error != null)
        {
            await DisplayAlert("Preview Error", error, "OK");
            return;
        }

        if (png != null)
        {
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
            var size = ReadPngSize(png);
            imagePixelWidth = size?.Item1 ?? 0;
            imagePixelHeight = size?.Item2 ?? 0;
            PreviewImage.IsVisible = true;
            ResetZoom();
            ZoomControls.IsVisible = true;
        }
        else if (dump != null)
        {
            PreviewText.Text = dump;
            PreviewText.IsVisible = true;
            ZoomControls.IsVisible = false;

            if (asset.Asset is Shader shaderAsset)
            {
                shaderPreviewDescriptor = ShaderPreviewHelper.Build(shaderAsset);
                ShaderPreviewToggleButton.Text = "Show 3D Preview";
                ShaderPreviewToggleButton.IsVisible = true;
            }
        }
        else
        {
            ZoomControls.IsVisible = false;
        }
    }

    private bool showingShaderViewer;
    private ShaderPreviewHelper.Descriptor shaderPreviewDescriptor;

    private async void OnShaderPreviewToggleClicked(object sender, EventArgs e)
    {
        showingShaderViewer = !showingShaderViewer;
        if (showingShaderViewer)
        {
            PreviewText.IsVisible = false;
            MeshPreviewWebView.IsVisible = true;
            ShaderPreviewToggleButton.Text = "Show Source";
            await ShowShaderPreviewInViewerAsync(shaderPreviewDescriptor);
        }
        else
        {
            MeshPreviewWebView.IsVisible = false;
            PreviewText.IsVisible = true;
            ShaderPreviewToggleButton.Text = "Show 3D Preview";
        }
    }

    private static (double, double)? ReadPngSize(byte[] png)
    {
        if (png == null || png.Length < 24)
            return null;
        if (png[12] != 'I' || png[13] != 'H' || png[14] != 'D' || png[15] != 'R')
            return null;
        int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        int height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return (width, height);
    }

    private const double MinZoom = 0.1;
    private const double MaxZoom = 5.0;
    private double imagePixelWidth;
    private double imagePixelHeight;
    private double zoomFactor = 1.0;

    private void ApplyZoom()
    {
        if (imagePixelWidth <= 0 || imagePixelHeight <= 0)
            return;

        var viewportWidth = ImagePreviewScroll.Width > 0 ? ImagePreviewScroll.Width - 20 : imagePixelWidth;
        var viewportHeight = ImagePreviewScroll.Height > 0 ? ImagePreviewScroll.Height - 20 : imagePixelHeight;
        var fitScale = Math.Min(viewportWidth / imagePixelWidth, viewportHeight / imagePixelHeight);
        if (fitScale <= 0 || double.IsInfinity(fitScale) || double.IsNaN(fitScale))
            fitScale = 1.0;

        PreviewImage.WidthRequest = imagePixelWidth * fitScale * zoomFactor;
        PreviewImage.HeightRequest = imagePixelHeight * fitScale * zoomFactor;
    }

    private void ResetZoom()
    {
        zoomFactor = 1.0;
        ApplyZoom();
    }

    private void AdjustZoom(double delta)
    {
        zoomFactor = Math.Clamp(zoomFactor + delta, MinZoom, MaxZoom);
        ApplyZoom();
    }

    private void OnZoomInClicked(object sender, EventArgs e) => AdjustZoom(0.05);

    private void OnZoomOutClicked(object sender, EventArgs e) => AdjustZoom(-0.05);

    private async void OnExportSelectedClicked(object sender, EventArgs e)
    {
        if (selectedAsset == null)
        {
            await DisplayAlert("Export", "Select an asset first.", "OK");
            return;
        }
        await ExportAssetsAsync(new List<AssetItem> { selectedAsset });
    }

    private async void OnExportAllClicked(object sender, EventArgs e)
    {
        await ExportAssetsAsync(Studio.visibleAssets);
    }

    private async Task ExportAssetsAsync(List<AssetItem> assets)
    {
        var folder = await FolderPicker.PickFolderAsync();
        if (string.IsNullOrEmpty(folder))
            return;

        Studio.ExportAssets(folder, assets, ExportType.Convert);
    }
}
