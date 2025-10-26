using System.Numerics;
using System.Text;
using System.Xml;
using hypbreath;
using Newtonsoft.Json;
using Raylib_CsLo;
using static Raylib_CsLo.RayGui;
using static Raylib_CsLo.Raylib;
using static Raylib_CsLo.RayMath;
using Rectangle = Raylib_CsLo.Rectangle;



try
{
    RespirationDataBridge.Instance.Connect("/dev/ttyUSB0");
}
catch (Exception ex)
{
    Console.WriteLine($"RespirationDataBridge Connect Error: {ex}");
}


try
{
    HeartRateDataBridge.Instance.Connect("/dev/ttyACM0");
}
catch (Exception ex)
{
    Console.WriteLine($"HeartRateDataBridge Connect Error: {ex}");
}



SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
InitWindow(800, 800, "HypRender");
SetTargetFPS(GetMonitorRefreshRate(GetCurrentMonitor()));

bool showGameConfig = false, showHardwareStatus = false;

float sizeMul = 150;

// value off the sensor nomralised between 0 - 1 for min breath and max
float breth = .24f;
float maxBreth = .45f;
float minBreth = .1f;

// what we are trying to drive the users breathing to % from min to max
float desiredBreath = 0;
BreathingDirection desiredBreathingDirection = BreathingDirection.In;

// values that control the animation
float outerCircleAmount = 0;
float innerCircleAmount = 0;

// logic mode control
Mode mode = Mode.UserMeasureMode;

// tunable params
float inHoldDwellTime = 3.5f;
float outHoldDwellTime = 3.5f;

// brething process value
float stopHoldWaitAt = 0;


//the direction that we think the user is going right now
BreathingDirection currentDirection = BreathingDirection.Hold;
float guessWait = .125f;
float directionGuess = 0; // 0 = no idea -1 = out +1 = in
float nextBrethDirectionGuessTime = 0;
int guessCount = 0;
int determinAtThisManyGuesses = 5;
float lastBrethValue = 0;


// Syncronisicity timer. Computed peroticly
float nextSyncCheck = 0;
const float syncCheckPeriod = .125f;
float lowSync = .05f;
float midSync = .02f;
float highSync = .001f;
const int lowSyncScore = 1;
const int midSyncScore = 5;
const int highSyncScore = 10;
int syncScore = 0;

///////// Breathing Measured params 
// should be measured off of a rolling avg of subject peek breathing
float breathingVolume = 0;


if (File.Exists("default.config")) Load("default.config");

while (!WindowShouldClose())
{

    if (RespirationDataBridge.Instance.Connected)
    {
        breth = Remap(RespirationDataBridge.Instance.Volume, 0, breathingVolume, 0, 1);
        if (RespirationDataBridge.Instance.Volume > breathingVolume)
        {
            breathingVolume = RespirationDataBridge.Instance.Volume;
        }
    }

    if (maxBreth > 1) { maxBreth = 1; }


    if (GetTime() > nextSyncCheck)
    {
        nextSyncCheck = (float)(GetTime() + syncCheckPeriod);

        if (mode == Mode.ProgramControl)
        {
            float wantedVsNowDiff = MathF.Abs(desiredBreath - breth);
            int dir = Math.Sign(desiredBreath - breth);

            if (highSync > wantedVsNowDiff)
            {
                syncScore += highSyncScore * dir;
            }
            else if (midSync > wantedVsNowDiff)
            {
                syncScore += midSyncScore * dir;
            }
            else if (lowSync > wantedVsNowDiff)
            {
                syncScore += lowSyncScore * dir;
            }

            if (0 > syncScore) syncScore = 0;

        }
    }
    string syncDebug = $"{(desiredBreath - breth):00.0000}";


    if (GetTime() > nextBrethDirectionGuessTime)
    {
        nextBrethDirectionGuessTime = (float)(GetTime() + guessWait);

        bool determinable = false;

        if (breth > lastBrethValue)
        {
            determinable = true;
            directionGuess += .1f;
        }

        if (lastBrethValue > breth)
        {
            determinable = true;
            directionGuess -= .1f;
        }

        if (determinable)
        {
        }
        lastBrethValue = breth;
        guessCount += 1;

        if (guessCount > determinAtThisManyGuesses)
        {
            if (0 > directionGuess) currentDirection = BreathingDirection.Out;
            else if (0 < directionGuess) currentDirection = BreathingDirection.In;
            else currentDirection = BreathingDirection.Hold;

            directionGuess = 0;
            guessCount = 0;

        }
    }

    string guessdebug = $"{directionGuess:.00} {guessCount} {currentDirection}";


    var lastMode = mode;

    if (IsKeyPressed(KeyboardKey.KEY_R))
    {
        mode = 0;
        maxBreth = 0;
        minBreth = 1;

    }

    if (IsKeyPressed(KeyboardKey.KEY_C))
    {
        showGameConfig = !showGameConfig;
    }

    if (IsKeyPressed(KeyboardKey.KEY_V))
    {
        showHardwareStatus = !showHardwareStatus;
    }

    if (IsKeyPressed(KeyboardKey.KEY_UP)) mode += 1;
    if (IsKeyPressed(KeyboardKey.KEY_DOWN)) mode -= 1;

    if (lastMode != mode)
    {
        switch (mode)
        {
            case Mode.UserMeasureMode:
                break;
            case Mode.ProgramControl:
                desiredBreath = breth;
                break;
        }
    }


    if (mode == Mode.ProgramControl)
    {
        // show the user where we want them to be in the cycle

        if (desiredBreathingDirection is BreathingDirection.InHold or BreathingDirection.OutHold)
        {
            if (GetTime() > stopHoldWaitAt)
            {
                if (desiredBreathingDirection == BreathingDirection.InHold) desiredBreathingDirection = BreathingDirection.Out;
                if (desiredBreathingDirection == BreathingDirection.OutHold) desiredBreathingDirection = BreathingDirection.In;
            }
        }
        else
        {
            float ammount = GetFrameTime() * .1f;

            if (desiredBreathingDirection == BreathingDirection.Out) ammount = -ammount;

            desiredBreath += ammount;

            if (desiredBreath > maxBreth)
            {
                desiredBreathingDirection = BreathingDirection.InHold;
                stopHoldWaitAt = (float)(GetTime() + inHoldDwellTime);
                desiredBreath = maxBreth;
            }
            else if (minBreth > desiredBreath)
            {
                desiredBreathingDirection = BreathingDirection.OutHold;
                stopHoldWaitAt = (float)(GetTime() + outHoldDwellTime);
                desiredBreath = minBreth;
            }
        }


        outerCircleAmount = desiredBreath; // 0 -> max_breath
                                           // show the user where they are
        innerCircleAmount = breth;

        // hack to make it move with the program for testing the guessing machine
        // breth = outerCircleAmount;
    }

    if (mode == Mode.UserMeasureMode)
    {
        if (breth > maxBreth)
        {
            maxBreth = breth;
        }

        if (breth < minBreth)
        {
            minBreth = breth;
        }

        outerCircleAmount = maxBreth;
        innerCircleAmount = breth;
    }

    Vector2 center = new Vector2(GetScreenWidth() * .5f, GetScreenHeight() * .5f);
    BeginDrawing();
    ClearBackground(BLACK);



    // DrawCircleV(center, max_breth * mul, DARKGREEN);
    DrawCircleV(center, innerCircleAmount * sizeMul, YELLOW);

    DrawRingLines(center, (outerCircleAmount * sizeMul), (outerCircleAmount * sizeMul) + 1, 0, 360, 300, LIGHTGRAY);
    DrawRingLines(center, (maxBreth * sizeMul), (maxBreth * sizeMul) + 1, 0, 360, 300, LIGHTGRAY with { a = 128 });
    DrawRingLines(center, (minBreth * sizeMul), (minBreth * sizeMul) + 1, 0, 360, 300, LIGHTGRAY with { a = 128 });

    DrawText($"{mode} {desiredBreathingDirection}\n{guessdebug}\n{syncDebug}", 0, GetScreenHeight() - (24 * 5), 24, GREEN);


    string syncScoreText = $"{syncScore:000000}";
    DrawText(syncScoreText, GetScreenWidth() - 100, 0, 12, YELLOW);



    if (showGameConfig)
    {
        int i = 0;
        GuiLabel(new Rectangle(0, 32 * i++, 400, 32), "Tidal Breath (Normlised)");
        breth = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"now {breth:.0000}", breth, 0, 1);
        minBreth = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"min {minBreth:.0000}", minBreth, 0, 1);
        maxBreth = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"max {maxBreth:.0000}", maxBreth, 0, 1);
        desiredBreath = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"des {desiredBreath:.0000}", desiredBreath, 0, 1);
        GuiLabel(new Rectangle(0, 32 * i++, 400, 32), "Sizing");
        sizeMul = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"siz {sizeMul:.0000}", sizeMul, 1, 1000);

        GuiLabel(new Rectangle(0, 32 * i++, 400, 32), "Box Breathing Params");
        inHoldDwellTime = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"ihdt {inHoldDwellTime:.0000}", inHoldDwellTime, 0, 15);
        outHoldDwellTime = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"ihdt {outHoldDwellTime:.0000}", outHoldDwellTime, 0, 15);

        GuiLabel(new Rectangle(0, 32 * i++, 400, 32), "sync totaling params`");
        lowSync = GuiSlider(new Rectangle(0, 32 * i, 400 / 3, 32), "", "", lowSync, 0, .1f);
        midSync = GuiSlider(new Rectangle((400 / 3) * 1, 32 * i, 400 / 3, 32), "", "", midSync, 0, .1f);
        highSync = GuiSlider(new Rectangle((400 / 3) * 2, 32 * i, 400 / 3, 32), "",
        $"L M H {lowSync:0.000} {midSync:0.000} {highSync:0.000}", highSync, 0, .1f);
        i += 1;


        breathingVolume = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"ihdt {breathingVolume:.0000}", breathingVolume, 0, 20);


    }

    if (showHardwareStatus)
    {
        int i = 0;

        GuiLabel(new Rectangle(0, 32 * i++, 400, 32), "Lungs");
        GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"{RespirationDataBridge.Instance.Raw}", RespirationDataBridge.Instance.Raw, 0, 60000);
        GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"{RespirationDataBridge.Instance.Volume:0.00}", RespirationDataBridge.Instance.Volume, 0, 15);
        GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"{RespirationDataBridge.Instance.Flow:0.00}", RespirationDataBridge.Instance.Flow, -20, 20);

        GuiLabel(new Rectangle(0, 32 * i++, 400, 32), "Heart");
        GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"Red {HeartRateDataBridge.Instance.Red}",
            HeartRateDataBridge.Instance.Red, HeartRateDataBridge.Instance.RedAvgMin, HeartRateDataBridge.Instance.RedAvgMax);



        GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"IR  {HeartRateDataBridge.Instance.Ir}",
            HeartRateDataBridge.Instance.Ir, HeartRateDataBridge.Instance.IrAvgMin, HeartRateDataBridge.Instance.IrAvgMax);

        GuiGraph(new Rectangle(0, 32 * i, 400, 64), HeartRateDataBridge.Instance.IrHistory, PURPLE);
        GuiGraph(new Rectangle(0, 32 * i++, 400, 64), HeartRateDataBridge.Instance.RedHistory, RED);

        i++;

        GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"{HeartRateDataBridge.Instance.HeartRate}",
            HeartRateDataBridge.Instance.HeartRate, 0, 100);


    }



    EndDrawing();
}


CloseWindow();
Save("default.config");


static void GuiGraph(Rectangle sz, float[] data, Color color)
{
    float center = data.Average();
    float low = data.Min();
    float high = data.Max();


    DrawRectangleLinesEx(sz, 2, WHITE);


    Vector2 last = new();

    for (int i = 0; i < data.Length; i++)
    {
        var pt = data[i];
        var px = Remap(i, 0, data.Length, sz.x, sz.x + sz.width);
        var py = Remap(pt, low, high, sz.y, sz.y + sz.height);

        Vector2 now = new(px, py);

        if (last != Vector2.Zero)
        {
            DrawLineV(now, last, color);
        }

        last = now;
        DrawPixel((int)px, (int)py, color);


    }


}


void Save(string path)
{
    var config = new Config
    {
        Version = 2,
        InHoldDwellTime = inHoldDwellTime,
        OutHoldDwellTime = outHoldDwellTime,
        MinBreth = minBreth,
        MaxBreth = maxBreth,
        LowSync = lowSync,
        MidSync = midSync,
        HighSync = highSync
    };

    var cfg = JsonConvert.SerializeObject(config);

    File.WriteAllText(path, cfg.ToString());
}

void Load(string path)
{
    var tf = File.ReadAllText(path);
    var cfg = JsonConvert.DeserializeObject<Config>(tf);

    if (cfg.Version != 2) return;

    inHoldDwellTime = cfg.InHoldDwellTime;
    outHoldDwellTime = cfg.OutHoldDwellTime;
    minBreth = cfg.MinBreth;
    maxBreth = cfg.MaxBreth;
    lowSync = cfg.LowSync;
    midSync = cfg.MidSync;
    highSync = cfg.HighSync;
}

enum BreathingDirection { Hold, In, InHold, Out, OutHold }

enum Mode
{
    UserMeasureMode,
    ProgramControl,
}

[JsonObject(MemberSerialization.OptIn)]
class Config
{
    [JsonProperty] public int Version;
    [JsonProperty] public float InHoldDwellTime, OutHoldDwellTime, MinBreth, MaxBreth, LowSync, MidSync, HighSync;
}
