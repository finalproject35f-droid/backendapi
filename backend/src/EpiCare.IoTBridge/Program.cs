using System.IO.Ports;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

var options = BridgeOptions.FromArgs(args);

Console.WriteLine("EpiCare IoT Bridge");
Console.WriteLine($"Serial Port : {options.PortName}");
Console.WriteLine($"Baud Rate   : {options.BaudRate}");
Console.WriteLine($"Backend URL : {options.BackendBaseUrl}");
Console.WriteLine($"Patient ID  : {options.PatientId}");
Console.WriteLine();

using var httpClient = new HttpClient
{
    BaseAddress = new Uri(options.BackendBaseUrl),
    Timeout = TimeSpan.FromSeconds(15)
};

using var serialPort = new SerialPort(options.PortName, options.BaudRate)
{
    NewLine = "\n",
    ReadTimeout = 1000
};

serialPort.Open();
Console.WriteLine("Serial port opened. Waiting for Proteus readings...");

while (true)
{
    try
    {
        var rawLine = serialPort.ReadLine().Trim();
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            continue;
        }

        var payload = BuildPayload(rawLine, options);
        var response = await httpClient.PostAsJsonAsync("/api/iot/readings", payload);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Backend rejected reading: {(int)response.StatusCode} {responseBody}");
            Console.ResetColor();
            continue;
        }

        var command = TryReadDeviceCommand(responseBody);
        Console.ForegroundColor = command switch
        {
            "S" or "SEIZURE" => ConsoleColor.Red,
            "P" or "WARNING" => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };
        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} -> {command} | {rawLine}");
        Console.ResetColor();

        if (options.WriteCommandBackToSerial && command is not null)
        {
            serialPort.WriteLine(command);
        }
    }
    catch (TimeoutException)
    {
        // Serial read timeout is normal when no sample is available yet.
    }
    catch (JsonException exception)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Invalid JSON from serial: {exception.Message}");
        Console.ResetColor();
    }
    catch (Exception exception)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(exception);
        Console.ResetColor();
        await Task.Delay(1000);
    }
}

static JsonObject BuildPayload(string rawLine, BridgeOptions options)
{
    var node = JsonNode.Parse(rawLine) as JsonObject
        ?? throw new JsonException("Serial payload must be a JSON object.");

    node["patientId"] = options.PatientId;
    node["deviceId"] = options.DeviceId;
    node["capturedAt"] = DateTimeOffset.UtcNow;

    if (node["ecg"] is JsonValue ecgValue && ecgValue.TryGetValue<double>(out var ecg))
    {
        node["ecg"] = ecg;
    }

    if (node["emg"] is JsonValue emgValue && emgValue.TryGetValue<double>(out var emg))
    {
        node["emg"] = emg;
    }

    return node;
}

static string? TryReadDeviceCommand(string responseBody)
{
    try
    {
        var obj = JsonNode.Parse(responseBody) as JsonObject;
        return obj?["deviceCommand"]?.ToString();
    }
    catch
    {
        return null;
    }
}

internal sealed record BridgeOptions(
    string PortName,
    int BaudRate,
    string BackendBaseUrl,
    string PatientId,
    string DeviceId,
    bool WriteCommandBackToSerial)
{
    public static BridgeOptions FromArgs(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                values[args[i][2..]] = args[i + 1];
                i++;
            }
        }

        var portName = Get(values, "port", "EPICARE_SERIAL_PORT", "COM3");
        var backend = Get(values, "backend", "EPICARE_BACKEND_URL", "http://localhost:8080");
        var patientId = Get(values, "patient", "EPICARE_PATIENT_ID", "demo-patient");
        var deviceId = Get(values, "device", "EPICARE_DEVICE_ID", "proteus-simulator");
        var baud = int.TryParse(Get(values, "baud", "EPICARE_BAUD_RATE", "115200"), out var parsedBaud)
            ? parsedBaud
            : 115200;
        var writeBack = bool.TryParse(Get(values, "write-command", "EPICARE_WRITE_COMMAND_BACK", "false"), out var parsedWriteBack)
            && parsedWriteBack;

        return new BridgeOptions(portName, baud, backend.TrimEnd('/'), patientId, deviceId, writeBack);
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string argName,
        string envName,
        string fallback)
    {
        if (values.TryGetValue(argName, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return Environment.GetEnvironmentVariable(envName) ?? fallback;
    }
}
