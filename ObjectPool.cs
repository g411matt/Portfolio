using System.Collections.Generic;

/// <summary>
/// abtract class for an object pool of reference types
/// Stores objects in a deactivated state as determined by subclasses until needed 
/// and dynamically increases pool size as needed
/// Author: Matt Gall
/// </summary>
public abstract class ObjectPool<T> where T : class
{
    /// <summary>
    /// template to clone pool objects from
    /// </summary>
    private T _templateObject;

    /// <summary>
    /// queue of objects available for use
    /// </summary>
    private Queue<T> _objectPool;

    /// <summary>
    /// list of pooled objects in active use
    /// </summary>
    private List<T> _activePool;

    protected T TemplateObject { get { return _templateObject; } }

    /// <summary>
    ///  when creating the pool generate an initial number of objects in the pool determined by subclasses
    /// </summary>
    public ObjectPool(T template)
    {
        _templateObject = template;
        _objectPool = new Queue<T>();
        _activePool = new List<T>();

        for (int i = 0; i < GetStartingPoolSize(); i++)
        {
            _objectPool.Enqueue(CloneTemplate());
        }
    }

    /// <summary>
    /// requests an object from the pool, the pool will expand as needed to its limit
    /// </summary>
    public T RequestPoolObj()
    {
        T obj = null;
        // dequeue next object if available
        if (_objectPool.Count > 0)
        {
            obj = _objectPool.Dequeue();
        }
        // expand the pool if needed and able
        if (obj == null && _activePool.Count < GetMaxPoolSize())
        {
            obj = CloneTemplate();
        }
        // activate the object and return it, will be null if the pool ran out of space
        if (obj != null)
        {
            _activePool.Add(obj);
            ActivateObject(obj);
        }
        return obj;
    }

    /// <summary>
    /// returns the object to the pool while deactivating it
    /// </summary>
    public void ReturnPoolObj(T obj)
    {
        if (obj != null && _activePool.Contains(obj))
        {
            _activePool.Remove(obj);
            _objectPool.Enqueue(obj);
            DeactivateObject(obj);
        }
    }

    // content for subclasses to implement

    protected virtual int GetStartingPoolSize()
    {
        return 10;
    }

    protected virtual int GetMaxPoolSize()
    {
        return 20;
    }
    /// <summary>
    /// CloneTemplate should return a deactivated object
    /// </summary>
    protected abstract T CloneTemplate();

    protected abstract void ActivateObject(T obj);
    protected abstract void DeactivateObject(T obj);
}
