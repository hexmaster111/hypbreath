using System.Numerics;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Raylib_CsLo;
using static Raylib_CsLo.RayGui;
using static Raylib_CsLo.Raylib;
using static Raylib_CsLo.RayMath;
using Rectangle = Raylib_CsLo.Rectangle;



SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
InitWindow(800, 800, "HypRender");
SetTargetFPS(GetMonitorRefreshRate(GetCurrentMonitor()));

bool showConfig = false;

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

if (File.Exists("default.config")) Load("default.config");

while (!WindowShouldClose())
{

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
        showConfig = !showConfig;
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
    int i = 0;
    if (showConfig)
    {   
    breth = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"now {breth:.0000}", breth, 0, 1);
    minBreth = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"min {minBreth:.0000}", minBreth, 0, 1);
    maxBreth = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"max {maxBreth:.0000}", maxBreth, 0, 1);
    desiredBreath = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"des {desiredBreath:.0000}", desiredBreath, 0, 1);
    sizeMul = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"siz {sizeMul:.0000}", sizeMul, 1, 1000);
    inHoldDwellTime = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"ihdt {inHoldDwellTime:.0000}", inHoldDwellTime, 0, 15);
    outHoldDwellTime = GuiSlider(new Rectangle(0, 32 * i++, 400, 32), "", $"ihdt {outHoldDwellTime:.0000}", outHoldDwellTime, 0, 15);
    }
    

    // DrawCircleV(center, max_breth * mul, DARKGREEN);
    DrawCircleV(center, innerCircleAmount * sizeMul, YELLOW);

    DrawRingLines(center, (outerCircleAmount * sizeMul), (outerCircleAmount * sizeMul) + 1, 0, 360, 300, LIGHTGRAY);
    DrawRingLines(center, (maxBreth * sizeMul), (maxBreth * sizeMul) + 1, 0, 360, 300, LIGHTGRAY with { a = 128 });
    DrawRingLines(center, (minBreth * sizeMul), (minBreth * sizeMul) + 1, 0, 360, 300, LIGHTGRAY with { a = 128 });

    DrawText($"{mode} {desiredBreathingDirection}\n{guessdebug}", 0, GetScreenHeight() - (24 * 5), 24, GREEN);

    EndDrawing();
}


CloseWindow();
Save("default.config");



void Save(string path)
{
    var config = new Config
    {
        Version = 1,
        InHoldDwellTime = inHoldDwellTime,
        OutHoldDwellTime = outHoldDwellTime,
        MinBreth = minBreth,
        MaxBreth = maxBreth
    };

    var cfg = JsonConvert.SerializeObject(config);

    File.WriteAllText(path, cfg.ToString());
}

void Load(string path)
{
    var tf = File.ReadAllText(path);
    var cfg = JsonConvert.DeserializeObject<Config>(tf);

    inHoldDwellTime = cfg.InHoldDwellTime;
    outHoldDwellTime = cfg.OutHoldDwellTime;
    minBreth = cfg.MinBreth;
    maxBreth = cfg.MaxBreth;
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
    [JsonProperty] public float InHoldDwellTime, OutHoldDwellTime, MinBreth, MaxBreth;
}