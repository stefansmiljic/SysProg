using System.Net;
using Aspose.Cells;

namespace ExcelWebServer;

public class HttpServer
{
    private readonly HttpListener _listener;
    private readonly Cache _cache;
    private bool _running;
    private readonly Thread _listenerThread;

    public HttpServer(string url = "http://localhost:5050/")
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(url);
        _cache = new Cache();
        _running = false;
        _listenerThread = new Thread(Loop);
    }
    private static void SendResponse(HttpListenerContext context, byte[] responseBody, string contentType, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var logString =
            $"REQUEST:\n{context.Request.HttpMethod} {context.Request.RawUrl} HTTP/{context.Request.ProtocolVersion}\n" +
            $"Host: {context.Request.UserHostName}\nUser-agent: {context.Request.UserAgent}\n-------------------\n" +
            $"RESPONSE:\nStatus: {statusCode}\nDate: {DateTime.Now}\nContent-Type: {contentType}" +
            $"\nContent-Length: {responseBody.Length}\n";
        context.Response.ContentType = contentType;
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentLength64 = responseBody.Length;
        using (Stream outputStream = context.Response.OutputStream)
        {
            outputStream.Write(responseBody, 0, responseBody.Length);
        }
        Console.WriteLine(logString);
    }

    private void AcceptConnection(HttpListenerContext context)
    {
        if (context.Request.HttpMethod != "GET")
        {
            SendResponse(context, "Method not allowed!"u8.ToArray(), "text/plain",
                HttpStatusCode.MethodNotAllowed);
            return;
        }
        string filename = context.Request.Url!.AbsolutePath.TrimStart('/');
        string value;
        if (_cache.HasKey(filename))
        {
            value = _cache.ReadFromCache(filename);
        }
        else
        {
            if (File.Exists(filename))
            {
                var loadOptions = new LoadOptions(LoadFormat.Csv);
                var workbook = new Workbook(filename, loadOptions);
                workbook.Save($"{filename}.xlsx", SaveFormat.Xlsx);
                value = $"{filename}.xlsx";
                _cache.AddToCache(filename, value, 1000);
            }
            else
            {
                SendResponse(context, "Error 404: File not found!"u8.ToArray(), "text/plain", HttpStatusCode.BadRequest);
                return;
            }
        }

        try
        {
            Console.WriteLine($"Downloading file {value}");
            SendResponse(context, File.ReadAllBytes(value), "application/vnd.ms-excel");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    public void Start()
    {
        _listener.Start();
        _running = true;
        Console.WriteLine("Server started!");
        _listenerThread.Start();
    }
    private void Loop()
    {
        while (_running)
        { 
            try
            {
                var context = _listener.GetContext();
                if (_running) ThreadPool.QueueUserWorkItem(state => { AcceptConnection(context); });            
            }
            catch(HttpListenerException) 
            {
                Console.WriteLine("Server stopped listening!");
            }
        }
    }
    public void Stop()
    {
        _running = false;
        _listener.Stop();
        _listenerThread.Join(); 
    }
}