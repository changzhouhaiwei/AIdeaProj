using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

class CustomBuildProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 10;

    public void OnPostprocessBuild(BuildReport report)
    {
        Debug.Log("打包完成 输出路径 " + report.summary.outputPath);
    }
}