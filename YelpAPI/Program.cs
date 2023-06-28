using Yelp.Api;
using Yelp.Api.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Net;
using System.Text;

namespace YelpAPI
{
    public class Business
    {
        public string? Name { get; set; }
        public string? Price { get; set; }
        public float Rating { get; set; }
    }

    public class BusinessObserver : IObserver<Business>
    {
        private readonly string name;
        public BusinessObserver(string name)
        {
            this.name = name;
        }
        public void OnNext(Business business)
        {
            if(business.Price!=null)
                Console.WriteLine($"{name}: {business.Name} with price of: {business.Price}");
            else return;
        }
        public void OnError(Exception e)
        {
            Console.WriteLine($"{name}: Error happened: {e.Message}");
        }
        public void OnCompleted()
        {
            Console.WriteLine($"{name}: All businesses returned successfully!");
        }
    }

    public class BusinessStream: IObservable<Business>
    {
        private readonly Subject<Business> businessSubject;
        public BusinessStream()
        {
            businessSubject = new Subject<Business>();
        }
        public void GetBusinesses(string location, string categories, float rating, IScheduler scheduler)
        {
            string apiKey = "KvmoCqAgp5sh4Z7albmGxJy30DbYbYoOHnk5xmVp6zeVVqKFBa1bEIvAn3r3SPI0hWtYmxfqTLXRvJRUVVTs1WaXQ-LjC2AQ1EYFLGKBvP_B6DRLGtfDoh9qXFWQZHYx";
            var client = new Client(apiKey);
            Observable.Start( async () =>
            {
                try
                {
                    var request = new SearchRequest
                    {
                        OpenNow = true,
                        Location = location,
                        Categories = categories
                    };
                    float ratingSum = 0;
                    float averageRating;
                    var searchResults = await client.SearchBusinessesAllAsync(request);
                    var businesses = searchResults.Businesses.Where(p => p.Rating > rating).ToList();
                    businesses.Sort((p, q) =>
                    {
                        if (p.Price == null && q.Price == null)
                            return 0;
                        if (p.Price == null)
                            return -1;
                        if (q.Price == null)
                            return 1;
                        return p.Price.Length.CompareTo(q.Price.Length);
                    });
                    foreach (var business in businesses)
                    {
                        ratingSum += business.Rating;
                        var newBusiness = new Business
                        {
                            Name = business.Name,
                            Price = business.Price,
                            Rating = business.Rating
                        };
                        businessSubject.OnNext(newBusiness);
                    }
                    businessSubject.OnCompleted();
                    averageRating = ratingSum / (businesses.Count);
                    Console.WriteLine("Average rating is: " + averageRating);
                }
                catch(Exception ex)
                {
                    businessSubject.OnError(ex);
                }
                
            }, scheduler);
        }
        public IDisposable Subscribe(IObserver<Business> observer)
        {
            return businessSubject.Subscribe(observer);
        }
    }

    public class HttpServer
    {
        private readonly string url;
        private BusinessStream? businessStream;
        private IDisposable? subscription1;
        private IDisposable? subscription2;
        private IDisposable? subscription3;

        public HttpServer(string url)
        {
            this.url = url;
        }

        public void Start()
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(url);

            listener.Start();
            Console.WriteLine("Server started. Listening for incoming requests...");

            while (true)
            {
                var context = listener.GetContext();
                Task.Run(() => HandleRequest(context));
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            byte[] buffer;

            if (request.HttpMethod == "GET")
            {
                string location = request.QueryString["location"]!;
                string categories = request.QueryString["categories"]!;
                float rating;

                bool validInput = false;
                if (float.TryParse(request.QueryString["rating"], out rating))
                        validInput = true;
                if (String.IsNullOrEmpty(location) || String.IsNullOrEmpty(categories) || !validInput)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    buffer = Encoding.UTF8.GetBytes("Bad request!");
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else
                {

                    IScheduler scheduler = NewThreadScheduler.Default;

                    businessStream = new BusinessStream();
                    var observer1 = new BusinessObserver("Observer 1");
                    var observer2 = new BusinessObserver("Observer 2");
                    var observer3 = new BusinessObserver("Observer 3");

                    var filteredStream = businessStream;

                    subscription1 = filteredStream.Subscribe(observer1);
                    subscription2 = filteredStream.Subscribe(observer2);
                    subscription3 = filteredStream.Subscribe(observer3);

                    businessStream.GetBusinesses(location, categories, rating, scheduler);

                    response.StatusCode = (int)HttpStatusCode.OK;
                    buffer = Encoding.UTF8.GetBytes("Request received. Processing businesses...");
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.OutputStream.Close();
            }
        }

        public void Stop()
        {
            subscription1!.Dispose();
            subscription2!.Dispose();
            subscription3!.Dispose();
        }
    }


    internal class Program
    {
        public static void Main()
        {
            HttpServer server;
            string url = "http://localhost:8080/";
            server = new HttpServer(url);
            server.Start();
        }
    }
}