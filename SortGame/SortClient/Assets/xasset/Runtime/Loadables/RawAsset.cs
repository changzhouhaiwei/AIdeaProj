using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace xasset
{
    public sealed class RawAsset : Loadable
    {
        public static readonly Dictionary<string, RawAsset> Cache = new Dictionary<string, RawAsset>();
        public static Func<ManifestBundle, IRawAssetHandler> customHandler;
        private IRawAssetHandler _handler;
        private ManifestBundle _info;
        public Action<RawAsset> completed;
        public string savePath { get; private set; }
        public ulong offset => _handler.offset;
        public long size => _info.size;

        public Task<RawAsset> Task
        {
            get
            {
                var tcs = new TaskCompletionSource<RawAsset>();
                completed += operation => { tcs.SetResult(this); };
                return tcs.Task;
            }
        }

        public override void LoadImmediate()
        {
            if (isDone) return;
            _handler.LoadImmediate();
            Finish(_handler.error);
        }

        protected override void OnLoad()
        {
            if (Versions.SimulationMode)
            {
                savePath = pathOrURL;
                var fileInfo = new FileInfo(savePath);
                if (!fileInfo.Exists)
                {
                    Finish("File not found.");
                    return;
                }

                // 防止报错。
                _info = new ManifestBundle { size = fileInfo.Length };
                _handler = new RawAssetHandler { savePath = savePath};
                Finish();
                return;
            }

            _info = Versions.GetBundle(pathOrURL);
            if (_info == null)
            {
                Finish("File not found.");
                return;
            }

            savePath = Downloader.GetDownloadDataPath(_info.nameWithAppendHash);
            var file = new FileInfo(savePath);
            if (file.Exists)
            {
                if (file.Length == _info.size)
                {
                    Finish();
                    return;
                }

                File.Delete(savePath);
            }

            if (customHandler != null) _handler = customHandler(_info);

            if (_handler == null)
            {
                var url = Versions.IsDownloaded(_info) ? PathManager.GetPlayerDataURL(_info.nameWithAppendHash) : Downloader.GetDownloadDataPath(_info.nameWithAppendHash);
                _handler = new RawAssetHandler {url = url, savePath = savePath};
            }

            _handler.Init();
            status = LoadableStatus.Loading;
        }

        protected override void OnUnload()
        {
            _handler.Dispose();
            Cache.Remove(pathOrURL);
        }

        protected override void OnComplete()
        {
            if (completed == null) return;

            var saved = completed;
            completed?.Invoke(this);
            completed -= saved;
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading) return;
            UpdateLoading();
        }

        protected override void OnUnused()
        {
            completed = null;
        }

        private void UpdateLoading()
        {
            _handler.Update();

            if (!_handler.isDone) return;

            if (!string.IsNullOrEmpty(_handler.error))
            {
                Finish(_handler.error);
                return;
            }

            Finish();
        }

        public static RawAsset LoadAsync(string filename)
        {
            return LoadInternal(filename);
        }

        public static RawAsset Load(string filename)
        {
            return LoadInternal(filename, true);
        }

        private static RawAsset LoadInternal(string filename, bool mustCompleteOnNextFrame = false)
        {
            PathManager.GetActualPath(ref filename);
            if (!Versions.Contains(filename)) throw new FileLoadException(filename);
            if (!Cache.TryGetValue(filename, out var asset))
            {
                asset = new RawAsset
                {
                    pathOrURL = filename
                };
                Cache.Add(filename, asset);
            }

            asset.Load();
            if (mustCompleteOnNextFrame) asset.LoadImmediate();
            return asset;
        }

        public static string GetSavePath(string filename)
        {
            var bundle = Versions.GetBundle(filename);
            return bundle == null ? null : Downloader.GetDownloadDataPath(bundle.nameWithAppendHash);
        }
    }

    public interface IRawAssetHandler
    {
        ulong offset { get; set; }
        string savePath { get; set; }
        string error { get; set; }
        bool isDone { get; }
        void Init();
        void LoadImmediate();
        void Dispose();
        void Update();
    }

    public class RawAssetHandler : IRawAssetHandler
    {
        private UnityWebRequest _request;
        public string url;

        public string error { get; set; }
        public bool isDone { get; protected set; }
        public ulong offset { get; set; } = 0;
        public string savePath { get; set; }

        public void Init()
        {
            _request = UnityWebRequest.Get(url);
            _request.downloadHandler = new DownloadHandlerFile(savePath);
            _request.SendWebRequest();
        }

        public void LoadImmediate()
        {
            while (!_request.isDone)
            {
            }
        }

        public void Dispose()
        {
            if (_request == null) return;
            _request.Dispose();
            _request = null;
        }

        public void Update()
        {
            if (_request == null)
            {
                error = "_request == null";
                isDone = true;
            }
            else
            {
                isDone = _request.isDone;
            }
        }
    }
}