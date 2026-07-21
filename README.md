# Bat2exe

Bat2exe 是一个 Windows 桌面工具，可将 `.bat` 或 `.cmd` 脚本打包为单个 `.exe` 文件。

## 功能

- 将 BAT/CMD 脚本生成独立 EXE
- 可将附加文件夹一并打包
- 支持设置运行密码和 ICO 图标
- 支持隐藏命令窗口和请求管理员权限
- 可选择临时释放或持久化运行文件夹

## 运行

直接双击 `Bat2exe.exe`，选择脚本并设置输出目录，然后点击“生成 EXE”。默认输出到程序当前目录下的 `dist` 文件夹。

## 编译

项目使用 Windows 11 自带的 .NET Framework C# 编译器，无需安装 Visual Studio 或第三方依赖：

```bat
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:Bat2exe.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll Bat2exe.cs
```

## 环境要求

- Windows 11
- .NET Framework 4.x

## 自动发布

推送 `v*` 格式的 Git 标签后，GitHub Actions 会自动编译程序、打包 `Bat2exe-版本-windows.zip`，并创建对应的 GitHub Release。

```powershell
git tag v0.2.0
git push origin v0.2.0
```

当前版本：`v0.2.0`
