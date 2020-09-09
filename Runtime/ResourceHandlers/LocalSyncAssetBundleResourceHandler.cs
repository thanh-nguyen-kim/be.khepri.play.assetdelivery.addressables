﻿using System;
using System.IO;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Khepri.AddressableAssets.ResourceHandlers
{
    public class LocalSyncAssetBundleResourceHandler : IAssetBundleResourceHandler
    {
        public event Action<IAssetBundleResourceHandler, bool, Exception> CompletedEvent;
        
        private AssetBundle assetBundle;
        private AssetBundleRequestOptions options;

        AssetBundleRequestOptions IAssetBundleResourceHandler.Options => options;

        public bool TryBeginOperation(ProvideHandle provideHandle)
        {
            string path = provideHandle.ResourceManager.TransformInternalId(provideHandle.Location);
            if (!File.Exists(path))
            {
                return false;
            }
            options = provideHandle.Location.Data as AssetBundleRequestOptions;
            provideHandle.SetProgressCallback(PercentComplete);
            BeginOperation(path);
            return true;
        }

        private float PercentComplete()
        {
            return 1f;
        }
        
        private void BeginOperation(string path)
        {
            Debug.LogFormat("[{0}.{1}] path={2}", nameof(LocalSyncAssetBundleResourceHandler), nameof(BeginOperation), path);
            assetBundle = AssetBundle.LoadFromFile(path, options?.Crc ?? 0);;
            CompletedEvent?.Invoke(this, assetBundle != null, null);
        }

        public AssetBundle GetAssetBundle()
        {
            return assetBundle;
        }
        
        public void Unload()
        {
            if (assetBundle != null)
            {
                assetBundle.Unload(true);
                assetBundle = null;
            }
        }

    }
}