namespace ExcelWebServer;

public class Cache
{
    private readonly ReaderWriterLockSlim _cacheLock;
    private readonly Dictionary<string, string> _cache;
    private const int CacheCapacity = 1024;
    
    public Cache()
    {
        _cacheLock = new ReaderWriterLockSlim();
        _cache = new Dictionary<string, string>(CacheCapacity);
    }

    public void AddToCache(string key, string value, int timeout)
    {
        if (!_cacheLock.TryEnterWriteLock(timeout)) return;
        if (_cache.ContainsKey(key))
        {
            throw new Exception("Element is already in cache");
        }

        _cache.Add(key, value);
        _cacheLock.ExitWriteLock();
    }

    public void RemoveFromCache(string key) // unused
    {
        _cacheLock.EnterReadLock();
        if (!_cache.Remove(key))
        {
            throw new Exception("Element is not in cache");
        }

        _cacheLock.ExitReadLock();
    }

    public string ReadFromCache(string key)
    {
        _cacheLock.EnterReadLock();
        try
        {
            return _cache[key];
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }
    }

    public bool HasKey(string key)
    {
        return _cache.ContainsKey(key);
    }
}