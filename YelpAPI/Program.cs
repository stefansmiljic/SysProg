using Yelp.Api;
using Yelp.Api.Models;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;

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
    internal class Program
    {
        public static void Main()
        {
            var businessStream = new BusinessStream();

            var observer1 = new BusinessObserver("Observer 1");
            var observer2 = new BusinessObserver("Observer 2");
            var observer3 = new BusinessObserver("Observer 3");

            var filteredStream = businessStream;

            var subscription1 = filteredStream.Subscribe(observer1);
            var subscription2 = filteredStream.Subscribe(observer2);
            var subscription3 = filteredStream.Subscribe(observer3);

            string location;
            string categories;
            float rating;

            do
            {
                Console.WriteLine("Please enter your location.");
                location = Console.ReadLine()!;
            }
            while (string.IsNullOrEmpty(location));

            do
            {
                Console.WriteLine("Please enter desired categories.");
                categories = Console.ReadLine()!;
            }
            while (string.IsNullOrEmpty(categories));

            bool validInput = false;

            do
            {
                Console.WriteLine("Please enter the minimal expected rating.");
                if (float.TryParse(Console.ReadLine(), out rating))
                    validInput = true;
                else Console.WriteLine("Invalid input. Please enter a valid floating-point number.");
            }
            while (!validInput);

            IScheduler scheduler = NewThreadScheduler.Default;

            businessStream.GetBusinesses(location, categories, rating, scheduler);
            Console.ReadLine();

            subscription1.Dispose();
            subscription2.Dispose();
            subscription3.Dispose();
        }
    }
}