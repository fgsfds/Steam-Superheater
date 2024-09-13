namespace Common.Client.Logger;

public sealed class LoggerToConsole : ILogger
{
    public string LogFile { get => throw new NotImplementedException(); init => throw new NotImplementedException(); }

    public void Error(string message)
    {
        //Debug.WriteLine($"[Error] {message}");
    }

    public void Info(string message)
    {
        //Debug.WriteLine($"[Info] {message}");
    }
}
