#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FishTilePlay
{
    /// <summary>
    /// 预跑结束后用本地 SES SMTP 配置发邮件。密码只从配置文件读取，不入库。
    /// </summary>
    public static class LevelDifficultyMail
    {
        public static string DefaultConfigPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ses-support-sofishgame.txt"));

        static readonly string[] Recipients =
        {
            "chengbo@sofish.com",
            "xieyasi@sofish.com",
            "wanghaiwei@sofish.com"
        };

        public static string ConfigPath
        {
            get
            {
                string pref = UnityEditor.EditorPrefs.GetString("LevelPreRun.Mail.ConfigPath", "");
                return string.IsNullOrWhiteSpace(pref) ? DefaultConfigPath : pref.Trim();
            }
        }

        public static string SendLevelResults(string outDir, string summary, int levelFrom, int levelTo)
        {
            return SendLevelResults(outDir, summary, levelFrom, levelTo, ConfigPath);
        }

        public static string SendLevelResults(
            string outDir, string summary, int levelFrom, int levelTo, string configPath)
        {
            string subject = levelFrom == levelTo
                ? $"关卡预跑完成 关卡{levelFrom}"
                : $"关卡预跑完成 关卡{levelFrom}-{levelTo}";
            var body = new StringBuilder();
            body.AppendLine(subject);
            body.AppendLine();
            if (!string.IsNullOrEmpty(outDir))
                body.AppendLine("输出目录：" + outDir);
            body.AppendLine();
            body.AppendLine(summary ?? "");
            return SendReport(subject, body.ToString(), outDir, configPath, requireData: true);
        }

        public static string SendStuckReport(
            string outDir,
            string summary,
            int levelFrom,
            int levelTo,
            string reason,
            string configPath)
        {
            string range = levelFrom == levelTo ? $"关卡{levelFrom}" : $"关卡{levelFrom}-{levelTo}";
            string subject = $"关卡预跑卡住 {range}";
            var body = new StringBuilder();
            body.AppendLine(subject);
            body.AppendLine();
            body.AppendLine("原因：" + (reason ?? "未知"));
            if (!string.IsNullOrEmpty(outDir))
                body.AppendLine("输出目录：" + outDir);
            body.AppendLine();
            body.AppendLine("已跑完数据见附件 runs.csv / summary.csv，报错见 error.log。");
            body.AppendLine();
            body.AppendLine(summary ?? "");
            return SendReport(subject, body.ToString(), outDir, configPath, requireData: false);
        }

        static string SendReport(
            string subject, string body, string outDir, string configPath, bool requireData)
        {
            if (!TryLoadConfig(configPath, out SmtpConfig cfg, out string error))
                return error;

            var attachments = CollectReportAttachments(outDir);
            if (requireData && !HasCsv(attachments))
                return "邮件未发送：输出目录没有 csv";
            if (attachments.Count == 0)
                return "邮件未发送：没有可附带的结果或日志";

            Exception last = null;
            int[] ports = cfg.Port == 587 ? new[] { 587 } : new[] { 587, cfg.Port };
            for (int i = 0; i < ports.Length; i++)
            {
                try
                {
                    Send(cfg, ports[i], Recipients, subject, body, attachments);
                    return $"已发邮件给 {string.Join("、", Recipients)}";
                }
                catch (Exception e)
                {
                    last = e;
                }
            }

            return "邮件发送失败：" + Unwrap(last);
        }

        static bool HasCsv(List<string> files)
        {
            for (int i = 0; i < files.Count; i++)
            {
                if (files[i].EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static List<string> CollectReportAttachments(string outDir)
        {
            var files = new List<string>();
            if (string.IsNullOrEmpty(outDir) || !Directory.Exists(outDir))
                return files;

            AddExisting(files, Directory.GetFiles(outDir, "*.csv"));
            AddExisting(files, Directory.GetFiles(outDir, "*.log"));
            string job = Path.Combine(outDir, "job.json");
            if (File.Exists(job))
                files.Add(job);
            files.Sort(StringComparer.OrdinalIgnoreCase);
            return files;
        }

        static void AddExisting(List<string> files, string[] found)
        {
            if (found == null)
                return;
            for (int i = 0; i < found.Length; i++)
            {
                if (!string.IsNullOrEmpty(found[i]) && File.Exists(found[i]) && !files.Contains(found[i]))
                    files.Add(found[i]);
            }
        }

        static void Send(
            SmtpConfig cfg,
            int port,
            string[] to,
            string subject,
            string body,
            List<string> attachments)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(cfg.From);
                for (int i = 0; i < to.Length; i++)
                    message.To.Add(to[i]);
                message.Subject = subject;
                message.Body = body;
                message.SubjectEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                for (int i = 0; i < attachments.Count; i++)
                    message.Attachments.Add(new Attachment(attachments[i]));

                using (var client = new SmtpClient(cfg.Host, port))
                {
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(cfg.User, cfg.Password);
                    client.Timeout = 45000;
                    client.Send(message);
                }
            }
        }

        static bool TryLoadConfig(string path, out SmtpConfig cfg, out string error)
        {
            cfg = default;
            error = "";
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                error = "邮件未发送：找不到 SES 配置 " + path;
                return false;
            }

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            string from = "";
            string user = "";
            string password = "";
            string host = "";
            int port = 465;
            string pending = "";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (string.IsNullOrEmpty(from))
                {
                    Match mail = Regex.Match(line, @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}");
                    if (mail.Success)
                        from = mail.Value;
                }

                if (!string.IsNullOrEmpty(pending))
                {
                    if (pending == "user")
                        user = line;
                    else if (pending == "password")
                        password = line;
                    pending = "";
                    continue;
                }

                if (StartsWithLabel(line, "SMTP 用户名") || StartsWithLabel(line, "SMTP Username"))
                {
                    string value = ValueAfterLabel(line);
                    if (string.IsNullOrEmpty(value))
                        pending = "user";
                    else
                        user = value;
                    continue;
                }

                if (StartsWithLabel(line, "SMTP 用户密码") || StartsWithLabel(line, "SMTP Password"))
                {
                    string value = ValueAfterLabel(line);
                    if (string.IsNullOrEmpty(value))
                        pending = "password";
                    else
                        password = value;
                    continue;
                }

                if (StartsWithLabel(line, "SMTP Server") || StartsWithLabel(line, "SMTP 服务器"))
                {
                    host = ValueAfterLabel(line);
                    continue;
                }

                if (StartsWithLabel(line, "TLS Port") || StartsWithLabel(line, "Port"))
                {
                    int.TryParse(ValueAfterLabel(line), out port);
                }
            }

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(user)
                || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(host) || port <= 0)
            {
                error = "邮件未发送：SES 配置不完整 " + path;
                return false;
            }

            cfg = new SmtpConfig
            {
                From = from,
                User = user,
                Password = password,
                Host = host,
                Port = port
            };
            return true;
        }

        static bool StartsWithLabel(string line, string label)
        {
            return line.StartsWith(label, StringComparison.OrdinalIgnoreCase);
        }

        static string ValueAfterLabel(string line)
        {
            int colon = line.IndexOf('：');
            if (colon < 0)
                colon = line.IndexOf(':');
            if (colon < 0 || colon >= line.Length - 1)
                return "";
            return line.Substring(colon + 1).Trim();
        }

        static string Unwrap(Exception e)
        {
            if (e == null)
                return "未知错误";
            Exception cur = e;
            while (cur.InnerException != null)
                cur = cur.InnerException;
            return cur.Message;
        }

        struct SmtpConfig
        {
            public string From;
            public string User;
            public string Password;
            public string Host;
            public int Port;
        }
    }
}
#endif
