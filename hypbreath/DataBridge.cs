using System;
using System.IO.Ports;
using System.Text;


namespace hypbreath;

/*
    A data bridge is a class that provides thread safe access to data
    from *some* other device or network connection. 

*/

public class HeartRateDataBridge
{
    public static HeartRateDataBridge Instance = new();
    public int Ir { get; set; }
    public int Red { get; set; }
    public float Temp { get; set; }
    public float Cr { get; set; }


    public int IrIndex;
    public float[] IrHistory = new float[100];
    public float IrAvgMin => IrHistory.Min();
    public float IrAvgMax => IrHistory.Max();



    public int RedIndex;
    public float[] RedHistory = new float[100];
    public float RedAvgMin => RedHistory.Min();
    public float RedAvgMax => RedHistory.Max();



    public bool Connected => Port != null && Port.IsOpen;

    public float HeartRate { get; private set; }


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


    // ID 30876
    // RD 25092

    // | Data Ident String | Value usage   |
    // | ----------------- | ------------- |
    // | "ID 30876"        | Raw Value     | -- 
    // | "RD 25092"        | Normal Flow   | -- 
    // | "TP 73.580002"    |               |
    // | "CR 0.600555"     |               |
    // | "ND"              |               |

    private void ProcessDataLine(string s)
    {
        if (s.StartsWith("ID "))
        {
            if (int.TryParse(s[3..], out var t))
            {
                Ir = t;
                IrHistory[IrIndex] = Ir = t;
                IrIndex += 1;
                IrIndex %= 100;
            }

        }
        else if (s.StartsWith("RD "))
        {
            if (int.TryParse(s[3..], out var t))
            {
                RedHistory[RedIndex] = Red = t;
                RedIndex += 1;
                RedIndex %= 100;
            }

        }
        else if (s.StartsWith("TP "))
        {
            if (float.TryParse(s[3..], out var t)) Temp = t;
        }
        else if (s.StartsWith("HR "))
        {
            if (float.TryParse(s[3..], out var t)) HeartRate = t;
        }
        else if (s.StartsWith("ND"))
        {
            HeartRate = 0;
        }
        else if (s.StartsWith("CR "))
        {
            if (int.TryParse(s[3..], out var t)) Cr = t;
        }
        else
        {
            Console.WriteLine($"Unknown Data: {s}");
        }
    }



}



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
