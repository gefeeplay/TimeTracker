using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace TimeTracker.Services;

public static class IconHelper
{
    #region Win32

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;
    private const uint SHGFI_SMALLICON = 0x1;

    #endregion

    /// <summary>
    /// Извлекает иконку процесса и сохраняет её в PNG.
    /// </summary>
    public static string? SaveIconToFile(string exePath, string processName)
    {
        try
        {
            SHGetFileInfo(exePath, 0, out SHFILEINFO shinfo, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_LARGEICON);

            if (shinfo.hIcon == IntPtr.Zero) return null;

            string baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TimeTracker",
                "icons"
            );

            if (!Directory.Exists(baseFolder))
                Directory.CreateDirectory(baseFolder);

            string filePath = Path.Combine(baseFolder, $"{processName}.png");

            // защита от повторной генерации
            if (File.Exists(filePath))
                return filePath;

            // Прямое сохранение через System.Drawing без создания BitmapImage
            using (var icon = Icon.FromHandle(shinfo.hIcon))
            using (var bitmap = icon.ToBitmap())
            {
                bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }

            DestroyIcon(shinfo.hIcon);
            Debug.WriteLine("Иконка успешно сохранена: " + filePath);
            return filePath;
        }
        catch
        {
            return null;
        }
    }
}