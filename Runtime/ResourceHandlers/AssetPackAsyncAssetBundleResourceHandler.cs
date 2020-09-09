﻿using System;
using System.IO;
using Google.Play.AssetDelivery;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Khepri.AddressableAssets.ResourceHandlers
{
    public class AssetPackAsyncAssetBundleResourceHandler : IAssetBundleResourceHandler
    {
        public event Action<IAssetBundleResourceHandler, bool, Exception> CompletedEvent;
        
        private PlayAssetPackRequest playAssetPackRequest;
        private AsyncOperation requestOperation;
        private AssetBundle assetBundle;
        private AssetBundleRequestOptions options;

        AssetBundleRequestOptions IAssetBundleResourceHandler.Options => options;

        public bool TryBeginOperation(ProvideHandle provideHandle)
        {
            string path = provideHandle.ResourceManager.TransformInternalId(provideHandle.Location);
            if (!path.StartsWith(Addressables.RuntimePath) || !path.EndsWith(".bundle"))
            {
                return false;
            }
            string assetPackName = Path.GetFileNameWithoutExtension(path);
            if (!AddressablesPlayAssetDelivery.IsPack(assetPackName))
            {
                return false;
            }
            options = provideHandle.Location.Data as AssetBundleRequestOptions;
            provideHandle.SetProgressCallback(PercentComplete);
            BeginOperation(assetPackName);
            return true;
        }

        private float PercentComplete()
        {
            return ((playAssetPackRequest?.DownloadProgress ?? .0f) + (requestOperation?.progress ?? .0f)) * .5f;
        }
        
        private void BeginOperation(string assetPackName)
        {
            Debug.LogFormat("[{0}.{1}] assetPackName={2}", nameof(AssetPackAsyncAssetBundleResourceHandler), nameof(BeginOperation), assetPackName);
            playAssetPackRequest = PlayAssetDelivery.RetrieveAssetPackAsync(assetPackName);
            playAssetPackRequest.Completed += request => OnPlayAssetPackRequestCompleted(assetPackName, request);
        }

        private void OnPlayAssetPackRequestCompleted(string assetPackName, PlayAssetPackRequest request)
        {
            if (request.Error != AssetDeliveryErrorCode.NoError)
            {
                CompletedEvent?.Invoke(this, false, new Exception($"Error downloading error pack: {request.Error}"));
                return;
            }
            if (request.Status != AssetDeliveryStatus.Available)
            {
                CompletedEvent?.Invoke(this, false, new Exception($"Error downloading status: {request.Status}"));
                return;
            }
            var assetLocation = request.GetAssetLocation(assetPackName);
            requestOperation = AssetBundle.LoadFromFileAsync(assetLocation.Path, /* crc= */ 0, assetLocation.Offset);
            requestOperation.completed += LocalRequestOperationCompleted;
        }
        
        private void LocalRequestOperationCompleted(AsyncOperation op)
        {
            assetBundle = (op as AssetBundleCreateRequest)?.assetBundle;
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
            if (playAssetPackRequest != null)
            {
                playAssetPackRequest.AttemptCancel();
                playAssetPackRequest = null;
            }
            requestOperation = null;
        }
    }
}