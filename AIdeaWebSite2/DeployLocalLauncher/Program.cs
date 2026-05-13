using System.Diagnostics;
using System.Windows.Forms;

static string RootDir() => Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

var root = RootDir();
var cmdPath = Path.Combine(root, "DeployLocal.cmd");

if (!File.Exists(cmdPath))
{
	MessageBox.Show(
		$"未找到 DeployLocal.cmd：\n{cmdPath}\n\n请将 DeployLocal.exe 与 DeployLocal.cmd 放在同一目录（AIdeaWebSite2 根目录，与 Site 文件夹同级）。",
		"DeployLocal",
		MessageBoxButtons.OK,
		MessageBoxIcon.Error);
	return;
}

try
{
	Process.Start(
		new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = $"/c \"\"{cmdPath}\"\"",
			WorkingDirectory = root,
			UseShellExecute = true,
		});
}
catch (Exception ex)
{
	MessageBox.Show(ex.Message, "DeployLocal", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
