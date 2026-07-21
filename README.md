# Bat2exe

![Bat2exe 图标](ico.png)

Bat2exe 是一个 Windows 桌面工具，可将 `.bat` 或 `.cmd` 脚本打包为单个 `.exe` 文件。

## 功能

- 将 BAT/CMD 脚本生成独立 EXE
- 可将附加文件夹一并打包
- 支持设置运行密码和 ICO、PNG、JPG、JPEG、BMP、GIF 正方形程序图标
- 支持隐藏命令窗口和请求管理员权限
- 可选择临时释放或持久化运行文件夹

## 运行

直接双击 `Bat2exe.exe`，选择脚本并设置输出目录，然后点击“生成 EXE”。默认输出到程序当前目录下的 `dist` 文件夹。
<img width="646" height="493" alt="ScreenShot_2026-07-21_151748_660" src="https://github.com/user-attachments/assets/5caba992-fa95-4675-af1b-6521f56b4d8a" />


## 编译

项目使用 Windows 11 自带的 .NET Framework C# 编译器，无需安装 Visual Studio 或第三方依赖：

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:Bat2exe.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /win32icon:bat2exe.ico /resource:bat2exe.ico,Bat2exe.Icon Bat2exe.cs
```

## 环境要求

- Windows 11
- .NET Framework 4.x

## 自动发布

推送 `v*` 格式的 Git 标签后，GitHub Actions 会自动编译程序，并将 `Bat2exe.exe` 作为唯一附件发布到对应的 GitHub Release。

```powershell
git tag v0.3.5
git push origin v0.3.5
```

当前版本：`v0.3.5`
