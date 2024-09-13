namespace Common.Client.Logger;

public interface ILogger
{
    string LogFile { get; init; }

    void Error(string message);
    void Info(string message);
}