namespace ExcelWebServer;


public struct FileAndDate
{
    public byte[] bytes;
    public DateTime created;
};
public class Cache
{
    private readonly ReaderWriterLockSlim _cacheLock;
    private readonly Dictionary<string, FileAndDate> _cache;
    private const int CacheCapacity = 3;

    
    
    public Cache()
    {
        _cacheLock = new ReaderWriterLockSlim();
        _cache = new Dictionary<string, FileAndDate>(CacheCapacity);
    }

    public void AddToCache(string key, byte[] value, int timeout)
    {
        if (!_cacheLock.TryEnterWriteLock(timeout)) return;
        if (_cache.ContainsKey(key))
        {
            throw new Exception("Element is already in cache");
        }
        FileAndDate fileAndDate = new FileAndDate()
        {
            bytes = value,
            created = DateTime.UtcNow
        };
        _cache.Add(key, fileAndDate);
        _cacheLock.ExitWriteLock();
    }

    public void RemoveFromCache(string key)
    {
        _cacheLock.EnterReadLock();
        if (!_cache.Remove(key))
        {
            throw new Exception("Element doesn't exist in cache!");
        }
        _cacheLock.ExitReadLock();
    }

    public byte[] ReadFromCache(string key)
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _cache[key].bytes;
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public bool HasKey(string key)
    {
        if(_cache.ContainsKey(key))
        {
            if(_cache[key].created.AddMinutes(10) >= DateTime.UtcNow)
            {
                return true;
            }
            else
            {
                RemoveFromCache(key); 
            }
        }
        return false;
    }
}