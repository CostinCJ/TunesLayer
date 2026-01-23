using System.Net;
using System.Text;
using System.Text.Json;
using TunesLayer.Core.Models;
using TunesLayer.Core.Services;

namespace TunesLayer.Integrations.OBS;

public class OBSWidgetService : IOBSWidgetService, IDisposable
{
    private readonly IMediaSessionService _mediaService;
    private readonly ISettingsService _settingsService;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private MediaInfo? _currentMedia;
    private bool _isPlaying;
    private bool _disposed;

    public bool IsRunning => _listener?.IsListening ?? false;
    public string WidgetUrl => $"http://localhost:{_settingsService.CurrentSettings.OBSWidgetPort}/widget";

    public OBSWidgetService(IMediaSessionService mediaService, ISettingsService settingsService)
    {
        _mediaService = mediaService;
        _settingsService = settingsService;
    }

    public void Start()
    {
        if (!_settingsService.CurrentSettings.OBSWidgetEnabled)
            return;

        try
        {
            var port = _settingsService.CurrentSettings.OBSWidgetPort;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();

            _cts = new CancellationTokenSource();

            // Subscribe to media changes
            _mediaService.MediaChanged += OnMediaChanged;
            _mediaService.PlaybackStateChanged += OnPlaybackStateChanged;

            // Start listening for requests
            Task.Run(() => ListenAsync(_cts.Token));

            System.Diagnostics.Debug.WriteLine($"OBS Widget server started on port {port}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to start OBS Widget server: {ex.Message}");
        }
    }

    private void OnMediaChanged(object? sender, MediaInfo media)
    {
        _currentMedia = media;
    }

    private void OnPlaybackStateChanged(object? sender, bool isPlaying)
    {
        _isPlaying = isPlaying;
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context), ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Listener error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            string responseContent;
            string contentType;

            switch (request.Url?.AbsolutePath.ToLowerInvariant())
            {
                case "/widget":
                    responseContent = GetWidgetHtml();
                    contentType = "text/html";
                    break;

                case "/api/now-playing":
                    responseContent = GetNowPlayingJson();
                    contentType = "application/json";
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    break;

                case "/api/album-art":
                    if (_currentMedia?.AlbumArtData != null)
                    {
                        response.ContentType = "image/png";
                        await response.OutputStream.WriteAsync(_currentMedia.AlbumArtData);
                        response.Close();
                        return;
                    }
                    response.StatusCode = 404;
                    response.Close();
                    return;

                default:
                    response.StatusCode = 404;
                    responseContent = "Not Found";
                    contentType = "text/plain";
                    break;
            }

            var buffer = Encoding.UTF8.GetBytes(responseContent);
            response.ContentType = contentType;
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Request handler error: {ex.Message}");
        }
        finally
        {
            response.Close();
        }
    }

    private string GetNowPlayingJson()
    {
        var data = new
        {
            isPlaying = _isPlaying,
            title = _currentMedia?.Title ?? "",
            artist = _currentMedia?.Artist ?? "",
            album = _currentMedia?.Album ?? "",
            source = _currentMedia?.SourceApp ?? "",
            albumArtUrl = _currentMedia?.AlbumArtData != null ? "/api/album-art" : null,
            timestamp = DateTime.Now.ToString("o")
        };

        return JsonSerializer.Serialize(data);
    }

    private string GetWidgetHtml()
    {
        var port = _settingsService.CurrentSettings.OBSWidgetPort;
        
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>TunesLayer - Now Playing</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, sans-serif;
            background: transparent;
            overflow: hidden;
        }}
        
        .widget {{
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 10px;
            background: rgba(0, 0, 0, 0.7);
            border-radius: 12px;
            max-width: 350px;
            backdrop-filter: blur(10px);
            animation: slideIn 0.3s ease-out;
        }}
        
        @keyframes slideIn {{
            from {{
                opacity: 0;
                transform: translateX(-20px);
            }}
            to {{
                opacity: 1;
                transform: translateX(0);
            }}
        }}
        
        .album-art {{
            width: 60px;
            height: 60px;
            border-radius: 8px;
            object-fit: cover;
            background: #333;
        }}
        
        .no-art {{
            width: 60px;
            height: 60px;
            border-radius: 8px;
            background: linear-gradient(135deg, #8B5CF6, #6366F1);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 24px;
        }}
        
        .info {{
            flex: 1;
            overflow: hidden;
        }}
        
        .title {{
            color: #fff;
            font-size: 14px;
            font-weight: 600;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }}
        
        .artist {{
            color: #aaa;
            font-size: 12px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            margin-top: 2px;
        }}
        
        .source {{
            color: #8B5CF6;
            font-size: 10px;
            margin-top: 4px;
        }}
        
        .hidden {{
            display: none;
        }}
        
        .playing-indicator {{
            display: flex;
            gap: 2px;
            align-items: flex-end;
            height: 16px;
        }}
        
        .bar {{
            width: 3px;
            background: #8B5CF6;
            border-radius: 2px;
            animation: equalizer 0.5s ease-in-out infinite alternate;
        }}
        
        .bar:nth-child(1) {{ animation-delay: 0s; height: 8px; }}
        .bar:nth-child(2) {{ animation-delay: 0.1s; height: 12px; }}
        .bar:nth-child(3) {{ animation-delay: 0.2s; height: 6px; }}
        .bar:nth-child(4) {{ animation-delay: 0.3s; height: 14px; }}
        
        @keyframes equalizer {{
            from {{ height: 4px; }}
            to {{ height: 16px; }}
        }}
        
        .paused .bar {{
            animation: none;
            height: 4px !important;
        }}
    </style>
</head>
<body>
    <div id=""widget"" class=""widget hidden"">
        <img id=""albumArt"" class=""album-art"" src="""" alt=""Album Art"">
        <div id=""noArt"" class=""no-art hidden"">♪</div>
        <div class=""info"">
            <div id=""title"" class=""title"">No music playing</div>
            <div id=""artist"" class=""artist""></div>
            <div id=""source"" class=""source""></div>
        </div>
        <div id=""indicator"" class=""playing-indicator"">
            <div class=""bar""></div>
            <div class=""bar""></div>
            <div class=""bar""></div>
            <div class=""bar""></div>
        </div>
    </div>
    
    <script>
        const widget = document.getElementById('widget');
        const albumArt = document.getElementById('albumArt');
        const noArt = document.getElementById('noArt');
        const title = document.getElementById('title');
        const artist = document.getElementById('artist');
        const source = document.getElementById('source');
        const indicator = document.getElementById('indicator');
        
        async function updateWidget() {{
            try {{
                const response = await fetch('http://localhost:{port}/api/now-playing');
                const data = await response.json();
                
                if (data.title) {{
                    widget.classList.remove('hidden');
                    title.textContent = data.title;
                    artist.textContent = data.artist;
                    source.textContent = data.source ? `♪ ${{data.source}}` : '';
                    
                    if (data.albumArtUrl) {{
                        albumArt.src = 'http://localhost:{port}' + data.albumArtUrl + '?t=' + Date.now();
                        albumArt.classList.remove('hidden');
                        noArt.classList.add('hidden');
                    }} else {{
                        albumArt.classList.add('hidden');
                        noArt.classList.remove('hidden');
                    }}
                    
                    if (data.isPlaying) {{
                        indicator.classList.remove('paused');
                    }} else {{
                        indicator.classList.add('paused');
                    }}
                }} else {{
                    widget.classList.add('hidden');
                }}
            }} catch (err) {{
                console.error('Failed to fetch now playing:', err);
            }}
        }}
        
        // Update every 2 seconds
        updateWidget();
        setInterval(updateWidget, 2000);
    </script>
</body>
</html>";
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _listener = null;

        _mediaService.MediaChanged -= OnMediaChanged;
        _mediaService.PlaybackStateChanged -= OnPlaybackStateChanged;
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();
        _cts?.Dispose();
        _disposed = true;
    }
}
