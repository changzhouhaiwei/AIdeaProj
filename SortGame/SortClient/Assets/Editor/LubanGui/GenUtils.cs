using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Luban.Editor
{
    internal static class GenUtils
    {
        internal static readonly string _DOTNET =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        public static void Gen(string arguments, string before, string after)
        {
            Debug.Log(arguments);

            IBeforeGen before_gen = CreateLifeCycle<IBeforeGen>(before);
            IAfterGen after_gen = CreateLifeCycle<IAfterGen>(after);

            before_gen?.Process();

            Process process;
            try
            {
                process = _Run(
                    _DOTNET,
                    arguments,
                    ".",
                    true
                );
            }
            catch (Exception e)
            {
                Debug.LogError("[Luban] 启动生成进程失败: " + e.Message);
                return;
            }

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stdout.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stderr.AppendLine(e.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (stdout.Length > 0)
                Debug.Log(stdout.ToString());
            if (stderr.Length > 0)
                Debug.Log(stderr.ToString());
            if (process.ExitCode != 0)
            {
                Debug.LogError("Error  生成出现错误 错误码" + process.ExitCode);
                Debug.LogError("[Luban] 生成失败，已跳过语言包导出与字体收集。");
                return;
            }

            after_gen?.Process();

            // 无论 after_gen 是否配了 I2 钩子，配表生成后都导出一次语言包。
            if (!(after_gen is LubanAfterGenI2Localization))
            {
                I2LocalizationOneClickImporter.ExportSheetAndImportReplace();
            }

            AssetDatabase.Refresh();
        }

        static T CreateLifeCycle<T>(string typeName) where T : class
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            var type = ResolveLifeCycleType(typeName);
            if (type == null)
            {
                Debug.LogError($"[Luban] 找不到生命周期类型: {typeName}");
                return null;
            }

            var instance = Activator.CreateInstance(type) as T;
            if (instance == null)
            {
                Debug.LogError($"[Luban] 无法创建生命周期实例: {typeName} -> {typeof(T).Name}");
            }

            return instance;
        }

        static Type ResolveLifeCycleType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            type = Type.GetType($"{typeName}, Luban.Editor");
            if (type != null)
            {
                return type;
            }

            foreach (var t in TypeCache.GetTypesDerivedFrom<IBeforeGen>())
            {
                if (t.FullName == typeName || t.Name == typeName)
                {
                    return t;
                }
            }

            foreach (var t in TypeCache.GetTypesDerivedFrom<IAfterGen>())
            {
                if (t.FullName == typeName || t.Name == typeName)
                {
                    return t;
                }
            }

            return null;
        }

        private static Process _Run(string exe,
                                    string arguments,
                                    string working_dir = ".",
                                    bool wait_exit = false)
        {
            try
            {
                bool redirect_standard_output = true;
                bool redirect_standard_error = true;
                bool use_shell_execute = false;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    redirect_standard_output = false;
                    redirect_standard_error = false;
                    use_shell_execute = true;
                }

                if (wait_exit)
                {
                    redirect_standard_output = true;
                    redirect_standard_error = true;
                    use_shell_execute = false;
                }

                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = use_shell_execute,
                    WorkingDirectory = working_dir,
                    RedirectStandardOutput = redirect_standard_output,
                    RedirectStandardError = redirect_standard_error,
                };

                Process process = Process.Start(info);
                if (process == null)
                    throw new InvalidOperationException("Process.Start 返回空");

                return process;
            }
            catch (Exception e)
            {
                throw new Exception($"dir: {Path.GetFullPath(working_dir)}, command: {exe} {arguments}", e);
            }
        }

    }
}