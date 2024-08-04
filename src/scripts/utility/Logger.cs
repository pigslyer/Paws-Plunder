using System;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

namespace PawsPlunder;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

public static class Logger
{
    private enum LogColor
    {
        white,
        green,
        yellow,
        red
    }

    private static void Print(LogLevel level, string message, LogColor color = LogColor.white, string filePath = "", int lineNumber = -1)
    {
        if (!OS.IsDebugBuild()) return;
        string levelName = Enum.GetName(typeof(LogLevel), level) ?? "Unknown";
        string colorName = Enum.GetName(typeof(LogColor), color) ?? "white";
        string fileName = Path.GetFileName(filePath);
        GD.PrintRich($"[color={colorName}][{levelName}] [{fileName}:{lineNumber}]: {message}[/color]");
    }

    public static void Debug<T>(
        T message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1) =>
        Print(LogLevel.Debug, message?.ToString() ?? "", LogColor.white, filePath, lineNumber);
    public static void Info<T>(
        T message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1) =>
        Print(LogLevel.Info, message?.ToString() ?? "", LogColor.green, filePath, lineNumber);
    public static void Warn<T>(
        T message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1) =>
        Print(LogLevel.Warn, message?.ToString() ?? "", LogColor.yellow, filePath, lineNumber);
    // GD.PushWarning(message) - idk if needed yet
    public static void Err<T>(
        T message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = -1)
    {
        Print(LogLevel.Error, message?.ToString() ?? "", LogColor.red, filePath, lineNumber);
        GD.PushError(message);
    }
}
