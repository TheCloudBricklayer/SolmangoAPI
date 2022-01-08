﻿using HandierCli;

namespace SolmangoAPI.Statics;

public static class Error
{
    public static void Fatal(string message, int exitCode = 1)
    {
        Logger.ConsoleInstance.LogError($"FATAL | {message}");
        Console.ReadKey();
        Environment.Exit(exitCode);
    }
}