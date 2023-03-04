using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlR.Viewer.Helpers;
internal static class FilePickerHelper
{
    private static readonly DevicePlatform[] _supportedPlatforms = 
    {
        DevicePlatform.Android,
        DevicePlatform.WinUI
    };
    public static FilePickerFileType GetSupportedPlatformsFileType(string fileType)
    {
        var typeDictionary = _supportedPlatforms.ToDictionary(
            x => x,
            x => (IEnumerable<string>)new[] { fileType });

        return new FilePickerFileType(typeDictionary);
    }
}
