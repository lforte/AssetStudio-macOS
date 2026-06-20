using AssetStudio;
using AssetStudioCore;
using System.Collections.ObjectModel;
using CoreGraphics;
using UIKit;

namespace AssetStudio.Maui;

public partial class MainPage : ContentPage
{
    private readonly ObservableCollection<AssetItem> displayedAssets = new();
    private readonly ObservableCollection<SceneTreeRow> sceneTreeRows = new();
    private readonly ObservableCollection<TypeTreeItem> assetClassRows = new();
    private List<AssetNode> sceneTreeRoots = new();
    private AssetItem selectedAsset;
    private double listColumnStartWidth;

    private enum SortColumn { Name, Container, Type, PathID, Size }
    private SortColumn sortColumn = SortColumn.Name;
    private bool sortAscending = true;

    private enum ListTab { SceneHierarchy, AssetList, AssetClasses }
    private ListTab currentListTab = ListTab.AssetList;

    private enum PreviewTab { Visual, Dump }
    private PreviewTab currentPreviewTab = PreviewTab.Visual;

    private static readonly Microsoft.Maui.Graphics.Color ActiveTabColor = Microsoft.Maui.Graphics.Color.FromArgb("#BBBBBB");
    private static readonly Microsoft.Maui.Graphics.Color InactiveTabColor = Microsoft.Maui.Graphics.Colors.Transparent;

    public MainPage()
    {
        InitializeComponent();
        AssetListView.ItemsSource = displayedAssets;
        SceneHierarchyListView.ItemsSource = sceneTreeRows;
        AssetClassesListView.ItemsSource = assetClassRows;

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

    private double containerColumnStartWidth;
    private double typeColumnStartWidth;
    private double pathIdColumnStartWidth;
    private double sizeColumnStartWidth;

    private void OnContainerColumnPanUpdated(object sender, PanUpdatedEventArgs e) =>
        ResizeColumn(e, ref containerColumnStartWidth, AssetListColumnWidths.Instance.Container, w => AssetListColumnWidths.Instance.Container = w);

    private void OnTypeColumnPanUpdated(object sender, PanUpdatedEventArgs e) =>
        ResizeColumn(e, ref typeColumnStartWidth, AssetListColumnWidths.Instance.Type, w => AssetListColumnWidths.Instance.Type = w);

    private void OnPathIdColumnPanUpdated(object sender, PanUpdatedEventArgs e) =>
        ResizeColumn(e, ref pathIdColumnStartWidth, AssetListColumnWidths.Instance.PathID, w => AssetListColumnWidths.Instance.PathID = w);

    private void OnSizeColumnPanUpdated(object sender, PanUpdatedEventArgs e) =>
        ResizeColumn(e, ref sizeColumnStartWidth, AssetListColumnWidths.Instance.Size, w => AssetListColumnWidths.Instance.Size = w);

    private static void ResizeColumn(PanUpdatedEventArgs e, ref double startWidth, double currentWidth, Action<double> apply)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                startWidth = currentWidth;
                break;
            case GestureStatus.Running:
                apply(Math.Clamp(startWidth + e.TotalX, 30, 400));
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
        var (productName, treeRoots) = await Task.Run(() => Studio.BuildAssetData());
        var classMap = await Task.Run(() => Studio.BuildClassStructure());

        sceneTreeRoots = treeRoots;
        RefreshSceneTree();
        RefreshAssetClasses(classMap);
        RefreshList();

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

        assets = sortColumn switch
        {
            SortColumn.Name => sortAscending ? assets.OrderBy(a => a.Text, StringComparer.OrdinalIgnoreCase) : assets.OrderByDescending(a => a.Text, StringComparer.OrdinalIgnoreCase),
            SortColumn.Container => sortAscending ? assets.OrderBy(a => a.Container, StringComparer.OrdinalIgnoreCase) : assets.OrderByDescending(a => a.Container, StringComparer.OrdinalIgnoreCase),
            SortColumn.Type => sortAscending ? assets.OrderBy(a => a.TypeString, StringComparer.OrdinalIgnoreCase) : assets.OrderByDescending(a => a.TypeString, StringComparer.OrdinalIgnoreCase),
            SortColumn.PathID => sortAscending ? assets.OrderBy(a => a.m_PathID) : assets.OrderByDescending(a => a.m_PathID),
            SortColumn.Size => sortAscending ? assets.OrderBy(a => a.FullSize) : assets.OrderByDescending(a => a.FullSize),
            _ => assets,
        };

        displayedAssets.Clear();
        foreach (var asset in assets)
        {
            displayedAssets.Add(asset);
        }

        NameHeaderLabel.Text = "Name" + SortArrow(SortColumn.Name);
        ContainerHeaderLabel.Text = "Container" + SortArrow(SortColumn.Container);
        TypeHeaderLabel.Text = "Type" + SortArrow(SortColumn.Type);
        PathIdHeaderLabel.Text = "PathID" + SortArrow(SortColumn.PathID);
        SizeHeaderLabel.Text = "Size" + SortArrow(SortColumn.Size);
    }

    private string SortArrow(SortColumn column) => sortColumn == column ? (sortAscending ? " ▲" : " ▼") : "";

    private void SetSortColumn(SortColumn column)
    {
        if (sortColumn == column)
            sortAscending = !sortAscending;
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
        RefreshList();
    }

    private void OnNameHeaderTapped(object sender, TappedEventArgs e) => SetSortColumn(SortColumn.Name);
    private void OnContainerHeaderTapped(object sender, TappedEventArgs e) => SetSortColumn(SortColumn.Container);
    private void OnTypeHeaderTapped(object sender, TappedEventArgs e) => SetSortColumn(SortColumn.Type);
    private void OnPathIdHeaderTapped(object sender, TappedEventArgs e) => SetSortColumn(SortColumn.PathID);
    private void OnSizeHeaderTapped(object sender, TappedEventArgs e) => SetSortColumn(SortColumn.Size);

    private void RefreshSceneTree()
    {
        sceneTreeRows.Clear();
        foreach (var root in sceneTreeRoots)
        {
            var rootRow = new SceneTreeRow(root, 0);
            sceneTreeRows.Add(rootRow);
            if (rootRow.HasChildren)
            {
                rootRow.IsExpanded = true;
                foreach (var child in root.Nodes)
                {
                    sceneTreeRows.Add(new SceneTreeRow(child, 1));
                }
            }
        }
    }

    private void ToggleTreeRow(SceneTreeRow row)
    {
        if (!row.HasChildren)
            return;

        var index = sceneTreeRows.IndexOf(row);
        if (index < 0)
            return;

        if (row.IsExpanded)
        {
            var removeCount = 0;
            while (index + 1 + removeCount < sceneTreeRows.Count && sceneTreeRows[index + 1 + removeCount].Depth > row.Depth)
                removeCount++;
            for (var k = 0; k < removeCount; k++)
                sceneTreeRows.RemoveAt(index + 1);
            row.IsExpanded = false;
        }
        else
        {
            row.IsExpanded = true;
            var insertAt = index + 1;
            foreach (var child in row.Node.Nodes)
            {
                sceneTreeRows.Insert(insertAt, new SceneTreeRow(child, row.Depth + 1));
                insertAt++;
            }
        }
    }

    private void OnSceneTreeRowSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not SceneTreeRow row)
            return;

        // Defer everything to the next run loop tick: acting synchronously inside a
        // SelectionChanged callback that originated in one of the CollectionView's own cells
        // (whether that's mutating the bound ObservableCollection or touching other UI) has
        // been observed to crash the underlying UICollectionView on Mac Catalyst (SIGABRT).
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Clear the selection so tapping the same row again later still raises
                // SelectionChanged (CollectionView doesn't re-raise it for a no-op selection).
                SceneHierarchyListView.SelectedItem = null;

                ToggleTreeRow(row);

                if (row.Node.GameObject != null)
                {
                    selectedAsset = null;
                    ExportSelectedButton.IsEnabled = false;
                    await UpdatePreviewAsync(row.Node.GameObject, null);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Scene Hierarchy Error", FormatException(ex), "OK");
            }
        });
    }

    private void RefreshAssetClasses(Dictionary<string, SortedDictionary<int, TypeTreeItem>> classMap)
    {
        assetClassRows.Clear();
        foreach (var versionGroup in classMap.Values)
        {
            foreach (var item in versionGroup.Values)
            {
                assetClassRows.Add(item);
            }
        }
    }

    private void OnAssetClassSelected(object sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection.FirstOrDefault() as TypeTreeItem;
        if (item == null)
            return;

        selectedAsset = null;
        ExportSelectedButton.IsEnabled = false;

        PreviewImage.IsVisible = false;
        MeshPreviewWebView.IsVisible = false;
        FallbackPreviewImage.IsVisible = false;
        ZoomControls.IsVisible = false;
        WireframeToggleButton.IsVisible = false;
        DumpText.Text = item.ToString();
        SetPreviewTab(PreviewTab.Dump);
    }

    private void SetListTab(ListTab tab)
    {
        currentListTab = tab;

        AssetListHeaderRow.IsVisible = tab == ListTab.AssetList;
        AssetClassesHeaderRow.IsVisible = tab == ListTab.AssetClasses;

        AssetListView.IsVisible = tab == ListTab.AssetList;
        SceneHierarchyListView.IsVisible = tab == ListTab.SceneHierarchy;
        AssetClassesListView.IsVisible = tab == ListTab.AssetClasses;

        SceneHierarchyTabButton.BackgroundColor = tab == ListTab.SceneHierarchy ? ActiveTabColor : InactiveTabColor;
        AssetListTabButton.BackgroundColor = tab == ListTab.AssetList ? ActiveTabColor : InactiveTabColor;
        AssetClassesTabButton.BackgroundColor = tab == ListTab.AssetClasses ? ActiveTabColor : InactiveTabColor;
    }

    private void OnSceneHierarchyTabClicked(object sender, EventArgs e) => SetListTab(ListTab.SceneHierarchy);
    private void OnAssetListTabClicked(object sender, EventArgs e) => SetListTab(ListTab.AssetList);
    private void OnAssetClassesTabClicked(object sender, EventArgs e) => SetListTab(ListTab.AssetClasses);

    private void SetPreviewTab(PreviewTab tab)
    {
        currentPreviewTab = tab;
        VisualPreviewPanel.IsVisible = tab == PreviewTab.Visual;
        DumpText.IsVisible = tab == PreviewTab.Dump;
        VisualPreviewTabButton.BackgroundColor = tab == PreviewTab.Visual ? ActiveTabColor : InactiveTabColor;
        DumpTabButton.BackgroundColor = tab == PreviewTab.Dump ? ActiveTabColor : InactiveTabColor;
    }

    private void OnVisualPreviewTabClicked(object sender, EventArgs e) => SetPreviewTab(PreviewTab.Visual);
    private void OnDumpTabClicked(object sender, EventArgs e) => SetPreviewTab(PreviewTab.Dump);

    private const int MaxPreviewTextLength = 200_000;
    private int previewGeneration;

    private async void OnAssetSelected(object sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection.FirstOrDefault() as AssetItem;
        selectedAsset = item;
        ExportSelectedButton.IsEnabled = item != null;
        await UpdatePreviewAsync(item?.Asset, item);
    }

    private async Task UpdatePreviewAsync(Object asset, AssetItem item)
    {
        PreviewImage.IsVisible = false;
        MeshPreviewWebView.IsVisible = false;
        FallbackPreviewImage.IsVisible = false;
        ZoomControls.IsVisible = false;
        WireframeToggleButton.IsVisible = false;
        WireframeToggleButton.Text = "Wireframe";
        wireframeEnabled = false;
        AudioPlayerPanel.IsVisible = false;
        AudioPlayer.Stop();
        currentAudioWav = null;
        DumpText.Text = string.Empty;

        if (asset == null)
            return;

        var generation = ++previewGeneration;
        StatusLabel.Text = "Loading preview...";

        string dump = null;
        string dumpError = null;
        byte[] png = null;
        string meshJson = null;
        byte[] wav = null;
        string visualError = null;

        await Task.Run(() =>
        {
            try
            {
                dump = Studio.DumpAsset(asset);
            }
            catch (Exception ex)
            {
                dumpError = FormatException(ex);
            }

            try
            {
                if (item != null)
                {
                    png = PreviewHelper.GetPreviewPng(item);
                }
                if (asset is Mesh mesh)
                {
                    meshJson = MeshViewerHelper.BuildGeometryJson(mesh);
                }
            }
            catch (Exception ex)
            {
                visualError = FormatException(ex);
            }

            // FMOD can't decode every legacy/unsupported audio format - that's not an error
            // worth interrupting the user with, it just means no playback preview is available.
            try
            {
                if (asset is AudioClip)
                {
                    wav = AudioPreviewHelper.GetWav(asset);
                }
            }
            catch
            {
                wav = null;
            }
        });

        if (generation != previewGeneration)
            return; // a newer selection was made while this one was loading; discard this result

        StatusLabel.Text = "Ready";

        if (dump != null)
        {
            if (dump.Length > MaxPreviewTextLength)
            {
                dump = dump.Substring(0, MaxPreviewTextLength) + "\n... [truncated for preview - export this asset to see the full content]";
            }
            DumpText.Text = dump;
        }
        else if (dumpError != null)
        {
            DumpText.Text = dumpError;
        }

        if (visualError != null)
        {
            await DisplayAlert("Preview Error", visualError, "OK");
            FallbackPreviewImage.IsVisible = true;
            return;
        }

        if (asset is Mesh && meshJson != null)
        {
            MeshPreviewWebView.IsVisible = true;
            WireframeToggleButton.IsVisible = true;
            try
            {
                await ShowMeshInViewerAsync(meshJson);
            }
            catch (Exception ex)
            {
                await DisplayAlert("3D Preview Error", FormatException(ex), "OK");
            }
        }
        else if (asset is Shader shaderAsset)
        {
            MeshPreviewWebView.IsVisible = true;
            WireframeToggleButton.IsVisible = true;
            var descriptor = ShaderPreviewHelper.Build(shaderAsset);
            try
            {
                await ShowShaderPreviewInViewerAsync(descriptor);
            }
            catch (Exception ex)
            {
                await DisplayAlert("3D Preview Error", FormatException(ex), "OK");
            }
        }
        else if (png != null)
        {
            PreviewImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
            var size = ReadPngSize(png);
            imagePixelWidth = size?.Item1 ?? 0;
            imagePixelHeight = size?.Item2 ?? 0;
            PreviewImage.IsVisible = true;
            ResetZoom();
            ZoomControls.IsVisible = true;
        }
        else if (wav != null)
        {
            currentAudioWav = wav;
            AudioClipNameLabel.Text = item?.Text ?? "Audio Clip";
            AudioPlayerPanel.IsVisible = true;
        }
        else
        {
            // No visual representation (AudioClip with an unsupported codec, MonoBehaviour,
            // Animator, GameObject, ...): fall back to the original app's generic placeholder.
            FallbackPreviewImage.IsVisible = true;
        }
    }

    private byte[] currentAudioWav;

    private async void OnAudioPlayClicked(object sender, EventArgs e)
    {
        if (currentAudioWav == null)
            return;
        try
        {
            AudioPlayer.Play(currentAudioWav);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Audio Playback Error", FormatException(ex), "OK");
        }
    }

    private void OnAudioStopClicked(object sender, EventArgs e) => AudioPlayer.Stop();

    private bool wireframeEnabled;

    private async void OnWireframeToggleClicked(object sender, EventArgs e)
    {
        wireframeEnabled = !wireframeEnabled;
        WireframeToggleButton.Text = wireframeEnabled ? "Wireframe: On" : "Wireframe";
        await MeshPreviewWebView.EvaluateJavaScriptAsync(wireframeEnabled ? "setWireframe(true)" : "setWireframe(false)");
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
