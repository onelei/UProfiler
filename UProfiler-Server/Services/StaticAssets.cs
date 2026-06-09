namespace UProfiler.Server.Services;

public static class StaticAssets
{
    public const string Version = "9";
    public static string Css(string name) => $"/css/{name}?v={Version}";
    public static string Js(string name) => $"/js/{name}?v={Version}";
    public static string VendorJs(string name) => $"/js/vendor/{name}?v={Version}";
}
