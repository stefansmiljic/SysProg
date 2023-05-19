using ExcelWebServer;

HttpServer server = new HttpServer();
server.Start();
Console.WriteLine("Press Enter to stop the server...");
while (Console.ReadKey().Key != ConsoleKey.Enter) { }
server.Stop();
Console.WriteLine("Server stopped!");
