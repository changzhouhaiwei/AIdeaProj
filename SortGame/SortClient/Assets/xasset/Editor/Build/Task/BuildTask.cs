using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace xasset.editor
{
    public class BuildTask
    {
        public readonly List<GroupAsset> bundledAssets = new List<GroupAsset>();
        public readonly List<ManifestBundle> bundles = new List<ManifestBundle>();
        public readonly List<string> changes = new List<string>();
        public readonly bool forceRebuild;
        public readonly List<BuildTaskJob> jobs = new List<BuildTaskJob>();
        public readonly string outputPath;
        public readonly List<GroupAsset> rawAssets = new List<GroupAsset>();
        public readonly Stopwatch stopwatch = new Stopwatch();
        public int buildVersion;

        public BuildTask(Build build, int version = -1) : this(build.name)
        {
            forceRebuild = build.forceRebuild;
            buildVersion = version;
            var options = build.options;
            jobs.Add(new CollectAssets(this, build.groups));
            jobs.Add(new CheckAssets(this));
            if (options.autoGroupWithReference) jobs.Add(new AutoGroup(this));

            // forceRebuild must also force Unity's AB pipeline. MonoBehaviour renames keep the
            // same prefab GUID/AssetFileHash, so incremental builds otherwise reuse old type trees.
            var abOptions = options.buildAssetBundleOptions;
            if (forceRebuild)
            {
                abOptions |= UnityEditor.BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }

            jobs.Add(new BuildBundles(this, abOptions));
            jobs.Add(new CreateManifest(this, options.packAllBundlesToBinary));
        }

        public BuildTask(string build)
        {
            Settings.GetDefaultSettings().Initialize();
            name = build;
            outputPath = Settings.PlatformBuildPath;
        }

        public string name { get; }

        public void Run()
        {
            stopwatch.Start();
            foreach (var job in jobs)
            {
                try
                {
                    job.Run();
                }
                catch (Exception e)
                {
                    job.error = e.Message;
                    Debug.LogException(e);
                }

                if (string.IsNullOrEmpty(job.error)) continue;
                break;
            }
            UnityEditor.EditorUtility.ClearProgressBar();
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds / 1000f;
            Debug.LogFormat("Run BuildTask for {0} with {1}s", name, elapsed);
        }


        public string GetBuildPath(string filename)
        {
            return $"{outputPath}/{filename}";
        }

        public void SaveManifest(Manifest manifest)
        {
            var timestamp = DateTime.Now.ToFileTime();
            manifest.name = name.ToLower();
            var filename = $"{manifest.name}.json";
            File.WriteAllText(GetBuildPath(filename), JsonUtility.ToJson(manifest));
            var path = GetBuildPath(filename);
            var hash = Utility.ComputeHash(path);
            var file = $"{manifest.name}_v{manifest.version}_{hash}.json";
            var moveTo = GetBuildPath(file);
            if (File.Exists(moveTo))
            {
                File.Delete(moveTo);
            }
            File.Move(GetBuildPath(filename), GetBuildPath(file));
            changes.Add(file);
            // save version
            SaveVersion(file, timestamp, hash);
            // write record
            SaveRecord(file, timestamp);
        }

        private void SaveRecord(string file, long timestamp)
        {
            var elapsed = stopwatch.ElapsedMilliseconds / 1000f;
            var newSize = 0L;
            foreach (var newFile in changes)
            {
                var info = new FileInfo(GetBuildPath(newFile));
                if (info.Exists) newSize += info.Length;
            }

            var record = new BuildRecord
            {
                file = file,
                build = name,
                changes = changes,
                size = newSize,
                timestamp = timestamp,
                date = DateTime.Now.ToString("yyyy年MM月dd日-HH时mm分ss秒"),
                platform = Settings.GetPlatformName(),
                elapsed = elapsed
            };
            var records = BuildRecords.GetRecords();
            records.data.Insert(0, record);
            records.Save();
        }

        private void SaveVersion(string file, long timestamp, string hash)
        {
            var buildVersions = BuildVersions.Load(GetBuildPath(Versions.Filename));
            var info = new FileInfo(GetBuildPath(file));
            buildVersions.Set(name, file, info.Length, timestamp, hash);
            buildVersions.encryptionEnabled = Settings.EncryptionEnabled;

            var savePath = GetBuildPath(Versions.Filename);
            File.WriteAllText(savePath, JsonUtility.ToJson(buildVersions));
            var newPath = savePath.Replace(Versions.Filename, $"versions_{Utility.ComputeHash(savePath)}.json");
            File.Copy(savePath, newPath);

            changes.Add(Path.GetFileName(newPath));
        }
    }
}