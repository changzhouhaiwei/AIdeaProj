using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

public class Proto2CS : MonoBehaviour
{
    [MenuItem("游戏工具/Proto2CS", priority = 10)]
    public static void AllProto2CS()
    {
        // 指定包含多个.proto文件的目录
        string protoDir = string.Empty;
        switch (Environment.MachineName)
        {
            case "DESKTOP-VH4CVD8":
                // zhuhongjin
                protoDir = "F:/Workspace/HeroHehe/HeroHeheProto";
                break;
            case "DESKTOP-JG5BBPR":
                break;
            case "DESKTOP-AB9VPHA":
                protoDir = "G:/HeheheProto";
                break;
            case "DESKTOP-6DKTO9Q":
                // 姜海良
                protoDir = "D:/Hehehe/Proto";
                break;
        }

        string rootDir = Environment.CurrentDirectory;
        string protoc;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            protoc = Path.Combine(rootDir, "Tools/protobuf/protoc-3.20.3-win64/bin/protoc.exe");
        }
        else
        {
            protoc = Path.Combine(rootDir, "Tools/protobuf/protoc-3.20.3-win64/bin/protoc");
        }

        string hotfixMessageCodePath = Path.Combine(rootDir, "Assets", "Scripts", "HotFix", "GameProto/");
        if (Directory.Exists(hotfixMessageCodePath))
        {
            Directory.Delete(hotfixMessageCodePath, true);
        }
        Directory.CreateDirectory(hotfixMessageCodePath);

        StringBuilder argument2 = new StringBuilder($"--csharp_out=\"{hotfixMessageCodePath}\" --proto_path=\"{protoDir}\"");
        string[] protoFiles = Directory.GetFiles(protoDir, "*.proto");
        foreach (string protoFile in protoFiles)
        {
            if (Path.GetFileNameWithoutExtension(protoFile).Equals("DB"))
            {
                continue;
            }
            argument2 = argument2.Append($" {protoFile}");
            Run(protoc, argument2.ToString(), waitExit: true);
        }

        UnityEngine.Debug.Log("proto2cs succeed!");

        AssetDatabase.Refresh();
    }

    private static Process Run(string exe, string arguments, string workingDirectory = ".", bool waitExit = false)
    {
        try
        {
            bool redirectStandardOutput = true;
            bool redirectStandardError = true;
            bool useShellExecute = false;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                redirectStandardOutput = false;
                redirectStandardError = false;
                useShellExecute = true;
            }

            if (waitExit)
            {
                redirectStandardOutput = true;
                redirectStandardError = true;
                useShellExecute = false;
            }

            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = useShellExecute,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = redirectStandardOutput,
                RedirectStandardError = redirectStandardError,
            };

            Process process = Process.Start(info);

            if (waitExit)
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"{process.StandardOutput.ReadToEnd()} {process.StandardError.ReadToEnd()}");
                }
            }

            return process;
        }
        catch (Exception e)
        {
            throw new Exception($"dir: {Path.GetFullPath(workingDirectory)}, command: {exe} {arguments}", e);
        }
    }
}