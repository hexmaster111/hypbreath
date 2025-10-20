using System;
using System.IO.Ports;
using System.Text;


namespace hypbreath;

/*
    A data bridge is a class that provides thread safe access to data
    from *some* other device or network connection. 

*/

// public class HeartRateDataBridge
// {
//     public static HeartRateDataBridge Instance = new();
//     public float HeartRate;
// }

public class RespirationDataBridge
{
    public static RespirationDataBridge Instance = new();

    public float Volume { get; set; }
    public float Flow { get; set; }
    public int Raw { get; set; }

    public bool Connected => Port != null && Port.IsOpen;

    public SerialPort Port;
    public Thread Reader;

    public void Close()
    {
        Port.Close();
        Port = null;
    }

    public void Connect(string comport)
    {
        if (Port != null && Port.IsOpen)
        {
            Close();
        }

        Port = new SerialPort(comport);
        Port.BaudRate = 115200;
        Port.Open();
        Reader = new Thread(ThreadMain);
        Reader.IsBackground = true;
        Reader.Start();
    }

    public void ThreadMain()
    {
        while (Port.IsOpen)
        {
            string s = Port.ReadLine();
            ProcessDataLine(s);
        }
    }

    // | Data Ident String | Value usage   |
    // | ----------------- | ------------- |
    // | "RV 32740"        | Raw Value     | -- dont really need
    // | "NF -0.01"        | Normal Flow   | -- 
    // | "NV 0.00"         | Normal Volume | -- totalised  
    private void ProcessDataLine(string s)
    {
        if (s.StartsWith("RV "))
        {
            if (int.TryParse(s[3..], out var t)) Raw = t;
        }
        else if (s.StartsWith("NF "))
        {
            if (float.TryParse(s[3..], out var t)) Flow = t;
        }
        else if (s.StartsWith("NV "))
        {
            if (float.TryParse(s[3..], out var t)) Volume = t;
        }
        else
        {
            Console.WriteLine($"Unknown Data: {s}");
        }
    }
}
