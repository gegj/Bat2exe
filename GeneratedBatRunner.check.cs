using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    private const string BatName = "test.bat";
    private const string SaltBase64 = "AQIDBAUGBwgJCgsMDQ4PEA==";
    private const string IvBase64 = "ERITFBUWFxgZGhscHR4fIA==";
    private const bool PasswordRequired = false;
    private const string PasswordVerifierBase64 = "";
    private const string EmbeddedKeyBase64 = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=";
    private const bool HideCommandWindow = true;
    private const bool PersistRuntimeFolder = true;
    private const string RuntimeFolderName = "test_files";
    private const bool HasExtraFiles = false;
    private static readonly string[] EncryptedBatChunks = new string[]
    {
        "YWJj"
    };

    private static readonly ExtraFileInfo[] ExtraFiles = new ExtraFileInfo[]
    {
    };

    private sealed class ExtraFileInfo
    {
        public readonly string RelativePath;
        public readonly string ResourceName;
        public readonly string IvBase64;

        public ExtraFileInfo(string relativePath, string resourceName, string ivBase64)
        {
            RelativePath = relativePath;
            ResourceName = resourceName;
            IvBase64 = ivBase64;
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            byte[] key = GetDecryptionKey();
            if (key == null)
            {
                return 1;
            }

            string tempFolder = Path.Combine(Path.GetTempPath(), "bat2exe_run_" + Guid.NewGuid().ToString("N"));
            string runtimeFolder = GetRuntimeFolder(tempFolder);
            Directory.CreateDirectory(tempFolder);
            Directory.CreateDirectory(runtimeFolder);
            HidePathIfPossible(tempFolder);
            if (!PersistRuntimeFolder)
            {
                HidePathIfPossible(runtimeFolder);
            }
            string batExtension = Path.GetExtension(BatName);
            if (string.IsNullOrEmpty(batExtension))
            {
                batExtension = ".cmd";
            }
            string batFileName = PersistRuntimeFolder ? "payload" + batExtension : "payload_" + Guid.NewGuid().ToString("N") + batExtension;
            string launcherFileName = "run_" + Guid.NewGuid().ToString("N") + ".cmd";
            string batFolder = PersistRuntimeFolder ? runtimeFolder : tempFolder;
            string batPath = Path.Combine(batFolder, batFileName);
            string launcherPath = Path.Combine(tempFolder, launcherFileName);

            try
            {
                string encryptedText = string.Concat(EncryptedBatChunks);
                byte[] encryptedBat = Convert.FromBase64String(encryptedText);
                byte[] iv = Convert.FromBase64String(IvBase64);
                byte[] batBytes = AesDecrypt(encryptedBat, key, iv);
                File.WriteAllBytes(batPath, batBytes);
                Array.Clear(batBytes, 0, batBytes.Length);
                if (!PersistRuntimeFolder)
                {
                    HidePathIfPossible(batPath);
                }
                ExtractExtraFiles(runtimeFolder, key, !PersistRuntimeFolder);
                WriteLauncher(launcherPath, batPath, runtimeFolder);
                HidePathIfPossible(launcherPath);
                return RunBat(launcherFileName, tempFolder, args);
            }
            finally
            {
                TryDeleteDirectory(tempFolder);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("BAT 脚本运行失败，请检查脚本内容或相关文件是否完整。\n\n详细信息：" + SanitizeMessage(ex.Message), "运行失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    private static string GetRuntimeFolder(string tempFolder)
    {
        if (!PersistRuntimeFolder)
        {
            return tempFolder;
        }

        string folderName = RuntimeFolderName.Trim();
        if (folderName.Length == 0)
        {
            folderName = Path.GetFileNameWithoutExtension(Application.ExecutablePath) + "_files";
        }

        return Path.Combine(GetExeFolder(), folderName);
    }

    private static byte[] GetDecryptionKey()
    {
        if (!PasswordRequired)
        {
            return Convert.FromBase64String(EmbeddedKeyBase64);
        }

        byte[] salt = Convert.FromBase64String(SaltBase64);

        for (int i = 0; i < 3; i++)
        {
            string password = PasswordDialog.Ask();
            if (password == null)
            {
                return null;
            }

            byte[] key = DeriveKey(password, salt);
            string verifier = Convert.ToBase64String(MakeVerifierBytes(key));
            if (verifier == PasswordVerifierBase64)
            {
                return key;
            }

            MessageBox.Show("密码不正确，请重新输入。", "密码错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        MessageBox.Show("连续 3 次密码错误，程序已退出。", "密码错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        return null;
    }

    private static int RunBat(string launcherFileName, string tempFolder, string[] args)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");
        if (string.IsNullOrEmpty(startInfo.FileName))
        {
            startInfo.FileName = "cmd.exe";
        }

        // 只把临时启动器的文件名交给 cmd.exe，避免在命令行参数里暴露临时 BAT 完整路径。
        startInfo.Arguments = "/d /c " + QuoteArgument(launcherFileName) + JoinArguments(args);
        startInfo.WorkingDirectory = tempFolder;
        startInfo.UseShellExecute = false;

        if (HideCommandWindow)
        {
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
        }

        Process process = Process.Start(startInfo);
        process.WaitForExit();
        return process.ExitCode;
    }

    private static void WriteLauncher(string launcherPath, string batPath, string runtimeFolder)
    {
        string text =
            "@echo off\r\n" +
            "setlocal\r\n" +
            (HasExtraFiles ? "set \"PATH=" + runtimeFolder + ";%PATH%\"\r\n" : "") +
            "pushd " + QuoteForBatch(runtimeFolder) + " >nul 2>nul\r\n" +
            "call " + QuoteForBatch(batPath) + " %*\r\n" +
            "set \"_bat2exe_exit=%ERRORLEVEL%\"\r\n" +
            "popd >nul 2>nul\r\n" +
            "exit /b %_bat2exe_exit%\r\n";

        File.WriteAllText(launcherPath, text, Encoding.Default);
    }

    private static string QuoteForBatch(string value)
    {
        if (value == null)
        {
            return "\"\"";
        }

        return "\"" + value.Replace("\"", "") + "\"";
    }

    private static string JoinArguments(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < args.Length; i++)
        {
            builder.Append(' ');
            builder.Append(QuoteArgument(args[i]));
        }
        return builder.ToString();
    }

    private static string QuoteArgument(string value)
    {
        if (value == null)
        {
            return "\"\"";
        }
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static string GetExeFolder()
    {
        return Path.GetDirectoryName(Application.ExecutablePath);
    }

    private static string SanitizeMessage(string message)
    {
        if (message == null)
        {
            return "";
        }

        try
        {
            string tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (tempPath.Length > 0)
            {
                message = message.Replace(tempPath, "%TEMP%");
            }
        }
        catch
        {
        }

        return message;
    }

    private static void ExtractExtraFiles(string targetFolder, byte[] key, bool hideExtractedFiles)
    {
        if (ExtraFiles.Length == 0)
        {
            return;
        }

        Assembly assembly = Assembly.GetExecutingAssembly();
        for (int i = 0; i < ExtraFiles.Length; i++)
        {
            ExtraFileInfo info = ExtraFiles[i];
            string outputPath = GetSafeExtractPath(targetFolder, info.RelativePath);
            string parentFolder = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parentFolder))
            {
                Directory.CreateDirectory(parentFolder);
                if (hideExtractedFiles)
                {
                    HidePathIfPossible(parentFolder);
                }
            }

            using (Stream stream = assembly.GetManifestResourceStream(info.ResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("缺少附加文件资源：" + info.RelativePath);
                }

                byte[] encryptedFile = ReadAllBytes(stream);
                byte[] fileIv = Convert.FromBase64String(info.IvBase64);
                byte[] fileBytes = AesDecrypt(encryptedFile, key, fileIv);
                File.WriteAllBytes(outputPath, fileBytes);
                Array.Clear(fileBytes, 0, fileBytes.Length);
                Array.Clear(encryptedFile, 0, encryptedFile.Length);
            }

            if (hideExtractedFiles)
            {
                HidePathIfPossible(outputPath);
            }
        }
    }

    private static string GetSafeExtractPath(string targetFolder, string relativePath)
    {
        string cleaned = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        string fullPath = Path.GetFullPath(Path.Combine(targetFolder, cleaned));
        string root = Path.GetFullPath(targetFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("附加文件路径不安全：" + relativePath);
        }

        return fullPath;
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using (MemoryStream memory = new MemoryStream())
        {
            byte[] buffer = new byte[8192];
            int readCount;
            while ((readCount = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                memory.Write(buffer, 0, readCount);
            }

            return memory.ToArray();
        }
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        // 根据密码生成解密钥匙。密码相同、盐值相同，才能得到同一把钥匙。
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(password, salt, 120000))
        {
            return kdf.GetBytes(32);
        }
    }

    private static byte[] MakeVerifierBytes(byte[] key)
    {
        byte[] marker = Encoding.UTF8.GetBytes("bat2exe-small-password-check");
        byte[] combined = new byte[key.Length + marker.Length];
        Buffer.BlockCopy(key, 0, combined, 0, key.Length);
        Buffer.BlockCopy(marker, 0, combined, key.Length, marker.Length);

        using (SHA256 sha = SHA256.Create())
        {
            return sha.ComputeHash(combined);
        }
    }

    private static byte[] AesDecrypt(byte[] data, byte[] key, byte[] iv)
    {
        using (AesManaged aes = new AesManaged())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                return decryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }
    }

    private static void HidePathIfPossible(string path)
    {
        try
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                FileAttributes attributes = File.GetAttributes(path);
                File.SetAttributes(path, attributes | FileAttributes.Hidden | FileAttributes.Temporary);
            }
        }
        catch
        {
        }
    }

    private static void SecureDeleteFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            File.SetAttributes(filePath, FileAttributes.Normal);
            long length = new FileInfo(filePath).Length;

            if (length > 0)
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[8192];
                    long remaining = length;

                    while (remaining > 0)
                    {
                        int writeCount = (int)Math.Min(buffer.Length, remaining);
                        stream.Write(buffer, 0, writeCount);
                        remaining -= writeCount;
                    }

                    stream.Flush();
                }
            }

            File.Delete(filePath);
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string folder)
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (!Directory.Exists(folder))
                {
                    return;
                }

                string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    SecureDeleteFile(files[i]);
                }

                Directory.Delete(folder, true);
                return;
            }
            catch
            {
                Thread.Sleep(120);
            }
        }
    }
}

internal sealed class PasswordDialog : Form
{
    private readonly TextBox passwordTextBox = new TextBox();
    public string Password
    {
        get { return passwordTextBox.Text; }
    }

    public PasswordDialog()
    {
        Text = "需要密码";
        Width = 360;
        Height = 150;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        Font = new Font("Microsoft YaHei UI", 9F);

        Label label = new Label();
        label.Text = "请输入运行密码：";
        label.Left = 16;
        label.Top = 16;
        label.Width = 300;
        Controls.Add(label);

        passwordTextBox.Left = 16;
        passwordTextBox.Top = 42;
        passwordTextBox.Width = 310;
        passwordTextBox.UseSystemPasswordChar = true;
        Controls.Add(passwordTextBox);

        Button okButton = new Button();
        okButton.Text = "确定";
        okButton.Left = 166;
        okButton.Top = 78;
        okButton.Width = 75;
        okButton.DialogResult = DialogResult.OK;
        Controls.Add(okButton);

        Button cancelButton = new Button();
        cancelButton.Text = "取消";
        cancelButton.Left = 251;
        cancelButton.Top = 78;
        cancelButton.Width = 75;
        cancelButton.DialogResult = DialogResult.Cancel;
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public static string Ask()
    {
        using (PasswordDialog dialog = new PasswordDialog())
        {
            return dialog.ShowDialog() == DialogResult.OK ? dialog.Password : null;
        }
    }
}

