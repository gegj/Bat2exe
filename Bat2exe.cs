// Bat2exe v0.3.5
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Bat2exe")]
[assembly: AssemblyProduct("Bat2exe")]
[assembly: AssemblyVersion("0.3.5.0")]
[assembly: AssemblyFileVersion("0.3.5.0")]

public sealed class Bat2exe : Form
{
    private readonly TextBox batTextBox = new TextBox();
    private readonly TextBox extraFolderTextBox = new TextBox();
    private readonly TextBox outputTextBox = new TextBox();
    private readonly TextBox exeNameTextBox = new TextBox();
    private readonly TextBox passwordTextBox = new TextBox();
    private readonly TextBox iconTextBox = new TextBox();
    private readonly TextBox runtimeFolderNameTextBox = new TextBox();
    private readonly CheckBox hideWindowCheckBox = new CheckBox();
    private readonly CheckBox adminCheckBox = new CheckBox();
    private readonly CheckBox persistRuntimeFolderCheckBox = new CheckBox();
    private readonly CheckBox openFolderCheckBox = new CheckBox();
    private readonly Button buildButton = new Button();
    private readonly Label statusLabel = new Label();

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Bat2exe());
    }

    public Bat2exe()
    {
        Text = "Bat2exe";
        Width = 660;
        Height = 500;
        MinimumSize = new Size(600, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        System.Drawing.Icon app_icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (app_icon != null)
        {
            Icon = app_icon;
        }

        BuildUi();
    }

    private void BuildUi()
    {
        int top = 20;
        AddFileRow("BAT 文件", batTextBox, "选择", ChooseBat, top);
        top += 40;
        AddFileRow("附加文件夹", extraFolderTextBox, "选择", ChooseExtraFolder, top);
        top += 40;
        AddFileRow("输出目录", outputTextBox, "选择", ChooseOutputFolder, top);
        top += 40;
        AddTextRow("EXE 名称", exeNameTextBox, top, false);
        top += 40;
        AddTextRow("运行密码", passwordTextBox, top, true);
        top += 40;
        AddFileRow("程序图标", iconTextBox, "选择", ChooseIcon, top);

        persistRuntimeFolderCheckBox.Text = "持久化运行环境文件夹";
        persistRuntimeFolderCheckBox.Left = 96;
        persistRuntimeFolderCheckBox.Top = 268;
        persistRuntimeFolderCheckBox.Width = 180;
        persistRuntimeFolderCheckBox.CheckedChanged += ToggleRuntimeFolderName;
        Controls.Add(persistRuntimeFolderCheckBox);

        AddTextRow("释放文件夹", runtimeFolderNameTextBox, 300, false);
        runtimeFolderNameTextBox.Enabled = false;

        hideWindowCheckBox.Text = "运行时隐藏黑窗口";
        hideWindowCheckBox.Left = 96;
        hideWindowCheckBox.Top = 340;
        hideWindowCheckBox.Width = 160;
        Controls.Add(hideWindowCheckBox);

        adminCheckBox.Text = "需要管理员权限";
        adminCheckBox.Left = 270;
        adminCheckBox.Top = 340;
        adminCheckBox.Width = 150;
        Controls.Add(adminCheckBox);

        openFolderCheckBox.Text = "生成后打开文件夹";
        openFolderCheckBox.Left = 430;
        openFolderCheckBox.Top = 340;
        openFolderCheckBox.Width = 160;
        openFolderCheckBox.Checked = true;
        Controls.Add(openFolderCheckBox);

        buildButton.Text = "生成 EXE";
        buildButton.Left = 96;
        buildButton.Top = 378;
        buildButton.Width = 520;
        buildButton.Height = 34;
        buildButton.Click += StartBuild;
        Controls.Add(buildButton);

        statusLabel.Text = "请选择 BAT 文件，然后点击“生成 EXE”。";
        statusLabel.Left = 96;
        statusLabel.Top = 422;
        statusLabel.Width = 540;
        statusLabel.Height = 24;
        statusLabel.ForeColor = Color.FromArgb(55, 55, 55);
        Controls.Add(statusLabel);

        outputTextBox.Text = Path.Combine(Environment.CurrentDirectory, "dist");
    }

    private void AddTextRow(string labelText, TextBox textBox, int top, bool password)
    {
        Label label = new Label();
        label.Text = labelText;
        label.Left = 22;
        label.Top = top + 5;
        label.Width = 75;
        Controls.Add(label);

        textBox.Left = 96;
        textBox.Top = top;
        textBox.Width = 520;
        if (password)
        {
            textBox.UseSystemPasswordChar = true;
        }
        Controls.Add(textBox);
    }

    private void AddFileRow(string labelText, TextBox textBox, string buttonText, EventHandler clickHandler, int top)
    {
        Label label = new Label();
        label.Text = labelText;
        label.Left = 22;
        label.Top = top + 5;
        label.Width = 75;
        Controls.Add(label);

        textBox.Left = 96;
        textBox.Top = top;
        textBox.Width = 424;
        Controls.Add(textBox);

        Button button = new Button();
        button.Text = buttonText;
        button.Left = 530;
        button.Top = top - 1;
        button.Width = 86;
        button.Height = 27;
        button.Click += clickHandler;
        Controls.Add(button);
    }

    private void ChooseBat(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "选择 BAT 文件";
            dialog.Filter = "BAT/CMD 文件|*.bat;*.cmd|所有文件|*.*";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            batTextBox.Text = dialog.FileName;

            // 默认把 EXE 名称设置成 BAT 文件名，用户也可以自己改。
            if (string.IsNullOrWhiteSpace(exeNameTextBox.Text))
            {
                exeNameTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private void ChooseOutputFolder(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "选择 EXE 输出目录";

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                outputTextBox.Text = dialog.SelectedPath;
            }
        }
    }

    private void ChooseExtraFolder(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "选择要一起打包的文件夹（可包含 exe、dll 和其它文件）";

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                extraFolderTextBox.Text = dialog.SelectedPath;
            }
        }
    }

    private void ChooseIcon(object sender, EventArgs e)
    {
        using (OpenFileDialog dialog = new OpenFileDialog())
        {
            dialog.Title = "选择程序图标";
            dialog.Filter = "常见图片格式|*.ico;*.png;*.jpg;*.jpeg;*.bmp;*.gif|ICO 图标|*.ico|PNG 图片|*.png|JPEG 图片|*.jpg;*.jpeg|BMP 图片|*.bmp|GIF 图片|*.gif|所有文件|*.*";

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string error_message;
            if (!try_validate_icon_image(dialog.FileName, out error_message))
            {
                MessageBox.Show(this, error_message, "图标格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            iconTextBox.Text = dialog.FileName;
        }
    }

    private static bool try_validate_icon_image(string image_path, out string error_message)
    {
        error_message = "";

        if (!File.Exists(image_path))
        {
            error_message = "找不到图标文件，请重新选择。";
            return false;
        }

        string extension = Path.GetExtension(image_path).ToLowerInvariant();
        if (extension != ".ico" && extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".bmp" && extension != ".gif")
        {
            error_message = "仅支持 ICO、PNG、JPG、JPEG、BMP 和 GIF 格式。";
            return false;
        }

        try
        {
            int width;
            int height;

            if (extension == ".ico")
            {
                using (System.Drawing.Icon selected_icon = new System.Drawing.Icon(image_path))
                {
                    width = selected_icon.Width;
                    height = selected_icon.Height;
                }
            }
            else
            {
                using (Image selected_image = Image.FromFile(image_path))
                {
                    width = selected_image.Width;
                    height = selected_image.Height;
                }
            }

            if (width != height)
            {
                error_message = "程序图标必须是 1:1 正方形图片。当前尺寸：" + width + " x " + height + "。";
                return false;
            }
        }
        catch (Exception)
        {
            error_message = "无法读取图片，文件格式错误或文件已损坏。";
            return false;
        }

        return true;
    }

    private void ToggleRuntimeFolderName(object sender, EventArgs e)
    {
        runtimeFolderNameTextBox.Enabled = persistRuntimeFolderCheckBox.Checked;
    }

    private void StartBuild(object sender, EventArgs e)
    {
        BuildConfig config;

        if (!TryCollectConfig(out config))
        {
            return;
        }

        SetBusy(true);
        SetStatus("正在准备生成 EXE...");

        Thread worker = new Thread(delegate()
        {
            try
            {
                string exePath = BuildExe(config);
                BeginInvoke(new Action(delegate()
                {
                    SetBusy(false);
                    SetStatus("生成成功：" + exePath);
                    MessageBox.Show(this, "EXE 已生成：\n" + exePath, "生成成功", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (config.OpenFolderAfterBuild)
                    {
                        using (Process explorer_process = Process.Start("explorer.exe", "/select,\"" + exePath + "\""))
                        {
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(delegate()
                {
                    SetBusy(false);
                    SetStatus("生成失败，请查看提示。");
                    MessageBox.Show(this, ex.Message, "生成失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        });

        worker.IsBackground = true;
        worker.Start();
    }

    private bool TryCollectConfig(out BuildConfig config)
    {
        config = new BuildConfig();

        string batPath = batTextBox.Text.Trim();
        string extraFolder = extraFolderTextBox.Text.Trim();
        string outputFolder = outputTextBox.Text.Trim();
        string exeName = MakeSafeFileName(exeNameTextBox.Text);
        string iconPath = iconTextBox.Text.Trim();
        string runtimeFolderName = runtimeFolderNameTextBox.Text.Trim();

        if (!File.Exists(batPath))
        {
            MessageBox.Show(this, "找不到 BAT 文件，请重新选择。", "请检查设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        string suffix = Path.GetExtension(batPath).ToLowerInvariant();
        if (suffix != ".bat" && suffix != ".cmd")
        {
            MessageBox.Show(this, "请选择 .bat 或 .cmd 文件。", "请检查设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            MessageBox.Show(this, "请选择输出目录。", "请检查设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (extraFolder.Length > 0 && !Directory.Exists(extraFolder))
        {
            MessageBox.Show(this, "找不到附加文件夹，请重新选择，或者把附加文件夹留空。", "请检查设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (iconPath.Length > 0)
        {
            string error_message;
            if (!try_validate_icon_image(iconPath, out error_message))
            {
                MessageBox.Show(this, error_message, "图标格式错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        if (persistRuntimeFolderCheckBox.Checked)
        {
            if (!IsSafeFolderName(runtimeFolderName))
            {
                MessageBox.Show(this, "释放文件夹名称只能使用中文、字母、数字、空格、点、横线和下划线。", "请检查设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }
        else
        {
            runtimeFolderName = "";
        }

        config.BatPath = batPath;
        config.ExtraFolder = extraFolder;
        config.OutputFolder = outputFolder;
        config.ExeName = exeName;
        config.Password = passwordTextBox.Text;
        config.IconPath = iconPath;
        config.HideCommandWindow = hideWindowCheckBox.Checked;
        config.RequireAdmin = adminCheckBox.Checked;
        config.PersistRuntimeFolder = persistRuntimeFolderCheckBox.Checked;
        config.RuntimeFolderName = runtimeFolderName;
        config.OpenFolderAfterBuild = openFolderCheckBox.Checked;
        return true;
    }

    private string BuildExe(BuildConfig config)
    {
        string cscPath = FindCscPath();
        if (cscPath == null)
        {
            throw new InvalidOperationException("找不到 C# 编译器。请确认 Windows 的 .NET Framework 功能正常。");
        }

        Directory.CreateDirectory(config.OutputFolder);
        string outputExe = Path.Combine(config.OutputFolder, config.ExeName + ".exe");
        string compiled_output = Path.Combine(config.OutputFolder, "." + config.ExeName + "." + Guid.NewGuid().ToString("N") + ".tmp.exe");
        PayloadFile[] extraFiles = CollectExtraFiles(config, outputExe);

        SetStatus("正在加密 BAT 内容...");
        byte[] batBytes = File.ReadAllBytes(config.BatPath);
        byte[] salt = RandomBytes(16);
        byte[] iv = RandomBytes(16);
        byte[] key;
        bool passwordRequired = !string.IsNullOrEmpty(config.Password);
        string verifier = "";
        string embeddedKey = "";

        if (passwordRequired)
        {
            key = DeriveKey(config.Password, salt);
            verifier = Convert.ToBase64String(MakeVerifierBytes(key));
        }
        else
        {
            // 没有设置密码时，也随机生成一把钥匙，避免 BAT 原文直接暴露在 EXE 里。
            key = RandomBytes(32);
            embeddedKey = Convert.ToBase64String(key);
        }

        byte[] encryptedBat = AesEncrypt(batBytes, key, iv);
        Array.Clear(batBytes, 0, batBytes.Length);
        string tempFolder = Path.Combine(Path.GetTempPath(), "bat2exe_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);

        string sourcePath = Path.Combine(tempFolder, "GeneratedBatRunner.cs");
        string manifestPath = Path.Combine(tempFolder, "app.manifest");
        string responsePath = Path.Combine(tempFolder, "csc.rsp");

        try
        {
            string compiler_icon_path = config.IconPath;
            if (string.IsNullOrEmpty(compiler_icon_path))
            {
                using (Stream icon_stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Bat2exe.Icon"))
                {
                    if (icon_stream != null)
                    {
                        compiler_icon_path = Path.Combine(tempFolder, "bat2exe.ico");
                        using (FileStream icon_file = File.Create(compiler_icon_path))
                        {
                            icon_stream.CopyTo(icon_file);
                        }
                    }
                }
            }
            else if (Path.GetExtension(compiler_icon_path).ToLowerInvariant() != ".ico")
            {
                compiler_icon_path = Path.Combine(tempFolder, "custom_icon.ico");
                convert_image_to_icon(config.IconPath, compiler_icon_path);
            }

            if (extraFiles.Length > 0)
            {
                SetStatus("正在打包附加文件...");
                PrepareExtraResources(extraFiles, tempFolder, key);
            }

            string sourceCode = BuildStubSource(
                Path.GetFileName(config.BatPath),
                Convert.ToBase64String(encryptedBat),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(iv),
                passwordRequired,
                verifier,
                embeddedKey,
                config.HideCommandWindow,
                config.PersistRuntimeFolder,
                config.RuntimeFolderName,
                extraFiles
            );

            File.WriteAllText(sourcePath, sourceCode, new UTF8Encoding(true));
            File.WriteAllText(manifestPath, BuildManifest(config.RequireAdmin), new UTF8Encoding(true));

            SetStatus("正在生成 EXE...");
            StringBuilder response = new StringBuilder();
            response.AppendLine("/nologo");
            response.AppendLine("/optimize+");
            response.AppendLine("/target:winexe");
            response.AppendLine("/codepage:65001");
            response.AppendLine("/r:System.Windows.Forms.dll");
            response.AppendLine("/r:System.Drawing.dll");
            response.AppendLine("/win32manifest:" + QuoteForCompiler(manifestPath));
            response.AppendLine("/out:" + QuoteForCompiler(compiled_output));

            if (!string.IsNullOrEmpty(compiler_icon_path))
            {
                response.AppendLine("/win32icon:" + QuoteForCompiler(compiler_icon_path));
            }

            for (int i = 0; i < extraFiles.Length; i++)
            {
                response.AppendLine("/resource:" + QuoteForCompiler(extraFiles[i].EncryptedPath) + "," + extraFiles[i].ResourceName);
            }

            response.AppendLine(QuoteForCompiler(sourcePath));
            File.WriteAllText(responsePath, response.ToString(), new UTF8Encoding(false));

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = cscPath;
            startInfo.Arguments = "@" + QuoteForCompiler(responsePath);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("无法启动 C# 编译器。");
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string detail = (stderr + "\n" + stdout).Trim();
                    if (detail.Length == 0)
                    {
                        detail = "编译器没有返回详细信息。";
                    }

                    throw new InvalidOperationException("生成 EXE 失败。\n\n" + detail);
                }
            }

            if (!File.Exists(compiled_output))
            {
                throw new InvalidOperationException("编译器已经结束，但没有找到生成的 EXE 文件。");
            }

            if (File.Exists(outputExe))
            {
                File.Replace(compiled_output, outputExe, null);
            }
            else
            {
                File.Move(compiled_output, outputExe);
            }

            return outputExe;
        }
        finally
        {
            Array.Clear(key, 0, key.Length);
            Array.Clear(encryptedBat, 0, encryptedBat.Length);
            try_delete_file(compiled_output);
            TryDeleteDirectory(tempFolder);
        }
    }

    private static void convert_image_to_icon(string image_path, string icon_path)
    {
        int[] icon_sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };
        List<byte[]> icon_images = new List<byte[]>();

        using (Image source_image = Image.FromFile(image_path))
        {
            for (int i = 0; i < icon_sizes.Length; i++)
            {
                int size = icon_sizes[i];
                using (Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                using (MemoryStream image_stream = new MemoryStream())
                {
                    graphics.Clear(Color.Transparent);
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(source_image, new Rectangle(0, 0, size, size));
                    bitmap.Save(image_stream, ImageFormat.Png);
                    icon_images.Add(image_stream.ToArray());
                }
            }
        }

        using (FileStream icon_stream = File.Create(icon_path))
        using (BinaryWriter writer = new BinaryWriter(icon_stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)icon_sizes.Length);

            int image_offset = 6 + icon_sizes.Length * 16;
            for (int i = 0; i < icon_sizes.Length; i++)
            {
                byte dimension = icon_sizes[i] == 256 ? (byte)0 : (byte)icon_sizes[i];
                writer.Write(dimension);
                writer.Write(dimension);
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(icon_images[i].Length);
                writer.Write(image_offset);
                image_offset += icon_images[i].Length;
            }

            for (int i = 0; i < icon_images.Count; i++)
            {
                writer.Write(icon_images[i]);
            }
        }
    }

    private static string BuildStubSource(
        string batName,
        string encryptedBatBase64,
        string saltBase64,
        string ivBase64,
        bool passwordRequired,
        string passwordVerifierBase64,
        string embeddedKeyBase64,
        bool hideCommandWindow,
        bool persistRuntimeFolder,
        string runtimeFolderName,
        PayloadFile[] extraFiles)
    {
        StringBuilder code = new StringBuilder();

        code.AppendLine("using System;");
        code.AppendLine("using System.Diagnostics;");
        code.AppendLine("using System.Drawing;");
        code.AppendLine("using System.IO;");
        code.AppendLine("using System.Reflection;");
        code.AppendLine("using System.Security.Cryptography;");
        code.AppendLine("using System.Text;");
        code.AppendLine("using System.Threading;");
        code.AppendLine("using System.Windows.Forms;");
        code.AppendLine("");
        code.AppendLine("internal static class Program");
        code.AppendLine("{");
        code.AppendLine("    private const string BatName = " + CsString(batName) + ";");
        code.AppendLine("    private const string SaltBase64 = " + CsString(saltBase64) + ";");
        code.AppendLine("    private const string IvBase64 = " + CsString(ivBase64) + ";");
        code.AppendLine("    private const bool PasswordRequired = " + (passwordRequired ? "true" : "false") + ";");
        code.AppendLine("    private const string PasswordVerifierBase64 = " + CsString(passwordVerifierBase64) + ";");
        code.AppendLine("    private const string EmbeddedKeyBase64 = " + CsString(embeddedKeyBase64) + ";");
        code.AppendLine("    private const bool HideCommandWindow = " + (hideCommandWindow ? "true" : "false") + ";");
        code.AppendLine("    private const bool PersistRuntimeFolder = " + (persistRuntimeFolder ? "true" : "false") + ";");
        code.AppendLine("    private const string RuntimeFolderName = " + CsString(runtimeFolderName) + ";");
        code.AppendLine("    private const bool HasExtraFiles = " + (extraFiles.Length > 0 ? "true" : "false") + ";");
        code.AppendLine("    private static readonly string[] EncryptedBatChunks = new string[]");
        code.AppendLine("    {");
        AppendStringChunks(code, encryptedBatBase64, 7600);
        code.AppendLine("    };");
        code.AppendLine("");
        code.AppendLine("    private static readonly ExtraFileInfo[] ExtraFiles = new ExtraFileInfo[]");
        code.AppendLine("    {");
        for (int i = 0; i < extraFiles.Length; i++)
        {
            code.AppendLine("        new ExtraFileInfo(" +
                CsString(extraFiles[i].RelativePath) + ", " +
                CsString(extraFiles[i].ResourceName) + ", " +
                CsString(extraFiles[i].IvBase64) + ")" +
                (i + 1 < extraFiles.Length ? "," : ""));
        }
        code.AppendLine("    };");
        code.AppendLine("");
        code.AppendLine("    private sealed class ExtraFileInfo");
        code.AppendLine("    {");
        code.AppendLine("        public readonly string RelativePath;");
        code.AppendLine("        public readonly string ResourceName;");
        code.AppendLine("        public readonly string IvBase64;");
        code.AppendLine("");
        code.AppendLine("        public ExtraFileInfo(string relativePath, string resourceName, string ivBase64)");
        code.AppendLine("        {");
        code.AppendLine("            RelativePath = relativePath;");
        code.AppendLine("            ResourceName = resourceName;");
        code.AppendLine("            IvBase64 = ivBase64;");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    [STAThread]");
        code.AppendLine("    private static int Main(string[] args)");
        code.AppendLine("    {");
        code.AppendLine("        try");
        code.AppendLine("        {");
        code.AppendLine("            byte[] key = GetDecryptionKey();");
        code.AppendLine("            if (key == null)");
        code.AppendLine("            {");
        code.AppendLine("                return 1;");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            string tempFolder = Path.Combine(Path.GetTempPath(), \"bat2exe_run_\" + Guid.NewGuid().ToString(\"N\"));");
        code.AppendLine("            string runtimeFolder = GetRuntimeFolder(tempFolder);");
        code.AppendLine("            Directory.CreateDirectory(tempFolder);");
        code.AppendLine("            Directory.CreateDirectory(runtimeFolder);");
        code.AppendLine("            HidePathIfPossible(tempFolder);");
        code.AppendLine("            if (!PersistRuntimeFolder)");
        code.AppendLine("            {");
        code.AppendLine("                HidePathIfPossible(runtimeFolder);");
        code.AppendLine("            }");
        code.AppendLine("            string batExtension = Path.GetExtension(BatName);");
        code.AppendLine("            if (string.IsNullOrEmpty(batExtension))");
        code.AppendLine("            {");
        code.AppendLine("                batExtension = \".cmd\";");
        code.AppendLine("            }");
        code.AppendLine("            string batFileName = PersistRuntimeFolder ? \"payload\" + batExtension : \"payload_\" + Guid.NewGuid().ToString(\"N\") + batExtension;");
        code.AppendLine("            string launcherFileName = \"run_\" + Guid.NewGuid().ToString(\"N\") + \".cmd\";");
        code.AppendLine("            string batFolder = PersistRuntimeFolder ? runtimeFolder : tempFolder;");
        code.AppendLine("            string batPath = Path.Combine(batFolder, batFileName);");
        code.AppendLine("            string launcherPath = Path.Combine(tempFolder, launcherFileName);");
        code.AppendLine("");
        code.AppendLine("            try");
        code.AppendLine("            {");
        code.AppendLine("                string encryptedText = string.Concat(EncryptedBatChunks);");
        code.AppendLine("                byte[] encryptedBat = Convert.FromBase64String(encryptedText);");
        code.AppendLine("                byte[] iv = Convert.FromBase64String(IvBase64);");
        code.AppendLine("                byte[] batBytes = AesDecrypt(encryptedBat, key, iv);");
        code.AppendLine("                Array.Clear(encryptedBat, 0, encryptedBat.Length);");
        code.AppendLine("                File.WriteAllBytes(batPath, batBytes);");
        code.AppendLine("                Array.Clear(batBytes, 0, batBytes.Length);");
        code.AppendLine("                if (!PersistRuntimeFolder)");
        code.AppendLine("                {");
        code.AppendLine("                    HidePathIfPossible(batPath);");
        code.AppendLine("                }");
        code.AppendLine("                ExtractExtraFiles(runtimeFolder, key, !PersistRuntimeFolder);");
        code.AppendLine("                WriteLauncher(launcherPath, batPath, runtimeFolder);");
        code.AppendLine("                HidePathIfPossible(launcherPath);");
        code.AppendLine("                return RunBat(launcherFileName, tempFolder, args);");
        code.AppendLine("            }");
        code.AppendLine("            finally");
        code.AppendLine("            {");
        code.AppendLine("                TryDeleteDirectory(tempFolder);");
        code.AppendLine("                Array.Clear(key, 0, key.Length);");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine("        catch (Exception ex)");
        code.AppendLine("        {");
        code.AppendLine("            MessageBox.Show(\"BAT 脚本运行失败，请检查脚本内容或相关文件是否完整。\\n\\n详细信息：\" + SanitizeMessage(ex.Message), \"运行失败\", MessageBoxButtons.OK, MessageBoxIcon.Error);");
        code.AppendLine("            return 1;");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static string GetRuntimeFolder(string tempFolder)");
        code.AppendLine("    {");
        code.AppendLine("        if (!PersistRuntimeFolder)");
        code.AppendLine("        {");
        code.AppendLine("            return tempFolder;");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        string folderName = RuntimeFolderName.Trim();");
        code.AppendLine("        if (folderName.Length == 0)");
        code.AppendLine("        {");
        code.AppendLine("            folderName = Path.GetFileNameWithoutExtension(Application.ExecutablePath) + \"_files\";");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        return Path.Combine(GetExeFolder(), folderName);");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static byte[] GetDecryptionKey()");
        code.AppendLine("    {");
        code.AppendLine("        if (!PasswordRequired)");
        code.AppendLine("        {");
        code.AppendLine("            return Convert.FromBase64String(EmbeddedKeyBase64);");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        byte[] salt = Convert.FromBase64String(SaltBase64);");
        code.AppendLine("");
        code.AppendLine("        for (int i = 0; i < 3; i++)");
        code.AppendLine("        {");
        code.AppendLine("            string password = PasswordDialog.Ask();");
        code.AppendLine("            if (password == null)");
        code.AppendLine("            {");
        code.AppendLine("                return null;");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            byte[] key = DeriveKey(password, salt);");
        code.AppendLine("            string verifier = Convert.ToBase64String(MakeVerifierBytes(key));");
        code.AppendLine("            if (verifier == PasswordVerifierBase64)");
        code.AppendLine("            {");
        code.AppendLine("                return key;");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            Array.Clear(key, 0, key.Length);");
        code.AppendLine("            MessageBox.Show(\"密码不正确，请重新输入。\", \"密码错误\", MessageBoxButtons.OK, MessageBoxIcon.Warning);");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        MessageBox.Show(\"连续 3 次密码错误，程序已退出。\", \"密码错误\", MessageBoxButtons.OK, MessageBoxIcon.Error);");
        code.AppendLine("        return null;");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static int RunBat(string launcherFileName, string tempFolder, string[] args)");
        code.AppendLine("    {");
        code.AppendLine("        ProcessStartInfo startInfo = new ProcessStartInfo();");
        code.AppendLine("        startInfo.FileName = Environment.GetEnvironmentVariable(\"ComSpec\");");
        code.AppendLine("        if (string.IsNullOrEmpty(startInfo.FileName))");
        code.AppendLine("        {");
        code.AppendLine("            startInfo.FileName = \"cmd.exe\";");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        // 只把临时启动器的文件名交给 cmd.exe，避免在命令行参数里暴露临时 BAT 完整路径。");
        code.AppendLine("        startInfo.Arguments = \"/d /c \" + QuoteArgument(launcherFileName) + JoinArguments(args);");
        code.AppendLine("        startInfo.WorkingDirectory = tempFolder;");
        code.AppendLine("        startInfo.UseShellExecute = false;");
        code.AppendLine("");
        code.AppendLine("        if (HideCommandWindow)");
        code.AppendLine("        {");
        code.AppendLine("            startInfo.CreateNoWindow = true;");
        code.AppendLine("            startInfo.WindowStyle = ProcessWindowStyle.Hidden;");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        using (Process process = Process.Start(startInfo))");
        code.AppendLine("        {");
        code.AppendLine("            if (process == null)");
        code.AppendLine("            {");
        code.AppendLine("                throw new InvalidOperationException(\"无法启动命令行程序。\");");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            process.WaitForExit();");
        code.AppendLine("            return process.ExitCode;");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static void WriteLauncher(string launcherPath, string batPath, string runtimeFolder)");
        code.AppendLine("    {");
        code.AppendLine("        string text =");
        code.AppendLine("            \"@echo off\\r\\n\" +");
        code.AppendLine("            \"setlocal\\r\\n\" +");
        code.AppendLine("            (HasExtraFiles ? \"set \\\"PATH=\" + runtimeFolder + \";%PATH%\\\"\\r\\n\" : \"\") +");
        code.AppendLine("            \"pushd \" + QuoteForBatch(runtimeFolder) + \" >nul 2>nul\\r\\n\" +");
        code.AppendLine("            \"call \" + QuoteForBatch(batPath) + \" %*\\r\\n\" +");
        code.AppendLine("            \"set \\\"_bat2exe_exit=%ERRORLEVEL%\\\"\\r\\n\" +");
        code.AppendLine("            \"popd >nul 2>nul\\r\\n\" +");
        code.AppendLine("            \"exit /b %_bat2exe_exit%\\r\\n\";");
        code.AppendLine("");
        code.AppendLine("        File.WriteAllText(launcherPath, text, Encoding.Default);");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static string QuoteForBatch(string value)");
        code.AppendLine("    {");
        code.AppendLine("        if (value == null)");
        code.AppendLine("        {");
        code.AppendLine("            return \"\\\"\\\"\";");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        return \"\\\"\" + value.Replace(\"\\\"\", \"\") + \"\\\"\";");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static string JoinArguments(string[] args)");
        code.AppendLine("    {");
        code.AppendLine("        if (args == null || args.Length == 0)");
        code.AppendLine("        {");
        code.AppendLine("            return \"\";");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        StringBuilder builder = new StringBuilder();");
        code.AppendLine("        for (int i = 0; i < args.Length; i++)");
        code.AppendLine("        {");
        code.AppendLine("            builder.Append(' ');");
        code.AppendLine("            builder.Append(QuoteArgument(args[i]));");
        code.AppendLine("        }");
        code.AppendLine("        return builder.ToString();");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static string QuoteArgument(string value)");
        code.AppendLine("    {");
        code.AppendLine("        if (value == null)");
        code.AppendLine("        {");
        code.AppendLine("            return \"\\\"\\\"\";");
        code.AppendLine("        }");
        code.AppendLine("        return \"\\\"\" + value.Replace(\"\\\"\", \"\\\\\\\"\") + \"\\\"\";");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static string GetExeFolder()");
        code.AppendLine("    {");
        code.AppendLine("        return Path.GetDirectoryName(Application.ExecutablePath);");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static string SanitizeMessage(string message)");
        code.AppendLine("    {");
        code.AppendLine("        if (message == null)");
        code.AppendLine("        {");
        code.AppendLine("            return \"\";");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        try");
        code.AppendLine("        {");
        code.AppendLine("            string tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);");
        code.AppendLine("            if (tempPath.Length > 0)");
        code.AppendLine("            {");
        code.AppendLine("                message = message.Replace(tempPath, \"%TEMP%\");");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine("        catch");
        code.AppendLine("        {");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        return message;");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static void ExtractExtraFiles(string targetFolder, byte[] key, bool hideExtractedFiles)");
        code.AppendLine("    {");
        code.AppendLine("        if (ExtraFiles.Length == 0)");
        code.AppendLine("        {");
        code.AppendLine("            return;");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        Assembly assembly = Assembly.GetExecutingAssembly();");
        code.AppendLine("        for (int i = 0; i < ExtraFiles.Length; i++)");
        code.AppendLine("        {");
        code.AppendLine("            ExtraFileInfo info = ExtraFiles[i];");
        code.AppendLine("            string outputPath = GetSafeExtractPath(targetFolder, info.RelativePath);");
        code.AppendLine("            string parentFolder = Path.GetDirectoryName(outputPath);");
        code.AppendLine("            if (!string.IsNullOrEmpty(parentFolder))");
        code.AppendLine("            {");
        code.AppendLine("                Directory.CreateDirectory(parentFolder);");
        code.AppendLine("                if (hideExtractedFiles)");
        code.AppendLine("                {");
        code.AppendLine("                    HidePathIfPossible(parentFolder);");
        code.AppendLine("                }");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            using (Stream stream = assembly.GetManifestResourceStream(info.ResourceName))");
        code.AppendLine("            {");
        code.AppendLine("                if (stream == null)");
        code.AppendLine("                {");
        code.AppendLine("                    throw new InvalidOperationException(\"缺少附加文件资源：\" + info.RelativePath);");
        code.AppendLine("                }");
        code.AppendLine("");
        code.AppendLine("                byte[] encryptedFile = ReadAllBytes(stream);");
        code.AppendLine("                byte[] fileIv = Convert.FromBase64String(info.IvBase64);");
        code.AppendLine("                byte[] fileBytes = AesDecrypt(encryptedFile, key, fileIv);");
        code.AppendLine("                File.WriteAllBytes(outputPath, fileBytes);");
        code.AppendLine("                Array.Clear(fileBytes, 0, fileBytes.Length);");
        code.AppendLine("                Array.Clear(encryptedFile, 0, encryptedFile.Length);");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            if (hideExtractedFiles)");
        code.AppendLine("            {");
        code.AppendLine("                HidePathIfPossible(outputPath);");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static string GetSafeExtractPath(string targetFolder, string relativePath)");
        code.AppendLine("    {");
        code.AppendLine("        string cleaned = relativePath.Replace('/', Path.DirectorySeparatorChar).Replace('\\\\', Path.DirectorySeparatorChar);");
        code.AppendLine("        string fullPath = Path.GetFullPath(Path.Combine(targetFolder, cleaned));");
        code.AppendLine("        string root = Path.GetFullPath(targetFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;");
        code.AppendLine("");
        code.AppendLine("        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))");
        code.AppendLine("        {");
        code.AppendLine("            throw new InvalidOperationException(\"附加文件路径不安全：\" + relativePath);");
        code.AppendLine("        }");
        code.AppendLine("");
        code.AppendLine("        return fullPath;");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static byte[] ReadAllBytes(Stream stream)");
        code.AppendLine("    {");
        code.AppendLine("        using (MemoryStream memory = new MemoryStream())");
        code.AppendLine("        {");
        code.AppendLine("            byte[] buffer = new byte[8192];");
        code.AppendLine("            int readCount;");
        code.AppendLine("            while ((readCount = stream.Read(buffer, 0, buffer.Length)) > 0)");
        code.AppendLine("            {");
        code.AppendLine("                memory.Write(buffer, 0, readCount);");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            return memory.ToArray();");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static byte[] DeriveKey(string password, byte[] salt)");
        code.AppendLine("    {");
        code.AppendLine("        // 根据密码生成解密钥匙。密码相同、盐值相同，才能得到同一把钥匙。");
        code.AppendLine("        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(password, salt, 120000))");
        code.AppendLine("        {");
        code.AppendLine("            return kdf.GetBytes(32);");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static byte[] MakeVerifierBytes(byte[] key)");
        code.AppendLine("    {");
        code.AppendLine("        byte[] marker = Encoding.UTF8.GetBytes(\"bat2exe-password-check\");");
        code.AppendLine("        byte[] combined = new byte[key.Length + marker.Length];");
        code.AppendLine("        Buffer.BlockCopy(key, 0, combined, 0, key.Length);");
        code.AppendLine("        Buffer.BlockCopy(marker, 0, combined, key.Length, marker.Length);");
        code.AppendLine("");
        code.AppendLine("        using (SHA256 sha = SHA256.Create())");
        code.AppendLine("        {");
        code.AppendLine("            return sha.ComputeHash(combined);");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static byte[] AesDecrypt(byte[] data, byte[] key, byte[] iv)");
        code.AppendLine("    {");
        code.AppendLine("        using (AesManaged aes = new AesManaged())");
        code.AppendLine("        {");
        code.AppendLine("            aes.KeySize = 256;");
        code.AppendLine("            aes.BlockSize = 128;");
        code.AppendLine("            aes.Mode = CipherMode.CBC;");
        code.AppendLine("            aes.Padding = PaddingMode.PKCS7;");
        code.AppendLine("            aes.Key = key;");
        code.AppendLine("            aes.IV = iv;");
        code.AppendLine("");
        code.AppendLine("            using (ICryptoTransform decryptor = aes.CreateDecryptor())");
        code.AppendLine("            {");
        code.AppendLine("                return decryptor.TransformFinalBlock(data, 0, data.Length);");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static void HidePathIfPossible(string path)");
        code.AppendLine("    {");
        code.AppendLine("        try");
        code.AppendLine("        {");
        code.AppendLine("            if (File.Exists(path) || Directory.Exists(path))");
        code.AppendLine("            {");
        code.AppendLine("                FileAttributes attributes = File.GetAttributes(path);");
        code.AppendLine("                File.SetAttributes(path, attributes | FileAttributes.Hidden | FileAttributes.Temporary);");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine("        catch");
        code.AppendLine("        {");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static void SecureDeleteFile(string filePath)");
        code.AppendLine("    {");
        code.AppendLine("        try");
        code.AppendLine("        {");
        code.AppendLine("            if (!File.Exists(filePath))");
        code.AppendLine("            {");
        code.AppendLine("                return;");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            File.SetAttributes(filePath, FileAttributes.Normal);");
        code.AppendLine("            long length = new FileInfo(filePath).Length;");
        code.AppendLine("");
        code.AppendLine("            if (length > 0)");
        code.AppendLine("            {");
        code.AppendLine("                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None))");
        code.AppendLine("                {");
        code.AppendLine("                    byte[] buffer = new byte[8192];");
        code.AppendLine("                    long remaining = length;");
        code.AppendLine("");
        code.AppendLine("                    while (remaining > 0)");
        code.AppendLine("                    {");
        code.AppendLine("                        int writeCount = (int)Math.Min(buffer.Length, remaining);");
        code.AppendLine("                        stream.Write(buffer, 0, writeCount);");
        code.AppendLine("                        remaining -= writeCount;");
        code.AppendLine("                    }");
        code.AppendLine("");
        code.AppendLine("                    stream.Flush();");
        code.AppendLine("                }");
        code.AppendLine("            }");
        code.AppendLine("");
        code.AppendLine("            File.Delete(filePath);");
        code.AppendLine("        }");
        code.AppendLine("        catch");
        code.AppendLine("        {");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    private static void TryDeleteDirectory(string folder)");
        code.AppendLine("    {");
        code.AppendLine("        for (int attempt = 0; attempt < 5; attempt++)");
        code.AppendLine("        {");
        code.AppendLine("            try");
        code.AppendLine("            {");
        code.AppendLine("                if (!Directory.Exists(folder))");
        code.AppendLine("                {");
        code.AppendLine("                    return;");
        code.AppendLine("                }");
        code.AppendLine("");
        code.AppendLine("                string[] files = Directory.GetFiles(folder, \"*\", SearchOption.AllDirectories);");
        code.AppendLine("                for (int i = 0; i < files.Length; i++)");
        code.AppendLine("                {");
        code.AppendLine("                    SecureDeleteFile(files[i]);");
        code.AppendLine("                }");
        code.AppendLine("");
        code.AppendLine("                Directory.Delete(folder, true);");
        code.AppendLine("                return;");
        code.AppendLine("            }");
        code.AppendLine("            catch");
        code.AppendLine("            {");
        code.AppendLine("                Thread.Sleep(120);");
        code.AppendLine("            }");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("}");
        code.AppendLine("");
        code.AppendLine("internal sealed class PasswordDialog : Form");
        code.AppendLine("{");
        code.AppendLine("    private readonly TextBox passwordTextBox = new TextBox();");
        code.AppendLine("    public string Password");
        code.AppendLine("    {");
        code.AppendLine("        get { return passwordTextBox.Text; }");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    public PasswordDialog()");
        code.AppendLine("    {");
        code.AppendLine("        Text = \"需要密码\";");
        code.AppendLine("        Width = 360;");
        code.AppendLine("        Height = 150;");
        code.AppendLine("        FormBorderStyle = FormBorderStyle.FixedDialog;");
        code.AppendLine("        StartPosition = FormStartPosition.CenterScreen;");
        code.AppendLine("        MaximizeBox = false;");
        code.AppendLine("        MinimizeBox = false;");
        code.AppendLine("        TopMost = true;");
        code.AppendLine("        Font = new Font(\"Microsoft YaHei UI\", 9F);");
        code.AppendLine("");
        code.AppendLine("        Label label = new Label();");
        code.AppendLine("        label.Text = \"请输入运行密码：\";");
        code.AppendLine("        label.Left = 16;");
        code.AppendLine("        label.Top = 16;");
        code.AppendLine("        label.Width = 300;");
        code.AppendLine("        Controls.Add(label);");
        code.AppendLine("");
        code.AppendLine("        passwordTextBox.Left = 16;");
        code.AppendLine("        passwordTextBox.Top = 42;");
        code.AppendLine("        passwordTextBox.Width = 310;");
        code.AppendLine("        passwordTextBox.UseSystemPasswordChar = true;");
        code.AppendLine("        Controls.Add(passwordTextBox);");
        code.AppendLine("");
        code.AppendLine("        Button okButton = new Button();");
        code.AppendLine("        okButton.Text = \"确定\";");
        code.AppendLine("        okButton.Left = 166;");
        code.AppendLine("        okButton.Top = 78;");
        code.AppendLine("        okButton.Width = 75;");
        code.AppendLine("        okButton.DialogResult = DialogResult.OK;");
        code.AppendLine("        Controls.Add(okButton);");
        code.AppendLine("");
        code.AppendLine("        Button cancelButton = new Button();");
        code.AppendLine("        cancelButton.Text = \"取消\";");
        code.AppendLine("        cancelButton.Left = 251;");
        code.AppendLine("        cancelButton.Top = 78;");
        code.AppendLine("        cancelButton.Width = 75;");
        code.AppendLine("        cancelButton.DialogResult = DialogResult.Cancel;");
        code.AppendLine("        Controls.Add(cancelButton);");
        code.AppendLine("");
        code.AppendLine("        AcceptButton = okButton;");
        code.AppendLine("        CancelButton = cancelButton;");
        code.AppendLine("    }");
        code.AppendLine("");
        code.AppendLine("    public static string Ask()");
        code.AppendLine("    {");
        code.AppendLine("        using (PasswordDialog dialog = new PasswordDialog())");
        code.AppendLine("        {");
        code.AppendLine("            return dialog.ShowDialog() == DialogResult.OK ? dialog.Password : null;");
        code.AppendLine("        }");
        code.AppendLine("    }");
        code.AppendLine("}");

        return code.ToString();
    }

    private static string BuildManifest(bool requireAdmin)
    {
        string level = requireAdmin ? "requireAdministrator" : "asInvoker";
        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
               "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">\n" +
               "  <trustInfo xmlns=\"urn:schemas-microsoft-com:asm.v3\">\n" +
               "    <security>\n" +
               "      <requestedPrivileges>\n" +
               "        <requestedExecutionLevel level=\"" + level + "\" uiAccess=\"false\" />\n" +
               "      </requestedPrivileges>\n" +
               "    </security>\n" +
               "  </trustInfo>\n" +
               "</assembly>\n";
    }

    private static void AppendStringChunks(StringBuilder code, string value, int chunkSize)
    {
        for (int offset = 0; offset < value.Length; offset += chunkSize)
        {
            int count = Math.Min(chunkSize, value.Length - offset);
            string chunk = value.Substring(offset, count);
            code.AppendLine("        " + CsString(chunk) + (offset + count < value.Length ? "," : ""));
        }
    }

    private static PayloadFile[] CollectExtraFiles(BuildConfig config, string outputExe)
    {
        if (string.IsNullOrWhiteSpace(config.ExtraFolder))
        {
            return new PayloadFile[0];
        }

        string extraRoot = NormalizeFolder(config.ExtraFolder);
        string outputFolder = NormalizeFolder(config.OutputFolder);
        string batPath = Path.GetFullPath(config.BatPath);
        string outputPath = Path.GetFullPath(outputExe);
        bool outputFolderIsInsideExtraRoot = !SamePath(extraRoot, outputFolder) && IsPathInsideFolder(outputFolder, extraRoot);

        List<PayloadFile> files = new List<PayloadFile>();
        string[] sourceFiles = Directory.GetFiles(extraRoot, "*", SearchOption.AllDirectories);

        for (int i = 0; i < sourceFiles.Length; i++)
        {
            string sourcePath = Path.GetFullPath(sourceFiles[i]);

            if (SamePath(sourcePath, batPath) || SamePath(sourcePath, outputPath))
            {
                continue;
            }

            if (outputFolderIsInsideExtraRoot && IsPathInsideFolder(sourcePath, outputFolder))
            {
                continue;
            }

            string relativePath = MakeRelativePath(extraRoot, sourcePath);
            if (relativePath.Length == 0)
            {
                continue;
            }

            PayloadFile file = new PayloadFile();
            file.SourcePath = sourcePath;
            file.RelativePath = relativePath;
            files.Add(file);
        }

        return files.ToArray();
    }

    private static void PrepareExtraResources(PayloadFile[] files, string tempFolder, byte[] key)
    {
        for (int i = 0; i < files.Length; i++)
        {
            byte[] plainBytes;
            try
            {
                plainBytes = File.ReadAllBytes(files[i].SourcePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("读取附加文件失败：\n" + files[i].SourcePath + "\n\n请确认这个文件没有被其它程序占用，然后再试一次。", ex);
            }

            byte[] fileIv = RandomBytes(16);
            byte[] encryptedBytes;
            try
            {
                encryptedBytes = AesEncrypt(plainBytes, key, fileIv);
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
            }

            files[i].ResourceName = "bat2exe.extra." + i.ToString("000000") + ".bin";
            files[i].IvBase64 = Convert.ToBase64String(fileIv);
            files[i].EncryptedPath = Path.Combine(tempFolder, files[i].ResourceName);
            File.WriteAllBytes(files[i].EncryptedPath, encryptedBytes);
        }
    }

    private static string MakeRelativePath(string rootFolder, string fullPath)
    {
        string root = NormalizeFolder(rootFolder) + Path.DirectorySeparatorChar;
        string normalizedPath = Path.GetFullPath(fullPath);

        if (!normalizedPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        return normalizedPath.Substring(root.Length);
    }

    private static string NormalizeFolder(string folder)
    {
        string path = Path.GetFullPath(folder);
        string root = Path.GetPathRoot(path);

        while (path.Length > root.Length &&
            (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.AltDirectorySeparatorChar))
        {
            path = path.Substring(0, path.Length - 1);
        }

        return path;
    }

    private static bool SamePath(string left, string right)
    {
        return string.Equals(NormalizeFolder(left), NormalizeFolder(right), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPathInsideFolder(string path, string folder)
    {
        string normalizedFolder = NormalizeFolder(folder) + Path.DirectorySeparatorChar;
        string normalizedPath = Path.GetFullPath(path);
        return normalizedPath.StartsWith(normalizedFolder, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        // PBKDF2：可以理解成“把密码反复搅拌很多次”，让别人更难暴力猜密码。
        using (Rfc2898DeriveBytes kdf = new Rfc2898DeriveBytes(password, salt, 120000))
        {
            return kdf.GetBytes(32);
        }
    }

    private static byte[] AesEncrypt(byte[] data, byte[] key, byte[] iv)
    {
        // AES：常见的对称加密算法，可以理解成“同一把钥匙负责上锁和开锁”。
        using (AesManaged aes = new AesManaged())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                return encryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }
    }

    private static byte[] MakeVerifierBytes(byte[] key)
    {
        byte[] marker = Encoding.UTF8.GetBytes("bat2exe-password-check");
        byte[] combined = new byte[key.Length + marker.Length];
        Buffer.BlockCopy(key, 0, combined, 0, key.Length);
        Buffer.BlockCopy(marker, 0, combined, key.Length, marker.Length);

        using (SHA256 sha = SHA256.Create())
        {
            return sha.ComputeHash(combined);
        }
    }

    private static byte[] RandomBytes(int length)
    {
        byte[] data = new byte[length];
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(data);
        }
        return data;
    }

    private static string MakeSafeFileName(string rawName)
    {
        string name = rawName == null ? "" : rawName.Trim();

        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - 4);
        }

        name = Regex.Replace(name, "[\\\\/:*?\"<>|]+", "_");
        return string.IsNullOrWhiteSpace(name) ? "converted_bat" : name;
    }

    private static bool IsSafeFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return false;
        }

        string name = folderName.Trim();
        if (name == "." || name == "..")
        {
            return false;
        }

        if (name.IndexOf("..", StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        if (name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal))
        {
            return false;
        }

        if (Path.IsPathRooted(name) || name.IndexOf(Path.DirectorySeparatorChar) >= 0 || name.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
        {
            return false;
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        for (int i = 0; i < invalidChars.Length; i++)
        {
            if (name.IndexOf(invalidChars[i]) >= 0)
            {
                return false;
            }
        }

        for (int i = 0; i < name.Length; i++)
        {
            char ch = name[i];
            if (!char.IsLetterOrDigit(ch) && ch != ' ' && ch != '.' && ch != '-' && ch != '_')
            {
                return false;
            }
        }

        return true;
    }

    private static string FindCscPath()
    {
        string runtimeCsc = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "csc.exe");
        if (File.Exists(runtimeCsc))
        {
            return runtimeCsc;
        }

        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string[] candidates = new string[]
        {
            Path.Combine(windows, "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe"),
            Path.Combine(windows, "Microsoft.NET", "Framework", "v4.0.30319", "csc.exe"),
            Path.Combine(windows, "Microsoft.NET", "Framework64", "v3.5", "csc.exe"),
            Path.Combine(windows, "Microsoft.NET", "Framework", "v3.5", "csc.exe")
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
            {
                return candidates[i];
            }
        }

        return null;
    }

    private static string QuoteForCompiler(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static string CsString(string value)
    {
        if (value == null)
        {
            return "\"\"";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append('"');

        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];

            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (ch < 32)
                    {
                        builder.Append("\\u");
                        builder.Append(((int)ch).ToString("x4"));
                    }
                    else
                    {
                        builder.Append(ch);
                    }
                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static void TryDeleteDirectory(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
        catch
        {
        }
    }

    private static void try_delete_file(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private void SetBusy(bool busy)
    {
        buildButton.Enabled = !busy;
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(SetStatus), text);
            return;
        }

        statusLabel.Text = text;
    }

    private sealed class BuildConfig
    {
        public string BatPath;
        public string ExtraFolder;
        public string OutputFolder;
        public string ExeName;
        public string Password;
        public string IconPath;
        public bool HideCommandWindow;
        public bool RequireAdmin;
        public bool PersistRuntimeFolder;
        public string RuntimeFolderName;
        public bool OpenFolderAfterBuild;
    }

    private sealed class PayloadFile
    {
        public string SourcePath;
        public string RelativePath;
        public string ResourceName;
        public string IvBase64;
        public string EncryptedPath;
    }
}
