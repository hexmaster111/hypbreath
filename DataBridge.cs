using System;

namespace hypbreath;


/*
    A data bridge is a class that provides thread safe access to data
    from *some* other device or network connection. 

*/

public class HeartRateDataBridge
{
    public static HeartRateDataBridge Instance = new();

    public float[] IRValue = [], GreenValue = [];
    public float HeartRate;
}

public class RespirationDataBridge
{
    public static RespirationDataBridge Instance = new();
}
