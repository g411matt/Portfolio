using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Snippet of an AssetLoader that takes into account dependencies, no specific asset type or load method is defined only that it is asyncronous loading
/// Fail conditions aren't taken into account for the snippet because identifying and handling fails can vary greatly depending on what kind of loading/downloading system is in use
/// Author: Matt Gall
/// </summary>

public enum LoadState 
{
    Loaded,
    Loading,
    Unloading,
    Unloaded,
    Waiting,
}

/// <summary>
/// Data structure for an Asset, 
/// it knows its id, the ids of its dependencies, 
/// the number of internal references to it, and its load state
/// </summary>
public abstract class Asset 
{
    public delegate void AssetLoadEvent();

    /// <summary>
    /// asset id
    /// </summary>
    protected uint _id;
    /// <summary>
    /// asset content
    /// </summary>
    protected object _content = null;
    /// <summary>
    /// current load state of the asset
    /// </summary>
    protected LoadState _loadState = LoadState.Unloaded;
    /// <summary>
    /// dependency id list
    /// </summary>
    protected uint[] _dependencies;

    /// <summary>
    /// reference counter to prevent unloading
    /// </summary>
    private int _references = 0;

    protected event AssetLoadEvent onCompleteEvent;
    protected event AssetLoadEvent onUnloadEvent;

    public uint Id { get { return _id; } }
    public object Content { get { return _content; } }
    public LoadState LoadState { get { return _loadState; } }
    public uint[] Dependencies { get { return _dependencies; } }

    /// <summary>
    /// Simplified handling of external references, assumes that other classes using the loader will only unload if the asset is no longer needed anywhere
    /// </summary>
    public bool HasBeenExternallyLoaded { get; set; }

    public Asset(uint id, uint[] dependencies)
    {
        _id = id;
        _dependencies = dependencies;
    }

    /// <summary>
    /// start the process to load the asset
    /// </summary>
    /// <param name="onComplete"></param>
    public void LoadAsync(AssetLoadEvent onComplete)
    {
        // if the asset is already loaded then insta-complete
        if (_loadState == LoadState.Loaded)
        {
            if (onComplete != null)
            {
                onCompleteEvent += onComplete;
            }
            OnLoadComplete();
        }
        // if the asset is loading/waiting on dependencies then tack on the new complete event and wait
        else if (_loadState == LoadState.Loading || _loadState == LoadState.Waiting)
        {
            if (onComplete != null)
            {
                onCompleteEvent += onComplete;
            }
        }
        // if the asset is unloading then wait for that to finish and call load again
        else if (_loadState == LoadState.Unloading)
        {
            onUnloadEvent += () => LoadAsync(onComplete);
        }
        else
        {
            // check if the dependencies are loaded and call a load if they aren't
            bool ready = true;
            if (_dependencies.Length > 0)
            {
                for (int i = 0; i < _dependencies.Length; i++) 
                {
                    var depLoad = AssetDependencyLoader.GetAsset(_dependencies[i]);
                    if (depLoad != null)
                    {
                        depLoad._references++; // note the dependency is in use by another asset so it doesn't unload
                        // if the dependency is in any state other than loaded then call loadasync to set the complete function
                        if (depLoad.LoadState != LoadState.Loaded)
                        {
                            depLoad.LoadAsync(() => CheckDependencies(onComplete));
                            ready = false;
                        }
                    }
                }
            }
            if (ready)
            {
                _loadState = LoadState.Loading;
                LoadAsyncInternal(onComplete);
            }
            else
            {
                _loadState = LoadState.Waiting;
            }
        }
    }

    /// <summary>
    /// follow up load function once all dependencies are loaded, load the asset
    /// </summary>
    /// <param name="onComplete"></param>
    private void CheckDependencies(AssetLoadEvent onComplete)
    {
        if (_loadState == LoadState.Loaded)
        {
            return;
        }
        bool ready = true;
        if (_dependencies.Length > 0)
        {
            for (int i = 0; i < _dependencies.Length; i++)
            {
                var depLoad = AssetDependencyLoader.GetAsset(_dependencies[i]);
                ready &= depLoad.LoadState == LoadState.Loaded;
            }
        }
        if (ready)
        {
            _loadState = LoadState.Loading;
            LoadAsyncInternal(onComplete);
        }
    }

    /// <summary>
    /// start process to unload the asset including dependencies not in use
    /// </summary>
    /// <param name="onComplete"></param>
    public void UnloadAsync(AssetLoadEvent onComplete)
    {
        if (_references > 0 || HasBeenExternallyLoaded)
        {
            return;
        }
        else
        {
            if (onComplete != null)
            {
                onUnloadEvent += onComplete;
            }
            // dereference and attempt to unload dependencies
            if (_dependencies.Length > 0)
            {
                for (int i = 0; i < _dependencies.Length; i++)
                {
                    var depLoad = AssetDependencyLoader.GetAsset(_dependencies[i]);
                    if (depLoad != null)
                    {
                        depLoad._references--;
                        depLoad.UnloadAsync(null);
                    }
                }
            }
            _loadState = LoadState.Unloading;
            UnloadAsyncInternal(onComplete);
        }
    }

    protected abstract void LoadAsyncInternal(AssetLoadEvent onComplete);
    protected abstract void UnloadAsyncInternal(AssetLoadEvent onComplete);

    protected virtual void OnLoadComplete()
    {
        _loadState = LoadState.Loaded;
        onCompleteEvent?.Invoke();
        onCompleteEvent = null;
    }

    protected virtual void OnUnloadComplete()
    {
        _loadState = LoadState.Unloaded;
        onUnloadEvent?.Invoke();
        onUnloadEvent = null;
    }
}

/// <summary>
/// static class to manage the asset list
/// </summary>
public static class AssetDependencyLoader
{
    private static Dictionary<uint, Asset> _assetDictionary = new Dictionary<uint, Asset>();

    public static void LoadAssetList()
    {
        // assume this fills the _assetDictionary, not important for the snippet
    }

    /// <summary>
    /// will attempt to load the asset with the specified id, the asset will first load any dependencies it needs before loading itself
    /// </summary>
    /// <param name="id"></param>
    /// <param name="onLoaded"></param>
    public static void LoadAssetAsync(uint id, Asset.AssetLoadEvent onLoaded)
    {
        if(_assetDictionary.TryGetValue(id, out Asset toLoad))
        {
            if (toLoad.LoadState == LoadState.Loaded)
            {
                onLoaded?.Invoke();
            }
            else
            {
                toLoad.HasBeenExternallyLoaded = true;
                toLoad.LoadAsync(onLoaded);
            }
        }
    }

    /// <summary>
    /// will attempt to unload the asset and any dependencies that will no longer be in use
    /// </summary>
    /// <param name="id"></param>
    /// <param name="onUnloaded"></param>
    public static void UnloadAssetAsync(uint id, Asset.AssetLoadEvent onUnloaded)
    {
        if (_assetDictionary.TryGetValue(id, out Asset toLoad))
        {
            if (toLoad.LoadState == LoadState.Unloaded)
            {
                onUnloaded?.Invoke();
            }
            else
            {
                toLoad.HasBeenExternallyLoaded = false;
                toLoad.UnloadAsync(onUnloaded);
            }
        }
    }

    public static Asset GetAsset(uint id)
    {
        if (_assetDictionary.TryGetValue(id, out Asset toLoad))
        {
            return toLoad;
        }
        return null;
    }
}
