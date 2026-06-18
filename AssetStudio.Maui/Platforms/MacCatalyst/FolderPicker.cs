using System.Linq;
using System.Threading.Tasks;
using UIKit;
using UniformTypeIdentifiers;

namespace AssetStudio.Maui;

public static class FolderPicker
{
    public static Task<string> PickFolderAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        var picker = new UIDocumentPickerViewController(new[] { UTTypes.Folder });
        picker.AllowsMultipleSelection = false;
        picker.DidPickDocumentAtUrls += (s, e) =>
        {
            var url = e.Urls?.FirstOrDefault();
            tcs.TrySetResult(url?.Path);
        };
        picker.WasCancelled += (s, e) => tcs.TrySetResult(null);

        var topController = GetTopViewController();
        topController?.PresentViewController(picker, true, null);

        return tcs.Task;
    }

    private static UIViewController GetTopViewController()
    {
        var window = UIApplication.SharedApplication.Windows.FirstOrDefault(w => w.IsKeyWindow);
        var controller = window?.RootViewController;
        while (controller?.PresentedViewController != null)
        {
            controller = controller.PresentedViewController;
        }
        return controller;
    }
}
