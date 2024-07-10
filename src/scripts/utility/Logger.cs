using System;
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

    private static void Print(LogLevel level, string message, LogColor color = LogColor.white)
    {
        if (!OS.IsDebugBuild()) return;
        string levelName = Enum.GetName(typeof(LogLevel), level) ?? "Unknown";
        string colorName = Enum.GetName(typeof(LogColor), color) ?? "white";
        GD.PrintRich($"[color={colorName}]{levelName}: {message}[/color]");
    }

    public static void Debug(string message) => Print(LogLevel.Debug, message, LogColor.white);
    public static void Info(string message) => Print(LogLevel.Info, message, LogColor.green);
    public static void Warn(string message) => Print(LogLevel.Warn, message, LogColor.yellow);
    public static void Error(string message)
    {
        Print(LogLevel.Error, message, LogColor.red);
        GD.PushError(message);
    }
}