using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace xasset.editor
{
    public class AutoGroup : BuildTaskJob
    {
        public AutoGroup(BuildTask task) : base(task)
        {
        }

        public override void Run()
        {
            var bundledAssets = _task.bundledAssets;
            var pathWithAssets = new Dictionary<string, GroupAsset>();
            foreach (var bundledAsset in bundledAssets) pathWithAssets[bundledAsset.path] = bundledAsset;

            var dependencyWithReferences = new Dictionary<string, List<GroupAsset>>();
            for (var i = 0; i < bundledAssets.Count; i++)
            {
                var asset = bundledAssets[i];
                if (asset.findReferences)
                {
                    var dependencies = Settings.GetDependencies(asset.path);
                    DisplayProgressBar("分析依赖", asset.path, i, bundledAssets.Count);
                    foreach (var dependency in dependencies)
                    {
                        if (pathWithAssets.ContainsKey(dependency)) continue;

                        if (!dependencyWithReferences.TryGetValue(dependency, out var assets))
                        {
                            assets = new List<GroupAsset>();
                            dependencyWithReferences[dependency] = assets;
                        }

                        assets.Add(asset);
                    }
                }
            }

            if (dependencyWithReferences.Count <= 0) return;
            var auto = ScriptableObject.CreateInstance<Group>();
            auto.name = "Auto";
            var index = 0;
            foreach (var pair in dependencyWithReferences)
            {
                var path = pair.Key;
                var assets = pair.Value;
                DisplayProgressBar("自动分组", path, index, dependencyWithReferences.Count);
                var bundles = new HashSet<string>(assets.ConvertAll(input => input.bundle)).ToList();
                bundles.Sort();
                // 非公共依赖不主动创建自动分组。
                if (assets.Count <= 1) continue;
                if (bundles.Count <= 1) continue;
                if (pathWithAssets.ContainsKey(path)) continue;
                // 这个是符合按需加载的最优策略，尽可能把同时使用的打包到一起。
                var asset = auto.CreateAsset(path, path, true);
                asset.bundle = $"auto_{Utility.ComputeHash(Encoding.UTF8.GetBytes(string.Join("_", bundles)))}{Settings.BundleExtension}";
                bundledAssets.Add(asset);
                pathWithAssets.Add(path, asset);
                index++;
            }
        }
    }
}