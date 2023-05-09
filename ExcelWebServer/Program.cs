using ExcelWebServer;

HttpServer server = new HttpServer();
server.Start();
Console.WriteLine("Press any key to stop the server...");
Console.ReadKey();
server.Stop();
Console.WriteLine("Server stopped!");