using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using KModkit;
using Newtonsoft.Json;
using Souvenir;
using Souvenir.Reflection;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Souvenir
/// Created by Timwi
/// </summary>
public class SouvenirModule : MonoBehaviour
{
    #region Fields
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMBossModule BossModule;
    public KMModSettings ModSettings;
    public KMSelectable[] Answers;
    public GameObject AnswersParent;
    public GameObject[] TpNumbers;
    public Sprite[] KeypadSprites;
    public Sprite[] MemorySprites;
    public Sprite[] ArithmelogicSprites;
    public Sprite[] ExampleSprites;
    public Sprite[] MahjongSprites;
    public Sprite[] PatternCubeSprites;
    public Sprite[] PerspectivePegsSprites;
    public Sprite[] PlanetsSprites;
    public Sprite[] SymbolicCoordinatesSprites;
    public Sprite[] WavetappingSprites;
    public Sprite[] FlagsSprites;
    public Sprite[] Tiles4x4Sprites;
    public Sprite[] EncryptedEquationsSprites;

    public TextMesh TextMesh;
    public Renderer TextRenderer;
    public Renderer SurfaceRenderer;
    public GameObject WarningIcon;
    public Material FontMaterial;
    public Font[] Fonts;
    public Texture[] FontTextures;
    public Mesh HighlightShort; // 6 answers, 2 columns
    public Mesh HighlightLong;  // 4 answers, 2 columns
    public Mesh HighlightVeryLong;  // 4 long answers, 1 column

    /// <summary>May be set to a question name while playing the test harness to skip to that question.</summary>
    public string TestQuestion;
    /// <summary>May be used if the prefab of a different module is available in the project</summary>
    public bool ModulePresent;

    public static readonly string[] _defaultIgnoredModules = {
        "Souvenir",
        "Forget Everything",
        "Forget Me Not",
        "Forget This",
        "Turn The Key",
        "The Time Keeper",
        "Simon's Stages",
        "Purgatory"
    };

    private Config config;
    private readonly List<QuestionBatch> _questions = new List<QuestionBatch>();
    private readonly HashSet<KMBombModule> _legitimatelyNoQuestions = new HashSet<KMBombModule>();
    private readonly HashSet<string> supportedModuleNames = new HashSet<string>();
    private readonly HashSet<string> ignoredModules = new HashSet<string>();
    private bool _isActivated = false;

    private QandA _currentQuestion = null;
    private bool _isSolved = false;
    private bool _animating = false;
    private bool _exploded = false;
    private int _avoidQuestions = 0;   // While this is > 0, temporarily avoid asking questions; currently only used when Souvenir is hidden by a Mystery Module

    [NonSerialized]
    public double SurfaceSizeFactor;

    private Dictionary<string, int> _moduleCounts = new Dictionary<string, int>();
    private Dictionary<string, int> _modulesSolved = new Dictionary<string, int>();
    private int _coroutinesActive;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private Dictionary<string, Func<KMBombModule, IEnumerable<object>>> _moduleProcessors;
    private Dictionary<Question, SouvenirQuestionAttribute> _attributes;
    #endregion

    #region Module ID constant declarations
    // The values here are the “ModuleType” property on the KMBombModule components.
    const string _3DMaze = "spwiz3DMaze";
    const string _3DTunnels = "3dTunnels";
    const string _Accumulation = "accumulation";
    const string _AdventureGame = "spwizAdventureGame";
    const string _AffineCycle = "affineCycle";
    const string _Algebra = "algebra";
    const string _AlphabeticalRuling = "alphabeticalRuling";
    const string _AlphaBits = "alphaBits";
    const string _Arithmelogic = "arithmelogic";
    const string _BamboozledAgain = "bamboozledAgain";
    const string _BamboozlingButton = "bamboozlingButton";
    const string _Bartending = "BartendingModule";
    const string _BigCircle = "BigCircle";
    const string _BinaryLEDs = "BinaryLeds";
    const string _Bitmaps = "BitmapsModule";
    const string _BlindMaze = "BlindMaze";
    const string _Blockbusters = "blockbusters";
    const string _BlueArrows = "blueArrowsModule";
    const string _BobBarks = "ksmBobBarks";
    const string _Boggle = "boggle";
    const string _Boxing = "boxing";
    const string _Braille = "BrailleModule";
    const string _BrokenButtons = "BrokenButtonsModule";
    const string _BrushStrokes = "brushStrokes";
    const string _Bulb = "TheBulbModule";
    const string _BurglarAlarm = "burglarAlarm";
    const string _Button = "BigButton";
    const string _ButtonSequences = "buttonSequencesModule";
    const string _CaesarCycle = "caesarCycle";
    const string _Calendar = "calendar";
    const string _ChallengeAndContact = "challengeAndContact";
    const string _CheapCheckout = "CheapCheckoutModule";
    const string _Chess = "ChessModule";
    const string _ChineseCounting = "chineseCounting";
    const string _ChordQualities = "ChordQualities";
    const string _Code = "theCodeModule";
    const string _Coffeebucks = "coffeebucks";
    const string _ColorBraille = "ColorBrailleModule";
    const string _ColorDecoding = "Color Decoding";
    const string _ColoredKeys = "lgndColoredKeys";
    const string _ColoredSquares = "ColoredSquaresModule";
    const string _ColoredSwitches = "ColoredSwitchesModule";
    const string _ColorMorse = "ColorMorseModule";
    const string _Coordinates = "CoordinatesModule";
    const string _Corners = "CornersModule";
    const string _Crackbox = "CrackboxModule";
    const string _Creation = "CreationModule";
    const string _CrypticCycle = "crypticCycle";
    const string _Cube = "cube";
    const string _DACHMaze = "DACH";
    const string _DeckOfManyThings = "deckOfManyThings";
    const string _DecoloredSquares = "DecoloredSquaresModule";
    const string _DiscoloredSquares = "DiscoloredSquaresModule";
    const string _DoubleColor = "doubleColor";
    const string _DoubleOh = "DoubleOhModule";
    const string _DrDoctor = "DrDoctorModule";
    const string _ElderFuthark = "elderFuthark";
    const string _EncryptedEquations = "EncryptedEquationsModule";
    const string _EncryptedHangman = "encryptedHangman";
    const string _EncryptedMorse = "EncryptedMorse";
    const string _EquationsX = "equationsXModule";
    const string _Etterna = "etterna";
    const string _FactoryMaze = "factoryMaze";
    const string _FastMath = "fastMath";
    const string _FaultyRGBMaze = "faultyrgbMaze";
    const string _Flags = "FlagsModule";
    const string _FlashingLights = "flashingLights";
    const string _ForgetTheColors = "ForgetTheColors";
    const string _FreeParking = "freeParking";
    const string _Functions = "qFunctions";
    const string _Gamepad = "TheGamepadModule";
    const string _GiantsDrink = "giantsDrink";
    const string _GreenArrows = "greenArrowsModule";
    const string _GridLock = "GridlockModule";
    const string _Gryphons = "gryphons";
    const string _LogicalButtons = "logicalButtonsModule";
    const string _HereditaryBaseNotation = "hereditaryBaseNotationModule";
    const string _Hexabutton = "hexabutton";
    const string _Hexamaze = "HexamazeModule";
    const string _HexOS = "hexOS";
    const string _HiddenColors = "lgndHiddenColors";
    const string _HillCycle = "hillCycle";
    const string _Hogwarts = "HogwartsModule";
    const string _HorribleMemory = "horribleMemory";
    const string _HumanResources = "HumanResourcesModule";
    const string _Hunting = "hunting";
    const string _Hypercube = "TheHypercubeModule";
    const string _IceCream = "iceCreamModule";
    const string _IdentityParade = "identityParade";
    const string _Instructions = "instructions";
    const string _iPhone = "iPhone";
    const string _JewelVault = "jewelVault";
    const string _JumbleCycle = "jumbleCycle";
    const string _Kudosudoku = "KudosudokuModule";
    const string _Lasers = "lasers";
    const string _LEDEncryption = "LEDEnc";
    const string _LEDMath = "lgndLEDMath";
    const string _LEGOs = "LEGOModule";
    const string _Listening = "Listening";
    const string _LogicGates = "logicGates";
    const string _LondonUnderground = "londonUnderground";
    const string _Mafia = "MafiaModule";
    const string _Mahjong = "MahjongModule";
    const string _MaritimeFlags = "MaritimeFlagsModule";
    const string _Maze = "Maze";
    const string _Maze3 = "maze3";
    const string _Mazematics = "mazematics";
    const string _MazeScrambler = "MazeScrambler";
    const string _MegaMan2 = "megaMan2";
    const string _MelodySequencer = "melodySequencer";
    const string _MemorableButtons = "memorableButtons";
    const string _Memory = "Memory";
    const string _Microcontroller = "Microcontroller";
    const string _Minesweeper = "MinesweeperModule";
    const string _ModernCipher = "modernCipher";
    const string _ModuleMaze = "ModuleMaze";
    const string _MonsplodeFight = "monsplodeFight";
    const string _MonsplodeTradingCards = "monsplodeCards";
    const string _Moon = "moon";
    const string _MorseAMaze = "MorseAMaze";
    const string _MorseButtons = "morseButtons";
    const string _Morsematics = "MorseV2";
    const string _MorseWar = "MorseWar";
    const string _MouseInTheMaze = "MouseInTheMaze";
    const string _Murder = "murder";
    const string _MysticSquare = "MysticSquareModule";
    const string _MysteryModule = "mysterymodule";
    const string _Necronomicon = "necronomicon";
    const string _Neutralization = "neutralization";
    const string _NandMs = "NandMs";
    const string _Navinums = "navinums";
    const string _NotButton = "NotButton";
    const string _NotKeypad = "NotKeypad";
    const string _NotMaze = "NotMaze";
    const string _NotMorseCode = "NotMorseCode";
    const string _NotSimaze = "NotSimaze";
    const string _NotWhosOnFirst = "NotWhosOnFirst";
    const string _NumberedButtons = "numberedButtonsModule";
    const string _ObjectShows = "objectShows";
    const string _OddOneOut = "OddOneOutModule";
    const string _OnlyConnect = "OnlyConnectModule";
    const string _OrangeArrows = "orangeArrowsModule";
    const string _OrderedKeys = "orderedKeys";
    const string _OrientationCube = "OrientationCube";
    const string _Palindromes = "palindromes";
    const string _PartialDerivatives = "partialDerivatives";
    const string _PassportControl = "passportControl";
    const string _PatternCube = "PatternCubeModule";
    const string _PerspectivePegs = "spwizPerspectivePegs";
    const string _Pie = "pieModule";
    const string _PigpenCycle = "pigpenCycle";
    const string _PlaceholderTalk = "placeholderTalk";
    const string _Planets = "planets";
    const string _PlayfairCycle = "playfairCycle";
    const string _Poetry = "poetry";
    const string _PolyhedralMaze = "PolyhedralMazeModule";
    const string _Probing = "Probing";
    const string _PurpleArrows = "purpleArrowsModule";
    const string _Quintuples = "quintuples";
    const string _RecoloredSwitches = "R4YRecoloredSwitches";
    const string _RedArrows = "redArrowsModule";
    const string _Retirement = "retirement";
    const string _ReverseMorse = "reverseMorse";
    const string _RGBMaze = "rgbMaze";
    const string _Rhythms = "MusicRhythms";
    const string _RoleReversal = "roleReversal";
    const string _Rule = "theRule";
    const string _ScavengerHunt = "scavengerHunt";
    const string _SchlagDenBomb = "qSchlagDenBomb";
    const string _SeaShells = "SeaShells";
    const string _Semamorse = "semamorse";
    const string _ShapesBombs = "ShapesBombs";
    const string _ShapeShift = "shapeshift";
    const string _ShellGame = "shellGame";
    const string _SillySlots = "SillySlots";
    const string _SimonSamples = "simonSamples";
    const string _SimonSays = "Simon";
    const string _SimonScrambles = "simonScrambles";
    const string _SimonScreams = "SimonScreamsModule";
    const string _SimonSelects = "simonSelectsModule";
    const string _SimonSends = "SimonSendsModule";
    const string _SimonShrieks = "SimonShrieksModule";
    const string _SimonSimons = "simonSimons";
    const string _SimonSings = "SimonSingsModule";
    const string _SimonSounds = "simonSounds";
    const string _SimonSpeaks = "SimonSpeaksModule";
    const string _SimonsStar = "simonsStar";
    const string _SimonStates = "SimonV2";
    const string _SimonStops = "simonStops";
    const string _SimonStores = "simonStores";
    const string _SkewedSlots = "SkewedSlotsModule";
    const string _Skyrim = "skyrim";
    const string _Snooker = "snooker";
    const string _SonicTheHedgehog = "sonic";
    const string _Sorting = "sorting";
    const string _Souvenir = "SouvenirModule";
    const string _Sphere = "sphere";
    const string _SplittingTheLoot = "SplittingTheLootModule";
    const string _Switch = "BigSwitch";
    const string _Switches = "switchModule";
    const string _SymbolCycle = "SymbolCycleModule";
    const string _SymbolicCoordinates = "symbolicCoordinates";
    const string _Synonyms = "synonyms";
    const string _TapCode = "tapCode";
    const string _TashaSqueals = "tashaSqueals";
    const string _TenButtonColorCode = "TenButtonColorCode";
    const string _TextField = "TextField";
    const string _ThinkingWires = "thinkingWiresModule";
    const string _ThirdBase = "ThirdBase";
    const string _TicTacToe = "TicTacToeModule";
    const string _Timezone = "timezone";
    const string _TransmittedMorse = "transmittedMorseModule";
    const string _TurtleRobot = "turtleRobot";
    const string _TwoBits = "TwoBits";
    const string _UltimateCycle = "ultimateCycle";
    const string _Ultracube = "TheUltracubeModule";
    const string _UncoloredSquares = "UncoloredSquaresModule";
    const string _UncoloredSwitches = "R4YUncoloredSwitches";
    const string _UnfairCipher = "unfairCipher";
    const string _UnownCipher = "UnownCipher";
    const string _USAMaze = "USA";
    const string _VaricoloredSquares = "VaricoloredSquaresModule";
    const string _Vcrcs = "VCRCS";
    const string _Vectors = "vectorsModule";
    const string _Vexillology = "vexillology";
    const string _VisualImpairment = "visual_impairment";
    const string _Wavetapping = "Wavetapping";
    const string _WhosOnFirst = "WhosOnFirst";
    const string _Wire = "wire";
    const string _WireOrdering = "kataWireOrdering";
    const string _WireSequence = "WireSequence";
    const string _Yahtzee = "YahtzeeModule";
    const string _YellowArrows = "yellowArrowsModule";
    const string _Zoni = "lgndZoni";
    #endregion

    #region Souvenir’s own module logic
    void Start()
    {
        _moduleId = _moduleIdCounter;
        _moduleIdCounter++;

        _moduleProcessors = new Dictionary<string, Func<KMBombModule, IEnumerable<object>>>()
        {
            { _3DMaze, Process3DMaze },
            { _3DTunnels, Process3DTunnels },
            { _Accumulation, ProcessAccumulation },
            { _AdventureGame, ProcessAdventureGame },
            { _AffineCycle, ProcessAffineCycle },
            { _Algebra, ProcessAlgebra },
            { _AlphabeticalRuling, ProcessAlphabeticalRuling },
            { _AlphaBits, ProcessAlphaBits },
            { _Arithmelogic, ProcessArithmelogic },
            { _BamboozledAgain, ProcessBamboozledAgain },
            { _BamboozlingButton, ProcessBamboozlingButton },
            { _Bartending, ProcessBartending },
            { _BigCircle, ProcessBigCircle },
            { _BinaryLEDs, ProcessBinaryLEDs },
            { _Bitmaps, ProcessBitmaps },
            { _BlindMaze, ProcessBlindMaze },
            { _Blockbusters, ProcessBlockbusters },
            { _BlueArrows, ProcessBlueArrows },
            { _BobBarks, ProcessBobBarks },
            { _Boggle, ProcessBoggle },
            { _Boxing, ProcessBoxing },
            { _Braille, ProcessBraille },
            { _BrokenButtons, ProcessBrokenButtons },
            { _BrushStrokes, ProcessBrushStrokes },
            { _Bulb, ProcessBulb },
            { _BurglarAlarm, ProcessBurglarAlarm },
            { _Button, ProcessButton },
            { _ButtonSequences, ProcessButtonSequences },
            { _CaesarCycle, ProcessCaesarCycle },
            { _Calendar, ProcessCalendar },
            { _ChallengeAndContact, ProcessChallengeAndContact },
            { _CheapCheckout, ProcessCheapCheckout },
            { _Chess, ProcessChess },
            { _ChineseCounting, ProcessChineseCounting },
            { _ChordQualities, ProcessChordQualities },
            { _Code, ProcessCode },
            { _Coffeebucks, ProcessCoffeebucks },
            { _ColorBraille, ProcessColorBraille },
            { _ColorDecoding, ProcessColorDecoding },
            { _ColoredKeys, ProcessColoredKeys },
            { _ColoredSquares, ProcessColoredSquares },
            { _ColoredSwitches, ProcessColoredSwitches },
            { _ColorMorse, ProcessColorMorse },
            { _Coordinates, ProcessCoordinates },
            { _Corners, ProcessCorners },
            { _Crackbox, ProcessCrackbox },
            { _Creation, ProcessCreation },
            { _CrypticCycle, ProcessCrypticCycle },
            { _Cube, ProcessCube },
            { _DACHMaze, ProcessDACHMaze },
            { _DeckOfManyThings, ProcessDeckOfManyThings },
            { _DecoloredSquares, ProcessDecoloredSquares },
            { _DiscoloredSquares, ProcessDiscoloredSquares },
            { _DoubleColor, ProcessDoubleColor },
            { _DoubleOh, ProcessDoubleOh },
            { _DrDoctor, ProcessDrDoctor },
            { _ElderFuthark, ProcessElderFuthark },
            { _EncryptedEquations, ProcessEncryptedEquations },
            { _EncryptedHangman, ProcessEncryptedHangman },
            { _EncryptedMorse, ProcessEncryptedMorse },
            { _EquationsX, ProcessEquationsX },
            { _Etterna, ProcessEtterna },
            { _FactoryMaze, ProcessFactoryMaze },
            { _FastMath, ProcessFastMath },
            { _FaultyRGBMaze, ProcessFaultyRGBMaze },
            { _Flags, ProcessFlags },
            { _FlashingLights, ProcessFlashingLights },
            { _ForgetTheColors, ProcessForgetTheColors },
            { _FreeParking, ProcessFreeParking },
            { _Functions, ProcessFunctions },
            { _Gamepad, ProcessGamepad },
            { _GiantsDrink, ProcessGiantsDrink },
            { _GreenArrows, ProcessGreenArrows },
            { _GridLock, ProcessGridLock },
            { _Gryphons, ProcessGryphons },
            { _HereditaryBaseNotation, ProcessHereditaryBaseNotation },
            { _Hexabutton, ProcessHexabutton },
            { _Hexamaze, ProcessHexamaze },
            { _HexOS, ProcessHexOS },
            { _HiddenColors, ProcessHiddenColors },
            { _HillCycle, ProcessHillCycle },
            { _Hogwarts, ProcessHogwarts },
            { _HorribleMemory, ProcessHorribleMemory },
            { _HumanResources, ProcessHumanResources },
            { _Hunting, ProcessHunting },
            { _Hypercube, ProcessHypercube },
            { _IceCream, ProcessIceCream },
            { _IdentityParade, ProcessIdentityParade },
            { _Instructions, ProcessInstructions },
            { _iPhone, ProcessiPhone },
            { _JewelVault, ProcessJewelVault },
            { _JumbleCycle, ProcessJumbleCycle },
            { _Kudosudoku, ProcessKudosudoku },
            { _Lasers, ProcessLasers },
            { _LEDEncryption, ProcessLEDEncryption },
            { _LEDMath, ProcessLEDMath },
            { _LEGOs, ProcessLEGOs },
            { _Listening, ProcessListening },
            { _LogicalButtons, ProcessLogicalButtons },
            { _LogicGates, ProcessLogicGates },
            { _LondonUnderground, ProcessLondonUnderground },
            { _Mafia, ProcessMafia },
            { _Mahjong, ProcessMahjong },
            { _MaritimeFlags, ProcessMaritimeFlags },
            { _Maze, ProcessMaze },
            { _Maze3, ProcessMaze3 },
            { _Mazematics, ProcessMazematics },
            { _MazeScrambler, ProcessMazeScrambler },
            { _MegaMan2, ProcessMegaMan2 },
            { _MelodySequencer, ProcessMelodySequencer },
            { _MemorableButtons, ProcessMemorableButtons },
            { _Memory, ProcessMemory },
            { _Microcontroller, ProcessMicrocontroller },
            { _Minesweeper, ProcessMinesweeper },
            { _ModernCipher, ProcessModernCipher },
            { _ModuleMaze, ProcessModuleMaze },
            { _MonsplodeFight, ProcessMonsplodeFight },
            { _MonsplodeTradingCards, ProcessMonsplodeTradingCards },
            { _Moon, ProcessMoon },
            { _MorseAMaze, ProcessMorseAMaze },
            { _MorseButtons, ProcessMorseButtons },
            { _Morsematics, ProcessMorsematics },
            { _MorseWar, ProcessMorseWar },
            { _MouseInTheMaze, ProcessMouseInTheMaze },
            { _Murder, ProcessMurder },
            { _MysticSquare, ProcessMysticSquare },
            { _MysteryModule, ProcessMysteryModule },
            { _Necronomicon, ProcessNecronomicon },
            { _Neutralization, ProcessNeutralization },
            { _NandMs, ProcessNandMs },
            { _Navinums, ProcessNavinums },
            { _NotButton, ProcessNotButton },
            { _NotKeypad, ProcessNotKeypad },
            { _NotMaze, ProcessNotMaze },
            { _NotMorseCode, ProcessNotMorseCode },
            { _NotSimaze, ProcessNotSimaze },
            { _NotWhosOnFirst, ProcessNotWhosOnFirst },
            { _NumberedButtons, ProcessNumberedButtons },
            { _ObjectShows, ProcessObjectShows },
            { _OddOneOut, ProcessOddOneOut },
            { _OnlyConnect, ProcessOnlyConnect },
            { _OrangeArrows, ProcessOrangeArrows },
            { _OrderedKeys, ProcessOrderedKeys },
            { _OrientationCube, ProcessOrientationCube },
            { _Palindromes, ProcessPalindromes },
            { _PartialDerivatives, ProcessPartialDerivatives },
            { _PassportControl, ProcessPassportControl },
            { _PatternCube, ProcessPatternCube },
            { _PerspectivePegs, ProcessPerspectivePegs },
            { _Pie, ProcessPie },
            { _PigpenCycle, ProcessPigpenCycle },
            { _PlaceholderTalk, ProcessPlaceholderTalk },
            { _Planets, ProcessPlanets },
            { _PlayfairCycle, ProcessPlayfairCycle },
            { _Poetry, ProcessPoetry },
            { _PolyhedralMaze, ProcessPolyhedralMaze },
            { _Probing, ProcessProbing },
            { _PurpleArrows, ProcessPurpleArrows },
            { _Quintuples, ProcessQuintuples },
            { _RecoloredSwitches, ProcessRecoloredSwitches },
            { _RedArrows, ProcessRedArrows },
            { _Retirement, ProcessRetirement },
            { _ReverseMorse, ProcessReverseMorse },
            { _RGBMaze, ProcessRGBMaze},
            { _Rhythms, ProcessRhythms },
            { _RoleReversal, ProcessRoleReversal },
            { _Rule, ProcessRule },
            { _ScavengerHunt, ProcessScavengerHunt },
            { _SchlagDenBomb, ProcessSchlagDenBomb },
            { _SeaShells, ProcessSeaShells },
            { _Semamorse, ProcessSemamorse },
            { _ShapesBombs, ProcessShapesAndBombs },
            { _ShapeShift, ProcessShapeShift },
            { _ShellGame, ProcessShellGame },
            { _SillySlots, ProcessSillySlots },
            { _SimonSamples, ProcessSimonSamples },
            { _SimonSays, ProcessSimonSays },
            { _SimonScrambles, ProcessSimonScrambles },
            { _SimonScreams, ProcessSimonScreams },
            { _SimonSelects, ProcessSimonSelects },
            { _SimonSends, ProcessSimonSends },
            { _SimonShrieks, ProcessSimonShrieks },
            { _SimonSimons, ProcessSimonSimons },
            { _SimonSings, ProcessSimonSings },
            { _SimonSounds, ProcessSimonSounds },
            { _SimonSpeaks, ProcessSimonSpeaks },
            { _SimonsStar, ProcessSimonsStar },
            { _SimonStates, ProcessSimonStates },
            { _SimonStops, ProcessSimonStops },
            { _SimonStores, ProcessSimonStores},
            { _SkewedSlots, ProcessSkewedSlots },
            { _Skyrim, ProcessSkyrim },
            { _Snooker, ProcessSnooker },
            { _SonicTheHedgehog, ProcessSonicTheHedgehog },
            { _Sorting, ProcessSorting },
            { _Souvenir, ProcessSouvenir },
            { _Sphere, ProcessSphere },
            { _SplittingTheLoot, ProcessSplittingTheLoot },
            { _Switch, ProcessSwitch },
            { _Switches, ProcessSwitches },
            { _SymbolCycle, ProcessSymbolCycle },
            { _SymbolicCoordinates, ProcessSymbolicCoordinates },
            { _Synonyms, ProcessSynonyms },
            { _TapCode, ProcessTapCode },
            { _TashaSqueals, ProcessTashaSqueals },
            { _TenButtonColorCode, ProcessTenButtonColorCode },
            { _TextField, ProcessTextField },
            { _ThinkingWires, ProcessThinkingWires },
            { _ThirdBase, ProcessThirdBase },
            { _TicTacToe, ProcessTicTacToe },
            { _Timezone, ProcessTimezone },
            { _TransmittedMorse, ProcessTransmittedMorse },
            { _TurtleRobot, ProcessTurtleRobot },
            { _TwoBits, ProcessTwoBits },
            { _UltimateCycle, ProcessUltimateCycle },
            { _Ultracube, ProcessUltracube },
            { _UncoloredSquares, ProcessUncoloredSquares },
            { _UncoloredSwitches, ProcessUncoloredSwitches },
            { _UnfairCipher, ProcessUnfairCipher },
            { _UnownCipher, ProcessUnownCipher },
            { _USAMaze, ProcessUSAMaze },
            { _VaricoloredSquares, ProcessVaricoloredSquares },
            {  _Vcrcs, ProcessVcrcs },
            { _Vectors, ProcessVectors },
            { _Vexillology, ProcessVexillology },
            { _VisualImpairment, ProcessVisualImpairment },
            { _Wavetapping, ProcessWavetapping },
            { _WhosOnFirst, ProcessWhosOnFirst },
            { _Wire, ProcessWire },
            { _WireOrdering, ProcessWireOrdering },
            { _WireSequence, ProcessWireSequence },
            { _Yahtzee, ProcessYahtzee },
            { _YellowArrows, ProcessYellowArrows },
            { _Zoni, ProcessZoni }
        };

        if (!string.IsNullOrEmpty(ModSettings.SettingsPath))
        {
            bool rewriteFile;
            try
            {
                config = JsonConvert.DeserializeObject<Config>(ModSettings.Settings);
                if (config != null)
                {
                    var dictionary = JsonConvert.DeserializeObject<IDictionary<string, object>>(ModSettings.Settings);
                    object key;
                    // Rewrite the file if any keys listed in TweaksEditorSettings are not in it.
                    rewriteFile = ((List<Dictionary<string, object>>) Config.TweaksEditorSettings[0]["Listings"])
                        .Any(o => o.TryGetValue("Key", out key) && !dictionary.ContainsKey((string) key));
                    config.UpdateExcludedModules();
                }
                else
                {
                    config = new Config();
                    rewriteFile = true;
                }
            }
            catch (JsonSerializationException ex)
            {
                Debug.LogErrorFormat("<Souvenir #{0}> The mod settings file is invalid.", _moduleId);
                Debug.LogException(ex, this);
                config = new Config();
                rewriteFile = true;
            }
            if (rewriteFile)
            {
                using (var writer = new StreamWriter(ModSettings.SettingsPath))
                    new JsonSerializer() { Formatting = Formatting.Indented }.Serialize(writer, config);
            }
        }
        else
            config = new Config();

        var ignoredList = BossModule.GetIgnoredModules(Module, _defaultIgnoredModules);
        Debug.LogFormat(@"<Souvenir #{0}> Ignored modules: {1}", _moduleId, ignoredList.JoinString(", "));
        ignoredModules.UnionWith(ignoredList);

        Bomb.OnBombExploded += delegate { _exploded = true; StopAllCoroutines(); };
        Bomb.OnBombSolved += delegate
        {
            // This delegate gets invoked when _any_ bomb in the room is solved,
            // so we need to check if the bomb this module is on is actually solved
            if (Bomb.GetSolvedModuleNames().Count == Bomb.GetSolvableModuleNames().Count)
                StopAllCoroutines();
        };

        _attributes = typeof(Question).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(f => Ut.KeyValuePair((Question) f.GetValue(null), GetQuestionAttribute(f)))
            .Where(kvp => kvp.Value != null)
            .ToDictionary();

        var origRotation = SurfaceRenderer.transform.rotation;
        SurfaceRenderer.transform.eulerAngles = new Vector3(0, 180, 0);
        SurfaceSizeFactor = SurfaceRenderer.bounds.size.x / (2 * .834) * .9;
        SurfaceRenderer.transform.rotation = origRotation;

        disappear();
        WarningIcon.SetActive(false);
        SetWordWrappedText(Ut.NewArray(
            "I see dead defusers.",     // “I see dead people”, (Sixth Sense)
            "Welcome... to the real bomb.",     // “Welcome... to the real world.” (The Matrix)
            "I’m gonna make him a bomb he can’t defuse.",   // “I’m gonna make him an offer he can’t refuse.” (The Godfather)
            "Defuse it again, Sam.",    // “Play it again, Sam.” (Casablanca) (misquote)
            "Louis, I think this is the beginning of a beautiful explosion.",   // “Louis, I think this is the beginning of a beautiful friendship.” (Casablanca)
            "Here’s looking at you, defuser.",  // “Here’s looking at you, kid.” (Casablanca)
            "Hey. I could defuse this bomb in ten seconds flat.",   // “Hey. I could clear the sky in ten seconds flat.” (MLP:FiM, Friendship is Magic - Part 1)
            "Go ahead, solve my bomb.", // “Go ahead, make my day.” (Sudden Impact / Dirty Harry series)
            "May the bomb be with you.",    // “May the Force be with you.” (Star Wars IV: A New Hope)
            "I love the smell of explosions in the morning.",   // “I love the smell of napalm in the morning.” (Apocalypse Now)
            "Blowing up means never having to say you’re sorry.",   // “Love means never having to say you're sorry.” (Love Story)
            "The stuff that bombs are made of.",    // “The Stuff That Dreams Are Made Of” (“Coming Around Again” album by Carly Simon)
            "E.T. defuse bomb.",    // “E.T. phone home.” (E.T. the Extra-Terrestrial)
            "Bomb. James Bomb.",    // “Bond. James Bond.” (Dr. No / James Bond series)
            "You can’t handle the bomb!",   // “You can’t handle the truth!” (A Few Good Men)
            "Blow up the usual suspects.",  // “Round up the usual suspects.” (Casablanca)
            "You’re gonna need a bigger bomb.", // “You’re gonna need a bigger boat.” (Jaws)
            "Bombs are like a box of chocolates. You never know what you’re gonna get.",    // “My mom always said life was like a box of chocolates. You never know what you're gonna get.” (Forrest Gump)
            "Houston, we have a module.",   // “Houston, we have a problem.” (Apollo 13)
            "Elementary, my dear expert.",  // “Elementary, my dear Watson.” (Sherlock Holmes) (misquote)
            "Forget it, Jake, it’s KTANE.",     // “Forget it, Jake, it’s Chinatown.” (Chinatown)
            "I have always depended on the fitness of experts.",    // “I’ve always depended on the kindness of strangers.” (A Streetcar Named Desire)
            "A bomb. Exploded, not defused.",   // “A Martini. Shaken, not stirred.” (Diamonds Are Forever (novel) / James Bond)
            "I’m the king of the bomb!",    // “I’m the king of the world!” (Titanic)
            "Blow me up, Scotty.",  // “Beam me up, Scotty!” (Star Trek misquote)
            "Yabba dabba boom!",    // “Yabba dabba doo!” (Flintstones)
            "This bomb will self-destruct in five seconds.",    // “This tape will self-destruct in five seconds.” (Mission: Impossible)
            "Defusing is futile.",  // “Resistance is futile.” (Star Trek: The Next Generation)
            "Is that your final answer?",   // direct quote (Who Wants to be a Millionaire?)
            "A bomb’s best friend is his defuser.", // “A man’s best friend is his dog.” (attorney George Graham Vest, 1870 Warrensburg)
            "Keep your experts close, but your bomb closer.",   // “Keep your friends close and your enemies closer.” (The Prince / Machiavelli)
            "Fasten your seatbelts. It’s going to be a bomby night.",   // “Fasten your seat belts, it’s going to be a bumpy night.” (All About Eve)
            "Show me the modules!", // “Show me the money!” (Jerry Maguire)
            "We’ll always have batteries.", // “We’ll always have Paris.” (Casablanca)
            "Say hello to my little bomb.", // “Say hello to my little friend!” (Scarface)
            "You’re a defuser, Harry.", // “You’re a wizard, Harry.” (Harry Potter and the Philosopher’s Stone)
            "I’m sorry, Dave. I’m afraid I can’t defuse that.", // “I’m sorry, Dave. I’m afraid I can’t do that.” (2001: A Space Odyssey)
            "You either die a defuser, or you live long enough to see yourself become the expert.", // “Well, I guess you either die a hero or you live long enough to see yourself become the villain.” (The Dark Knight)
            "This isn’t defusing. This is exploding... with style.",    // “This isn’t flying. This is falling... with style.” (Toy Story)
            "Could you describe the module, sir?",  // “Could you describe the ruckus, sir?” (The Breakfast Club)
            "You want widgets? I got twenty.",  // “You want thingamabobs? I got twenty.” (The Little Mermaid)
            "We don’t need no stinking widgets.",   // “We don’t need no stinking badges!” (The Treasure of the Sierra Madre)
            "Say edgework one more goddamn time.",  // “Say what one more goddamn time.” (Pulp Fiction)
            "How do you like them modules?",    // “How do you like them apples?” (Good Will Hunting)
            "Introducing: The Double... Decker... Bomb!",   // “Introducing: The Double... Decker... Couch!” (The LEGO Movie)
            "Have you got your wires crossed?", // “Have you got your lions crossed?” (The Lion King)
            "Don’t cross the wires.",   // “Don’t cross the streams.” (Ghostbusters)
            "Wanna hear the most annoying explosion in the world?", // “Wanna hear the most annoying sound in the world?” (Dumb & Dumber)
            "Manuals? Where we’re going, we don’t need manuals.",   // “Roads? Where we’re going, we don’t need roads.” (Back to the Future)
            "On a long enough time line, the survival rate for everyone will drop to zero.", // direct quote (Fight Club (novel))
            "This is your bomb, and it’s ending one minute at a time.", // “This is your life and it’s ending one minute at a time.” (Fight Club)
            "The first rule of defusal is, you keep talking about defusal.",    // “The first rule of Fight Club is, you don’t talk about Fight Club.” (Fight Club)
            "Well, here’s another nice mess you’ve gotten me into!",     // direct quote (Sons of the Desert / Oliver Hardy)
            "You know how to defuse, don’t you, Steve? You just put your wires together and cut.",  // “You know how to whistle, don’t you Steve? You just put your lips together, and blow.” (To Have And Have Not)
            "Mrs. Defuser, you’re trying to disarm me. Aren’t you?",    // “Mrs. Robinson, you’re trying to seduce me. Aren’t you?” (The Graduate)
            "We defuse bombs.",  // “We rob banks.“ (Bonnie and Clyde)
            "Somebody set up us the bomb.",  // direct quote (Zero Wing)
            "Luke, I am your expert.", // “Luke, I am your father.“ (Star Wars V: The Empire Strikes Back) (misquote)
            "Everybody knows that the best way to learn is under intense life threatening crisis.", // direct quote (Spider-Man: Into the Spider-Verse)
            "It needs to be about 20 percent more exploded.", // “It needs to be about 20 percent cooler.” (MLP:FiM, Suited for Success)
            "I am a bomb. What’s your excuse?", // “I am a child. What’s your excuse?” (Steven Universe, Change your Mind)
            "The same thing we do every time, expert. Try to defuse the bomb!", // “The same thing we do every time, Pinky. Try to take over the world!” (Pinky and the Brain)
            "Anybody order fried defuser?", // “Anybody order fried sauerkraut?” (Once Upon a Time in Hollywood)
            "I’ve got some defusers I need to blow into smithereens!", // “I’ve got some children I need to make into corpses!” (Gravity Falls, Weirdmageddon 3: Take Back The Falls)
            "I imagine blowing up so much it feels more like a memory.", // “I imagine death so much it feels more like a memory.” (Hamilton)
            "I am inevitable.", // direct quote (Avengers: Endgame)
            "Dread it, run from it, bombs still explode.", // “Dread it, run from it, destiny still arrives.” (Avengers: Infinity War)
            "In time, you will know what it’s like to lose. To feel so desperately that you’re right, yet to strike all the same.", // “In time, you will know what it’s like to lose. To feel so desperately that you’re right, yet to fail all the same.” (Avengers: Infinity War)
            "Defuser, I’ve come to bargain.", // “Dormammu, I’ve come to bargain.” (Doctor Strange)
            "I can do this all day.", // direct quote (Captain America: Civil War)
            "There... are... FOUR! BOMBS!!!", // “There... are... FOUR! LIGHTS!!!” (Star Trek TNG, Chain of Command)
            "It’s a beautiful thing, the detonation of bombs." // “It’s a beautiful thing, the destruction of words.” (1984)

        ).PickRandom(), 1.75);

        if (transform.parent != null)
        {
            FieldInfo<object> fldType = null;
            for (int i = 0; i < transform.parent.childCount; i++)
            {
                var gameObject = transform.parent.GetChild(i).gameObject;
                var module = gameObject.GetComponent<KMBombModule>();
                if (module != null)
                {
                    if ((config.ExcludeIgnoredModules && ignoredModules.Contains(module.ModuleDisplayName)) || config.ExcludedModules.Contains(module.ModuleType))
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning {1} because it is excluded in the mod settings.", _moduleId, module.ModuleDisplayName);
                    }
                    else
                    {
                        StartCoroutine(ProcessModule(module));
                    }
                }
                else if (!config.ExcludeVanillaModules)
                {
                    var vanillaModule = transform.parent.GetChild(i).gameObject.GetComponent("BombComponent");
                    if (vanillaModule != null)
                    {
                        // For vanilla modules, we will attach a temporary KMBombModule component to the module.
                        // We'll remove it after the coroutine starts.
                        // The routine will already have a reference to the actual BombComponent by then.
                        if (fldType == null) fldType = GetField<object>(vanillaModule.GetType(), "ComponentType", true);
                        if (fldType == null) continue;
                        var typeCode = (int) fldType.GetFrom(vanillaModule);
                        string type; string displayName;
                        switch (typeCode)
                        {
                            case 3: type = "BigButton"; displayName = "The Button"; break;
                            case 5: type = "Simon"; displayName = "Simon Says"; break;
                            case 6: type = "WhosOnFirst"; displayName = "Who's on First"; break;
                            case 7: type = "Memory"; displayName = "Memory"; break;
                            case 10: type = "WireSequence"; displayName = "Wire Sequence"; break;
                            case 11: type = "Maze"; displayName = "Maze"; break;
                            default: continue;  // Other components are not supported modules.
                        }
                        module = gameObject.AddComponent<KMBombModule>();
                        module.ModuleType = type;
                        module.ModuleDisplayName = displayName;
                        StartCoroutine(ProcessModule(module));
                        Destroy(module);
                    }
                }
            }
        }

        _isActivated = false;
        Module.OnActivate += delegate
        {
            _isActivated = true;
            if (Application.isEditor && !ModulePresent)
            {
                // Testing in Unity
                foreach (var entry in _attributes)
                {
                    if (entry.Value.Type != AnswerType.Sprites && (entry.Value.AllAnswers == null || entry.Value.AllAnswers.Length == 0) &&
                        (entry.Value.ExampleAnswers == null || entry.Value.ExampleAnswers.Length == 0) && entry.Value.AnswerGenerator == null)
                    {
                        Debug.LogWarningFormat("<Souvenir #{0}> Question {1} has no answers. You should specify either SouvenirQuestionAttribute.AllAnswers or SouvenirQuestionAttribute.ExampleAnswers (with preferredWrongAnswers in-game), or add an AnswerGeneratorAttribute to the question enum value.", _moduleId, entry.Key);
                        WarningIcon.SetActive(true);
                    }
                }
                StartCoroutine(TestModeCoroutine());
            }
            else
            {
                // Playing for real
                for (int i = 0; i < 6; i++)
                    setAnswerHandler(i, HandleAnswer);
                disappear();
                StartCoroutine(Play());
            }
        };
    }

    private static SouvenirQuestionAttribute GetQuestionAttribute(FieldInfo field)
    {
        var attribute = field.GetCustomAttribute<SouvenirQuestionAttribute>();
        if (attribute != null)
            attribute.AnswerGenerator = field.GetCustomAttribute<AnswerGeneratorAttribute>();
        return attribute;
    }

    private IEnumerator TestModeCoroutine()
    {
        Debug.LogFormat(this, "<Souvenir #{0}> Entering Unity testing mode. To select a question, set SouvenirModule.TestQuestion and click on the game view.", _moduleId);
        var questions = Ut.GetEnumValues<Question>();
        var curQuestion = 0;
        var curOrd = 0;
        var curExample = 0;
        Action showQuestion = () =>
        {
            SouvenirQuestionAttribute attr;
            if (!_attributes.TryGetValue(questions[curQuestion], out attr))
            {
                Debug.LogErrorFormat("<Souvenir #{1}> Error: Question {0} has no attribute.", questions[curQuestion], _moduleId);
                return;
            }
            if (attr.ExampleExtraFormatArguments != null && attr.ExampleExtraFormatArguments.Length > 0 && attr.ExampleExtraFormatArgumentGroupSize > 0)
            {
                var numExamples = attr.ExampleExtraFormatArguments.Length / attr.ExampleExtraFormatArgumentGroupSize;
                curExample = (curExample % numExamples + numExamples) % numExamples;
            }
            var fmt = new object[attr.ExampleExtraFormatArgumentGroupSize + 1];
            fmt[0] = curOrd == 0 ? attr.AddThe ? "The\u00a0" + attr.ModuleName : attr.ModuleName : string.Format("the {0} you solved {1}", attr.ModuleName, ordinal(curOrd));
            for (int i = 0; i < attr.ExampleExtraFormatArgumentGroupSize; i++)
                fmt[i + 1] = attr.ExampleExtraFormatArguments[curExample * attr.ExampleExtraFormatArgumentGroupSize + i];
            try
            {
                switch (attr.Type)
                {
                    case AnswerType.Sprites:
                        var answerSprites = attr.SpriteField == null ? ExampleSprites : (Sprite[]) typeof(SouvenirModule).GetField(attr.SpriteField, BindingFlags.Instance | BindingFlags.Public).GetValue(this) ?? ExampleSprites;
                        if (answerSprites != null)
                            answerSprites.Shuffle();
                        SetQuestion(new QandASprite(
                            module: attr.ModuleNameWithThe,
                            question: string.Format(attr.QuestionText, fmt),
                            correct: 0,
                            answers: answerSprites));
                        break;

                    default:
                        var answers = new List<string>(attr.NumAnswers);
                        if (attr.AllAnswers != null) answers.AddRange(attr.AllAnswers);
                        else if (attr.ExampleAnswers != null) answers.AddRange(attr.ExampleAnswers);
                        if (answers.Count <= attr.NumAnswers)
                        {
                            if (attr.AnswerGenerator != null)
                                answers.AddRange(attr.AnswerGenerator.GetAnswers(this).Except(answers).Distinct().Take(attr.NumAnswers - answers.Count));
                            answers.Shuffle();
                        }
                        else
                        {
                            answers.Shuffle();
                            answers.RemoveRange(attr.NumAnswers, answers.Count - attr.NumAnswers);
                        }
                        SetQuestion(new QandAText(
                            module: attr.ModuleNameWithThe,
                            question: string.Format(attr.QuestionText, fmt),
                            correct: 0,
                            answers: answers.ToArray(),
                            font: Fonts[attr.Type == AnswerType.DynamicFont ? 0 : (int) attr.Type],
                            fontTexture: FontTextures[attr.Type == AnswerType.DynamicFont ? 0 : (int) attr.Type],
                            fontMaterial: FontMaterial,
                            layout: attr.Layout));
                        break;
                }
            }
            catch (FormatException e)
            {
                Debug.LogErrorFormat("<Souvenir #{3}> FormatException {0}\nQuestionText={1}\nfmt=[{2}]", e.Message, attr.QuestionText, fmt.JoinString(", ", "\"", "\""), _moduleId);
            }
        };
        showQuestion();

        setAnswerHandler(0, _ =>
        {
            curQuestion = (curQuestion + questions.Length - 1) % questions.Length;
            curExample = 0;
            curOrd = 0;
            showQuestion();
        });
        setAnswerHandler(1, _ =>
        {
            curQuestion = (curQuestion + 1) % questions.Length;
            curExample = 0;
            curOrd = 0;
            showQuestion();
        });
        setAnswerHandler(2, _ => { if (curOrd > 0) curOrd--; showQuestion(); });
        setAnswerHandler(3, _ => { curOrd++; showQuestion(); });
        setAnswerHandler(4, _ => { curExample--; showQuestion(); });
        setAnswerHandler(5, _ => { curExample++; showQuestion(); });

        if (TwitchPlaysActive)
            ActivateTwitchPlaysNumbers();

        while (true)
        {
            if (TestQuestion != null && Application.isFocused)
            {
                TestQuestion = TestQuestion.Trim();
                if (TestQuestion.Length > 0)
                {
                    var i = questions.IndexOf(q => q.ToString().StartsWith(TestQuestion, StringComparison.InvariantCultureIgnoreCase));
                    if (i < 0)
                        Debug.LogFormat(this, "<Souvenir #{0}> No question matching '{1}' was found.", _moduleId, TestQuestion);
                    else
                    {
                        curQuestion = i;
                        curExample = 0;
                        curOrd = 0;
                        showQuestion();
                    }
                }
                TestQuestion = null;
            }
            yield return null;
        }
    }

    void setAnswerHandler(int index, Action<int> handler)
    {
        Answers[index].OnInteract = delegate
        {
            Answers[index].AddInteractionPunch();
            handler(index);
            return false;
        };
    }

    private void disappear()
    {
        TextMesh.gameObject.SetActive(false);
        AnswersParent.SetActive(false);
    }

    private void HandleAnswer(int index)
    {
        if (_animating || _isSolved)
            return;

        if (_currentQuestion == null || index >= _currentQuestion.NumAnswers)
            return;

        Debug.LogFormat("[Souvenir #{0}] Clicked answer #{1} ({2}). {3}.", _moduleId, index + 1, _currentQuestion.DebugAnswers.Skip(index).First(), _currentQuestion.CorrectIndex == index ? "Correct" : "Wrong");

        if (_currentQuestion.CorrectIndex == index)
        {
            StartCoroutine(CorrectAnswer());
        }
        else
        {
            Module.HandleStrike();
            if (!_exploded)
            {
                // Blink the correct answer, then move on to the next question
                _animating = true;
                StartCoroutine(revealThenMoveOn());
            }
        }
    }

    private IEnumerator CorrectAnswer()
    {
        _animating = true;
        Audio.PlaySoundAtTransform("Answer", transform);
        dismissQuestion();
        yield return new WaitForSeconds(.5f);
        _animating = false;
    }

    private void dismissQuestion()
    {
        _currentQuestion = null;
        disappear();
    }

    private IEnumerator revealThenMoveOn()
    {
        var on = false;
        for (int i = 0; i < 14; i++)
        {
            _currentQuestion.BlinkCorrectAnswer(on, this);
            on = !on;
            yield return new WaitForSeconds(.1f);
        }

        dismissQuestion();
        _animating = false;
    }

    private IEnumerator Play()
    {
        if (TwitchPlaysActive)
            ActivateTwitchPlaysNumbers();

        var numPlayableModules = Bomb.GetSolvableModuleNames().Count(x => !ignoredModules.Contains(x));

        while (true)
        {
            // A module handler can increment this value temporarily to delay asking questions. (Currently only the Mystery Module handler does this when Souvenir is hidden by a Mystery Module.)
            while (_avoidQuestions > 0)
                yield return new WaitForSeconds(.1f);

            var numSolved = Bomb.GetSolvedModuleNames().Count(x => !ignoredModules.Contains(x));
            if (_questions.Count == 0 && (numSolved >= numPlayableModules || _coroutinesActive == 0))
            {
                // Very rare case: another coroutine could still be waiting to detect that a module is solved and then add another question to the queue
                yield return new WaitForSeconds(.1f);

                // If still no new questions, all supported modules are solved and we’re done. (Or maybe a coroutine is stuck in a loop, but then it’s bugged and we need to cancel it anyway.)
                if (_questions.Count == 0)
                    break;
            }

            IEnumerable<QuestionBatch> eligible = _questions;

            // If we reached the end of the bomb, everything is eligible.
            if (numSolved < numPlayableModules)
                // Otherwise, make sure there has been another solved module since
                eligible = eligible.Where(e => e.NumSolved < numSolved);

            var numEligibles = eligible.Count();

            if ((numSolved < numPlayableModules && numEligibles < 3) || numEligibles == 0)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            var batch = eligible.PickRandom();
            _questions.Remove(batch);
            if (batch.Questions.Length == 0)
                continue;

            SetQuestion(batch.Questions.PickRandom());
            while (_currentQuestion != null || _animating)
                yield return new WaitForSeconds(.5f);
        }

        Debug.LogFormat("[Souvenir #{0}] Questions exhausted. Module solved.", _moduleId);
        _isSolved = true;
        Module.HandlePass();
    }

    private void SetQuestion(QandA q)
    {
        Debug.LogFormat("[Souvenir #{0}] Asking question: {1}", _moduleId, q.DebugString);
        _currentQuestion = q;
        SetWordWrappedText(q.QuestionText, q.DesiredHeightFactor);
        q.SetAnswers(this);
        AnswersParent.SetActive(true);
        Audio.PlaySoundAtTransform("Question", transform);
    }

    private static readonly double[][] _acceptableWidths = Ut.NewArray(
        // First value is y (vertical text advancement), second value is width of the Surface mesh at this y
        new[] { 0.834 - 0.834, 0.834 + 0.3556 },
        new[] { 0.834 - 0.7628, 0.834 + 0.424 },
        new[] { 0.834 - 0.6864, 0.834 + 0.424 },
        new[] { 0.834 - 0.528, 0.834 + 0.5102 },
        new[] { 0.834 - 0.4452, 0.834 + 0.6618 },
        new[] { 0.834 - 0.4452, 0.834 + 0.7745 },
        new[] { 0.834 - 0.391, 0.834 + 0.834 }
    );

    private void SetWordWrappedText(string text, double desiredHeightFactor)
    {
        var low = 1;
        var high = 256;
        var desiredHeight = desiredHeightFactor * SurfaceSizeFactor;
        var wrappeds = new Dictionary<int, string>();
        var origRotation = TextMesh.transform.rotation;
        TextMesh.transform.eulerAngles = new Vector3(90, 0, 0);

        while (high - low > 1)
        {
            var mid = (low + high) / 2;
            TextMesh.fontSize = mid;

            TextMesh.text = "\u00a0";
            var size = TextRenderer.bounds.size;
            var widthOfASpace = size.x;
            var heightOfALine = size.z;
            var wrapWidths = new List<double>();

            var wrappedSB = new StringBuilder();
            var first = true;
            foreach (var line in Ut.WordWrap(
                text,
                line =>
                {
                    var y = line * heightOfALine / SurfaceSizeFactor;
                    if (line < wrapWidths.Count)
                        return wrapWidths[line];
                    while (wrapWidths.Count < line)
                        wrapWidths.Add(0);
                    var i = 1;
                    while (i < _acceptableWidths.Length && _acceptableWidths[i][0] < y)
                        i++;
                    if (i == _acceptableWidths.Length)
                        wrapWidths.Add(_acceptableWidths[i - 1][1] * SurfaceSizeFactor);
                    else
                    {
                        var lambda = (y - _acceptableWidths[i - 1][0]) / (_acceptableWidths[i][0] - _acceptableWidths[i - 1][0]);
                        wrapWidths.Add((_acceptableWidths[i - 1][1] * (1 - lambda) + _acceptableWidths[i][1] * lambda) * SurfaceSizeFactor);
                    }

                    return wrapWidths[line];
                },
                widthOfASpace,
                str =>
                {
                    TextMesh.text = str;
                    return TextRenderer.bounds.size.x;
                },
                allowBreakingWordsApart: false
            ))
            {
                if (line == null)
                {
                    // There was a word that was too long to fit into a line.
                    high = mid;
                    wrappedSB = null;
                    break;
                }
                if (!first)
                    wrappedSB.Append('\n');
                first = false;
                wrappedSB.Append(line);
            }

            if (wrappedSB != null)
            {
                var wrapped = wrappedSB.ToString();
                wrappeds[mid] = wrapped;
                TextMesh.text = wrapped;
                size = TextRenderer.bounds.size;
                if (size.z > desiredHeight)
                    high = mid;
                else
                    low = mid;
            }
        }

        TextMesh.fontSize = low;
        TextMesh.text = wrappeds[low];
        TextMesh.transform.rotation = origRotation;
        TextMesh.gameObject.SetActive(true);
    }

    private IEnumerator ProcessModule(KMBombModule module)
    {
        _coroutinesActive++;
        var moduleType = module.ModuleType;
        _moduleCounts.IncSafe(moduleType);
        var iterator = _moduleProcessors.Get(moduleType, null);

        if (iterator != null)
        {
            supportedModuleNames.Add(module.ModuleDisplayName);
            yield return null;  // Ensures that the module’s Start() method has run
            Debug.LogFormat("<Souvenir #{1}> Module {0}: Start processing.", moduleType, _moduleId);

            // I’d much rather just put a ‘foreach’ loop inside a ‘try’ block, but Unity’s C# version doesn’t allow ‘yield return’ inside of ‘try’ blocks yet
            using (var e = iterator(module).GetEnumerator())
            {
                while (true)
                {
                    bool canMoveNext;
                    try { canMoveNext = e.MoveNext(); }
                    catch (AbandonModuleException ex)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning {1} because: {2}", _moduleId, module.ModuleDisplayName, ex.Message);
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogFormat("<Souvenir #{0}> The {1} handler threw an exception ({2}):\n{3}", _moduleId, module.ModuleDisplayName, ex.GetType().FullName, ex.StackTrace);
                        yield break;
                    }
                    if (TwitchAbandonModule.Contains(module))
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning {1} because Twitch Plays told me to.", _moduleId, module.ModuleDisplayName);
                        yield break;
                    }
                    if (!canMoveNext)
                        break;
                    yield return e.Current;
                }
            }

            if (!_legitimatelyNoQuestions.Contains(module) && !_questions.Any(q => q.Module == module))
            {
                Debug.LogFormat("[Souvenir #{0}] There was no question generated for {1}. Please report this to Andrio or the implementer for that module as this may indicate a bug in Souvenir. Remember to send them this logfile.", _moduleId, module.ModuleDisplayName);
                WarningIcon.SetActive(true);
            }
            Debug.LogFormat("<Souvenir #{1}> Module {0}: Finished processing.", moduleType, _moduleId);
        }
        else
        {
            Debug.LogFormat("<Souvenir #{1}> Module {0}: Not supported.", moduleType, _moduleId);
        }

        _coroutinesActive--;
    }
    #endregion

    #region Helper methods for Reflection (used by module handlers)
    private Component GetComponent(KMBombModule module, string name)
    {
        return GetComponent(module.gameObject, name);
    }
    private Component GetComponent(GameObject module, string name)
    {
        var comp = module.GetComponent(name);
        if (comp == null)
        {
            comp = module.GetComponents(typeof(Component)).FirstOrDefault(c => c.GetType().FullName == name);
            if (comp == null)
                throw new AbandonModuleException("{0} game object has no {1} component. Components are: {2}", module.name, name, module.GetComponents(typeof(Component)).Select(c => c.GetType().FullName).JoinString(", "));
        }
        return comp;
    }

    private FieldInfo<T> GetField<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {1} field {0} of type {2} from a null object.", name, isPublic ? "public" : "non-public", typeof(T).FullName);
        return new FieldInfo<T>(target, GetFieldImpl<T>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private FieldInfo<T> GetField<T>(Type targetType, string name, bool isPublic = false, bool noThrow = false)
    {
        if (targetType == null && !noThrow)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type {2} from a null type.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new FieldInfo<T>(null, GetFieldImpl<T>(targetType, name, isPublic, BindingFlags.Instance, noThrow));
    }

    private IntFieldInfo GetIntField(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type int from a null object.", isPublic ? "public" : "non-public", name);
        return new IntFieldInfo(target, GetFieldImpl<int>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private ArrayFieldInfo<T> GetArrayField<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type {2}[] from a null object.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new ArrayFieldInfo<T>(target, GetFieldImpl<T[]>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private ListFieldInfo<T> GetListField<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {0} field {1} of type List<{2}> from a null object.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new ListFieldInfo<T>(target, GetFieldImpl<List<T>>(target.GetType(), name, isPublic, BindingFlags.Instance));
    }

    private FieldInfo<T> GetStaticField<T>(Type targetType, string name, bool isPublic = false)
    {
        if (targetType == null)
            throw new AbandonModuleException("Attempt to get {0} static field {1} of type {2} from a null type.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return new FieldInfo<T>(null, GetFieldImpl<T>(targetType, name, isPublic, BindingFlags.Static));
    }

    private FieldInfo GetFieldImpl<T>(Type targetType, string name, bool isPublic, BindingFlags bindingFlags, bool noThrow = false)
    {
        FieldInfo fld; Type type = targetType;
        while (type != null && type != typeof(object))
        {
            fld = type.GetField(name, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | bindingFlags);
            if (fld != null)
                goto found;

            // In case it’s actually an auto-implemented property and not a field.
            fld = type.GetField("<" + name + ">k__BackingField", BindingFlags.NonPublic | bindingFlags);
            if (fld != null)
                goto found;

            // Reflection won’t return private fields in base classes unless we check those explicitly
            type = type.BaseType;
        }

        if (noThrow)
            return null;
        throw new AbandonModuleException("Type {0} does not contain {1} field {2}. Fields are: {3}", targetType, isPublic ? "public" : "non-public", name,
            targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(f => string.Format("{0} {1} {2}", f.IsPublic ? "public" : "private", f.FieldType.FullName, f.Name)).JoinString(", "));

        found:
        if (!typeof(T).IsAssignableFrom(fld.FieldType))
        {
            if (noThrow)
                return null;
            throw new AbandonModuleException("Type {0} has {1} field {2} of type {3} but expected type {4}.", targetType, isPublic ? "public" : "non-public", name, fld.FieldType.FullName, typeof(T).FullName);
        }
        return fld;
    }

    private MethodInfo<T> GetMethod<T>(object target, string name, int numParameters, bool isPublic = false)
    {
        return GetMethodImpl<T>(typeof(T), target, name, numParameters, isPublic);
    }

    private MethodInfo<object> GetMethod(object target, string name, int numParameters, bool isPublic = false)
    {
        return GetMethodImpl<object>(typeof(void), target, name, numParameters, isPublic);
    }

    private MethodInfo<T> GetMethodImpl<T>(Type returnType, object target, string name, int numParameters, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {1} method {0} of return type {2} from a null object.", name, isPublic ? "public" : "non-public", returnType.FullName);

        var bindingFlags = (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | BindingFlags.Instance;
        var targetType = target.GetType();
        var mths = targetType.GetMethods(bindingFlags).Where(m => m.Name == name && m.GetParameters().Length == numParameters && returnType.IsAssignableFrom(m.ReturnType)).Take(2).ToArray();
        if (mths.Length == 0)
            throw new AbandonModuleException("Type {0} does not contain {1} method {2} with return type {3} and {4} parameters.", targetType, isPublic ? "public" : "non-public", name, returnType.FullName, numParameters);
        if (mths.Length > 1)
            throw new AbandonModuleException("Type {0} contains multiple {1} methods {2} with return type {3} and {4} parameters.", targetType, isPublic ? "public" : "non-public", name, returnType.FullName, numParameters);
        return new MethodInfo<T>(target, mths[0]);
    }

    private PropertyInfo<T> GetProperty<T>(object target, string name, bool isPublic = false)
    {
        if (target == null)
            throw new AbandonModuleException("Attempt to get {1} property {0} of type {2} from a null object.", name, isPublic ? "public" : "non-public", typeof(T).FullName);
        return GetPropertyImpl<T>(target, target.GetType(), name, isPublic, BindingFlags.Instance);
    }

    private PropertyInfo<T> GetStaticProperty<T>(Type targetType, string name, bool isPublic = false)
    {
        if (targetType == null)
            throw new AbandonModuleException("Attempt to get {0} static property {1} of type {2} from a null type.", isPublic ? "public" : "non-public", name, typeof(T).FullName);
        return GetPropertyImpl<T>(null, targetType, name, isPublic, BindingFlags.Static);
    }

    private PropertyInfo<T> GetPropertyImpl<T>(object target, Type targetType, string name, bool isPublic, BindingFlags bindingFlags)
    {
        var fld = targetType.GetProperty(name, (isPublic ? BindingFlags.Public : BindingFlags.NonPublic) | bindingFlags);
        if (fld == null)
            throw new AbandonModuleException("Type {0} does not contain {1} property {2}. Properties are: {3}", targetType, isPublic ? "public" : "non-public", name,
                targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Select(f => string.Format("{0} {1} {2}", f.GetGetMethod().IsPublic ? "public" : "private", f.PropertyType.FullName, f.Name)).JoinString(", "));
        if (!typeof(T).IsAssignableFrom(fld.PropertyType))
            throw new AbandonModuleException("Type {0} has {1} field {2} of type {3} but expected type {4}.", targetType, isPublic ? "public" : "non-public", name, fld.PropertyType.FullName, typeof(T).FullName, _moduleId);
        return new PropertyInfo<T>(target, fld);
    }
    #endregion

    #region Other helper methods (used by module handlers)
    private void addQuestion(KMBombModule module, Question question, string[] formatArguments = null, string[] correctAnswers = null, string[] preferredWrongAnswers = null)
    {
        addQuestions(module, makeQuestion(question, module.ModuleType, formatArguments, correctAnswers, preferredWrongAnswers));
    }

    private void addQuestion(KMBombModule module, Question question, string[] formatArguments = null, Sprite[] correctAnswers = null, Sprite[] preferredWrongAnswers = null)
    {
        addQuestions(module, makeQuestion(question, module.ModuleType, formatArguments, correctAnswers, preferredWrongAnswers));
    }

    private void addQuestions(KMBombModule module, IEnumerable<QandA> questions)
    {
        var qs = questions.Where(q => q != null).ToArray();
        if (qs.Length == 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Empty question batch provided for {1}.", _moduleId, module.ModuleDisplayName);
            return;
        }
        Debug.LogFormat("<Souvenir #{0}> Adding question batch:\n{1}", _moduleId, qs.Select(q => "    • " + q.DebugString).JoinString("\n"));
        _questions.Add(new QuestionBatch
        {
            NumSolved = Bomb.GetSolvedModuleNames().Count,
            Questions = qs,
            Module = module
        });
    }

    private void addQuestions(KMBombModule module, params QandA[] questions)
    {
        addQuestions(module, (IEnumerable<QandA>) questions);
    }

    private string titleCase(string str)
    {
        return str.Length < 1 ? str : char.ToUpperInvariant(str[0]) + str.Substring(1).ToLowerInvariant();
    }

    private QandA makeQuestion(Question question, string moduleKey, string[] formatArgs = null, string[] correctAnswers = null, string[] preferredWrongAnswers = null)
    {
        return makeQuestion(question, moduleKey,
            (attr, q, correct, answers) =>
            {
                if (attr.Type == AnswerType.DynamicFont || attr.Type == AnswerType.Sprites)
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> The module handler for {1} attempted to output a question that requires a sprite or dynamic font, but didn’t supply one.", _moduleId, moduleKey);
                    throw new InvalidOperationException();
                }
                return new QandAText(attr.ModuleNameWithThe, q, correct, answers.ToArray(), Fonts[(int) attr.Type], FontTextures[(int) attr.Type], FontMaterial, attr.Layout);
            },
            formatArgs, correctAnswers, preferredWrongAnswers);
    }

    private QandA makeQuestion(Question question, string moduleKey, Font font, Texture fontTexture, string[] formatArgs = null, string[] correctAnswers = null, string[] preferredWrongAnswers = null)
    {
        return makeQuestion(question, moduleKey,
            (attr, q, correct, answers) =>
            {
                if (attr.Type != AnswerType.DynamicFont)
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> The module handler for {1} attempted to use a dynamic font but the corresponding question is not marked as AnswerType.DynamicFont.", _moduleId, moduleKey);
                    throw new InvalidOperationException();
                }
                return new QandAText(attr.ModuleNameWithThe, q, correct, answers.ToArray(), font, fontTexture, FontMaterial, attr.Layout);
            },
            formatArgs, correctAnswers, preferredWrongAnswers);
    }

    private QandA makeQuestion(Question question, string moduleKey, string[] formatArgs = null, Sprite[] correctAnswers = null, Sprite[] preferredWrongAnswers = null)
    {
        return makeQuestion(question, moduleKey,
            (attr, q, correct, answers) =>
            {
                if (attr.Type != AnswerType.Sprites)
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> The module handler for {1} attempted to use a sprite but the corresponding question is not marked as AnswerType.Sprite.", _moduleId, moduleKey);
                    throw new InvalidOperationException();
                }
                return new QandASprite(attr.ModuleNameWithThe, q, correct, answers.ToArray());
            },
            formatArgs, correctAnswers, preferredWrongAnswers);
    }

    private QandA makeQuestion<T>(Question question, string moduleKey, Func<SouvenirQuestionAttribute, string, int, T[], QandA> questionConstructor, string[] formatArgs = null, T[] correctAnswers = null, T[] preferredWrongAnswers = null)
    {
        SouvenirQuestionAttribute attr;
        if (!_attributes.TryGetValue(question, out attr))
        {
            Debug.LogErrorFormat("<Souvenir #{1}> Question {0} has no SouvenirQuestionAttribute.", question, _moduleId);
            return null;
        }

        var allAnswers = attr.AllAnswers as T[];
        if (allAnswers != null)
        {
            var inconsistency = correctAnswers.Except(allAnswers).FirstOrDefault();
            if (inconsistency != null)
            {
                Debug.LogErrorFormat("<Souvenir #{2}> Question {0}: invalid answer: {1}.", question, inconsistency.ToString() ?? "<null>", _moduleId);
                return null;
            }
            if (preferredWrongAnswers != null)
            {
                var inconsistency2 = preferredWrongAnswers.Except(allAnswers).FirstOrDefault();
                if (inconsistency2 != null)
                {
                    Debug.LogErrorFormat("<Souvenir #{2}> Question {0}: invalid preferred wrong answer: {1}.", question, inconsistency2.ToString() ?? "<null>", _moduleId);
                    return null;
                }
            }
        }

        var answers = new List<T>(attr.NumAnswers);
        if (allAnswers == null && attr.AnswerGenerator == null)
        {
            if (preferredWrongAnswers == null || preferredWrongAnswers.Length == 0)
            {
                Debug.LogErrorFormat("<Souvenir #{0}> Question {1} has no answers. You must specify either the full set of possible answers in SouvenirQuestionAttribute.AllAnswers, provide possible wrong answers through the preferredWrongAnswers parameter, or add an AnswerGeneratorAttribute to the question enum value.", _moduleId, question);
                return null;
            }
            answers.AddRange(preferredWrongAnswers.Except(correctAnswers).Distinct());
        }
        else
        {
            // Pick 𝑛−1 random wrong answers.
            if (allAnswers != null) answers.AddRange(allAnswers.Except(correctAnswers));
            if (answers.Count <= attr.NumAnswers - 1)
            {
                if (attr.AnswerGenerator != null && typeof(T) == typeof(string))
                    answers.AddRange(attr.AnswerGenerator.GetAnswers(this).Except(answers.Concat(correctAnswers) as IEnumerable<string>).Distinct().Take(attr.NumAnswers - 1 - answers.Count) as IEnumerable<T>);
                if (answers.Count == 0 && (preferredWrongAnswers == null || preferredWrongAnswers.Length == 0))
                {
                    Debug.LogErrorFormat("<Souvenir #{0}> Question {1}'s answer generator did not generate any answers.", _moduleId, question);
                    return null;
                }
            }
            else
            {
                answers.Shuffle();
                answers.RemoveRange(attr.NumAnswers - 1, answers.Count - (attr.NumAnswers - 1));
            }
            // Add the preferred wrong answers, if any. If we had added them earlier, they’d come up too rarely.
            if (preferredWrongAnswers != null)
                answers.AddRange(preferredWrongAnswers.Except(answers.Concat(correctAnswers)).Distinct());
        }
        answers.Shuffle();
        if (answers.Count >= attr.NumAnswers) answers.RemoveRange(attr.NumAnswers - 1, answers.Count - (attr.NumAnswers - 1));

        var correctIndex = Rnd.Range(0, answers.Count + 1);
        answers.Insert(correctIndex, correctAnswers.PickRandom());

        var numSolved = _modulesSolved.Get(moduleKey);
        if (numSolved < 1)
        {
            Debug.LogErrorFormat("<Souvenir #{0}> Abandoning {1} ({2}) because you forgot to increment the solve count.", _moduleId, attr.ModuleName, moduleKey);
            return null;
        }

        var allFormatArgs = new string[formatArgs != null ? formatArgs.Length + 1 : 1];
        allFormatArgs[0] = _moduleCounts.Get(moduleKey) > 1
            ? string.Format("the {0} you solved {1}", attr.ModuleName, ordinal(numSolved))
            : attr.AddThe ? "The\u00a0" + attr.ModuleName : attr.ModuleName;
        if (formatArgs != null)
            Array.Copy(formatArgs, 0, allFormatArgs, 1, formatArgs.Length);

        return questionConstructor(attr, string.Format(attr.QuestionText, allFormatArgs), correctIndex, answers.ToArray());
    }

    internal string[] GetAnswers(Question question)
    {
        SouvenirQuestionAttribute attr;
        if (!_attributes.TryGetValue(question, out attr))
            throw new InvalidOperationException(string.Format("<Souvenir #{0}> Question {1} is missing from the _attributes dictionary.", _moduleId, question));
        return attr.AllAnswers;
    }

    private string ordinal(int number)
    {
        if (number < 0)
            return "(" + number + ")th";

        switch (number)
        {
            case 1: return "first";
            case 2: return "second";
            case 3: return "third";
        }

        switch ((number / 10) % 10 == 1 ? 0 : number % 10)
        {
            case 1: return number + "st";
            case 2: return number + "nd";
            case 3: return number + "rd";
            default: return number + "th";
        }
    }
    #endregion

    #region Module handlers
    /* Generalized handlers for modules that are extremely similar */

    private IEnumerable<object> processSpeakingEvilCycle1(KMBombModule module, string componentName, Question question, string moduleId)
    {
        var comp = GetComponent(module, componentName);
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(moduleId);

        var messages = GetArrayField<string>(comp, "message").Get();
        var responses = GetArrayField<string>(comp, "response").Get();
        var index = GetIntField(comp, "r").Get(ix =>
            ix < 0 ? "negative" :
            ix >= messages.Length ? string.Format("greater than ‘message’ length ({0})", messages.Length) :
            ix >= responses.Length ? string.Format("greater than ‘response’ length ({0})", responses.Length) : null);

        addQuestions(module,
          makeQuestion(question, moduleId, new[] { "message" }, new[] { Regex.Replace(messages[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }),
          makeQuestion(question, moduleId, new[] { "response" }, new[] { Regex.Replace(responses[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }));
    }

    private IEnumerable<object> processSpeakingEvilCycle2(KMBombModule module, string componentName, string moduleName, Question question, string moduleId)
    {
        var comp = GetComponent(module, componentName);
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(moduleId);

        var words = GetArrayField<string[]>(comp, "message").Get(expectedLength: 2);
        var messages = words[0];
        var responses = words[1];
        var index = GetIntField(comp, "r").Get(ix =>
            ix < 0 ? "‘r’ is negative." :
            ix >= messages.Length ? string.Format("‘r’ is greater than ‘message’ length ({0}).", messages.Length) :
            ix >= responses.Length ? string.Format("‘r’ is greater than ‘response’ length ({0}).", responses.Length) : null);

        addQuestions(module,
          makeQuestion(question, moduleId, new[] { "message" }, new[] { Regex.Replace(messages[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }),
          makeQuestion(question, moduleId, new[] { "response" }, new[] { Regex.Replace(responses[index], @"(?<!^).", m => m.Value.ToLowerInvariant()) }));
    }


    /* Actual module processors start here */

    private IEnumerable<object> Process3DMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "ThreeDMazeModule");
        var fldIsComplete = GetField<bool>(comp, "isComplete");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var map = GetField<object>(comp, "map").Get();
        var mapData = GetField<Array>(map, "mapData").Get(arr => arr.GetLength(0) != 8 || arr.GetLength(1) != 8 ? string.Format("size {0},{1}, expected 8,8", arr.GetLength(0), arr.GetLength(1)) : null);
        var fldLabel = GetField<char>(mapData.GetValue(0, 0), "label", isPublic: true);
        var chars = new HashSet<char>();
        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
            {
                var ch = fldLabel.GetFrom(mapData.GetValue(i, j));
                if ("ABCDH".Contains(ch))
                    chars.Add(ch);
            }
        var correctMarkings = chars.OrderBy(c => c).JoinString();

        char bearing;
        if (correctMarkings == "ABC") bearing = fldLabel.GetFrom(mapData.GetValue(1, 1));
        else if (correctMarkings == "ABD") bearing = fldLabel.GetFrom(mapData.GetValue(7, 0));
        else if (correctMarkings == "ABH") bearing = fldLabel.GetFrom(mapData.GetValue(0, 1));
        else if (correctMarkings == "ACD") bearing = fldLabel.GetFrom(mapData.GetValue(1, 2));
        else if (correctMarkings == "ACH") bearing = fldLabel.GetFrom(mapData.GetValue(0, 1));
        else if (correctMarkings == "ADH") bearing = fldLabel.GetFrom(mapData.GetValue(5, 0));
        else if (correctMarkings == "BCD") bearing = fldLabel.GetFrom(mapData.GetValue(6, 1));
        else if (correctMarkings == "BCH") bearing = fldLabel.GetFrom(mapData.GetValue(2, 2));
        else if (correctMarkings == "BDH") bearing = fldLabel.GetFrom(mapData.GetValue(3, 1));
        else if (correctMarkings == "CDH") bearing = fldLabel.GetFrom(mapData.GetValue(5, 1));
        else
        {
            Debug.LogFormat(@"<Souvenir #{1}> Abandoning 3D Maze because unexpected markings: ""{0}"".", correctMarkings, _moduleId);
            yield break;
        }

        if (!"NSWE".Contains(bearing))
        {
            Debug.LogFormat("<Souvenir #{1}> Abandoning 3D Maze because unexpected bearing: '{0}'.", bearing, _moduleId);
            yield break;
        }

        while (!fldIsComplete.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_3DMaze);
        addQuestions(module,
            makeQuestion(Question._3DMazeMarkings, _3DMaze, correctAnswers: new[] { correctMarkings }),
            makeQuestion(Question._3DMazeBearing, _3DMaze, correctAnswers: new[] { bearing == 'N' ? "North" : bearing == 'S' ? "South" : bearing == 'W' ? "West" : "East" }));
    }

    private IEnumerable<object> Process3DTunnels(KMBombModule module)
    {
        var comp = GetComponent(module, "ThreeDTunnels");
        var fldSolved = GetField<bool>(comp, "_solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_3DTunnels);

        var symbols = GetStaticField<string>(comp.GetType(), "_symbols").Get();
        var targetNodeNames = GetField<List<int>>(comp, "_targetNodes")
            .Get(tns => tns.Any(tn => tn < 0 || tn >= symbols.Length) ? "invalid symbols" : null)
            .Select(tn => symbols[tn].ToString())
            .ToArray();
        addQuestions(module, targetNodeNames.Select((tn, ix) => makeQuestion(Question._3DTunnelsTargetNode, _3DTunnels, new[] { ordinal(ix + 1) }, new[] { tn }, targetNodeNames)));
    }

    private IEnumerable<object> ProcessAccumulation(KMBombModule module)
    {
        var comp = GetComponent(module, "accumulationScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Accumulation);

        var colorNames = new Dictionary<int, string> {
            { 9, "Blue" },
            { 23, "Brown" },
            { 4, "Green" },
            { 15, "Grey" },
            { 26, "Lime" },
            { 2, "Orange" },
            { 8, "Pink" },
            { 17, "Red" },
            { 11, "White" },
            { 10, "Yellow" }
        };

        var borderIx = GetIntField(comp, "borderValue").Get(v => !colorNames.ContainsKey(v) ? "value is not in the dictionary" : null);
        var bgNames = GetArrayField<Material>(comp, "chosenBackgroundColours", isPublic: true)
            .Get(expectedLength: 5)
            .Select(x => char.ToUpperInvariant(x.name[0]) + x.name.Substring(1))
            .ToArray();

        addQuestions(module,
            makeQuestion(Question.AccumulationBorderColor, _Accumulation, correctAnswers: new[] { colorNames[borderIx] }),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "first" }, new[] { bgNames[0] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "second" }, new[] { bgNames[1] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "third" }, new[] { bgNames[2] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "fourth" }, new[] { bgNames[3] }, bgNames),
            makeQuestion(Question.AccumulationBackgroundColor, _Accumulation, new[] { "fifth" }, new[] { bgNames[4] }, bgNames));
    }

    private IEnumerable<object> ProcessAdventureGame(KMBombModule module)
    {
        var comp = GetComponent(module, "AdventureGameModule");
        var fldInvWeaponCount = GetIntField(comp, "InvWeaponCount");
        var fldSelectedItem = GetIntField(comp, "SelectedItem");
        var mthItemName = GetMethod<string>(comp, "ItemName", 1);
        var mthShouldUseItem = GetMethod<bool>(comp, "ShouldUseItem", 1);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var invValues = GetField<IList>(comp, "InvValues").Get();   // actually List<AdventureGameModule.ITEM>
        var buttonUse = GetField<KMSelectable>(comp, "ButtonUse", isPublic: true).Get(b => b.OnInteract == null ? "ButtonUse.OnInteract is null" : null);
        var enemy = GetField<object>(comp, "SelectedEnemy").Get();
        var textEnemy = GetField<TextMesh>(comp, "TextEnemy", isPublic: true).Get();
        var invWeaponCount = fldInvWeaponCount.Get(v => v == 0 ? "zero" : null);
        var numWeapons = GetIntField(comp, "NumWeapons").Get(v => v == 0 ? "zero" : null);

        var prevInteract = buttonUse.OnInteract;
        var origInvValues = new List<int>(invValues.Cast<int>());
        var correctItemsUsed = 0;
        var qs = new List<Func<QandA>>();
        var solved = false;

        buttonUse.OnInteract = delegate
        {
            var selectedItem = fldSelectedItem.Get();
            var itemUsed = origInvValues[selectedItem];
            var shouldUse = mthShouldUseItem.Invoke(selectedItem);
            for (int j = invWeaponCount; j < invValues.Count; j++)
                shouldUse &= !mthShouldUseItem.Invoke(j);

            var ret = prevInteract();

            if (invValues.Count != origInvValues.Count)
            {
                // If the length of the inventory has changed, the user used a correct non-weapon item.
                var itemIndex = ++correctItemsUsed;
                qs.Add(() => makeQuestion(Question.AdventureGameCorrectItem, _AdventureGame, new[] { ordinal(itemIndex) }, new[] { titleCase(mthItemName.Invoke(itemUsed)) }));
                origInvValues.Clear();
                origInvValues.AddRange(invValues.Cast<int>());
            }
            else if (shouldUse)
            {
                // The user solved the module.
                solved = true;
                textEnemy.text = "Victory!";
            }

            return ret;
        };

        while (!solved)
            yield return new WaitForSeconds(.1f);

        buttonUse.OnInteract = prevInteract;
        _modulesSolved.IncSafe(_AdventureGame);
        var enemyName = enemy.ToString();
        enemyName = enemyName.Substring(0, 1).ToUpperInvariant() + enemyName.Substring(1).ToLowerInvariant();
        addQuestions(module, qs.Select(q => q()).Concat(new[] { makeQuestion(Question.AdventureGameEnemy, _AdventureGame, correctAnswers: new[] { enemyName }) }));
    }

    private IEnumerable<object> ProcessAffineCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "AffineCycleScript", Question.AffineCycleWord, _AffineCycle);
    }

    private IEnumerable<object> ProcessAlgebra(KMBombModule module)
    {
        var comp = GetComponent(module, "algebraScript");
        var fldStage = GetIntField(comp, "stage");

        while (fldStage.Get() <= 3)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Algebra);

        addQuestions(module, Enumerable.Range(0, 2).Select(i => makeQuestion(
            question: i == 0 ? Question.AlgebraEquation1 : Question.AlgebraEquation2,
            moduleKey: _Algebra,
            correctAnswers: new[] { GetField<Texture>(comp, string.Format("level{0}Equation", i + 1)).Get().name.Replace(';', '/') })));
    }

    private IEnumerable<object> ProcessAlphabeticalRuling(KMBombModule module)
    {
        var comp = GetComponent(module, "AlphabeticalRuling");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldStage = GetIntField(comp, "currentStage");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var letterDisplay = GetField<TextMesh>(comp, "LetterDisplay", isPublic: true).Get();
        var numberDisplays = GetArrayField<TextMesh>(comp, "NumberDisplays", isPublic: true).Get(expectedLength: 2);
        var curStage = 0;
        var letters = new char[3];
        var numbers = new int[3];
        while (!fldSolved.Get())
        {
            var newStage = fldStage.Get();
            if (newStage != curStage)
            {
                if (letterDisplay.text.Length != 1 || letterDisplay.text[0] < 'A' || letterDisplay.text[0] > 'Z')
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Alphabetical Ruling because ‘LetterDisplay’ shows {1} (expected single letter A–Z).", _moduleId, letterDisplay.text);
                    yield break;
                }
                letters[newStage - 1] = letterDisplay.text[0];
                int number;
                if (!int.TryParse(numberDisplays[0].text, out number) || number < 1 || number > 9)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Alphabetical Ruling because ‘NumberDisplay[0]’ shows {1} (expected integer 1–9).", _moduleId, numberDisplays[0].text);
                    yield break;
                }
                numbers[newStage - 1] = number;
                curStage = newStage;
            }

            yield return null;
        }
        _modulesSolved.IncSafe(_AlphabeticalRuling);

        if (letters.Any(l => l < 'A' || l > 'Z') || numbers.Any(n => n < 1 || n > 9))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Alphabetical Ruling because the captured letters/numbers are unexpected (letters: [{1}], numbers: [{2}]).", _moduleId, letters.JoinString(", "), numbers.JoinString(", "));
            yield break;
        }

        var qs = new List<QandA>();
        for (var ix = 0; ix < letters.Length; ix++)
            qs.Add(makeQuestion(Question.AlphabeticalRulingLetter, _AlphabeticalRuling, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { letters[ix].ToString() }));
        for (var ix = 0; ix < numbers.Length; ix++)
            qs.Add(makeQuestion(Question.AlphabeticalRulingNumber, _AlphabeticalRuling, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { numbers[ix].ToString() }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessAlphaBits(KMBombModule module)
    {
        var comp = GetComponent(module, "AlphaBitsScript");

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };

        var displayedCharacters = new[] { "displayTL", "displayML", "displayBL", "displayTR", "displayMR", "displayBR" }.Select(fieldName => GetField<TextMesh>(comp, fieldName, isPublic: true).Get().text.Trim()).ToArray();
        if (displayedCharacters.Any(ch => ch.Length != 1 || ((ch[0] < 'A' || ch[0] > 'V') && (ch[0] < '0' || ch[0] > '9'))))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Alpha-Bits because the displayed characters are {1} (expected six single-character strings 0–9/A–V each).",
                _moduleId, displayedCharacters.Select(str => string.Format(@"""{0}""", str)).JoinString(", "));
            yield break;
        }

        while (!isSolved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_AlphaBits);

        // If the correct answer is '0' or 'O', don't include these as wrong answers.
        addQuestions(module, Enumerable.Range(0, 6).Select(displayIx => makeQuestion(
            Question.AlphaBitsDisplayedCharacters,
            _AlphaBits,
            formatArgs: new[] { new[] { "top", "middle", "bottom" }[displayIx % 3], new[] { "left", "right" }[displayIx / 3] },
            correctAnswers: new[] { displayedCharacters[displayIx] },
            preferredWrongAnswers: new AnswerGenerator.Strings(displayedCharacters[displayIx] == "0" || displayedCharacters[displayIx] == "O" ? "1-9A-NP-V" : "0-9A-V")
                .GetAnswers(this).Distinct().Take(6).ToArray())));
    }

    private IEnumerable<object> ProcessArithmelogic(KMBombModule module)
    {
        var comp = GetComponent(module, "Arithmelogic");
        var fldSymbolNum = GetIntField(comp, "submitSymbol");
        var fldSelectableValues = GetArrayField<int[]>(comp, "selectableValues");
        var fldCurrentDisplays = GetArrayField<int>(comp, "currentDisplays");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Arithmelogic);

        var symbolNum = fldSymbolNum.Get(min: 0, max: 21);
        var selVal = fldSelectableValues.Get(expectedLength: 3, validator: arr => arr.Length != 4 ? string.Format("length {0}, expected 4", arr.Length) : null);
        var curDisp = fldCurrentDisplays.Get(expectedLength: 3, validator: val => val < 0 || val >= 4 ? string.Format("expected 0–3") : null);

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.ArithmelogicSubmit, _Arithmelogic, correctAnswers: new[] { ArithmelogicSprites[symbolNum] }, preferredWrongAnswers: ArithmelogicSprites));
        var screens = new[] { "left", "middle", "right" };
        for (int i = 0; i < 3; i++)
            qs.Add(makeQuestion(Question.ArithmelogicNumbers, _Arithmelogic, formatArgs: new[] { screens[i] },
                correctAnswers: Enumerable.Range(0, 4).Where(ix => ix != curDisp[i]).Select(ix => selVal[i][ix].ToString()).ToArray()));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessBamboozledAgain(KMBombModule module)
    {
        var comp = GetComponent(module, "BamboozledAgainScript");
        var fldDisplayTexts = GetArrayField<string[]>(comp, "message");
        var fldColorIndex = GetArrayField<int>(comp, "textRandomiser");
        var fldStage = GetIntField(comp, "pressCount");
        var fldCorrectButtons = GetArrayField<int[]>(comp, "answerKey");
        var fldButtonInfo = GetArrayField<string[]>(comp, "buttonRandomiser");
        var fldButtonTextMesh = GetArrayField<TextMesh>(comp, "buttonText", isPublic: true);

        //Beginning of correct button section.

        int stage = 0;

        string[] correctButtonTexts = new string[4];
        string[] correctButtonColors = new string[4];

        //The module cycle the stage count back to 0 regardless. So it gives no indications whether the module is solved or not on the fourth press.
        //Stores the first button in a separate variable. Then, restore it once the module is solved. Index 0 for text. Index 1 for color.

        string[] correctFirstStageButton = new string[2];

        bool dataAdded = false;

        //Not certain why, but the variable 'moduleSolved' in Bamboozled Again script becomes true at the start of the submit couroutine even though the answer may not be correct.
        //Hooking isSolved variable to mitigate the possible side effects.

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };

        while (!isSolved)
        {
            var newStage = fldStage.Get(min: 0, max: 3);
            if (!dataAdded)
            {
                var buttonInfo = fldButtonInfo.Get(expectedLength: 2, validator: v => v.Length != 6 ? "expected length 6" : null);
                var correctButtons = fldCorrectButtons.Get(expectedLength: 2, validator: v => v.Length != 4 ? "expected length 4" : null);
                if (stage == 0)
                {
                    correctFirstStageButton[0] = correctButtonTexts[stage];
                    correctFirstStageButton[1] = correctButtonColors[stage];
                }
                correctButtonTexts[stage] = Regex.Replace(buttonInfo[1][correctButtons[0][stage]], "#", " ");
                correctButtonColors[stage] = buttonInfo[0][correctButtons[0][stage]][0] + buttonInfo[0][correctButtons[0][stage]].Substring(1).ToLowerInvariant();
                dataAdded = true;
            }
            if (stage != newStage)
            {
                stage = newStage;
                dataAdded = false;
            }
            var buttonTextMesh = fldButtonTextMesh.Get();

            if (buttonTextMesh == null)
                yield break;

            //Check if the module is resetting. There is no flag indicating the module is resetting, but each button will have exactly a string with length of 1 on it.
            if (buttonTextMesh.Any(strMesh => strMesh.text.Length == 1))
                dataAdded = false;

            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_BamboozledAgain);

        //Restore the first button to the arrays.

        correctButtonTexts[0] = correctFirstStageButton[0];
        correctButtonColors[0] = correctFirstStageButton[1];

        //End of correct button section.

        //Beginning of the displayed texts section.

        var displayTexts = fldDisplayTexts.Get(expectedLength: 4, validator: v => v.Length != 8 ? "expected length 8" : null).ToArray();
        var colorIndex = fldColorIndex.Get(expectedLength: 8);

        if (displayTexts[0].Any(str => string.IsNullOrEmpty(str)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Bamboozled Again because 'displayText[0]' contains null or an empty string: [{1}]", _moduleId, displayTexts[0].Select(str => str ?? "<null>").JoinString(", "));
            yield break;
        }

        displayTexts[0] = displayTexts[0].Select(str => Regex.Replace(str, "#", " ")).ToArray();

        string[] firstRowTexts = displayTexts[0].Where((item, index) => index == 0 || index == 2 || index == 4).ToArray();
        string[] lastThreeTexts = displayTexts[0].Where((item, index) => index > 4 && index < 8).ToArray();
        string[] color = new string[14] { "White", "Red", "Orange", "Yellow", "Lime", "Green", "Jade", "Grey", "Cyan", "Azure", "Blue", "Violet", "Magenta", "Rose" };
        string[] displayColors = colorIndex.Select(index => color[index]).ToArray();

        //End of the displayed texts section.

        addQuestions(module,
            correctButtonTexts.Select((name, index) => makeQuestion(Question.BamboozledAgainButtonText, _BamboozledAgain,
                formatArgs: new[] { index == 3 ? "fourth" : ordinal(index + 1) },
                correctAnswers: new[] { name },
                preferredWrongAnswers: correctButtonTexts.Except(new[] { name }).ToArray())).Concat(
            correctButtonColors.Select((col, index) => makeQuestion(Question.BamboozledAgainButtonColor, _BamboozledAgain,
                formatArgs: new[] { index == 3 ? "fourth" : ordinal(index + 1) },
                correctAnswers: new[] { col },
                preferredWrongAnswers: correctButtonColors.Except(new[] { col }).ToArray()))).Concat(
            firstRowTexts.Select((text, index) => makeQuestion(Question.BamboozledAgainDisplayTexts1, _BamboozledAgain,
                formatArgs: new[] { ordinal(2 * index + 1) },
                correctAnswers: new[] { text },
                preferredWrongAnswers: firstRowTexts.Except(new[] { text }).ToArray()))).Concat(
            lastThreeTexts.Select((text, index) => makeQuestion(Question.BamboozledAgainDisplayTexts2, _BamboozledAgain,
                formatArgs: new[] { ordinal(index + 6) },
                correctAnswers: new[] { text },
                preferredWrongAnswers: lastThreeTexts.Except(new[] { text }).ToArray()))).Concat(
            displayColors.Select((col, index) => makeQuestion(Question.BamboozledAgainDisplayColor, _BamboozledAgain,
                formatArgs: new[] { ordinal(index + 1) },
                correctAnswers: new[] { col },
                preferredWrongAnswers: displayColors.Except(new[] { col }).ToArray()))));
    }

    private IEnumerable<object> ProcessBamboozlingButton(KMBombModule module)
    {
        var comp = GetComponent(module, "BamboozlingButtonScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldRandomiser = GetArrayField<int>(comp, "randomiser");
        var fldStage = GetIntField(comp, "stage");

        var moduleData = new int[2][];
        var stage = 0;

        while (!fldSolved.Get())
        {
            var randomiser = fldRandomiser.Get(expectedLength: 11);
            var newStage = fldStage.Get(min: 1, max: 2);
            if (stage != newStage || !randomiser.SequenceEqual(moduleData[newStage - 1]))
            {
                stage = newStage;
                moduleData[stage - 1] = randomiser.ToArray(); // Take a copy of the array.
            }
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_BamboozlingButton);

        var colors = new string[15] { "White", "Red", "Orange", "Yellow", "Lime", "Green", "Jade", "Grey", "Cyan", "Azure", "Blue", "Violet", "Magenta", "Rose", "Black" };
        var texts = new string[55] { "A LETTER", "A WORD", "THE LETTER", "THE WORD", "1 LETTER", "1 WORD", "ONE LETTER", "ONE WORD", "B", "C", "D", "E", "G", "K", "N", "P", "Q", "T", "V", "W", "Y", "BRAVO", "CHARLIE", "DELTA", "ECHO", "GOLF", "KILO", "NOVEMBER", "PAPA", "QUEBEC", "TANGO", "VICTOR", "WHISKEY", "YANKEE", "COLOUR", "RED", "ORANGE", "YELLOW", "LIME", "GREEN", "JADE", "CYAN", "AZURE", "BLUE", "VIOLET", "MAGENTA", "ROSE", "IN RED", "IN YELLOW", "IN GREEN", "IN CYAN", "IN BLUE", "IN MAGENTA", "QUOTE", "END QUOTE" };
        var qs = new List<QandA>();
        for (var i = 0; i < 2; i++)
        {
            qs.Add(makeQuestion(Question.BamboozlingButtonColor, _BamboozlingButton, new[] { (i + 1).ToString() }, new[] { colors[moduleData[i][0]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplayColor, _BamboozlingButton, new[] { (i + 1).ToString(), "fourth" }, new[] { colors[moduleData[i][1]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplayColor, _BamboozlingButton, new[] { (i + 1).ToString(), "fifth" }, new[] { colors[moduleData[i][2]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "first" }, new[] { texts[moduleData[i][3]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "third" }, new[] { texts[moduleData[i][4]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "fourth" }, new[] { texts[moduleData[i][5]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonDisplay, _BamboozlingButton, new[] { (i + 1).ToString(), "fifth" }, new[] { texts[moduleData[i][6]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonLabel, _BamboozlingButton, new[] { (i + 1).ToString(), "top" }, new[] { texts[moduleData[i][7]] }));
            qs.Add(makeQuestion(Question.BamboozlingButtonLabel, _BamboozlingButton, new[] { (i + 1).ToString(), "bottom" }, new[] { texts[moduleData[i][8]] }));
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessBartending(KMBombModule module)
    {
        var comp = GetComponent(module, "Maker");
        var fldSolved = GetField<bool>(comp, "_IsSolved");
        var fldIngredientIxs = GetArrayField<int>(comp, "ingIndices");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Bartending);

        var ingIxs = fldIngredientIxs.Get(expectedLength: 5, validator: ing => ing < 0 || ing > 4 ? "expected 0–4" : null);
        var ingredientNames = new[] { "Powdered Delta", "Flanergide", "Adelhyde", "Bronson Extract", "Karmotrine" };
        addQuestions(module, ingIxs.Select((ingIx, pos) => makeQuestion(Question.BartendingIngredients, _Bartending, formatArgs: new[] { (pos + 1).ToString() }, correctAnswers: new[] { ingredientNames[ingIx] })));
    }

    private IEnumerable<object> ProcessBigCircle(KMBombModule module)
    {
        var comp = GetComponent(module, "TheBigCircle");
        var fldSolved = GetField<bool>(comp, "_solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BigCircle);

        addQuestions(module, GetField<Array>(comp, "_currentSolution").Get(v => v.Length != 3 ? "expected length 3" : null).Cast<object>()
            .Select((color, ix) => makeQuestion(Question.BigCircleColors, _BigCircle, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { color.ToString() })));
    }

    private IEnumerable<object> ProcessBinaryLEDs(KMBombModule module)
    {
        var comp = GetComponent(module, "BinaryLeds");
        var fldSequences = GetField<int[,]>(comp, "sequences");
        var fldSequenceIndex = GetIntField(comp, "sequenceIndex");
        var fldColors = GetArrayField<int>(comp, "colorIndices");
        var fldSolutions = GetField<int[,]>(comp, "solutions");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldBlinkDelay = GetField<float>(comp, "blinkDelay");
        var mthGetIndexFromTime = GetMethod<int>(comp, "GetIndexFromTime", 2);

        int answer = -1;
        var wires = GetArrayField<KMSelectable>(comp, "wires", isPublic: true).Get(expectedLength: 3);

        for (int i = 0; i < wires.Length; i++)
        {
            // Need an extra scope to work around bug in Mono 2.0 C# compiler
            new Action<int, KMSelectable.OnInteractHandler>((j, oldInteract) =>
            {
                wires[j].OnInteract = delegate
                {
                    wires[j].OnInteract = oldInteract;  // Restore original interaction, so that this can only ever be called once per wire.
                    var wasSolved = fldSolved.Get();    // Get this before calling oldInteract()
                    var seqIx = fldSequenceIndex.Get();
                    var numIx = mthGetIndexFromTime.Invoke(Time.time, fldBlinkDelay.Get());
                    var colors = fldColors.Get(nullAllowed: true);  // We cannot risk throwing an exception during the module’s button handler
                    var solutions = fldSolutions.Get(nullAllowed: true);
                    var result = oldInteract();

                    if (wasSolved)
                        return result;

                    if (colors == null || colors.Length <= j)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning Binary LEDs because ‘colors’ array has unexpected length ({1}).", _moduleId,
                            colors == null ? "null" : colors.Length.ToString());
                        return result;
                    }

                    if (solutions == null || solutions.GetLength(0) <= seqIx || solutions.GetLength(1) <= colors[j])
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning Binary LEDs because ‘solutions’ array has unexpected lengths ({1}, {2}).", _moduleId,
                            solutions == null ? "null" : solutions.GetLength(0).ToString(),
                            solutions == null ? "null" : solutions.GetLength(1).ToString());
                        return result;
                    }

                    // Ignore if this wasn’t a solve
                    if (solutions[seqIx, colors[j]] != numIx)
                        return result;

                    // Find out which value is displayed
                    var sequences = fldSequences.Get(nullAllowed: true);

                    if (sequences == null || sequences.GetLength(0) <= seqIx || sequences.GetLength(1) <= numIx)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Abandoning Binary LEDs because ‘sequences’ array has unexpected lengths ({1}, {2}).", _moduleId,
                            sequences == null ? "null" : sequences.GetLength(0).ToString(),
                            sequences == null ? "null" : sequences.GetLength(1).ToString());
                        return result;
                    }

                    answer = sequences[seqIx, numIx];
                    return result;
                };
            })(i, wires[i].OnInteract);
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BinaryLEDs);

        if (answer != -1)
            addQuestion(module, Question.BinaryLEDsValue, correctAnswers: new[] { answer.ToString() });
    }

    private IEnumerable<object> ProcessBitmaps(KMBombModule module)
    {
        var comp = GetComponent(module, "BitmapsModule");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Bitmaps);

        var bitmap = GetArrayField<bool[]>(comp, "_bitmap").Get(expectedLength: 8, validator: arr => arr.Length != 8 ? "expected length 8" : null);
        var qCounts = new int[4];
        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if (bitmap[x][y])
                    qCounts[(y / 4) * 2 + (x / 4)]++;

        var preferredWrongAnswers = qCounts.SelectMany(i => new[] { i, 16 - i }).Distinct().Select(i => i.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "top left" }, new[] { qCounts[0].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "top right" }, new[] { qCounts[1].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "bottom left" }, new[] { qCounts[2].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "white", "bottom right" }, new[] { qCounts[3].ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "top left" }, new[] { (16 - qCounts[0]).ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "top right" }, new[] { (16 - qCounts[1]).ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "bottom left" }, new[] { (16 - qCounts[2]).ToString() }, preferredWrongAnswers),
            makeQuestion(Question.Bitmaps, _Bitmaps, new[] { "black", "bottom right" }, new[] { (16 - qCounts[3]).ToString() }, preferredWrongAnswers));
    }

    private IEnumerable<object> ProcessBlindMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "BlindMaze");
        var fldSolved = GetField<bool>(comp, "Solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BlindMaze);

        // Despite the name “currentMaze”, this field actually contains the number of solved modules when Blind Maze was solved
        var numSolved = GetIntField(comp, "currentMaze").Get(v => v < 0 ? "negative" : null);
        var buttonColors = GetArrayField<int>(comp, "buttonColors").Get(expectedLength: 4, validator: bc => bc < 0 || bc > 4 ? "expected 0–4" : null);

        var colorNames = new[] { "Red", "Green", "Blue", "Gray", "Yellow" };
        var buttonNames = new[] { "north", "east", "south", "west" };

        addQuestions(module,
            buttonColors.Select((col, ix) => makeQuestion(Question.BlindMazeColors, _BlindMaze, formatArgs: new[] { buttonNames[ix] }, correctAnswers: new[] { colorNames[col] }))
                .Concat(new[] { makeQuestion(Question.BlindMazeMaze, _BlindMaze, correctAnswers: new[] { ((numSolved + Bomb.GetSerialNumberNumbers().Last()) % 10).ToString() }) }));
    }

    private IEnumerable<object> ProcessBlockbusters(KMBombModule module)
    {
        var comp = GetComponent(module, "blockbustersScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var legalLetters = GetField<List<string>>(comp, "legalLetters", isPublic: true).Get();
        var tiles = GetField<Array>(comp, "tiles", isPublic: true).Get(arr => arr.Cast<object>().Any(v => v == null) ? "contains null" : null);
        var selectables = new KMSelectable[tiles.Length];
        var prevInteracts = new KMSelectable.OnInteractHandler[tiles.Length];
        string lastPress = null;

        for (int i = 0; i < tiles.Length; i++)
        {
            var selectable = selectables[i] = GetField<KMSelectable>(tiles.GetValue(i), "selectable", isPublic: true).Get();
            var prevInteract = prevInteracts[i] = selectable.OnInteract;
            var letter = GetField<TextMesh>(tiles.GetValue(i), "containedLetter", isPublic: true).Get();
            selectable.OnInteract = delegate
            {
                lastPress = letter.text;
                return prevInteract();
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Blockbusters);

        for (int i = 0; i < tiles.Length; i++)
            selectables[i].OnInteract = prevInteracts[i];

        if (lastPress == null)
        {
            Debug.LogFormat("[Souvenir #{0}] Abandoning Blockbusters because no pressed letter was retrieved.", _moduleId);
            yield break;
        }

        addQuestion(module, Question.BlockbustersLastLetter, correctAnswers: new[] { lastPress }, preferredWrongAnswers: legalLetters.ToArray());
    }

    private IEnumerable<object> ProcessBlueArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "BlueArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldCoord = GetField<string>(comp, "coord");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BlueArrows);

        string[] letters = { "CA", "C1", "CB", "C8", "CF", "C4", "CE", "C6", "3A", "31", "3B", "38", "3F", "34", "3E", "36", "GA", "G1", "GB", "G8", "GF", "G4", "GE", "G6", "7A", "71", "7B", "78", "7F", "74", "7E", "76", "DA", "D1", "DB", "D8", "DF", "D4", "DE", "D6", "5A", "51", "5B", "58", "5F", "54", "5E", "56", "HA", "H1", "HB", "H8", "HF", "H4", "HE", "H6", "2A", "21", "2B", "28", "2F", "24", "2E", "26" };
        string coord = fldCoord.Get(v => !letters.Contains(v) ? string.Format("expected one of: [{0}]", letters.JoinString(", ")) : null);
        addQuestion(module, Question.BlueArrowsInitialLetters, correctAnswers: new[] { coord });
    }

    private IEnumerable<object> ProcessBobBarks(KMBombModule module)
    {
        var comp = GetComponent(module, "BobBarks");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldIndicators = GetArrayField<int>(comp, "assigned");
        var fldFlashes = GetArrayField<int>(comp, "stages");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_BobBarks);

        string[] validDirections = { "top left", "top right", "bottom left", "bottom right" };
        string[] validLabels = { "BOB", "CAR", "CLR", "IND", "FRK", "FRQ", "MSA", "NSA", "SIG", "SND", "TRN", "BUB", "DOG", "ETC", "KEY" };

        int[] indicators = fldIndicators.Get(expectedLength: 4, validator: idn => idn < 0 || idn >= validLabels.Length ? string.Format("expected 0–{0}", validLabels.Length - 1) : null);
        int[] flashes = fldFlashes.Get(expectedLength: 5, validator: fn => fn < 0 || fn >= validDirections.Length ? string.Format("expected 0–{0}", validDirections.Length - 1) : null);

        // To provide preferred wrong answers, mostly.
        string[] labelsOnModule = { validLabels[indicators[0]], validLabels[indicators[1]], validLabels[indicators[2]], validLabels[indicators[3]] };

        addQuestions(module,
            Enumerable.Range(0, 4).Select(ix => makeQuestion(Question.BobBarksIndicators, _BobBarks,
                correctAnswers: new[] { labelsOnModule[ix] },
                formatArgs: new[] { validDirections[ix] },
                preferredWrongAnswers: labelsOnModule.Except(new[] { labelsOnModule[ix] }).ToArray()
            )).Concat(
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.BobBarksPositions, _BobBarks,
                correctAnswers: new[] { validDirections[flashes[ix]] },
                formatArgs: new[] { ordinal(ix + 1) }))
            ));
    }

    private IEnumerable<object> ProcessBoggle(KMBombModule module)
    {
        var comp = GetComponent(module, "boggle");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var map = GetField<char[,]>(comp, "letterMap").Get(m => m.GetLength(0) != 10 || m.GetLength(1) != 10 ? string.Format("size was {0}×{1}, expected 10×10", m.GetLength(0), m.GetLength(1)) : null);
        var visible = GetField<string>(comp, "visableLetters", isPublic: true).Get(v => v.Length != 4 ? "expected length 4" : null);
        var verOffset = GetIntField(comp, "verOffset").Get(min: 0, max: 6);
        var horOffset = GetIntField(comp, "horOffset").Get(min: 0, max: 6);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Boggle);

        var letters = new List<string>();
        for (int i = verOffset; i < verOffset + 4; i++)
            for (int j = horOffset; j < horOffset + 4; j++)
                letters.Add(map[i, j].ToString());

        addQuestion(module, Question.BoggleLetters, correctAnswers: visible.Select(v => v.ToString()).ToArray(), preferredWrongAnswers: letters.ToArray());
    }

    private IEnumerable<object> ProcessBoxing(KMBombModule module)
    {
        var comp = GetComponent(module, "boxing");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Boxing);

        var possibleNames = GetStaticField<string[]>(comp.GetType(), "possibleNames").Get();
        var possibleSubstituteNames = GetStaticField<string[]>(comp.GetType(), "possibleSubstituteNames").Get();
        var possibleLastNames = GetStaticField<string[]>(comp.GetType(), "possibleLastNames").Get();
        var contestantStrengths = GetArrayField<int>(comp, "contestantStrengths").Get(expectedLength: 5);
        var contestantIndices = GetArrayField<int>(comp, "contestantIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleNames.Length ? "out of range" : null);
        var lastNameIndices = GetArrayField<int>(comp, "lastNameIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleLastNames.Length ? "out of range" : null);
        var substituteIndices = GetArrayField<int>(comp, "substituteIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleSubstituteNames.Length ? "out of range" : null);
        var substituteLastNameIndices = GetArrayField<int>(comp, "substituteLastNameIndices").Get(expectedLength: 5, validator: v => v < 0 || v >= possibleLastNames.Length ? "out of range" : null);

        var qs = new List<QandA>();
        for (var ct = 0; ct < 5; ct++)
        {
            qs.Add(makeQuestion(Question.BoxingStrengthByContestant, _Boxing, formatArgs: new[] { possibleNames[contestantIndices[ct]] }, correctAnswers: new[] { contestantStrengths[ct].ToString() }));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "first name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleNames[contestantIndices[ct]] }, preferredWrongAnswers: possibleNames));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "last name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleLastNames[lastNameIndices[ct]] }, preferredWrongAnswers: possibleLastNames));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "substitute’s first name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleSubstituteNames[substituteIndices[ct]] }, preferredWrongAnswers: possibleSubstituteNames));
            qs.Add(makeQuestion(Question.BoxingContestantByStrength, _Boxing, formatArgs: new[] { "substitute’s last name", contestantStrengths[ct].ToString() }, correctAnswers: new[] { possibleLastNames[substituteLastNameIndices[ct]] }, preferredWrongAnswers: possibleLastNames));
        }
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "contestant’s first name", }, correctAnswers: contestantIndices.Select(ix => possibleNames[ix]).ToArray(), preferredWrongAnswers: possibleNames));
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "contestant’s last name" }, correctAnswers: lastNameIndices.Select(ix => possibleLastNames[ix]).ToArray(), preferredWrongAnswers: possibleLastNames));
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "substitute’s first name" }, correctAnswers: substituteIndices.Select(ix => possibleSubstituteNames[ix]).ToArray(), preferredWrongAnswers: possibleSubstituteNames));
        qs.Add(makeQuestion(Question.BoxingNames, _Boxing, formatArgs: new[] { "substitute’s last name" }, correctAnswers: substituteLastNameIndices.Select(ix => possibleLastNames[ix]).ToArray(), preferredWrongAnswers: possibleLastNames));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessBraille(KMBombModule module)
    {
        var comp = GetComponent(module, "BrailleModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Braille);
        addQuestion(module, Question.BrailleWord, correctAnswers: new[] { GetField<string>(comp, "_word").Get() });
    }

    private IEnumerable<object> ProcessBrokenButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "BrokenButtonModule");
        var fldSolved = GetField<bool>(comp, "Solved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BrokenButtons);

        var pressed = GetField<List<string>>(comp, "Pressed").Get();
        if (pressed.All(p => p.Length == 0))
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Broken Buttons because the only buttons you pressed were literally blank.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        // skip the literally blank buttons.
        addQuestions(module, pressed.Select((p, i) => p.Length == 0 ? null : makeQuestion(Question.BrokenButtons, _BrokenButtons, new[] { ordinal(i + 1) }, new[] { p }, pressed.Except(new[] { "" }).ToArray())));
    }

    private IEnumerable<object> ProcessBurglarAlarm(KMBombModule module)
    {
        var comp = GetComponent(module, "BurglarAlarmScript");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BurglarAlarm);

        var displayText = GetField<TextMesh>(comp, "DisplayText", isPublic: true).Get();
        displayText.text = "";

        var moduleNumber = GetArrayField<int>(comp, "moduleNumber").Get(expectedLength: 8, validator: mn => mn < 0 || mn > 9 ? "expected 0–9" : null);
        addQuestions(module, moduleNumber.Select((mn, ix) => makeQuestion(Question.BurglarAlarmDigits, _BurglarAlarm, new[] { ordinal(ix + 1) }, new[] { mn.ToString() }, moduleNumber.Select(n => n.ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessButton(KMBombModule module)
    {
        var comp = GetComponent(module, "ButtonComponent");
        var fldSolved = GetField<bool>(comp, "IsSolved", true);
        var propLightColor = GetProperty<object>(comp, "IndicatorColor", true);
        var ledOff = GetField<GameObject>(comp, "LED_Off", true).Get();

        var color = -1;
        while (!fldSolved.Get())
        {
            color = ledOff.activeSelf ? -1 : (int) propLightColor.Get();
            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_Button);
        if (color < 0)
        {
            Debug.LogFormat("[Souvenir #{0}] No question for The Button because the button was tapped (or I missed the light color).", _moduleId);
            _legitimatelyNoQuestions.Add(module);
        }
        else
        {
            string answer;
            switch (color)
            {
                case 0: answer = "red"; break;
                case 1: answer = "blue"; break;
                case 2: answer = "yellow"; break;
                case 3: answer = "white"; break;
                default: Debug.LogFormat("<Souvenir #{0}> Abandoning The Button because IndicatorColor is out of range ({1}).", _moduleId, color); yield break;
            }
            addQuestion(module, Question.ButtonLightColor, correctAnswers: new[] { answer });
        }
    }

    private IEnumerable<object> ProcessButtonSequences(KMBombModule module)
    {
        var comp = GetComponent(module, "ButtonSequencesModule");
        var fldButtonsActive = GetField<bool>(comp, "buttonsActive");

        while (fldButtonsActive.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ButtonSequences);

        var panelInfo = GetField<Array>(comp, "PanelInfo").Get(arr =>
            arr.Rank != 2 ? string.Format("has rank {0}, expected 2", arr.Rank) :
            arr.GetLength(1) != 3 ? string.Format("GetLength(1) == {0}, expected 3", arr.GetLength(1)) :
            Enumerable.Range(0, arr.GetLength(0)).Any(x => Enumerable.Range(0, arr.GetLength(1)).Any(y => arr.GetValue(x, y) == null)) ? "contains null" : null);

        var obj = panelInfo.GetValue(0, 0);
        var fldColor = GetIntField(obj, "color", isPublic: true);
        var colorNames = GetArrayField<string>(comp, "ColorNames").Get();
        var colorOccurrences = new Dictionary<int, int>();
        for (int i = panelInfo.GetLength(0) - 1; i >= 0; i--)
            for (int j = 0; j < 3; j++)
                colorOccurrences.IncSafe(fldColor.GetFrom(panelInfo.GetValue(i, j), v => v < 0 || v >= colorNames.Length ? string.Format("out of range; colorNames.Length={0} ([{1}])", colorNames.Length, colorNames.JoinString(", ")) : null));

        addQuestions(module, colorOccurrences.Select(kvp =>
            makeQuestion(Question.ButtonSequencesColorOccurrences, _ButtonSequences,
                formatArgs: new[] { colorNames[kvp.Key].ToLowerInvariant() },
                correctAnswers: new[] { kvp.Value.ToString() },
                preferredWrongAnswers: colorOccurrences.Values.Select(v => v.ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessCaesarCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "CaesarCycleScript", Question.CaesarCycleWord, _CaesarCycle);
    }

    private IEnumerable<object> ProcessCalendar(KMBombModule module)
    {
        var comp = GetComponent(module, "calendar");
        var fldLightsOn = GetField<bool>(comp, "_lightsOn");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");

        while (!fldLightsOn.Get())
            yield return new WaitForSeconds(.1f);

        var colorblindText = GetField<TextMesh>(comp, "colorblindText", isPublic: true).Get(v => v.text == null ? "text is null" : null);

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Calendar);

        addQuestion(module, Question.CalendarLedColor, correctAnswers: new[] { colorblindText.text });
    }

    private IEnumerable<object> ProcessChallengeAndContact(KMBombModule module)
    {
        var comp = GetComponent(module, "moduleScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldAnswers = GetArrayField<string>(comp, "answers");
        var fldFirstSet = GetArrayField<string>(comp, "possibleFirstAnswers");
        var fldSecondSet = GetArrayField<string>(comp, "possibleSecondAnswers");
        var fldThirdSet = GetArrayField<string>(comp, "possibleFinalAnswers");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ChallengeAndContact);

        string[] answers = fldAnswers.Get(expectedLength: 3);
        string[] firstSet = fldFirstSet.Get();
        string[] secondSet = fldSecondSet.Get();
        string[] thirdSet = fldThirdSet.Get();

        string[] allAnswers = new string[firstSet.Length + secondSet.Length + thirdSet.Length];
        firstSet.CopyTo(allAnswers, 0);
        secondSet.CopyTo(allAnswers, firstSet.Length);
        thirdSet.CopyTo(allAnswers, firstSet.Length + secondSet.Length);

        for (int i = 0; i < answers.Length; i++)
            answers[i] = char.ToUpperInvariant(answers[i][0]) + answers[i].Substring(1);
        for (int i = 0; i < allAnswers.Length; i++)
            allAnswers[i] = char.ToUpperInvariant(allAnswers[i][0]) + allAnswers[i].Substring(1);

        addQuestions(module,
            makeQuestion(Question.ChallengeAndContactAnswers, _ChallengeAndContact, new[] { "first" }, new[] { answers[0] }, allAnswers.Where(x => x[0] == answers[0][0]).ToArray()),
            makeQuestion(Question.ChallengeAndContactAnswers, _ChallengeAndContact, new[] { "second" }, new[] { answers[1] }, allAnswers.Where(x => x[0] == answers[1][0]).ToArray()),
            makeQuestion(Question.ChallengeAndContactAnswers, _ChallengeAndContact, new[] { "third" }, new[] { answers[2] }, allAnswers.Where(x => x[0] == answers[2][0]).ToArray()));
    }

    private IEnumerable<object> ProcessCheapCheckout(KMBombModule module)
    {
        var comp = GetComponent(module, "CheapCheckoutModule");
        var fldSolved = GetField<bool>(comp, "solved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var paids = new List<decimal> { GetField<decimal>(comp, "Display").Get() };
        var paid = GetField<decimal>(comp, "Paid").Get();
        if (paid != paids[0])
            paids.Add(paid);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_CheapCheckout);

        addQuestions(module, paids.Select((p, i) => makeQuestion(Question.CheapCheckoutPaid, _CheapCheckout,
            formatArgs: new[] { paids.Count == 1 ? "" : ordinal(i + 1) + " " },
            correctAnswers: new[] { "$" + p.ToString("N2") })));
    }

    private IEnumerable<object> ProcessChess(KMBombModule module)
    {
        var comp = GetComponent(module, "ChessBehaviour");
        var fldIndexSelected = GetArrayField<int>(comp, "indexSelected"); // this contains both the coordinates and the solution
        var fldIsSolved = GetField<bool>(comp, "isSolved", isPublic: true);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var indexSelected = fldIndexSelected.Get(expectedLength: 7, validator: b => b / 10 < 0 || b / 10 >= 6 || b % 10 < 0 || b % 10 >= 6 ? "unexpected value" : null);

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Chess);

        addQuestions(module, Enumerable.Range(0, 6).Select(i => makeQuestion(Question.ChessCoordinate, _Chess, new[] { ordinal(i + 1) }, new[] { "" + ((char) (indexSelected[i] / 10 + 'a')) + (indexSelected[i] % 10 + 1) })));
    }

    private IEnumerable<object> ProcessChineseCounting(KMBombModule module)
    {
        var comp = GetComponent(module, "chineseCounting");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ChineseCounting);

        var index1 = GetIntField(comp, "ledIndex").Get(0, 3);
        var index2 = GetIntField(comp, "led2Index").Get(0, 3);
        var ledColors = new[] { "White", "Red", "Green", "Orange" };

        addQuestions(module,
          makeQuestion(Question.ChineseCountingLED, _ChineseCounting, new[] { "left" }, new[] { ledColors[index1] }),
          makeQuestion(Question.ChineseCountingLED, _ChineseCounting, new[] { "right" }, new[] { ledColors[index2] }));
    }

    private IEnumerable<object> ProcessChordQualities(KMBombModule module)
    {
        var comp = GetComponent(module, "ChordQualities");
        var fldIsSolved = GetField<bool>(comp, "isSolved", isPublic: true);

        var givenChord = GetField<object>(comp, "givenChord").Get();
        var quality = GetField<object>(givenChord, "quality").Get();
        var qualityName = GetField<string>(quality, "name").Get();
        var lights = GetField<Array>(comp, "lights", isPublic: true).Get(v => v.Length != 12 ? "expected length 12" : null);
        var mthsSetOutputLight = lights.Cast<object>().Select(light => GetMethod<object>(light, "setOutputLight", numParameters: 1, isPublic: true)).ToArray();

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ChordQualities);

        foreach (var method in mthsSetOutputLight)
            method.Invoke(false);

        var noteNames = GetField<Array>(givenChord, "notes").Get(v => v.Length != 4 ? "expected length 4" : null).Cast<object>().Select(note => note.ToString().Replace("sharp", "♯")).ToArray();
        addQuestions(module,
            makeQuestion(Question.ChordQualitiesNotes, _ChordQualities, correctAnswers: noteNames),
            makeQuestion(Question.ChordQualitiesQuality, _ChordQualities, correctAnswers: new[] { qualityName }));
    }

    private IEnumerable<object> ProcessCode(KMBombModule module)
    {
        var comp = GetComponent(module, "TheCodeModule");
        var fldCode = GetIntField(comp, "moduleNumber");
        var fldResetBtn = GetField<KMSelectable>(comp, "ButtonR", isPublic: true);
        var fldSubmitBtn = GetField<KMSelectable>(comp, "ButtonS", isPublic: true);

        var code = fldCode.Get(min: 999, max: 9999);

        // Hook into the module’s OnPass handler
        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        yield return new WaitUntil(() => isSolved);
        _modulesSolved.IncSafe(_Code);

        // Block the submit/reset buttons
        fldResetBtn.Get().OnInteract = delegate { return false; };
        fldSubmitBtn.Get().OnInteract = delegate { return false; };

        addQuestions(module, makeQuestion(Question.CodeDisplayNumber, _Code, correctAnswers: new[] { code.ToString() }));
    }

    private IEnumerable<object> ProcessCoffeebucks(KMBombModule module)
    {
        var comp = GetComponent(module, "coffeebucksScript");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Coffeebucks);

        var names = GetArrayField<string>(comp, "nameOptions", isPublic: true).Get();
        var coffees = GetArrayField<string>(comp, "coffeeOptions", isPublic: true).Get();
        var currName = GetIntField(comp, "startName").Get(min: 0, max: names.Length - 1);
        var currCoffee = GetIntField(comp, "startCoffee").Get(min: 0, max: coffees.Length - 1);

        for (int i = 0; i < names.Length; i++)
            names[i] = char.ToUpperInvariant(names[i][0]) + names[i].Substring(1);
        for (int i = 0; i < coffees.Length; i++)
            coffees[i] = coffees[i].Replace("\n", " ");

        addQuestions(module,
            makeQuestion(Question.CoffeebucksClient, _Coffeebucks, correctAnswers: new[] { names[currName] }, preferredWrongAnswers: names),
            makeQuestion(Question.CoffeebucksCoffee, _Coffeebucks, correctAnswers: new[] { coffees[currCoffee] }, preferredWrongAnswers: coffees));
    }

    private IEnumerable<object> ProcessColorBraille(KMBombModule module)
    {
        var comp = GetComponent(module, "ColorBrailleModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColorBraille);

        var manglingNames = new Dictionary<string, string>
        {
            { "TopRowShiftedToTheRight", "Top row shifted to the right" },
            { "TopRowShiftedToTheLeft", "Top row shifted to the left" },
            { "MiddleRowShiftedToTheRight", "Middle row shifted to the right" },
            { "MiddleRowShiftedToTheLeft", "Middle row shifted to the left" },
            { "BottomRowShiftedToTheRight", "Bottom row shifted to the right" },
            { "BottomRowShiftedToTheLeft", "Bottom row shifted to the left" },
            { "EachLetterUpsideDown", "Each letter upside-down" },
            { "EachLetterHorizontallyFlipped", "Each letter horizontally flipped" },
            { "EachLetterVerticallyFlipped", "Each letter vertically flipped" },
            { "DotsAreInverted", "Dots are inverted" }
        };

        var allWordsType = comp.GetType().Assembly.GetType("ColorBraille.WordsData");
        if (allWordsType == null)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Braille because I cannot find the ColorBraille.WordsData type.", _moduleId);
            yield break;
        }
        var allWords = GetStaticField<Dictionary<string, int[]>>(allWordsType, "Words", isPublic: true).Get().Keys.ToArray();

        var words = GetArrayField<string>(comp, "_words").Get(expectedLength: 3);
        var mangling = GetField<object>(comp, "_mangling").Get(m => !manglingNames.ContainsKey(m.ToString()) ? "mangling is not in the dictionary" : null);
        addQuestions(module,
            makeQuestion(Question.ColorBrailleWords, _ColorBraille, formatArgs: new[] { "red" }, correctAnswers: new[] { words[0] }, preferredWrongAnswers: allWords),
            makeQuestion(Question.ColorBrailleWords, _ColorBraille, formatArgs: new[] { "green" }, correctAnswers: new[] { words[1] }, preferredWrongAnswers: allWords),
            makeQuestion(Question.ColorBrailleWords, _ColorBraille, formatArgs: new[] { "blue" }, correctAnswers: new[] { words[2] }, preferredWrongAnswers: allWords),
            makeQuestion(Question.ColorBrailleMangling, _ColorBraille, correctAnswers: new[] { manglingNames[mangling.ToString()] }));
    }

    private IEnumerable<object> ProcessColorDecoding(KMBombModule module)
    {
        var comp = GetComponent(module, "ColorDecoding");
        var fldInputButtons = GetArrayField<KMSelectable>(comp, "InputButtons", isPublic: true);
        var fldStageNum = GetIntField(comp, "stagenum");
        var fldIndicator = GetField<object>(comp, "indicator");
        var indicatorGrid = GetArrayField<GameObject>(comp, "IndicatorGrid", isPublic: true).Get();

        var patterns = new Dictionary<int, string>();
        var colors = new Dictionary<int, string[]>();
        var isSolved = false;
        var isAbandoned = false;

        var inputButtons = fldInputButtons.Get();
        var origInteract = inputButtons.Select(ib => ib.OnInteract).ToArray();
        object lastIndicator = null;

        var colorNameMapping = new Dictionary<string, string>
        {
            { "R", "Red" },
            { "G", "Green" },
            { "B", "Blue" },
            { "Y", "Yellow" },
            { "P", "Purple" }
        };

        var update = new Action(() =>
        {
            // We mustn’t throw an exception during the module’s button handler
            try
            {
                var ind = fldIndicator.Get();
                if (ReferenceEquals(ind, lastIndicator))
                    return;
                lastIndicator = ind;
                var indColors = GetField<IList>(ind, "indicator_colors").Get(
                    v => v.Count == 0 ? "no indicator colors" :
                    v.Cast<object>().Any(col => !colorNameMapping.ContainsKey(col.ToString())) ? "color is not in the color name mapping" : null);
                var stageNum = fldStageNum.Get();
                var patternName = GetField<object>(ind, "pattern").Get().ToString();
                patterns[stageNum] = patternName.Substring(0, 1) + patternName.Substring(1).ToLowerInvariant();
                colors[stageNum] = indColors.Cast<object>().Select(obj => colorNameMapping[obj.ToString()]).ToArray();
            }
            catch (AbandonModuleException amex)
            {
                Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Decoding because: {1}", _moduleId, amex.Message);
                isAbandoned = true;
            }
        });
        update();

        for (int ix = 0; ix < inputButtons.Length; ix++)
        {
            new Action<int>(i =>
            {
                inputButtons[i].OnInteract = delegate
                {
                    var ret = origInteract[i]();
                    if (isSolved || isAbandoned)
                        return ret;

                    if (fldStageNum.Get() >= 3)
                    {
                        for (int j = 0; j < indicatorGrid.Length; j++)
                            indicatorGrid[j].GetComponent<MeshRenderer>().material.color = Color.black;
                        isSolved = true;
                    }
                    else
                        update();

                    return ret;
                };
            })(ix);
        }

        while (!isSolved && !isAbandoned)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColorDecoding);

        for (int ix = 0; ix < inputButtons.Length; ix++)
            inputButtons[ix].OnInteract = origInteract[ix];

        if (isAbandoned)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Decoding.", _moduleId);
            yield break;
        }

        if (Enumerable.Range(0, 3).Any(k => !patterns.ContainsKey(k) || !colors.ContainsKey(k)))
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Color Decoding because I have a discontinuous set of stages: {1}/{2}.", _moduleId, patterns.Keys.JoinString(", "), colors.Keys.JoinString(", "));
            yield break;
        }

        addQuestions(module, Enumerable.Range(0, 3).SelectMany(stage => Ut.NewArray(
             colors[stage].Length <= 3 ? makeQuestion(Question.ColorDecodingIndicatorColors, _ColorDecoding, new[] { "appeared", ordinal(stage + 1) }, colors[stage]) : null,
             colors[stage].Length >= 3 ? makeQuestion(Question.ColorDecodingIndicatorColors, _ColorDecoding, new[] { "did not appear", ordinal(stage + 1) }, colorNameMapping.Keys.Except(colors[stage]).ToArray()) : null,
             makeQuestion(Question.ColorDecodingIndicatorPattern, _ColorDecoding, new[] { ordinal(stage + 1) }, new[] { patterns[stage] }))));
    }

    private IEnumerable<object> ProcessColoredKeys(KMBombModule module)
    {
        var comp = GetComponent(module, "ColoredKeysScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColoredKeys);

        var colors = GetArrayField<string>(comp, "loggingWords", isPublic: true).Get();
        var letters = GetArrayField<string>(comp, "letters", isPublic: true).Get();
        var displayWord = GetIntField(comp, "displayIndex").Get(0, colors.Length - 1);
        var displayColor = GetIntField(comp, "displayColIndex").Get(0, colors.Length - 1);
        var matsNames = GetArrayField<Material>(comp, "buttonmats", isPublic: true).Get().Select(x => x.name).ToArray();

        var btnLetter = Enumerable.Range(1, 4).Select(i => GetIntField(comp, string.Format("b{0}LetIndex", i)).Get(0, letters.Length - 1)).ToArray();
        var btnColor = Enumerable.Range(1, 4).Select(i => GetIntField(comp, string.Format("b{0}ColIndex", i)).Get(0, matsNames.Length - 1)).ToArray();

        addQuestions(module,
            makeQuestion(Question.ColoredKeysDisplayWord, _ColoredKeys, correctAnswers: new[] { colors[displayWord] }, preferredWrongAnswers: colors),
            makeQuestion(Question.ColoredKeysDisplayWordColor, _ColoredKeys, correctAnswers: new[] { colors[displayColor] }, preferredWrongAnswers: colors),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "top-left" }, new[] { letters[btnLetter[0]] }, letters),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "top-right" }, new[] { letters[btnLetter[1]] }, letters),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "bottom-left" }, new[] { letters[btnLetter[2]] }, letters),
            makeQuestion(Question.ColoredKeysKeyLetter, _ColoredKeys, new[] { "bottom-right" }, new[] { letters[btnLetter[3]] }, letters),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "top-left" }, new[] { matsNames[btnColor[0]] }, matsNames),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "top-right" }, new[] { matsNames[btnColor[1]] }, matsNames),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "bottom-left" }, new[] { matsNames[btnColor[2]] }, matsNames),
            makeQuestion(Question.ColoredKeysKeyColor, _ColoredKeys, new[] { "bottom-right" }, new[] { matsNames[btnColor[3]] }, matsNames));
    }

    private IEnumerable<object> ProcessColoredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "ColoredSquaresModule");
        var fldExpectedPresses = GetField<object>(comp, "_expectedPresses");

        // Colored Squares sets _expectedPresses to null when it’s solved
        while (fldExpectedPresses.Get(nullAllowed: true) != null)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_ColoredSquares);
        addQuestion(module, Question.ColoredSquaresFirstGroup, correctAnswers: new[] { GetField<object>(comp, "_firstStageColor").Get().ToString() });
    }

    private IEnumerable<object> ProcessColoredSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "ColoredSwitchesModule");
        var fldSwitches = GetIntField(comp, "_switchState");
        var fldSolution = GetIntField(comp, "_solutionState");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        var initial = fldSwitches.Get(0, (1 << 5) - 1);

        while (fldSolution.Get() == -1)
            yield return null;  // not waiting for .1 seconds this time to make absolutely sure we catch it before the player toggles another switch

        var afterReveal = fldSwitches.Get(0, (1 << 5) - 1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColoredSwitches);
        addQuestions(module,
            makeQuestion(Question.ColoredSwitchesInitialPosition, _ColoredSwitches, correctAnswers: new[] { Enumerable.Range(0, 5).Select(b => (initial & (1 << b)) != 0 ? "Q" : "R").Reverse().JoinString() }),
            makeQuestion(Question.ColoredSwitchesWhenLEDsCameOn, _ColoredSwitches, correctAnswers: new[] { Enumerable.Range(0, 5).Select(b => (afterReveal & (1 << b)) != 0 ? "Q" : "R").Reverse().JoinString() }));
    }

    private IEnumerable<object> ProcessColorMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "ColorMorseModule");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        // Once Color Morse is activated, ‘flashingEnabled’ is set to true, and then it is only set to false when the module is solved.
        var fldFlashingEnabled = GetField<bool>(comp, "flashingEnabled");
        while (fldFlashingEnabled.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ColorMorse);

        var numbers = GetArrayField<int>(comp, "Numbers").Get(expectedLength: 3);
        var colorNames = GetArrayField<string>(comp, "ColorNames", isPublic: true).Get();
        var colors = GetArrayField<int>(comp, "Colors").Get(expectedLength: 3, validator: c => c < 0 || c >= colorNames.Length ? "out of range" : null);

        var flashedColorNames = colors.Select(c => colorNames[c].Substring(0, 1) + colorNames[c].Substring(1).ToLowerInvariant()).ToArray();
        var flashedCharacters = numbers.Select(num => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(num, 1)).ToArray();

        addQuestions(module, Enumerable.Range(0, 3).SelectMany(ix => Ut.NewArray(
             makeQuestion(Question.ColorMorseColor, _ColorMorse, new[] { ordinal(ix + 1) }, new[] { flashedColorNames[ix] }, flashedColorNames),
             makeQuestion(Question.ColorMorseCharacter, _ColorMorse, new[] { ordinal(ix + 1) }, new[] { flashedCharacters[ix] }, flashedCharacters))));
    }

    private IEnumerable<object> ProcessCoordinates(KMBombModule module)
    {
        var comp = GetComponent(module, "CoordinatesModule");
        var fldFirstSubmitted = GetField<int?>(comp, "_firstCorrectSubmitted");

        while (fldFirstSubmitted.Get(nullAllowed: true) == null)
            yield return new WaitForSeconds(.1f);

        var fldClues = GetField<IList>(comp, "_clues");
        var clues = fldClues.Get();
        var index = fldFirstSubmitted.Get(v => v < 0 || v >= clues.Count ? string.Format("out of range; clues.Count={0}", clues.Count) : null).Value;
        var clue = clues[index];
        var fldClueText = GetField<string>(clue, "Text");
        var fldClueSystem = GetField<int?>(clue, "System");
        var clueText = fldClueText.Get();

        // The module sets ‘clues’ to null to indicate that it is solved.
        while (fldClues.Get(nullAllowed: true) != null)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Coordinates);
        var shortenCoordinate = Ut.Lambda((string str) =>
        {
            if (str == null)
                return null;

            str = str.Replace("\n", " ");
            if (str.Length > 13)
            {
                str = str
                    .Replace(",", "")
                    .Replace("north", "N")
                    .Replace("south", "S")
                    .Replace("west", "W")
                    .Replace("east", "E")
                    .Replace("up", "U")
                    .Replace("down", "D")
                    .Replace("left", "L")
                    .Replace("right", "R")
                    .Replace("top", "T")
                    .Replace("bottom", "B")
                    .Replace("middle", "M")
                    .Replace("center", "C")
                    .Replace("from", "fr.")
                    .Replace(" o’clock", "")
                    .Replace(" corner", "");
                str = Regex.Replace(str, @"\b[A-Z] [A-Z]\b", m => m.Value.Remove(1, 1));
            }
            return str;
        });

        // The size clue is the only one where fldClueSystem is null
        var sizeClue = clues.Cast<object>().Where(szCl => fldClueSystem.GetFrom(szCl, nullAllowed: true) == null).FirstOrDefault();
        addQuestions(module,
            makeQuestion(Question.CoordinatesFirstSolution, _Coordinates, correctAnswers: new[] { shortenCoordinate(clueText) }, preferredWrongAnswers: clues.Cast<object>().Select(c => shortenCoordinate(fldClueText.GetFrom(c))).Where(t => t != null).ToArray()),
            sizeClue == null ? null : makeQuestion(Question.CoordinatesSize, _Coordinates, correctAnswers: new[] { fldClueText.GetFrom(sizeClue) }));
    }

    private IEnumerable<object> ProcessCorners(KMBombModule module)
    {
        var comp = GetComponent(module, "CornersModule");
        var fldSolved = GetField<bool>(comp, "_moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Corners);

        var colorNames = new[] { "red", "green", "blue", "yellow" };
        var cornerNames = new[] { "top-left", "top-right", "bottom-right", "bottom-left" };

        var clampColors = GetArrayField<int>(comp, "_clampColors").Get(expectedLength: 4, validator: v => v < 0 || v >= colorNames.Length ? string.Format("expected 0–{0}", colorNames.Length - 1) : null);
        var qs = new List<QandA>();
        qs.AddRange(cornerNames.Select((corner, cIx) => makeQuestion(Question.CornersColors, _Corners, formatArgs: new[] { corner }, correctAnswers: new[] { colorNames[clampColors[cIx]] })));
        qs.AddRange(colorNames.Select((col, colIx) => makeQuestion(Question.CornersColorCount, _Corners, formatArgs: new[] { col }, correctAnswers: new[] { clampColors.Count(cc => cc == colIx).ToString() })));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessCrackbox(KMBombModule module)
    {
        var comp = GetComponent(module, "CrackboxScript");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Crackbox);

        var array = GetField<Array>(comp, "originalGridItems").Get(arr => arr.Length != 16 ? "expected length 16" : null);
        var obj = array.GetValue(0);
        var fldIsBlack = GetField<bool>(obj, "IsBlack", isPublic: true);
        var fldIsLocked = GetField<bool>(obj, "IsLocked", isPublic: true);
        var fldValue = GetIntField(obj, "Value", isPublic: true);

        var qs = new List<QandA>();
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                obj = array.GetValue(y * 4 + x);
                qs.Add(makeQuestion(Question.CrackboxInitialState, _Crackbox, new[] { ((char) ('A' + x)).ToString(), (y + 1).ToString() }, new[] { fldIsBlack.GetFrom(obj) ? "black" : !fldIsLocked.GetFrom(obj) ? "white" : fldValue.GetFrom(obj).ToString() }));
            }
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessCreation(KMBombModule module)
    {
        var comp = GetComponent(module, "CreationModule");
        var fldSolved = GetField<bool>(comp, "Solved");
        var fldDay = GetIntField(comp, "Day");
        var fldWeather = GetField<string>(comp, "Weather");

        var weatherNames = GetAnswers(Question.CreationWeather);

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var currentDay = fldDay.Get(min: 1, max: 1);
        var currentWeather = fldWeather.Get(cw => !weatherNames.Contains(cw) ? "unknown weather" : null);
        var allWeather = new List<string>();
        while (true)
        {
            while (fldDay.Get() == currentDay && !fldSolved.Get() && currentWeather == fldWeather.Get())
                yield return new WaitForSeconds(0.1f);

            if (fldSolved.Get())
                break;

            if (fldDay.Get() <= currentDay)
                allWeather.Clear();
            else
                allWeather.Add(currentWeather);

            currentDay = fldDay.Get(min: 1, max: 6);
            currentWeather = fldWeather.Get(cw => !weatherNames.Contains(cw) ? "unknown weather" : null);
        }

        _modulesSolved.IncSafe(_Creation);
        addQuestions(module, allWeather.Select((t, i) => makeQuestion(Question.CreationWeather, _Creation, new[] { ordinal(i + 1) }, new[] { t })));
    }

    private IEnumerable<object> ProcessCrypticCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "CrypticCycleScript", "Cryptic Cycle", Question.CrypticCycleWord, _CrypticCycle);
    }

    private IEnumerable<object> ProcessCube(KMBombModule module)
    {
        var comp = GetComponent(module, "theCubeScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Cube);

        var rotations = GetListField<int>(comp, "selectedRotations").Get(expectedLength: 6);
        var rotationNames = new[] { "rotate cw", "tip left", "tip backwards", "rotate ccw", "tip right", "tip forwards" };
        var allRotations = rotations.Select(r => rotationNames[r]).ToArray();

        addQuestions(module, rotations.Select((rot, ix) => makeQuestion(Question.CubeRotations, _Cube, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { rotationNames[rot] }, preferredWrongAnswers: allRotations)));
    }

    private IEnumerable<object> ProcessDACHMaze(KMBombModule module)
    {
        return ProcessWorldMaze(module, "DACHMaze", _DACHMaze, Question.DACHMazeOrigin);
    }

    private IEnumerable<object> ProcessDeckOfManyThings(KMBombModule module)
    {
        var comp = GetComponent(module, "deckOfManyThingsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldSolution = GetIntField(comp, "solution");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DeckOfManyThings);

        var deck = GetField<Array>(comp, "deck").Get(d => d.Length == 0 ? "deck is empty" : null);
        var btns = GetArrayField<KMSelectable>(comp, "btns", isPublic: true).Get(expectedLength: 2);
        var prevCard = GetField<KMSelectable>(comp, "prevCard", isPublic: true).Get();
        var nextCard = GetField<KMSelectable>(comp, "nextCard", isPublic: true).Get();

        prevCard.OnInteract = delegate { return false; };
        nextCard.OnInteract = delegate { return false; };
        foreach (var btn in btns)
            btn.OnInteract = delegate
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
                btn.AddInteractionPunch(0.5f);
                return false;
            };

        string firstCardDeck = deck.GetValue(0).GetType().ToString().Replace("Card", "");

        // correcting original misspelling
        if (firstCardDeck == "Artic")
            firstCardDeck = "Arctic";

        var solution = fldSolution.Get();

        if (solution == 0)
        {
            Debug.LogFormat("[Souvenir #{0}] No question for The Deck of Many Things because the solution was the first card.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        addQuestion(module, Question.DeckOfManyThingsFirstCard, correctAnswers: new[] { firstCardDeck });
    }

    private IEnumerable<object> ProcessDecoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "DecoloredSquaresModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DecoloredSquares);

        var colColor = GetField<string>(comp, "_color1").Get();
        var rowColor = GetField<string>(comp, "_color2").Get();

        addQuestions(module,
            makeQuestion(Question.DecoloredSquaresStartingPos, _DecoloredSquares, new[] { "column" }, new[] { colColor }),
            makeQuestion(Question.DecoloredSquaresStartingPos, _DecoloredSquares, new[] { "row" }, new[] { rowColor }));
    }

    private IEnumerable<object> ProcessDiscoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "DiscoloredSquaresModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DiscoloredSquares);

        var colorsRaw = GetField<Array>(comp, "_rememberedColors").Get(arr => arr.Length != 4 ? "expected length 4" : null);
        var positions = GetArrayField<int>(comp, "_rememberedPositions").Get(expectedLength: 4);
        var colors = colorsRaw.Cast<object>().Select(obj => obj.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[0] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites.First(k => k.name == (char) ('A' + (positions[0] % 4)) + (positions[0] / 4 + 1).ToString()) }),
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[1] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites.First(k => k.name == (char) ('A' + (positions[1] % 4)) + (positions[1] / 4 + 1).ToString()) }),
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[2] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites.First(k => k.name == (char) ('A' + (positions[2] % 4)) + (positions[2] / 4 + 1).ToString()) }),
            makeQuestion(Question.DiscoloredSquaresRememberedPositions, _DiscoloredSquares, new[] { colors[3] },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: new[] { Tiles4x4Sprites.First(k => k.name == (char) ('A' + (positions[3] % 4)) + (positions[3] / 4 + 1).ToString()) }));
    }

    private IEnumerable<object> ProcessDoubleColor(KMBombModule module)
    {
        var comp = GetComponent(module, "doubleColor");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldColor = GetIntField(comp, "screenColor");
        var fldStage = GetIntField(comp, "stageNumber");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var color1 = fldColor.Get(min: 0, max: 4);
        var stage = fldStage.Get(min: 1, max: 1);
        var submitBtn = GetField<KMSelectable>(comp, "submit", isPublic: true).Get();

        var prevInteract = submitBtn.OnInteract;
        submitBtn.OnInteract = delegate
        {
            var ret = prevInteract();
            stage = fldStage.Get();
            if (stage == 1)  // This means the user got a strike. Need to retrieve the new first stage color
                // We mustn’t throw an exception inside of the button handler, so don’t check min/max values here
                color1 = fldColor.Get();
            return ret;
        };

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DoubleColor);

        // Check the value of color1 because we might have reassigned it inside the button handler
        if (color1 < 0 || color1 > 4)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Double Color because first stage color has unexpected value: {1} (expected 0 to 4).", _moduleId, color1);
            yield break;
        }

        var color2 = fldColor.Get(min: 0, max: 4);

        var colorNames = new[] { "Green", "Blue", "Red", "Pink", "Yellow" };

        addQuestions(module,
            makeQuestion(Question.DoubleColorColors, _DoubleColor, new[] { "first" }, new[] { colorNames[color1] }),
            makeQuestion(Question.DoubleColorColors, _DoubleColor, new[] { "second" }, new[] { colorNames[color2] }));
    }

    private IEnumerable<object> ProcessDoubleOh(KMBombModule module)
    {
        var comp = GetComponent(module, "DoubleOhModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DoubleOh);

        var submitIndex = GetField<Array>(comp, "_functions").Get().Cast<object>().IndexOf(f => f.ToString() == "Submit");
        if (submitIndex < 0 || submitIndex > 4)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Double-Oh because submit button is at index {1} (expected 0–4).", _moduleId, submitIndex);
            yield break;
        }

        addQuestion(module, Question.DoubleOhSubmitButton, correctAnswers: new[] { "↕↔⇔⇕◆".Substring(submitIndex, 1) });
    }

    private IEnumerable<object> ProcessDrDoctor(KMBombModule module)
    {
        var comp = GetComponent(module, "DrDoctorModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_DrDoctor);

        var diagnoses = GetArrayField<string>(comp, "_selectableDiagnoses").Get();
        var symptoms = GetArrayField<string>(comp, "_selectableSymptoms").Get();
        var diagnoseText = GetField<TextMesh>(comp, "DiagnoseText", isPublic: true).Get();

        addQuestions(module,
            makeQuestion(Question.DrDoctorDiseases, _DrDoctor, correctAnswers: diagnoses.Except(new[] { diagnoseText.text }).ToArray()),
            makeQuestion(Question.DrDoctorSymptoms, _DrDoctor, correctAnswers: symptoms));
    }

    private IEnumerable<object> ProcessElderFuthark(KMBombModule module)
    {
        var comp = GetComponent(module, "ElderFutharkScript");

        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldPickedRuneNames = GetArrayField<string>(comp, "pickedRuneNames");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ElderFuthark);

        var pickedRuneNames = fldPickedRuneNames.Get();

        if (pickedRuneNames == null)
            yield break;

        if (pickedRuneNames.Length != 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Elder Futhark because pickedRuneNames has unexpected length {1}. Expected length 2", _moduleId, pickedRuneNames.Length);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.ElderFutharkRunes, _ElderFuthark, correctAnswers: new[] { pickedRuneNames[0] }, formatArgs: new[] { "first" }, preferredWrongAnswers: pickedRuneNames),
            makeQuestion(Question.ElderFutharkRunes, _ElderFuthark, correctAnswers: new[] { pickedRuneNames[1] }, formatArgs: new[] { "second" }, preferredWrongAnswers: pickedRuneNames));
    }

    private IEnumerable<object> ProcessEncryptedEquations(KMBombModule module)
    {
        var comp = GetComponent(module, "EncryptedEquations");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldEquation = GetField<object>(comp, "CurrentEquation");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_EncryptedEquations);

        var equation = fldEquation.Get();
        if (equation == null)
            yield break;
        var fldOperands = new[] { "LeftOperand", "MiddleOperand", "RightOperand" }.Select(fldName => GetField<object>(equation, fldName, isPublic: true)).ToArray();
        if (fldOperands.Contains(null))
            yield break;
        var operands = fldOperands.Select(fld => fld.Get()).ToArray();
        if (operands.Contains(null))
            yield break;
        var fldShapes = operands.Select(op => GetField<object>(op, "Shape", isPublic: true)).ToArray();
        if (fldShapes.Contains(null))
            yield break;
        var shapes = fldShapes.Select(fld => fld.Get()).ToArray();
        if (shapes.Contains(null))
            yield break;
        var fldTextureIndexes = shapes.Select(sh => GetIntField(sh, "TextureIndex", isPublic: true)).ToArray();
        if (fldTextureIndexes.Contains(null))
            yield break;
        var textureIndexes = fldTextureIndexes.Select(fld => fld.Get()).ToArray();
        if (textureIndexes.Any(tx => tx < 0 || tx >= EncryptedEquationsSprites.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Encrypted Equations because the ‘TextureIndex’ values for the shapes contained an unexpected value: [{1}]", _moduleId, textureIndexes.JoinString(", "));
            yield break;
        }

        addQuestions(module, textureIndexes
            .Select((txIx, opIx) => txIx == -1 ? null : new { Shape = EncryptedEquationsSprites[txIx], Ordinal = ordinal(opIx + 1) })
            .Where(inf => inf != null)
            .Select(inf => makeQuestion(Question.EncryptedEquationsShapes, _EncryptedEquations, formatArgs: new[] { inf.Ordinal }, correctAnswers: new[] { inf.Shape }, preferredWrongAnswers: EncryptedEquationsSprites)));
    }

    private IEnumerable<object> ProcessEncryptedHangman(KMBombModule module)
    {
        var comp = GetComponent(module, "HangmanScript");
        var fldSolved = GetField<bool>(comp, "isSolved", isPublic: true);
        var fldModuleName = GetField<string>(comp, "moduleName", isPublic: true);
        var fldEncryptionMethod = GetIntField(comp, "encryptionMethod");

        var moduleName = fldModuleName.Get();
        if (moduleName == null)
            yield break;
        if (moduleName.Length == 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Encrypted Hangman because ‘moduleName’ was empty.", _moduleId);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_EncryptedHangman);

        var wrongModuleNames = Bomb.GetSolvableModuleNames();
        // If there are less than 4 eligible modules, fill the remaining spaces with random other modules.
        if (wrongModuleNames.Count < 4)
            wrongModuleNames.AddRange(_attributes.Where(x => x.Value != null).Select(x => x.Value.ModuleNameWithThe).Distinct());

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.EncryptedHangmanModule, _EncryptedHangman, correctAnswers: new[] { moduleName }, preferredWrongAnswers: wrongModuleNames.ToArray()));
        if (fldEncryptionMethod != null)
        {
            var encryptionMethod = fldEncryptionMethod.Get();
            var encryptionMethodNames = new[] { "Caesar Cipher", "Playfair Cipher", "Rot-13 Cipher", "Atbash Cipher", "Affine Cipher", "Modern Cipher", "Vigenère Cipher" };
            if (encryptionMethod < 0 || encryptionMethod >= encryptionMethodNames.Length)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Encrypted Hangman because ‘encryptionMethod’ has unexpected value {1} (expected 0–{2}).", _moduleId, encryptionMethod, encryptionMethodNames.Length - 1);
                yield break;
            }
            qs.Add(makeQuestion(Question.EncryptedHangmanEncryptionMethod, _EncryptedHangman, correctAnswers: new[] { encryptionMethodNames[encryptionMethod] }));
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessEncryptedMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "EncryptedMorseModule");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldIndex = GetIntField(comp, "callResponseIndex");
        var fldCalls = GetStaticField<string[]>(comp.GetType(), "calls");
        var fldResponses = GetStaticField<string[]>(comp.GetType(), "responses");

        string[] formatCalls = { "Detonate", "Ready Now", "We're Dead", "She Sells", "Remember", "Great Job", "Solo This", "Keep Talk" };
        string[] formatResponses = { "Please No", "Cheesecake", "Sadface", "Sea Shells", "Souvenir", "Thank You", "I Dare You", "No Explode" };

        int index = fldIndex.Get();
        string[] calls = fldCalls.Get();
        string[] responses = fldResponses.Get();

        if (index < 0 || index > formatCalls.Length)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Encrypted Morse because 'callResponseIndex' points to an invalid call/response pair: {1}.", _moduleId, index);
            yield break;
        }
        if (formatCalls.Length != calls.Length || formatResponses.Length != responses.Length)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Encrypted Morse because the call/response pairs are not the expected ones.", _moduleId);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_EncryptedMorse);
        addQuestions(module,
            makeQuestion(Question.EncryptedMorseCallResponse, _EncryptedMorse, new[] { "received call" }, new[] { formatCalls[index] }, formatCalls),
            makeQuestion(Question.EncryptedMorseCallResponse, _EncryptedMorse, new[] { "sent response" }, new[] { formatResponses[index] }, formatResponses));
    }

    private IEnumerable<object> ProcessEquationsX(KMBombModule module)
    {
        var comp = GetComponent(module, "EquationsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldSymbolDisplay = GetField<GameObject>(comp, "symboldisplay", isPublic: true);

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var symbolObject = fldSymbolDisplay.Get();

        if (symbolObject == null)
            yield break;

        var symbol = symbolObject.GetComponentInChildren<TextMesh>().text;

        if (!new[] { "H(T)", "R", "\u03C7", "w", "Z(T)", "t", "m", "a", "K" }.Contains(symbol))
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Equations X because 'symbol' has an unexpected character: {1}", _moduleId, symbol);
            yield break;
        }

        // Equations X uses symbols that don’t translate well to Souvenir. This switch statement is used to correctly translate the answer.
        switch (symbol)
        {
            case "R":
                symbol = "P";
                break;
            case "w":
                symbol = "\u03C9";
                break;
            case "t":
                symbol = "\u03C4";
                break;
            case "m":
                symbol = "\u03BC";
                break;
            case "a":
                symbol = "\u03B1";
                break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_EquationsX);

        addQuestion(module, Question.EquationsXSymbols, correctAnswers: new[] { symbol });
    }

    private IEnumerable<object> ProcessEtterna(KMBombModule module)
    {
        var comp = GetComponent(module, "Etterna");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldCorrect = GetArrayField<byte>(comp, "correct");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Etterna);

        var correct = fldCorrect.Get();

        if (correct.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Etterna because ‘correct’ has unexpected number of values (expected 4 numbers in array): {1}", _moduleId, correct.Length);
            yield break;
        }

        for (int i = 0; i < correct.Length; i++)
            if (correct[i] > 32 || correct[i] == 0)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Etterna because ‘correct[{1}]’ has unexpected value (expected 1-32): {1}", _moduleId, i, correct[i]);
                yield break;
            }

        byte arrow = (byte) Rnd.Range(0, 4);
        addQuestion(module, Question.EtternaNumber, new[] { ordinal(arrow + 1) }, correctAnswers: new[] { correct[arrow].ToString() });
    }

    private IEnumerable<object> ProcessFactoryMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "FactoryMazeScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldUsedRooms = GetArrayField<string>(comp, "usedRooms");
        var fldStartRoom = GetIntField(comp, "startRoom");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_FactoryMaze);

        string[] usedRooms = fldUsedRooms.Get();
        int startRoom = fldStartRoom.Get();

        if (usedRooms == null)
            yield break;
        if (usedRooms.Length != 5)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Factory Maze: expected 'usedRooms' to have length 5, but was {1}.", _moduleId, usedRooms.Length);
            yield break;
        }
        if (startRoom < 0 || startRoom >= usedRooms.Length)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Factory Maze: 'startRoom' pointed to an unnexpected room: {1}.", _moduleId, startRoom);
            yield break;
        }

        for (int i = usedRooms.Length - 1; i >= 0; --i)
            usedRooms[i] = usedRooms[i].Replace('\n', ' ');

        addQuestion(module, Question.FactoryMazeStartRoom, correctAnswers: new[] { usedRooms[startRoom] }, preferredWrongAnswers: usedRooms);
    }

    private IEnumerable<object> ProcessFastMath(KMBombModule module)
    {
        var comp = GetComponent(module, "FastMathModule");
        var fldScreen = GetField<TextMesh>(comp, "Screen", isPublic: true);
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var prevLetters = new HashSet<string>();
        string letters = null;
        while (!fldSolved.Get())
        {
            var display = fldScreen.Get().text;
            if (display.Length != 3)
            {
                Debug.LogFormat(@"<Souvenir #{1}> Abandoning Fast Math because the screen contains something other than three characters: ""{0}"" ({2} characters).", display, _moduleId, display.Length);
                yield break;
            }
            letters = display[0] + "" + display[2];
            prevLetters.Add(letters);
            yield return new WaitForSeconds(.1f);
        }
        if (letters == null)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Fast Math because no letters were extracted before the module was solved.", _moduleId);
            yield break;
        }

        _modulesSolved.IncSafe(_FastMath);
        addQuestion(module, Question.FastMathLastLetters, correctAnswers: new[] { letters }, preferredWrongAnswers: prevLetters.ToArray());
    }

    private IEnumerable<object> ProcessFaultyRGBMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "FaultyRGBMazeScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldKeyPos = GetArrayField<int[]>(comp, "keylocations");
        var fldMazeNum = GetArrayField<int[]>(comp, "mazenumber");
        var fldExitPos = GetArrayField<int>(comp, "exitlocation");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_FaultyRGBMaze);

        var keyPos = fldKeyPos.Get();
        var mazeNum = fldMazeNum.Get();
        var exitPos = fldExitPos.Get();

        if (keyPos == null || mazeNum == null || exitPos == null)
            yield break;

        if (keyPos.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Faulty RGB Maze because 'KeyPos' has an unexpected length: Length = {1}", _moduleId, keyPos.Length);
            yield break;
        }

        if (keyPos.Any(key => key.Length != 2 || key.Any(number => number < 0 || number > 6)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Faulty RGB Maze because 'KeyPos' contains keys with invalid positions: [{1}]", _moduleId, keyPos.Select(key => string.Format("Length = {0}, ({1},{2})", key.Length, key[1], key[0])).JoinString("; "));
            yield break;
        }

        if (mazeNum.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Faulty RGB Maze because 'MazeNum' or has an unexpected length: Length = {1}", _moduleId, mazeNum.Length);
            yield break;
        }

        if (mazeNum.Any(maze => maze.Length != 2 || maze[0] < 0 || maze[0] > 15))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Faulty RGB Maze because 'MazeNum' contains mazes with invalid number: [{1}]", _moduleId, mazeNum.Select(maze => string.Format("Length = {0}, Maze {1}", maze.Length, maze[0])).JoinString("; "));
            yield break;
        }

        if (exitPos.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Faulty RGB Maze because 'exitPos' has an unexpected length: Length = {1}", _moduleId, exitPos.Length);
            yield break;
        }

        if (exitPos[1] < 0 || exitPos[1] > 6 || exitPos[2] < 0 || exitPos[2] > 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Faulty RGB Maze because 'exitPos' contains invalid coordinate: ({1},{2})", _moduleId, exitPos[2], exitPos[1]);
            yield break;
        }

        string[] colors = { "red", "green", "blue" };

        var qs = new List<QandA>();

        for (int index = 0; index < 3; index++)
        {
            qs.Add(makeQuestion(Question.FaultyRGBMazeKeys, _FaultyRGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { "ABCDEFG"[keyPos[index][1]] + (keyPos[index][0] + 1).ToString() }));
            qs.Add(makeQuestion(Question.FaultyRGBMazeNumber, _FaultyRGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { "0123456789abcdef"[mazeNum[index][0]].ToString() }));
        }

        qs.Add(makeQuestion(Question.FaultyRGBMazeExit, _FaultyRGBMaze,
            correctAnswers: new[] { "ABCDEFG"[exitPos[2]] + (exitPos[1] + 1).ToString() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessFlags(KMBombModule module)
    {
        var comp = GetComponent(module, "FlagsModule");
        var fldMainCountry = GetField<object>(comp, "mainCountry");
        var fldCountries = GetField<IList>(comp, "countries");
        var fldNumber = GetIntField(comp, "number");
        var fldCanInteract = GetField<bool>(comp, "canInteract");

        yield return null;

        var mainCountry = fldMainCountry.Get();
        var countries = fldCountries.Get();
        var number = fldNumber.Get();

        if (mainCountry == null || countries == null)
            yield break;
        if (countries.Count != 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Flags because ‘countries’ has length {1} (expected 7).", _moduleId, countries.Count);
            yield break;
        }
        if (number < 1 || number > 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Flags because ‘number’ has unexpected value {1} (expected 1–7).", _moduleId, number);
            yield break;
        }

        var propCountryName = GetProperty<string>(mainCountry, "CountryName", isPublic: true);
        var mainCountrySprite = FlagsSprites.FirstOrDefault(spr => spr.name == propCountryName.GetFrom(mainCountry));
        var otherCountrySprites = countries.Cast<object>().Select(country => FlagsSprites.FirstOrDefault(spr => spr.name == propCountryName.GetFrom(country))).ToArray();

        if (mainCountrySprite == null || otherCountrySprites.Any(spr => spr == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Flags because one of the countries has a name with no corresponding sprite: main country = {1}, other countries = [{2}].", _moduleId, propCountryName.GetFrom(mainCountry), countries.Cast<object>().Select(country => propCountryName.GetFrom(country)).JoinString(", "));
            yield break;
        }

        while (fldCanInteract.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Flags);

        addQuestions(module,
            // Displayed number
            makeQuestion(Question.FlagsDisplayedNumber, _Flags, correctAnswers: new[] { number.ToString() }),
            // Main country flag
            makeQuestion(Question.FlagsMainCountry, _Flags, correctAnswers: new[] { mainCountrySprite }, preferredWrongAnswers: otherCountrySprites),
            // Rest of the country flags
            makeQuestion(Question.FlagsCountries, _Flags, correctAnswers: otherCountrySprites, preferredWrongAnswers: FlagsSprites));
    }

    private IEnumerable<object> ProcessFlashingLights(KMBombModule module)
    {
        var comp = GetComponent(module, "doubleNegativesScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldTopColors = GetField<List<int>>(comp, "selectedColours");
        var fldBottomColors = GetField<List<int>>(comp, "selectedColours2");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_FlashingLights);

        var topColors = fldTopColors.Get();
        var bottomColors = fldBottomColors.Get();
        if (topColors == null || bottomColors == null)
            yield break;

        if (topColors.Count != 12)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Flashing Lights because ‘selectedColours’ list has unexpected length ({1} instead of 12).", _moduleId, topColors.Count);
            yield break;
        }
        if (bottomColors.Count != 12)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Flashing Lights because ‘selectedColours2’ list has unexpected length ({1} instead of 12).", _moduleId, bottomColors.Count);
            yield break;
        }

        var colorNames = new[] { "blue", "green", "red", "purple", "orange" };

        var topTotals = Enumerable.Range(1, 5).Select(num => topColors.Count(x => x == num)).ToArray();
        var bottomTotals = Enumerable.Range(1, 5).Select(num => bottomColors.Count(x => x == num)).ToArray();

        var qs = new List<QandA>();
        for (int i = 0; i < 5; i++)
        {
            qs.Add(makeQuestion(Question.FlashingLightsLEDFrequency, _FlashingLights, new[] { "top", colorNames[i] }, new[] { topTotals[i].ToString() }, new[] { bottomTotals[i].ToString() }));
            qs.Add(makeQuestion(Question.FlashingLightsLEDFrequency, _FlashingLights, new[] { "bottom", colorNames[i] }, new[] { bottomTotals[i].ToString() }, new[] { topTotals[i].ToString() }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessForgetTheColors(KMBombModule module)
    {
        var comp = GetComponent(module, "FTC");
        var fldStage = GetIntField(comp, "stage");
        var fldMaxStage = GetIntField(comp, "maxStage");

        var fldGear = GetField<List<byte>>(comp, "gear");
        var fldLargeDisplay = GetField<List<short>>(comp, "largeDisplay");
        var fldSineNumber = GetField<List<int>>(comp, "sineNumber");
        var fldGearColor = GetField<List<string>>(comp, "gearColor");
        var fldRuleColor = GetField<List<string>>(comp, "ruleColor");

        yield return null;

        int ftcCount;
        if (!_moduleCounts.TryGetValue(_ForgetTheColors, out ftcCount) || ftcCount > 1)
        {
            Debug.LogFormat("[Souvenir #{0}] Abandoning ForgetTheColors because there is more than one of them.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        var maxStage = fldMaxStage.Get();
        var stage = fldStage.Get();

        var gear = fldGear.Get();
        var largeDisplay = fldLargeDisplay.Get();
        var sineNumber = fldSineNumber.Get();
        var gearColor = fldGearColor.Get();
        var ruleColor = fldRuleColor.Get();

        if (maxStage < stage)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because the 'stage' had an unexpected value: expected 0-{1}, was {2}.", _moduleId, maxStage, stage);
            yield break;
        }

        string[] colors = { "Red", "Orange", "Yellow", "Green", "Cyan", "Blue", "Purple", "Pink", "Maroon", "White", "Gray" };

        var randomStage = 0;
        // Uncomment the line below if you want the module to pick a random stage instead of only stage 0.
        // var randomStage = Rnd.Range(0, Math.Min(maxStage, _coroutinesActive)) % 100;
        Debug.LogFormat("<Souvenir #{0}> Waiting for stage {1} of ForgetTheColors.", _moduleId, randomStage);
        while (fldStage.Get() <= randomStage)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ForgetTheColors);

        if (gear.Count <= randomStage || largeDisplay.Count <= randomStage || sineNumber.Count <= randomStage || gearColor.Count <= randomStage || ruleColor.Count <= randomStage)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because one or more of the lists have an unexpected level of entries. (Expected less than or equal {1}): Gear: {2}, LargeDisplay: {3}, SineNumber: {4}, GearColor: {5}, RuleColor: {6}", _moduleId, randomStage, gear.Count, largeDisplay.Count, sineNumber.Count, gearColor.Count, ruleColor.Count);
            yield break;
        }

        if (!new[] { gear.Count, largeDisplay.Count, sineNumber.Count, gearColor.Count, ruleColor.Count }.All(x => x == gear.Count))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because one or more of the lists aren't all the same length. (Expected {1}): Gear: {1}, LargeDisplay: {2}, SineNumber: {3}, GearColor: {4}, RuleColor: {5}", _moduleId, gear.Count, largeDisplay.Count, sineNumber.Count, gearColor.Count, ruleColor.Count);
            yield break;
        }

        for (int i = 0; i < gear.Count; i++)
        {
            if (gear[i] < 0 || gear[i] > 9)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because ‘gear[{1}]’ had an unexpected value. (Expected 0-9): {2}", _moduleId, i, gear[i]);
                yield break;
            }

            if (largeDisplay[i] < 0 || largeDisplay[i] > 990)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because ‘largeDisplay[{1}]’ had an unexpected value. (Expected 0-990): {2}", _moduleId, i, largeDisplay[i]);
                yield break;
            }

            if (sineNumber[i] < -99999 || sineNumber[i] > 99999)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because ‘sineNumber[{1}]’ had an unexpected value. (Expected (-99999)-99999): {2}", _moduleId, i, sineNumber[i]);
                yield break;
            }

            if (!colors.Contains(gearColor[i]))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because ‘gearColor[{1}]’ had an unexpected value. (Expected {2}): {3}", _moduleId, i, colors.JoinString(", "), sineNumber[i]);
                yield break;
            }

            if (!colors.Contains(ruleColor[i]))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning ForgetTheColors because ‘ruleColor[{1}]’ had an unexpected value. (Expected {2}): {3}", _moduleId, i, colors.JoinString(", "), ruleColor[i]);
                yield break;
            }
        }

        // Only generate a single question.
        switch (Rnd.Range(0, 5))
        {
            case 0:
                addQuestions(module, (makeQuestion(Question.ForgetTheColorsGearNumber, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { gear[randomStage].ToString() }, preferredWrongAnswers: new[] { Rnd.Range(0, 10).ToString() })));
                break;

            case 1:
                addQuestions(module, (makeQuestion(Question.ForgetTheColorsLargeDisplay, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { largeDisplay[randomStage].ToString() }, preferredWrongAnswers: new[] { Rnd.Range(0, 991).ToString() })));
                break;

            case 2:
                addQuestions(module, (makeQuestion(Question.ForgetTheColorsSineNumber, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { (Mathf.Abs(sineNumber[randomStage]) % 10).ToString() }, preferredWrongAnswers: new[] { Rnd.Range(0, 10).ToString() })));
                break;

            case 3:
                addQuestions(module, (makeQuestion(Question.ForgetTheColorsGearColor, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { gearColor[randomStage].ToString() }, preferredWrongAnswers: new[] { colors[Rnd.Range(0, colors.Length)] })));
                break;

            case 4:
                addQuestions(module, (makeQuestion(Question.ForgetTheColorsRuleColor, _ForgetTheColors, new[] { randomStage.ToString() }, correctAnswers: new[] { ruleColor[randomStage].ToString() }, preferredWrongAnswers: new[] { colors[Rnd.Range(0, colors.Length)] })));
                break;
        }
    }

    private IEnumerable<object> ProcessFreeParking(KMBombModule module)
    {
        var comp = GetComponent(module, "FreeParkingScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldTokens = GetArrayField<Material>(comp, "tokenOptions", isPublic: true);
        var fldSelected = GetIntField(comp, "tokenIndex");

        Material[] tokens = fldTokens.Get();
        int selected = fldSelected.Get();

        if (tokens == null)
            yield break;
        if (tokens.Length != 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Free Parking because the 'tokenOptions' had unexpected length: expected 7, was {1}.", _moduleId, tokens.Length);
            yield break;
        }
        if (selected < 0 || selected >= tokens.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Free Parking because the 'tokenIndex' points to illegal token: {1}.", _moduleId, selected);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_FreeParking);
        addQuestion(module, Question.FreeParkingToken, correctAnswers: new[] { tokens[selected].name });
    }

    private IEnumerable<object> ProcessFunctions(KMBombModule module)
    {
        var comp = GetComponent(module, "qFunctions");
        var fldFirstLastDigit = GetIntField(comp, "firstLastDigit");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldLeftNum = GetIntField(comp, "numberA");
        var fldRightNum = GetIntField(comp, "numberB");
        var fldLetter = GetField<string>(comp, "ruleLetter");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Functions);

        var lastDigit = fldFirstLastDigit.Get();
        if (lastDigit == -1)
        {
            Debug.LogFormat("[Souvenir #{0}] No questions for Functions because it was solved with no queries! This isn’t a bug, just impressive (or cheating).", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }
        else if (lastDigit > 9 || lastDigit < 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Functions because the first last digit is {1} when it should be from 0 to 9.", _moduleId, lastDigit);
            yield break;
        }

        var lNum = fldLeftNum.Get();
        var rNum = fldRightNum.Get();
        if (lNum > 999 || lNum < 1)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Functions because the number to the left of the letter {1} when it should have been from 1 to 999.", _moduleId, lNum);
            yield break;
        }
        if (rNum > 999 || rNum < 1)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Functions because the number to the right of the letter {1} when it should have been from 1 to 999.", _moduleId, rNum);
            yield break;
        }
        var theLetter = fldLetter.Get();
        if (theLetter == null || theLetter.Length != 1)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Functions because the displayed letter is not a single letter (it’s {1}).", _moduleId, theLetter ?? "<null>");
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.FunctionsLastDigit, _Functions, correctAnswers: new[] { lastDigit.ToString() }),
            makeQuestion(Question.FunctionsLeftNumber, _Functions, correctAnswers: new[] { lNum.ToString() }, preferredWrongAnswers:
                Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(1, 999).ToString()).Distinct().Take(6).ToArray()),
            makeQuestion(Question.FunctionsLetter, _Functions, correctAnswers: new[] { theLetter }),
            makeQuestion(Question.FunctionsRightNumber, _Functions, correctAnswers: new[] { rNum.ToString() }, preferredWrongAnswers:
                Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(1, 999).ToString()).Distinct().Take(6).ToArray()));
    }

    private IEnumerable<object> ProcessGiantsDrink(KMBombModule module)
    {
        var comp = GetComponent(module, "giantsDrinkScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldSolEvenStrikes = GetIntField(comp, "evenStrikes");
        var fldSolOddStrikes = GetIntField(comp, "oddStrikes");
        var fldLiquids = GetArrayField<int>(comp, "liquid");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_GiantsDrink);

        int sol = Bomb.GetStrikes() % 2 == 0 ? fldSolEvenStrikes.Get() : fldSolOddStrikes.Get();
        int[] liquids = fldLiquids.Get();
        string[] liquidNames = { "Red", "Blue", "Green", "Orange", "Purple", "Cyan" };

        if (liquids == null)
            yield break;
        if (liquids.Length != 2)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning The Giant's Drink because 'liquid' had length {1} (expected length 2).", _moduleId, liquids.Length);
            yield break;
        }
        if (sol < 0 || sol >= liquids.Length)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning The Giant's Drink because 'evenStrikes' or 'oddStrikes' pointed to illegal goblet: {1}.", _moduleId, sol);
            yield break;
        }

        addQuestion(module, Question.GiantsDrinkLiquid, correctAnswers: new[] { liquidNames[liquids[sol]] });
    }

    private IEnumerable<object> ProcessGreenArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "GreenArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldNumDisplay = GetField<GameObject>(comp, "numDisplay", isPublic: true);
        var fldStreak = GetIntField(comp, "streak");
        var fldAnimating = GetField<bool>(comp, "isanimating");

        yield return null;

        string numbers = null;
        bool activated = false;
        while (!fldSolved.Get())
        {
            int streak = fldStreak.Get();
            bool animating = fldAnimating.Get();
            if (streak == 6 && !animating && !activated)
            {
                var numDisplay = fldNumDisplay.Get();
                if (numDisplay == null)
                    yield break;
                numbers = numDisplay.GetComponent<TextMesh>().text;
                if (numbers == null)
                    yield break;
                activated = true;
            }
            if (streak == 0)
                activated = false;
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_GreenArrows);

        int number;
        if (!int.TryParse(numbers, out number))
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Green Arrows because the screen couldn’t be parsed correctly: “{0}”.", _moduleId, numbers);
            yield break;
        }

        if (number < 0 || number > 99)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Green Arrows because ‘number’ is out of range: number = {1}, expected 0-99", _moduleId, number);
            yield break;
        }

        addQuestions(module, makeQuestion(Question.GreenArrowsLastScreen, _GreenArrows, correctAnswers: new[] { number.ToString() }));
    }

    private IEnumerable<object> ProcessGridLock(KMBombModule module)
    {
        var comp = GetComponent(module, "GridlockModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldPages = GetArrayField<int[]>(comp, "_pages");
        var fldSolution = GetIntField(comp, "_solution");

        var colors = GetAnswers(Question.GridLockStartingColor);
        if (colors == null)
            yield break;

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var solution = fldSolution.Get();
        var pages = fldPages.Get();
        if (pages == null || pages.Length < 5 || pages.Length > 10 || solution < 0 || solution > 15 ||
            pages.Any(p => p == null || p.Length != 16 || p.Any(q => q < 0 || (q & 15) > 12 || (q & (15 << 4)) > (4 << 4))))
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Gridlock because unxpected values were found (pages={1}, solution={2}).", _moduleId, pages == null ? "<null>" : string.Format("[{0}]", pages.Select(p => string.Format("[{0}]", p.JoinString(", "))).JoinString(", ")), solution);
            yield break;
        }

        var start = pages[0].IndexOf(i => (i & 15) == 4);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_GridLock);
        addQuestions(module,
            makeQuestion(Question.GridLockStartingLocation, _GridLock, correctAnswers: new[] { ((char) ('A' + start % 4)).ToString() + (char) ('1' + start / 4) }),
            makeQuestion(Question.GridLockEndingLocation, _GridLock, correctAnswers: new[] { ((char) ('A' + solution % 4)).ToString() + (char) ('1' + solution / 4) }),
            makeQuestion(Question.GridLockStartingColor, _GridLock, correctAnswers: new[] { colors[(pages[0][start] >> 4) - 1] }));
    }

    private IEnumerable<object> ProcessGryphons(KMBombModule module)
    {
        var comp = GetComponent(module, "Gryphons");
        var fldAge = GetIntField(comp, "age");
        var fldName = GetField<string>(comp, "theirName");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Gryphons);

        var age = fldAge.Get();
        var name = fldName.Get();

        if (age < 23 || age > 34)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Gryphons because the gryphon's age is {1} when it should be from 23 to 34.", _moduleId, age);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.GryphonsName, _Gryphons, correctAnswers: new[] { name }),
            makeQuestion(Question.GryphonsAge, _Gryphons, correctAnswers: new[] { age.ToString() }, preferredWrongAnswers:
                Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(23, 34).ToString()).Distinct().Take(6).ToArray()));
    }

    private static readonly string[] _logicalButtonsButtonNames = new[] { "top", "bottom-left", "bottom-right" };
    private IEnumerable<object> ProcessLogicalButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "LogicalButtonsScript");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldStage = GetIntField(comp, "stage");
        var fldButtons = GetField<Array>(comp, "buttons");
        var fldGateOperator = GetField<object>(comp, "gateOperator");
        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var curStage = 0;
        var colors = new string[3][];
        var labels = new string[3][];
        var initialOperators = new string[3];

        FieldInfo<string> fldLabel = null;
        FieldInfo<object> fldColor = null;
        FieldInfo<int> fldIndex = null;
        MethodInfo<string> mthGetName = null;

        while (!fldSolved.Get())
        {
            var buttons = fldButtons.Get();
            if (buttons == null || buttons.Length != 3)
            {
                Debug.LogFormat(@"<Souvenir #{0}> Abandoning Logical Buttons because “buttons” {1} (expected length 3).", _moduleId, buttons == null ? "is null" : "has length " + buttons.Length);
                yield break;
            }
            var infs = buttons.Cast<object>().Select(obj =>
            {
                fldLabel = fldLabel ?? GetField<string>(obj, "<Label>k__BackingField");
                fldColor = fldColor ?? GetField<object>(obj, "<Color>k__BackingField");
                fldIndex = fldIndex ?? GetIntField(obj, "<Index>k__BackingField");
                return fldLabel == null || fldColor == null || fldIndex == null
                    ? null
                    : new { Label = fldLabel.GetFrom(obj), Color = fldColor.GetFrom(obj), Index = fldIndex.GetFrom(obj) };
            }).ToArray();
            if (infs.Length != 3 || infs.Any(inf => inf == null || inf.Label == null || inf.Color == null) || infs[0].Index != 0 || infs[1].Index != 1 || infs[2].Index != 2)
            {
                Debug.LogFormat(@"<Souvenir #{0}> Abandoning Logical Buttons because I got an unexpected value ([{1}]).", _moduleId, infs.Select(inf => inf == null ? "<null>" : inf.ToString()).JoinString(", "));
                yield break;
            }

            var gateOperator = fldGateOperator.Get();
            if (gateOperator != null && mthGetName == null)
            {
                var interfaceType = gateOperator.GetType().Assembly.GetType("ILogicalGateOperator");
                if (interfaceType == null)
                {
                    Debug.LogFormat(@"<Souvenir #{0}> Abandoning Logical Buttons because interface type ILogicalGateOperator not found.", _moduleId);
                    yield break;
                }
                var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                var mths = interfaceType.GetMethods(bindingFlags).Where(m => m.Name == "get_Name" && m.GetParameters().Length == 0 && typeof(string).IsAssignableFrom(m.ReturnType)).Take(2).ToArray();
                if (mths.Length == 0)
                {
                    Debug.LogFormat("<Souvenir #{5}> Type {0} does not contain {1} method {2} with return type {3} and {4} parameters.", interfaceType, "public", name, "string", 0, _moduleId);
                    yield break;
                }
                if (mths.Length > 1)
                {
                    Debug.LogFormat("<Souvenir #{5}> Type {0} contains multiple {1} methods {2} with return type {3} and {4} parameters.", interfaceType, "public", name, "string", 0, _moduleId);
                    yield break;
                }
                mthGetName = new MethodInfo<string>(null, mths[0]);
            }
            if (gateOperator == null || mthGetName == null)
                yield break;

            var clrs = infs.Select(inf => inf.Color.ToString()).ToArray();
            var lbls = infs.Select(inf => inf.Label).ToArray();
            var iOp = mthGetName.InvokeOn(gateOperator);

            var stage = fldStage.Get();
            if (stage != curStage || !clrs.SequenceEqual(colors[stage - 1]) || !lbls.SequenceEqual(labels[stage - 1]) || iOp != initialOperators[stage - 1])
            {
                if (stage != curStage && stage != curStage + 1)
                {
                    Debug.LogFormat(@"<Souvenir #{0}> Abandoning Logical Buttons because I must have missed a stage (it went from {1} to {2}).", _moduleId, curStage, stage);
                    yield break;
                }
                if (stage < 1 || stage > 3)
                {
                    Debug.LogFormat(@"<Souvenir #{0}> Abandoning Logical Buttons because ‘stage’ has unexpected value {1} (expected 1–3).", _moduleId, stage);
                    yield break;
                }

                colors[stage - 1] = clrs;
                labels[stage - 1] = lbls;
                initialOperators[stage - 1] = iOp;
                curStage = stage;
            }

            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_LogicalButtons);
        if (initialOperators.Any(io => io == null))
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Logical Buttons because there is a null initial operator ([{1}]).", _moduleId, initialOperators.Select(io => io == null ? "<null>" : string.Format(@"""{0}""", io)).JoinString(", "));
            yield break;
        }

        addQuestions(module,
            colors.SelectMany((clrs, stage) => clrs.Select((clr, btnIx) => makeQuestion(Question.LogicalButtonsColor, _LogicalButtons, new[] { _logicalButtonsButtonNames[btnIx], ordinal(stage + 1) }, new[] { clr })))
                .Concat(labels.SelectMany((lbls, stage) => lbls.Select((lbl, btnIx) => makeQuestion(Question.LogicalButtonsLabel, _LogicalButtons, new[] { _logicalButtonsButtonNames[btnIx], ordinal(stage + 1) }, new[] { lbl }))))
                .Concat(initialOperators.Select((op, stage) => makeQuestion(Question.LogicalButtonsOperator, _LogicalButtons, new[] { ordinal(stage + 1) }, new[] { op }))));
    }

    private IEnumerable<object> ProcessHereditaryBaseNotation(KMBombModule module)
    {
        var comp = GetComponent(module, "hereditaryBaseNotationScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldInitialNum = GetIntField(comp, "initialNumber");
        var fldBaseN = GetIntField(comp, "baseN");
        var mthNumberToBaseNString = GetMethod<string>(comp, "numberToBaseNString", numParameters: 2);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HereditaryBaseNotation);

        int baseN = fldBaseN.Get();

        if (baseN < 3 || baseN > 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Hereditary Base Notation because 'baseN' has an invalid value (expected 3 - 7): {1}", _moduleId, baseN);
            yield break;
        }

        int upperBound;

        switch (baseN)
        {
            case 3:
                upperBound = 19682;
                break;
            case 4:
                upperBound = 60000;
                break;
            case 5:
                upperBound = 80000;
                break;
            default:
                upperBound = 100000;
                break;
        }

        int initialNum = fldInitialNum.Get();

        if (initialNum < 1 || initialNum > upperBound)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Hereditary Base Notation because 'initialNum' has an invalid value (expected 1 - {1}): {2}", _moduleId, upperBound, initialNum);
            yield break;
        }

        string answer = mthNumberToBaseNString.Invoke(baseN, initialNum).ToString();
        HashSet<string> invalidAnswer = new HashSet<string>();
        invalidAnswer.Add(answer);

        //Generate fake options in the same base of the answer.
        while (invalidAnswer.Count() < 4)
        {
            var wrongAnswer = Rnd.Range(1, upperBound + 1);
            invalidAnswer.Add(mthNumberToBaseNString.Invoke(baseN, wrongAnswer).ToString());
        }

        invalidAnswer.Add(answer);
        addQuestions(module, makeQuestion(Question.HereditaryBaseNotationInitialNumber, _HereditaryBaseNotation, null, new[] { answer }, invalidAnswer.ToArray()));
    }

    private IEnumerable<object> ProcessHexabutton(KMBombModule module)
    {
        var comp = GetComponent(module, "hexabuttonScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldLabels = GetArrayField<string>(comp, "labels");
        var fldIndex = GetIntField(comp, "labelNum");

        string[] labels = fldLabels.Get();
        int index = fldIndex.Get();

        if (labels == null)
            yield break;
        if (index < 0 || index >= labels.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Hexabutton because 'labelNum' points to illegal label: {1}.", _moduleId, index);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Hexabutton);
        addQuestion(module, Question.HexabuttonLabel, correctAnswers: new[] { labels[index] });
    }

    private IEnumerable<object> ProcessHexamaze(KMBombModule module)
    {
        var comp = GetComponent(module, "HexamazeModule");
        var fldPawnColor = GetIntField(comp, "_pawnColor");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        if (comp == null | fldPawnColor == null || fldSolved == null)
            yield break;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Hexamaze);
        var pawnColor = fldPawnColor.Get();
        if (pawnColor < 0 || pawnColor >= 6)
        {
            Debug.LogFormat("<Souvenir #{1}> Abandoning Hexamaze because pawnColor has an unexpected value. {0}.", pawnColor, _moduleId);
            yield break;
        }

        addQuestion(module, Question.HexamazePawnColor, correctAnswers: new[] { new[] { "Red", "Yellow", "Green", "Cyan", "Blue", "Pink" }[pawnColor] });
    }

    private IEnumerable<object> ProcessHexOS(KMBombModule module)
    {
        var comp = GetComponent(module, "HexOS");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldDecipher = GetArrayField<char>(comp, "decipher");
        var fldSum = GetField<string>(comp, "sum");
        var fldScreen = GetField<string>(comp, "screen");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_HexOS);

        var decipher = fldDecipher.Get();
        if (decipher.Length != 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning hexOS because ‘decipher’ has unexpected length (expected 2): {1}", _moduleId, decipher.Length);
            yield break;
        }

        char[] validLetters = { ' ', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
        for (byte i = 0; i < decipher.Length; i++)
            if (!validLetters.Contains(decipher[i]))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning hexOS because ‘decipher[{1}]’ has unexpected character (expected ' '/A-Z): {2}", _moduleId, i, decipher[i]);
                yield break;
            }

        var screen = fldScreen.Get();

        if (screen.Length != 30)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning hexOS because ‘screen’ has unexpected length (expected 30): {1}", _moduleId, screen.Length);
            yield break;
        }

        for (byte i = 0; i < screen.Length; i++)
            if (!char.IsDigit(screen[i]))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning hexOS because ‘screen[{1}]’ has unexpected value (expected 0-9): {2}", _moduleId, i, screen[i]);
                yield break;
            }

        var sum = fldSum.Get();

        if (sum.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning hexOS because ‘sum’ has unexpected length (expected 4): {1}", _moduleId, sum.Length);
            yield break;
        }

        for (byte i = 0; i < sum.Length; i++)
            if (sum[i] != '0' && sum[i] != '1' && sum[i] != '2')
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning hexOS because ‘sum[{1}]’ has unexpected value (expected 0-2): {2}", _moduleId, i, sum[i]);
                yield break;
            }

        byte offset = (byte) Rnd.Range(0, 10);
        addQuestions(module,
            makeQuestion(Question.HexOSCipher, _HexOS, correctAnswers: new[] { decipher[0].ToString() + decipher[1].ToString(), decipher[1].ToString() + decipher[0].ToString() }, preferredWrongAnswers: Enumerable.Range(0, 50).Select(_ => validLetters[Rnd.Range(0, validLetters.Length)].ToString() + validLetters[Rnd.Range(0, validLetters.Length)].ToString()).Distinct().Take(6).ToArray()),
            makeQuestion(Question.HexOSScreen, _HexOS, new[] { ordinal(offset) }, correctAnswers: new[] { screen[offset * 3].ToString() + screen[(offset * 3) + 1].ToString() + screen[(offset * 3) + 2].ToString() }),
            makeQuestion(Question.HexOSSum, _HexOS, correctAnswers: new[] { sum }));
    }

    private IEnumerable<object> ProcessHiddenColors(KMBombModule module)
    {
        var comp = GetComponent(module, "HiddenColorsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldColor = GetIntField(comp, "LEDColor");
        var fldLed = GetField<Renderer>(comp, "LED", isPublic: true);
        var fldColors = GetArrayField<Material>(comp, "buttonColors", isPublic: true);

        yield return null;

        var ledcolor = fldColor.Get();
        var colors = fldColors.Get();
        var led = fldLed.Get();
        var ledcolors = new[] { "Red", "Blue", "Green", "Yellow", "Orange", "Purple", "Magenta", "White" };
        if (ledcolor < 0 || ledcolor >= 8)
            Debug.LogFormat("<Souvenir #{0}> Abandoning Hidden Colors because ‘LEDColor’ has an unexpected value: {1} (expected 0–7).", _moduleId, ledcolor);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HiddenColors);

        if (led != null && colors != null && colors.Length == 9)
            led.material = colors[8];
        addQuestion(module, Question.HiddenColorsLED, null, new[] { ledcolors[ledcolor] });
    }

    private IEnumerable<object> ProcessHillCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "HillCycleScript", "Hill Cycle", Question.HillCycleWord, _HillCycle);
    }

    private IEnumerable<object> ProcessHogwarts(KMBombModule module)
    {
        var comp = GetComponent(module, "HogwartsModule");
        var fldModuleNames = GetField<IDictionary>(comp, "_moduleNames");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Hogwarts);

        var dic = fldModuleNames.Get();
        if (dic == null || dic.Count == 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Hogwarts because _moduleNames is {1}.", _moduleId, dic == null ? "null" : "empty");
            yield break;
        }

        // TODO: Rock-Paper-Scissors-Lizard-Spock needs to be broken up in the question because hyphens don't break.
        addQuestions(module,
            dic.Keys.Cast<object>().Where(house => dic[house] != null).SelectMany(house => Ut.NewArray(
                makeQuestion(Question.HogwartsHouse, _Hogwarts,
                    formatArgs: new[] { dic[house].ToString() == "Rock-Paper-Scissors-L.-Sp." ? "Rock-Paper- Scissors-L.-Sp." : dic[house].ToString() },
                    correctAnswers: new[] { house.ToString() }),
                makeQuestion(Question.HogwartsModule, _Hogwarts,
                    formatArgs: new[] { house.ToString() },
                    correctAnswers: new[] { dic[house].ToString() },
                    preferredWrongAnswers: Bomb.GetSolvableModuleNames().ToArray()))));
    }

    private IEnumerable<object> ProcessHorribleMemory(KMBombModule module)
    {
        var comp = GetComponent(module, "cruelMemoryScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldPos = GetField<List<int>>(comp, "correctStagePositions", isPublic: true);
        var fldLbl = GetField<List<int>>(comp, "correctStageLabels", isPublic: true);
        var fldColors = GetField<List<string>>(comp, "correctStageColours", isPublic: true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HorribleMemory);

        List<int> pos = fldPos.Get();
        List<int> lbl = fldLbl.Get();
        List<string> colors = fldColors.Get();

        if (pos == null || lbl == null || colors == null)
            yield break;

        if (pos.Count != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Horrible Memory because 'correctStagePositions' has {1} elements instead of 5.", _moduleId, pos.Count);
            yield break;
        }
        if (lbl.Count != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Horrible Memory because 'correctStageLabels' has {1} elements instead of 5.", _moduleId, lbl.Count);
            yield break;
        }
        if (colors.Count != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Horrible Memory because 'correctStageColours' has {1} elements instead of 5.", _moduleId, colors.Count);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "first" }, new[] { pos[0].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "second" }, new[] { pos[1].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "third" }, new[] { pos[2].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "fourth" }, new[] { pos[3].ToString() }),
            makeQuestion(Question.HorribleMemoryPositions, _HorribleMemory, new[] { "fifth" }, new[] { pos[4].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "first" }, new[] { lbl[0].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "second" }, new[] { lbl[1].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "third" }, new[] { lbl[2].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "fourth" }, new[] { lbl[3].ToString() }),
            makeQuestion(Question.HorribleMemoryLabels, _HorribleMemory, new[] { "fifth" }, new[] { lbl[4].ToString() }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "first" }, new[] { colors[0] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "second" }, new[] { colors[1] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "third" }, new[] { colors[2] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "fourth" }, new[] { colors[3] }),
            makeQuestion(Question.HorribleMemoryColors, _HorribleMemory, new[] { "fifth" }, new[] { colors[4] }));
    }

    private IEnumerable<object> ProcessHumanResources(KMBombModule module)
    {
        var comp = GetComponent(module, "HumanResourcesModule");
        var fldPeople = comp == null ? null : GetStaticField<Array>(comp.GetType(), "_people");
        var people = fldPeople == null ? null : fldPeople.Get();
        var fldNames = GetArrayField<int>(comp, "_availableNames");
        var fldDescs = GetArrayField<int>(comp, "_availableDescs");
        var fldToHire = GetIntField(comp, "_personToHire");
        var fldToFire = GetIntField(comp, "_personToFire");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        if (people.Length != 16)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Human Resources because _people array has unexpected length ({1} instead of 16).", _moduleId, people.Length);
            yield break;
        }
        var person = people.GetValue(0);
        var fldName = GetField<string>(person, "Name", isPublic: true);
        var fldDesc = GetField<string>(person, "Descriptor", isPublic: true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_HumanResources);

        var names = fldNames.Get();
        var descs = fldDescs.Get();
        var toHire = fldToHire.Get();
        var toFire = fldToFire.Get();
        if (names == null || names.Length != 10 || descs == null || descs.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Human Resources because unexpected length: (names={1} (should be 10), descs={2} (should be 5)).", _moduleId,
                names == null ? "null" : names.Length.ToString(), descs == null ? "null" : descs.Length.ToString());
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.HumanResourcesEmployees, _HumanResources, new[] { "an employee that was not fired" }, names.Take(5).Where(ix => ix != toFire).Select(ix => fldName.GetFrom(people.GetValue(ix))).ToArray()),
            makeQuestion(Question.HumanResourcesEmployees, _HumanResources, new[] { "an applicant that was not hired" }, names.Skip(5).Where(ix => ix != toHire).Select(ix => fldName.GetFrom(people.GetValue(ix))).ToArray()),
            makeQuestion(Question.HumanResourcesDescriptors, _HumanResources, new[] { "red" }, descs.Take(3).Select(ix => fldDesc.GetFrom(people.GetValue(ix))).ToArray()),
            makeQuestion(Question.HumanResourcesDescriptors, _HumanResources, new[] { "green" }, descs.Skip(3).Select(ix => fldDesc.GetFrom(people.GetValue(ix))).ToArray()));
    }

    private IEnumerable<object> ProcessHunting(KMBombModule module)
    {
        var comp = GetComponent(module, "hunting");
        var fldStage = GetIntField(comp, "stage");
        var fldReverseClues = GetField<bool>(comp, "reverseClues");
        var fldAcceptingInput = GetField<bool>(comp, "acceptingInput");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var hasRowFirst = new bool[4];
        while (fldStage.Get() < 5)
        {
            hasRowFirst[fldStage.Get() - 1] = fldReverseClues.Get();
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_Hunting);
        var qs = new List<QandA>();
        foreach (var row in new[] { false, true })
            foreach (var first in new[] { false, true })
                qs.Add(makeQuestion(Question.HuntingColumnsRows, _Hunting,
                    formatArgs: new[] { row ? "row" : "column", first ? "first" : "second" },
                    correctAnswers: new[] { _attributes[Question.HuntingColumnsRows].AllAnswers[(hasRowFirst[0] ^ row ^ first ? 1 : 0) | (hasRowFirst[1] ^ row ^ first ? 2 : 0) | (hasRowFirst[2] ^ row ^ first ? 4 : 0)] }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessHypercube(KMBombModule module)
    {
        var comp = GetComponent(module, "TheHypercubeModule");
        var fldSequence = GetArrayField<int>(comp, "_rotations");
        var fldRotations = GetStaticField<string[]>(comp.GetType(), "_rotationNames");

        int[] sequence = fldSequence.Get();
        string[] rotations = fldRotations.Get();

        if (sequence == null || rotations == null)
            yield break;
        if (sequence.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Hypercube because '_rotations' had length {1} instead of 5.", _moduleId, sequence.Length);
            yield break;
        }
        for (int i = 0; i < sequence.Length; i++)
        {
            if (sequence[i] < 0 || sequence[i] >= rotations.Length)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning The Hypercube because the '_rotations[{1}]' pointed to illegal rotation: {2}.", _moduleId, i, sequence[i]);
                yield break;
            }
        }

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Hypercube);
        addQuestions(module,
            makeQuestion(Question.HypercubeRotations, _Hypercube, new[] { "first" }, new[] { rotations[sequence[0]] }),
            makeQuestion(Question.HypercubeRotations, _Hypercube, new[] { "second" }, new[] { rotations[sequence[1]] }),
            makeQuestion(Question.HypercubeRotations, _Hypercube, new[] { "third" }, new[] { rotations[sequence[2]] }),
            makeQuestion(Question.HypercubeRotations, _Hypercube, new[] { "fourth" }, new[] { rotations[sequence[3]] }),
            makeQuestion(Question.HypercubeRotations, _Hypercube, new[] { "fifth" }, new[] { rotations[sequence[4]] }));
    }

    private IEnumerable<object> ProcessIceCream(KMBombModule module)
    {
        var comp = GetComponent(module, "IceCreamModule");
        var fldCurrentStage = GetIntField(comp, "CurrentStage");
        var fldCustomers = GetArrayField<int>(comp, "CustomerNamesSolution");
        var fldSolution = GetArrayField<int>(comp, "Solution");
        var fldFlavourOptions = GetArrayField<int[]>(comp, "FlavorOptions");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var flavourNames = GetAnswers(Question.IceCreamFlavour);
        var customerNames = GetAnswers(Question.IceCreamCustomer);

        var flavours = new int[3][];
        var solution = new int[3];
        var customers = new int[3];

        for (var i = 0; i < 3; i++)
        {
            while (fldCurrentStage.Get() == i)
                yield return new WaitForSeconds(.1f);

            var options = fldFlavourOptions.Get();
            var sol = fldSolution.Get();
            var cus = fldCustomers.Get();

            if (options == null || sol == null || cus == null || options.Length != 3 || fldCurrentStage.Get() < i ||
                options.Any(x => x == null || x.Length != 5 || x.Any(y => y < 0 || y >= flavourNames.Length)) ||
                sol.Any(x => x < 0 || x >= flavourNames.Length) || cus.Any(x => x < 0 || x >= customerNames.Length))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Ice Cream because of unexpected values.", _moduleId);
                yield break;
            }
            flavours[i] = options[i].ToArray();
            solution[i] = flavours[i][sol[i]];
            customers[i] = cus[i];
        }
        var qs = new List<QandA>();
        _modulesSolved.IncSafe(_IceCream);

        for (var i = 0; i < 3; i++)
        {
            qs.Add(makeQuestion(Question.IceCreamFlavour, _IceCream, new[] { "was on offer, but not sold,", ordinal(i + 1) }, flavours[i].Where(ix => ix != solution[i]).Select(ix => flavourNames[ix]).ToArray()));
            qs.Add(makeQuestion(Question.IceCreamFlavour, _IceCream, new[] { "was not on offer", ordinal(i + 1) }, flavourNames.Where((f, ix) => !flavours[i].Contains(ix)).ToArray()));
            if (i != 2)
                qs.Add(makeQuestion(Question.IceCreamCustomer, _IceCream, new[] { ordinal(i + 1) }, new[] { customerNames[customers[i]] }, preferredWrongAnswers: customers.Select(ix => customerNames[ix]).ToArray()));
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessIdentityParade(KMBombModule module)
    {
        var comp = GetComponent(module, "identityParadeScript");
        var fldHairEntries = GetField<List<string>>(comp, "hairEntries");
        var fldBuildEntries = GetField<List<string>>(comp, "buildEntries");
        var fldAttireEntries = GetField<List<string>>(comp, "attireEntries");
        var fldButtonsToOverride = new[] { "hairLeft", "hairRight", "buildLeft", "buildRight", "attireLeft", "attireRight", "suspectLeft", "suspectRight", "convictBut" }.Select(fldName => GetField<KMSelectable>(comp, fldName, isPublic: true)).ToArray();
        var fldTextMeshes = new[] { "hairText", "buildText", "attireText", "suspectText" }.Select(fldName => GetField<TextMesh>(comp, fldName, isPublic: true)).ToArray();

        yield return null;

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_IdentityParade);

        var buttonsToOverride = fldButtonsToOverride.Select(f => f.Get()).ToArray();
        if (buttonsToOverride.Any(b => b == null))
            yield break;

        foreach (var btn in buttonsToOverride)
        {
            btn.OnInteract = delegate
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
                btn.AddInteractionPunch(0.5f);
                return false;
            };
        }

        var textMeshes = fldTextMeshes.Select(f => f.Get()).ToArray();
        if (textMeshes.Any(b => b == null))
            yield break;
        textMeshes[0].text = "Identity";
        textMeshes[1].text = "Parade";
        textMeshes[2].text = "has been";
        textMeshes[3].text = "solved";

        var hairs = fldHairEntries.Get();
        var builds = fldBuildEntries.Get();
        var attires = fldAttireEntries.Get();

        if (hairs == null || builds == null || attires == null)
            yield break;
        if (hairs.Count != 3 || builds.Count != 3 || attires.Count != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Identity Parade because ‘hairEntries’, ‘buildEntries’ and/or ‘attireEntries’ has unexpected length: {1}/{2}/{3} (expected 3).", _moduleId, hairs.Count, builds.Count, attires.Count);
            yield break;
        }

        var validHairs = new[] { "Black", "Blonde", "Brown", "Grey", "Red", "White" };
        var validBuilds = new[] { "Fat", "Hunched", "Muscular", "Short", "Slim", "Tall" };
        var validAttires = new[] { "Blazer", "Hoodie", "Jumper", "Suit", "T-shirt", "Tank top" };

        addQuestions(module,
            makeQuestion(Question.IdentityParadeHairColors, _IdentityParade, formatArgs: new[] { "was" }, correctAnswers: hairs.ToArray()),
            makeQuestion(Question.IdentityParadeHairColors, _IdentityParade, formatArgs: new[] { "was not" }, correctAnswers: validHairs.Except(hairs).ToArray()),
            makeQuestion(Question.IdentityParadeBuilds, _IdentityParade, formatArgs: new[] { "was" }, correctAnswers: builds.ToArray()),
            makeQuestion(Question.IdentityParadeBuilds, _IdentityParade, formatArgs: new[] { "was not" }, correctAnswers: validBuilds.Except(builds).ToArray()),
            makeQuestion(Question.IdentityParadeAttires, _IdentityParade, formatArgs: new[] { "was" }, correctAnswers: attires.ToArray()),
            makeQuestion(Question.IdentityParadeAttires, _IdentityParade, formatArgs: new[] { "was not" }, correctAnswers: validAttires.Except(attires).ToArray()));
    }

    private IEnumerable<object> ProcessInstructions(KMBombModule module)
    {
        var comp = GetComponent(module, "instructionsScript");
        var fldSolved = GetField<bool>(comp, "_solved");
        var fldScreens = GetField<int[,]>(comp, "screens");
        var fldEdgeworkScreens = GetStaticField<bool[]>(comp.GetType(), "edgeworkScreens");
        var fldEdgeworkPossibilities = GetArrayField<string>(comp, "edgeworkPossibilities");
        var fldButtonPossibilities = GetArrayField<string>(comp, "buttonPossibilities");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Instructions);

        var screens = fldScreens.Get();
        if (screens == null)
            yield break;
        if (screens.GetLength(0) != 5 || screens.GetLength(1) != 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Instructions because ‘screens’ has unexpected lengths: {1}/{2} (expected 5/2).", _moduleId, screens.GetLength(0), screens.GetLength(1));
            yield break;
        }

        var edgeworkScreens = fldEdgeworkScreens.Get();
        if (edgeworkScreens == null)
            yield break;
        if (edgeworkScreens.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Instructions because ‘edgeworkScreens’ has unexpected lengths: {1} (expected 5).", _moduleId, edgeworkScreens.Length);
            yield break;
        }

        var edgeworkPossibilities = fldEdgeworkPossibilities.Get();
        if (edgeworkPossibilities == null)
            yield break;
        var buttonPossibilities = fldButtonPossibilities.Get();
        if (buttonPossibilities == null)
            yield break;

        edgeworkPossibilities = edgeworkPossibilities.Select(ep => ep.ToLowerInvariant().Replace('\n', ' ')).ToArray();
        buttonPossibilities = buttonPossibilities.Select(bp => (bp.Length > 1 ? bp.ToLowerInvariant() : bp).Replace('\n', ' ')).ToArray();

        var qs = new List<QandA>();
        for (var btnIx = 0; btnIx < 5; btnIx++)
        {
            var ix = screens[btnIx, 0];
            var arr = edgeworkScreens[btnIx] ? edgeworkPossibilities : buttonPossibilities;
            if (ix < 0 || ix >= arr.Length)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Instructions because ‘screens[{1}, 0]’ has value {2} but {3} has length {4}.", _moduleId,
                    btnIx, ix, edgeworkScreens[btnIx] ? "edgeworkPossibilities" : "buttonPossibilities", arr.Length);
                yield break;
            }
            qs.Add(makeQuestion(edgeworkScreens[btnIx] ? Question.InstructionsPhrasesEdgework : Question.InstructionsPhrasesButtons, _Instructions,
                formatArgs: new[] { ordinal(btnIx + 1) }, correctAnswers: new[] { arr[ix] }, preferredWrongAnswers: arr));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessiPhone(KMBombModule module)
    {
        var comp = GetComponent(module, "iPhoneScript");
        var fldSolved = GetField<string>(comp, "solved");
        var fldDigits = GetField<List<string>>(comp, "pinDigits", isPublic: true);

        var digits = fldDigits.Get();

        if (digits == null)
            yield break;
        if (digits.Count != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning iPhone because ‘pinDigits’ has unexpected length {1} (expected 4).", _moduleId, digits.Count);
            yield break;
        }

        while (fldSolved.Get() != "solved")
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_iPhone);
        addQuestions(module,
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "first" }, new[] { digits[0] }, new[] { digits[1], digits[2], digits[3] }),
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "second" }, new[] { digits[1] }, new[] { digits[0], digits[2], digits[3] }),
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "third" }, new[] { digits[2] }, new[] { digits[1], digits[0], digits[3] }),
            makeQuestion(Question.iPhoneDigits, _iPhone, new[] { "fourth" }, new[] { digits[3] }, new[] { digits[1], digits[2], digits[0] }));
    }

    private IEnumerable<object> ProcessJewelVault(KMBombModule module)
    {
        var comp = GetComponent(module, "jewelWheelsScript");
        var fldWheels = GetArrayField<KMSelectable>(comp, "wheels", isPublic: true);
        var fldAssignedWheels = GetField<List<KMSelectable>>(comp, "assignedWheels");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        var wheels = fldWheels.Get();
        var assignedWheels = fldAssignedWheels.Get();

        if (wheels == null || assignedWheels == null)
            yield break;

        if (wheels.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Jewel Vault because ‘wheels’ has unexpected length {1} (expected 4).", _moduleId, wheels.Count());
            yield break;
        }
        if (assignedWheels.Count != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Jewel Vault because ‘assignedWheels’ has unexpected length {1} (expected 4).", _moduleId, assignedWheels.Count());
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_JewelVault);
        addQuestions(module, assignedWheels.Select((aw, ix) => makeQuestion(Question.JewelVaultWheels, _JewelVault, new[] { "ABCD".Substring(ix, 1) }, new[] { (Array.IndexOf(wheels, aw) + 1).ToString() })));
    }

    private IEnumerable<object> ProcessJumbleCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "JumbleCycleScript", "Jumble Cycle", Question.JumbleCycleWord, _JumbleCycle);
    }

    private IEnumerable<object> ProcessKudosudoku(KMBombModule module)
    {
        var comp = GetComponent(module, "KudosudokuModule");
        var fldShown = GetArrayField<bool>(comp, "_shown");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        var shown = fldShown.Get();
        if (shown == null || shown.Length != 16)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Kudosudoku because “_shown” {1} (expected length 16).", _moduleId, shown == null ? "is null" : "has length " + shown.Length);
            yield break;
        }
        // Take a copy of the array
        shown = shown.ToArray();

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Kudosudoku);

        addQuestions(module,
            makeQuestion(Question.KudosudokuPrefilled, _Kudosudoku, new[] { "pre-filled" },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: Enumerable.Range(0, 16).Where(ix => shown[ix]).Select(coord => Tiles4x4Sprites.First(k => k.name == (char) ('A' + (coord % 4)) + (coord / 4 + 1).ToString())).ToArray()),
            makeQuestion(Question.KudosudokuPrefilled, _Kudosudoku, new[] { "not pre-filled" },
                preferredWrongAnswers: Tiles4x4Sprites,
                correctAnswers: Enumerable.Range(0, 16).Where(ix => !shown[ix]).Select(coord => Tiles4x4Sprites.First(k => k.name == (char) ('A' + (coord % 4)) + (coord / 4 + 1).ToString())).ToArray()));
    }

    private IEnumerable<object> ProcessLasers(KMBombModule module)
    {
        var comp = GetComponent(module, "LasersModule");
        var fldLaserOrder = GetField<List<int>>(comp, "_laserOrder");
        var fldHatchesPressed = GetField<List<int>>(comp, "_hatchesAlreadyPressed");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Lasers);

        var laserOrder = fldLaserOrder.Get();
        var hatchesPressed = fldHatchesPressed.Get();

        if (laserOrder == null || hatchesPressed == null)
            yield break;
        if (laserOrder.Count != 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Lasers because ‘_laserOrder’ has unexpected length {1} (expected 9).", _moduleId, laserOrder.Count);
            yield break;
        }
        if (hatchesPressed.Count != 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Lasers because ‘_hatchesAlreadyPressed’ has unexpected length {1} (expected 7).", _moduleId, hatchesPressed.Count);
            yield break;
        }

        var hatchNames = new[] { "top-left", "top-middle", "top-right", "middle-left", "center", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };
        addQuestions(module, hatchesPressed.Select((hatch, ix) => makeQuestion(Question.LasersHatches, _Lasers, new[] { hatchNames[hatch] }, new[] { laserOrder[hatch].ToString() }, hatchesPressed.Select(number => laserOrder[number].ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessLEDEncryption(KMBombModule module)
    {
        var comp = GetComponent(module, "LEDEncryption");
        var fldButtons = GetArrayField<KMSelectable>(comp, "buttons", true);
        var fldMultipliers = GetArrayField<int>(comp, "layerMultipliers");
        var fldStage = GetIntField(comp, "layer");

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var buttons = fldButtons.Get();
        var multipliers = fldMultipliers.Get();
        if (buttons == null || multipliers == null)
            yield break;

        if (buttons.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LED Encryption because there is an unexpected number of buttons: {1} (expected 4).", _moduleId, buttons.Length);
            yield break;
        }

        if (buttons.Any(x => x == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LED Encryption because at least one of the buttons is null.", _moduleId);
            yield break;
        }

        var buttonLabels = buttons.Select(btn => btn.GetComponentInChildren<TextMesh>()).ToArray();
        if (buttonLabels.Any(x => x == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LED Encryption because at least one of the buttons’ TextMesh is null.", _moduleId);
            yield break;
        }

        if (multipliers.Length < 2 || multipliers.Length > 5 || multipliers.Any(multipler => multipler < 2 || multipler > 7))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LED Encryption because layerMultipliers has unexepected length {1} / Values [{2}] (Expected length 2-5, Expected values 2-7)", _moduleId, multipliers.Length, multipliers.Select(x => x.ToString()).JoinString(", "));
            yield break;
        }

        var numStages = multipliers.Length;
        var pressedLetters = new string[numStages];
        var wrongLetters = new HashSet<string>();

        while (fldStage.Get() < numStages)
        {
            foreach (var lbl in buttonLabels)
                wrongLetters.Add(lbl.text);

            // LED Encryption re-hooks the buttons at every press, so we have to re-hook it at each stage as well
            for (int i = 0; i < 4; i++)
                LEDEncryptionReassignButton(buttons[i], buttonLabels[i], fldStage, pressedLetters);

            var stage = fldStage.Get();
            while (fldStage.Get() == stage)
                yield return new WaitForSeconds(0.1f);
        }

        _modulesSolved.IncSafe(_LEDEncryption);
        addQuestions(module, Enumerable.Range(0, pressedLetters.Length - 1)
            .Where(i => pressedLetters[i] != null)
            .Select(stage => makeQuestion(Question.LEDEncryptionPressedLetters, _LEDEncryption, new[] { ordinal(stage + 1) }, new[] { pressedLetters[stage] }, wrongLetters.ToArray())));
    }

    private static void LEDEncryptionReassignButton(KMSelectable btn, TextMesh lbl, FieldInfo<int> fldStage, string[] pressedLetters)
    {
        var prev = btn.OnInteract;
        var stage = fldStage.Get();
        btn.OnInteract = delegate
        {
            var label = lbl.text;
            var ret = prev();
            if (fldStage.Get() > stage)
                pressedLetters[stage] = label;
            return ret;
        };
    }

    private IEnumerable<object> ProcessLEDMath(KMBombModule module)
    {
        var comp = GetComponent(module, "LEDMathScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldLedA = GetIntField(comp, "ledAIndex");
        var fldLedB = GetIntField(comp, "ledBIndex");
        var fldLedOp = GetIntField(comp, "ledOpIndex");

        yield return null;

        var ledA = fldLedA.Get();
        var ledB = fldLedB.Get();
        var ledOp = fldLedOp.Get();
        if (ledA < 0 || ledA > 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LED Math because ledAIndex has an unexpected value: {1} (expected 0-4).", _moduleId, ledA);
            yield break;
        }

        if (ledB < 0 || ledB > 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LED Math because ledBIndex has an unexpected value: {1} (expected 0-4).", _moduleId, ledB);
            yield break;
        }

        if (ledOp < 0 || ledOp > 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LED Math because ledOpIndex has an unexpected value: {1} (expected 0-4).", _moduleId, ledOp);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_LEDMath);

        var ledColors = new[] { "Red", "Blue", "Green", "Yellow" };

        addQuestions(module,
            makeQuestion(Question.LEDMathLights, _LEDMath, new[] { "LED A" }, new[] { ledColors[ledA] }),
            makeQuestion(Question.LEDMathLights, _LEDMath, new[] { "LED B" }, new[] { ledColors[ledB] }),
            makeQuestion(Question.LEDMathLights, _LEDMath, new[] { "the operator LED" }, new[] { ledColors[ledOp] }));
    }

    private IEnumerable<object> ProcessLEGOs(KMBombModule module)
    {
        var comp = GetComponent(module, "LEGOModule");
        var fldSolutionStruct = GetField<object>(comp, "SolutionStructure");
        var fldLeftButton = GetField<KMSelectable>(comp, "LeftButton", isPublic: true);
        var fldRightButton = GetField<KMSelectable>(comp, "RightButton", isPublic: true);
        var fldSubmission = GetArrayField<int>(comp, "Submission");
        var mthUpdate = GetMethod(comp, "UpdateDisplays", numParameters: 0);

        var solutionStruct = fldSolutionStruct.Get();
        if (solutionStruct == null)
            yield break;

        var fldPieces = GetField<IList>(solutionStruct, "Pieces", isPublic: true);
        if (fldPieces == null)
            yield break;

        var pieces = fldPieces.Get();
        if (pieces == null)
            yield break;
        if (pieces.Count != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning LEGOs because ‘SolutionStructure.Pieces’ has unexpected length {1} (expected 6).", _moduleId, pieces.Count);
            yield break;
        }

        // Hook into the module’s OnPass handler
        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        yield return new WaitUntil(() => isSolved);

        // Block the left/right buttons so the player can’t see the instruction pages anymore
        var leftButton = fldLeftButton.Get();
        var rightButton = fldRightButton.Get();

        if (leftButton == null || rightButton == null)
            yield break;

        leftButton.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, module.transform);
            leftButton.AddInteractionPunch(0.5f);
            return false;
        };
        rightButton.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, module.transform);
            rightButton.AddInteractionPunch(0.5f);
            return false;
        };

        // Erase the solution so the player can’t see brick sizes on it either
        var submission = fldSubmission.Get();
        if (submission == null)
            yield break;
        for (int i = 0; i < submission.Length; i++)
            submission[i] = 0;
        mthUpdate.Invoke();

        // Obtain the brick sizes and colors
        var fldBrickColors = GetIntField(pieces[0], "BrickColor", isPublic: true);
        var fldBrickDimensions = GetArrayField<int>(pieces[0], "Dimensions", isPublic: true);
        if (fldBrickColors == null || fldBrickDimensions == null)
            yield break;

        var brickColors = Enumerable.Range(0, 6).Select(i => fldBrickColors.GetFrom(pieces[i])).ToList();
        var brickDimensions = Enumerable.Range(0, 6).Select(i => fldBrickDimensions.GetFrom(pieces[i])).ToList();
        if (brickDimensions.Any(d => d == null))
            yield break;

        _modulesSolved.IncSafe(_LEGOs);
        var colorNames = new[] { "red", "green", "blue", "cyan", "magenta", "yellow" };
        addQuestions(module, Enumerable.Range(0, 6).Select(i => makeQuestion(Question.LEGOsPieceDimensions, _LEGOs, new[] { colorNames[brickColors[i]] }, new[] { brickDimensions[i][0] + "×" + brickDimensions[i][1] })));
    }

    private IEnumerable<object> ProcessListening(KMBombModule module)
    {
        var comp = GetComponent(module, "Listening");
        var fldIsActivated = GetField<bool>(comp, "isActivated");
        var fldCodeInput = GetArrayField<char>(comp, "codeInput");
        var fldCodeInputPosition = GetIntField(comp, "codeInputPosition");
        var fldSound = GetField<object>(comp, "sound");
        var fldDollarButton = GetField<KMSelectable>(comp, "DollarButton", isPublic: true);
        var fldPoundButton = GetField<KMSelectable>(comp, "PoundButton", isPublic: true);
        var fldStarButton = GetField<KMSelectable>(comp, "StarButton", isPublic: true);
        var fldAmpersandButton = GetField<KMSelectable>(comp, "AmpersandButton", isPublic: true);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var attr = _attributes.Get(Question.Listening);
        if (attr == null)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Listening because SouvenirQuestionAttribute for Question.Listening is null.", _moduleId);
            yield break;
        }

        var sound = fldSound.Get();
        var buttons = new[] { fldDollarButton.Get(), fldPoundButton.Get(), fldStarButton.Get(), fldAmpersandButton.Get() };
        if (sound == null || buttons.Contains(null))
            yield break;

        var prevInteracts = buttons.Select(btn => btn.OnInteract).ToArray();
        var nullIndex = Array.IndexOf(prevInteracts, null);
        if (nullIndex != -1)
        {
            Debug.LogFormat("<Souvenir #{1}> Abandoning Listening because buttons[{0}].OnInteract is null.", nullIndex, _moduleId);
            yield break;
        }

        var fldSoundCode = GetField<string>(sound, "code", isPublic: true);
        if (fldSoundCode == null)
            yield break;
        var correctCode = fldSoundCode.Get();
        if (correctCode == null)
            yield break;

        var code = "";
        var solved = false;
        for (int i = 0; i < 4; i++)
        {
            // Workaround bug in Mono 2.0 C# compiler
            new Action<int>(j =>
            {
                buttons[i].OnInteract = delegate
                {
                    var ret = prevInteracts[j]();
                    code += "$#*&"[j];
                    if (code.Length == 5)
                    {
                        if (code == correctCode)
                        {
                            solved = true;
                            // Sneaky: make it so that the player can no longer play the sound
                            fldIsActivated.Set(false);
                        }
                        code = "";
                    }
                    return ret;
                };
            })(i);
        }

        while (!solved)
            yield return new WaitForSeconds(.1f);

        for (int i = 0; i < 4; i++)
            buttons[i].OnInteract = prevInteracts[i];

        _modulesSolved.IncSafe(_Listening);
        addQuestion(module, Question.Listening, correctAnswers: new[] { correctCode });
    }

    private IEnumerable<object> ProcessLogicGates(KMBombModule module)
    {
        var comp = GetComponent(module, "LogicGates");
        var fldGates = GetField<IList>(comp, "_gates");
        var fldInputs = GetField<List<int>>(comp, "_inputs");
        var fldCurrentInputIndex = GetIntField(comp, "_currentInputIndex");
        var fldButtonNext = GetField<KMSelectable>(comp, "ButtonNext", isPublic: true);
        var fldButtonPrevious = GetField<KMSelectable>(comp, "ButtonPrevious", isPublic: true);

        var inputs = fldInputs.Get();
        var gates = fldGates.Get();
        var btnNext = fldButtonNext.Get();
        var btnPrevious = fldButtonPrevious.Get();
        if (inputs == null || inputs.Count == 0 || gates == null || gates.Count == 0 || btnNext == null || btnPrevious == null)
            yield break;

        var fldGateType = GetField<object>(gates[0], "GateType", isPublic: true);
        var tmpGateType = fldGateType == null ? null : fldGateType.Get();
        var fldGateTypeName = tmpGateType == null ? null : GetField<string>(tmpGateType, "Name", isPublic: true);
        if (fldGateType == null || tmpGateType == null || fldGateTypeName == null)
            yield break;

        var gateTypeNames = gates.Cast<object>().Select(obj => fldGateTypeName.GetFrom(fldGateType.GetFrom(obj)).ToString()).ToArray();
        string duplicate = null;
        bool isDuplicateInvalid = false;
        for (int i = 0; i < gateTypeNames.Length; i++)
            for (int j = i + 1; j < gateTypeNames.Length; j++)
                if (gateTypeNames[i] == gateTypeNames[j])
                {
                    if (duplicate != null)
                        isDuplicateInvalid = true;
                    else
                        duplicate = gateTypeNames[i];
                }

        yield return null;

        // Unfortunately Logic Gates has no “isSolved” field, so we need to hook into the button
        var solved = false;
        module.OnPass += delegate { solved = true; return true; };

        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_LogicGates);

        btnNext.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, module.transform);
            btnNext.AddInteractionPunch(0.2f);
            return false;
        };
        btnPrevious.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, module.transform);
            btnNext.AddInteractionPunch(0.2f);
            return false;
        };

        var qs = new List<QandA>();
        for (int i = 0; i < gateTypeNames.Length; i++)
            qs.Add(makeQuestion(Question.LogicGatesGates, _LogicGates, new[] { "gate " + (char) ('A' + i) }, new[] { gateTypeNames[i] }));
        if (!isDuplicateInvalid)
            qs.Add(makeQuestion(Question.LogicGatesGates, _LogicGates, new[] { "the duplicated gate" }, new[] { duplicate }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessLondonUnderground(KMBombModule module)
    {
        var comp = GetComponent(module, "londonUndergroundScript");
        var fldStage = GetIntField(comp, "levelsPassed");
        var fldDepartureStation = GetField<string>(comp, "departureStation");
        var fldDestinationStation = GetField<string>(comp, "destinationStation");
        var fldDepartureOptions = GetArrayField<string>(comp, "departureOptions");
        var fldDestinationOptions = GetArrayField<string>(comp, "destinationOptions");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        yield return null;

        var departures = new List<string>();
        var destinations = new List<string>();
        var extraOptions = new HashSet<string>();
        var lastStage = -1;
        while (!fldSolved.Get())
        {
            var stage = fldStage.Get();
            if (stage != lastStage)
            {
                if (stage == 0)
                {
                    // The player got a strike and the module reset
                    departures.Clear();
                    destinations.Clear();
                    extraOptions.Clear();
                }
                departures.Add(fldDepartureStation.Get());
                destinations.Add(fldDestinationStation.Get());

                foreach (var option in fldDepartureOptions.Get())
                    extraOptions.Add(option);
                foreach (var option in fldDestinationOptions.Get())
                    extraOptions.Add(option);
                lastStage = stage;
            }
            yield return null;
        }
        _modulesSolved.IncSafe(_LondonUnderground);
        var primary = departures.Union(destinations).ToArray();
        if (primary.Length < 4)
            primary = primary.Union(extraOptions).ToArray();

        addQuestions(module,
            departures.Select((dep, ix) => makeQuestion(Question.LondonUndergroundStations, _LondonUnderground, new[] { ordinal(ix + 1), "depart from" }, new[] { dep }, primary)).Concat(
            destinations.Select((dest, ix) => makeQuestion(Question.LondonUndergroundStations, _LondonUnderground, new[] { ordinal(ix + 1), "arrive to" }, new[] { dest }, primary))));
    }

    private IEnumerable<object> ProcessMafia(KMBombModule module)
    {
        var comp = GetComponent(module, "MafiaModule");
        var fldSuspects = GetField<Array>(comp, "_suspects");
        var fldGodfather = GetField<object>(comp, "_godfather");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        var godfather = fldGodfather.Get();
        var suspects = fldSuspects.Get();

        if (godfather == null || suspects == null || suspects.Length != 8)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mafia because ‘{1}’ is null or unexpected length ({2}).", _moduleId, godfather == null ? "godfather" : "suspects", suspects == null ? "null" : suspects.Length.ToString());
            yield break;
        }

        _modulesSolved.IncSafe(_Mafia);
        addQuestion(module, Question.MafiaPlayers, correctAnswers: suspects.Cast<object>().Select(obj => obj.ToString()).Except(new[] { godfather.ToString() }).ToArray());
    }

    private IEnumerable<object> ProcessMahjong(KMBombModule module)
    {
        var comp = GetComponent(module, "MahjongModule");
        var fldTaken = GetArrayField<bool>(comp, "_taken");
        var fldCountingRow = GetArrayField<int>(comp, "_countingRow");
        var fldMatchRow1 = GetArrayField<int>(comp, "_matchRow1");
        var fldMatchRow2 = GetArrayField<int>(comp, "_matchRow2");
        var fldCountingTile = GetField<MeshRenderer>(comp, "CountingTile", true);
        var fldParticleEffect = GetField<ParticleSystem>(comp, "Smoke1", true);
        var fldAudio = GetField<KMAudio>(comp, "Audio", true);
        var fldTileSelectables = GetArrayField<KMSelectable>(comp, "Tiles", true);

        yield return null;

        // Capture the player’s matching pairs until the module is solved
        var taken = fldTaken.Get();
        if (taken == null)
            yield break;

        var currentTaken = taken.ToArray();
        var matchedTiles = new List<int>();

        while (true)
        {
            yield return null;
            if (!currentTaken.SequenceEqual(taken))
            {
                matchedTiles.AddRange(Enumerable.Range(0, taken.Length).Where(ix => currentTaken[ix] != taken[ix]));
                if (taken.All(x => x))
                    break;
                currentTaken = taken.ToArray();
            }
        }
        _modulesSolved.IncSafe(_Mahjong);

        // Remove the counting tile, complete with smoke animation
        var countingTile = fldCountingTile.Get();
        var smoke = fldParticleEffect.Get();
        var countingRow = fldCountingRow.Get();
        if (countingTile == null || smoke == null || fldAudio.Get() == null || countingRow == null)
            yield break;

        if (countingTile.gameObject.activeSelf)     // Do it only if another Souvenir module on the same bomb hasn’t already done it
        {
            fldAudio.Get().PlaySoundAtTransform("Elimination", countingTile.transform);
            smoke.transform.localPosition = countingTile.transform.localPosition;
            smoke.Play();
            countingTile.gameObject.SetActive(false);
        }

        // Stuff for the “counting tile” question (bottom-left of the module)
        var countingTileName = countingTile.material.mainTexture.name.Replace(" normal", "");
        var countingTileSprite = MahjongSprites.FirstOrDefault(x => x.name == countingTileName);
        if (countingTileSprite == null)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mahjong because the sprite for the counting tile ({1}) doesn’t exist.", _moduleId, countingTileName);
            yield break;
        }

        // Stuff for the “matching tiles” question
        var matchRow1 = fldMatchRow1.Get();
        var matchRow2 = fldMatchRow2.Get();
        var tileSelectables = fldTileSelectables.Get();
        if (matchRow1 == null || matchRow2 == null || tileSelectables == null)
            yield break;

        var tileSprites = matchRow1.Concat(matchRow2).Select(ix => MahjongSprites[ix]).ToArray();
        var matchedTileSpriteNames = matchedTiles.Select(ix => tileSelectables[ix].GetComponent<MeshRenderer>().material.mainTexture.name.Replace(" normal", "").Replace(" highlighted", "")).ToArray();
        var matchedTileSprites = matchedTileSpriteNames.Select(name => tileSprites.FirstOrDefault(spr => spr.name == name)).ToArray();

        var invalidIx = matchedTileSprites.IndexOf(spr => spr == null);
        if (invalidIx != -1)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mahjong because the sprite for one of the matched tiles ({1}) doesn’t exist. matchedTileSpriteNames=[{2}], matchedTileSprites=[{3}], countingRow=[{4}], matchRow1=[{5}], matchRow2=[{6}], tileSprites=[{7}]",
                _moduleId, matchedTileSpriteNames[invalidIx], matchedTileSpriteNames.JoinString(", "), matchedTileSprites.Select(spr => spr == null ? "<null>" : spr.name).JoinString(", "),
                countingRow.JoinString(", "), matchRow1.JoinString(", "), matchRow2.JoinString(", "), tileSprites.Select(spr => spr.name).JoinString(", "));
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.MahjongCountingTile, _Mahjong, correctAnswers: new[] { MahjongSprites.First(x => x.name == countingTileName) }, preferredWrongAnswers: countingRow.Select(ix => MahjongSprites[ix]).ToArray()),
            makeQuestion(Question.MahjongMatches, _Mahjong, new[] { "first" }, correctAnswers: matchedTileSprites.Take(2).ToArray(), preferredWrongAnswers: tileSprites),
            makeQuestion(Question.MahjongMatches, _Mahjong, new[] { "second" }, correctAnswers: matchedTileSprites.Skip(2).Take(2).ToArray(), preferredWrongAnswers: tileSprites));
    }

    private IEnumerable<object> ProcessMaritimeFlags(KMBombModule module)
    {
        var comp = GetComponent(module, "MaritimeFlagsModule");
        var fldBearing = GetIntField(comp, "_bearingOnModule");
        var fldCallsign = GetField<object>(comp, "_callsign");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MaritimeFlags);

        var bearing = fldBearing.Get();
        var callsignObj = fldCallsign.Get();

        if (callsignObj == null || bearing < 0 || bearing >= 360)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Maritime Flags because callsign is null ({1}) or bearing is out of range ({2}).", _moduleId, callsignObj == null, bearing);
            yield break;
        }

        var fldCallsignName = GetField<string>(callsignObj, "Name", isPublic: true);
        if (fldCallsignName == null)
            yield break;
        var callsign = fldCallsignName.Get();
        if (callsign == null || callsign.Length != 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Maritime Flags because callsign is null or length not 7 (it’s {1}).", _moduleId, callsign == null ? "null" : callsign.Length.ToString());
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.MaritimeFlagsBearing, _MaritimeFlags, correctAnswers: new[] { bearing.ToString() }),
            makeQuestion(Question.MaritimeFlagsCallsign, _MaritimeFlags, correctAnswers: new[] { callsign.ToLowerInvariant() }));
    }

    private IEnumerable<object> ProcessMaze(KMBombModule module)
    {
        var component = GetComponent(module, "InvisibleWallsComponent");
        var fldSolved = GetField<bool>(component, "IsSolved", true);
        var propCurrentCell = GetProperty<object>(component, "CurrentCell", true);
        if (component == null || fldSolved == null || propCurrentCell == null)
            yield break;

        var currentCell = propCurrentCell.Get();  // Need to get the current cell at the start.
        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Maze);

        var coordinateChoice = Rnd.Range(0, 2);
        var fldCoordinate = GetIntField(currentCell, coordinateChoice == 0 ? "X" : "Y", true);
        if (fldCoordinate == null) yield break;

        addQuestion(module, Question.MazeStartingPosition, formatArguments: coordinateChoice == 0 ? new[] { "column", "left" } : new[] { "row", "top" },
            correctAnswers: new[] { (fldCoordinate.Get() + 1).ToString() });
    }

    private IEnumerable<object> ProcessMaze3(KMBombModule module)
    {
        var comp = GetComponent(module, "maze3Script");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldNode = GetIntField(comp, "node");

        var node = fldNode.Get();
        var colors = new[] { "Red", "Blue", "Yellow", "Green", "Magenta", "Orange" };

        if (node < 0 || node > 53)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Maze³ because 'node' has illegal value: {1}.", _moduleId, node);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Maze3);
        addQuestion(module, Question.Maze3StartingFace, correctAnswers: new[] { colors[node / 9] });
    }

    private IEnumerable<object> ProcessMazematics(KMBombModule module)
    {
        var comp = GetComponent(module, "Mazematics");
        var fldStartVal = GetIntField(comp, "startValue");
        var fldGoalVal = GetIntField(comp, "goalValue");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Mazematics);

        var startVal = fldStartVal.Get().ToString();
        var goalVal = fldGoalVal.Get().ToString();

        string[] possibleStartVals = Enumerable.Range(17, 33).Select(x => x.ToString()).ToArray();
        string[] possibleGoalVals = Enumerable.Range(0, 50).Select(x => x.ToString()).ToArray();

        if (!possibleStartVals.Contains(startVal) || !possibleGoalVals.Contains(goalVal))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mazemativs because either 'startValue' or 'goalValue' has illegal value (startVal = {1}; goalVal = {2}).", _moduleId, startVal, goalVal);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.MazematicsValue, _Mazematics, new[] { "initial" }, new[] { startVal }, possibleStartVals),
            makeQuestion(Question.MazematicsValue, _Mazematics, new[] { "goal" }, new[] { goalVal }, possibleGoalVals));
    }

    private IEnumerable<object> ProcessMazeScrambler(KMBombModule module)
    {
        var comp = GetComponent(module, "MazeScrambler");
        var fldSolved = GetField<bool>(comp, "SOLVED");
        var fldInd1X = GetIntField(comp, "IDX1");
        var fldInd1Y = GetIntField(comp, "IDY1");
        var fldInd2X = GetIntField(comp, "IDX2");
        var fldInd2Y = GetIntField(comp, "IDY2");
        var fldStartX = GetIntField(comp, "StartX");
        var fldStartY = GetIntField(comp, "StartY");
        var fldGoalX = GetIntField(comp, "GoalX");
        var fldGoalY = GetIntField(comp, "GoalY");

        const int x = 0;
        const int y = 1;

        var ind1 = new[] { fldInd1X.Get(), fldInd1Y.Get() };
        var ind2 = new[] { fldInd2X.Get(), fldInd2Y.Get() };
        var start = new[] { fldStartX.Get(), fldStartY.Get() };
        var goal = new[] { fldGoalX.Get(), fldGoalY.Get() };

        if (ind1[x] < 0 || ind1[x] > 2 || ind1[y] < 0 || ind1[y] > 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Maze Scrambler because Indicator 1 has unnexpected coordinates (expected 0 to 2): [{1}, {2}].", _moduleId, ind1[x], ind1[y]);
            yield break;
        }
        if (ind2[x] < 0 || ind2[x] > 2 || ind2[y] < 0 || ind2[y] > 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Maze Scrambler because Indicator 2 has unnexpected coordinates (expected 0 to 2): [{1}, {2}].", _moduleId, ind2[x], ind2[y]);
            yield break;
        }
        if (start[x] < 0 || start[x] > 2 || start[y] < 0 || start[y] > 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Maze Scrambler because Start has unnexpected coordinates (expected 0 to 2): [{1}, {2}].", _moduleId, start[x], start[y]);
            yield break;
        }
        if (goal[x] < 0 || goal[x] > 2 || goal[y] < 0 || goal[y] > 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Maze Scrambler because Goal has unnexpected coordinates (expected 0 to 2): [{1}, {2}].", _moduleId, goal[x], goal[y]);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        var positionNames = new[] { "top-left", "top-middle", "top-right", "middle-left", "center", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };

        _modulesSolved.IncSafe(_MazeScrambler);
        addQuestions(module,
            makeQuestion(Question.MazeScramblerStart, _MazeScrambler, correctAnswers: new[] { positionNames[start[y] * 3 + start[x]] }, preferredWrongAnswers: new[] { positionNames[goal[y] * 3 + goal[x]] }),
            makeQuestion(Question.MazeScramblerGoal, _MazeScrambler, correctAnswers: new[] { positionNames[goal[y] * 3 + goal[x]] }, preferredWrongAnswers: new[] { positionNames[start[y] * 3 + start[x]] }),
            makeQuestion(Question.MazeScramblerIndicators, _MazeScrambler, correctAnswers: new[] { positionNames[ind1[y] * 3 + ind1[x]], positionNames[ind2[y] * 3 + ind2[x]] }, preferredWrongAnswers: positionNames));
    }

    private IEnumerable<object> ProcessMegaMan2(KMBombModule module)
    {
        var comp = GetComponent(module, "Megaman2");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldRobotMasters = GetArrayField<string>(comp, "robotMasters");
        var fldSelectedWeapon = GetIntField(comp, "selectedWeapon");
        var fldSelectedMaster = GetIntField(comp, "selectedMaster");

        yield return null;

        var robotMasters = fldRobotMasters.Get();
        var selectedMaster = fldSelectedMaster.Get();
        var selectedWeapon = fldSelectedWeapon.Get();

        if (selectedMaster < 0 || selectedMaster >= robotMasters.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mega Man 2 because ‘selectedMaster’ does not have a valid value (current value is {1}).", _moduleId, selectedMaster);
            yield break;
        }

        if (selectedWeapon < 0 || selectedWeapon >= robotMasters.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mega Man 2 because ‘selectedWeapon’ does not have a valid value (current value is {1}).", _moduleId, selectedWeapon);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MegaMan2);

        addQuestions(module,
            makeQuestion(Question.MegaMan2SelectedMaster, _MegaMan2, correctAnswers: new[] { robotMasters[selectedMaster] }, preferredWrongAnswers: robotMasters),
            makeQuestion(Question.MegaMan2SelectedWeapon, _MegaMan2, correctAnswers: new[] { robotMasters[selectedWeapon] }, preferredWrongAnswers: robotMasters));
    }

    private IEnumerable<object> ProcessMelodySequencer(KMBombModule module)
    {
        var comp = GetComponent(module, "MelodySequencerScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldParts = GetArrayField<int[]>(comp, "parts");    // the 8 parts in their “correct” order
        var fldModuleParts = GetArrayField<int[]>(comp, "moduleParts");    // the parts as assigned to the slots

        var parts = fldParts.Get();
        var moduleParts = fldModuleParts.Get();
        if (parts == null || moduleParts == null)
            yield break;
        if (parts.Length != 8 || moduleParts.Length != 8)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Melody Sequencer because ‘parts’ or ‘moduleParts’ has unexpected lengths {1} and {2} (expected 8).", _moduleId, parts.Length, moduleParts.Length);
            yield break;
        }
        var partsPerSlot = Enumerable.Range(0, 8).Select(slot => parts.IndexOf(p => ReferenceEquals(p, moduleParts[slot]))).ToArray();
        Debug.LogFormat("<Souvenir #{0}> Melody Sequencer: parts are: [{1}].", _moduleId, partsPerSlot.JoinString(", "));

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MelodySequencer);

        var qs = new List<QandA>();
        var givenSlots = Enumerable.Range(0, partsPerSlot.Length).Where(slot => partsPerSlot[slot] != -1).Select(slot => (slot + 1).ToString()).ToArray();
        var givenParts = partsPerSlot.Where(part => part != -1).Select(part => (part + 1).ToString()).ToArray();
        for (int i = 0; i < partsPerSlot.Length; i++)
        {
            if (partsPerSlot[i] != -1)
            {
                qs.Add(makeQuestion(Question.MelodySequencerParts, _MelodySequencer, new[] { (partsPerSlot[i] + 1).ToString() }, new[] { (i + 1).ToString() }, preferredWrongAnswers: givenSlots));
                qs.Add(makeQuestion(Question.MelodySequencerSlots, _MelodySequencer, new[] { (i + 1).ToString() }, new[] { (partsPerSlot[i] + 1).ToString() }, preferredWrongAnswers: givenParts));
            }
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessMemorableButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "MemorableButtons");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldCombinedCode = GetField<string>(comp, "combinedCode", isPublic: true);
        var fldButtonLabels = GetArrayField<TextMesh>(comp, "buttonLabels", isPublic: true);

        var buttonLabels = fldButtonLabels.Get();
        if (buttonLabels == null)
            yield break;
        if (buttonLabels.Length == 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Memorable Buttons because ‘buttonLabels’ has unexpected length 0.", _moduleId);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MemorableButtons);

        var combinedCode = fldCombinedCode.Get();
        if (combinedCode == null)
            yield break;
        if (combinedCode.Length < 10 || combinedCode.Length > 15)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Memorable Buttons because ‘combinedCode’ has unexpected length {1} (expected 10–15).", _moduleId, combinedCode.Length);
            yield break;
        }

        addQuestions(module, combinedCode.Select((ch, ix) => makeQuestion(Question.MemorableButtonsSymbols, _MemorableButtons, buttonLabels[0].font, buttonLabels[0].GetComponent<MeshRenderer>().sharedMaterial.mainTexture, new[] { ordinal(ix + 1) }, correctAnswers: new[] { ch.ToString() })));
    }

    private IEnumerable<object> ProcessMemory(KMBombModule module)
    {
        var component = GetComponent(module, "MemoryComponent");
        var fldSolved = GetField<bool>(component, "IsSolved", true);
        var propDisplaySequence = GetProperty<string>(component, "DisplaySequence", true);
        var fldButtonIndicesPressed = GetField<List<int>>(component, "buttonIndicesPressed", false);
        var fldButtonLabelsPressed = GetField<List<string>>(component, "buttonLabelsPressed", false);
        if (fldSolved == null || propDisplaySequence == null || fldButtonIndicesPressed == null || fldButtonLabelsPressed == null)
            yield break;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Memory);

        var displaySequence = propDisplaySequence.Get();
        var indices = fldButtonIndicesPressed.Get();
        var labels = fldButtonLabelsPressed.Get();

        var stage = Rnd.Range(0, 4);
        addQuestions(module, new[] {
            makeQuestion(Question.MemoryDisplay, "Memory", new[] { (stage + 1).ToString() }, new[] { displaySequence[stage].ToString() }),
            makeQuestion(Question.MemoryPosition, "Memory", new[] { (stage + 1).ToString() }, new[] { MemorySprites[indices[stage]] }, MemorySprites),
            makeQuestion(Question.MemoryLabel, "Memory", new[] { (stage + 1).ToString() }, new[] { labels[stage][labels[stage].Length - 1].ToString() })
        });
    }

    private IEnumerable<object> ProcessMicrocontroller(KMBombModule module)
    {
        var comp = GetComponent(module, "Micro");
        var fldSolved = GetIntField(comp, "solved");
        var fldLedsOrder = GetField<List<int>>(comp, "LEDorder");
        var fldPositionTranslate = GetArrayField<int>(comp, "positionTranslate");

        while (fldSolved.Get() == 0)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Microcontroller);

        var ledsOrder = fldLedsOrder.Get();
        if (ledsOrder == null || (ledsOrder.Count != 6 && ledsOrder.Count != 8 && ledsOrder.Count != 10))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Microcontroller because ‘LEDorder’ is null or unexpected length (expected 6, 8 or 10): {1}.", _moduleId, ledsOrder == null ? "<null>" : ledsOrder.Count.ToString());
            yield break;
        }
        var positionTranslate = fldPositionTranslate.Get();
        if (positionTranslate == null || positionTranslate.Length != ledsOrder.Count)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Microcontroller because ‘positionTranslate’ is null or unexpected length (expected {1}): {2}.", _moduleId, ledsOrder.Count, positionTranslate == null ? "<null>" : positionTranslate.Length.ToString());
            yield break;
        }

        addQuestions(module, ledsOrder.Select((led, ix) => makeQuestion(Question.MicrocontrollerPinOrder, _Microcontroller,
            formatArgs: new[] { ordinal(ix + 1) },
            correctAnswers: new[] { (positionTranslate[led] + 1).ToString() },
            preferredWrongAnswers: Enumerable.Range(1, ledsOrder.Count).Select(i => i.ToString()).ToArray())));
    }

    private IEnumerable<object> ProcessMinesweeper(KMBombModule module)
    {
        var comp = GetComponent(module, "MinesweeperModule");
        var fldGrid = GetField<object>(comp, "Game");
        var fldStartingCell = GetField<object>(comp, "StartingCell");

        // Wait for activation as the above fields aren’t fully initialized until then
        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var propSolved = GetProperty<bool>(fldGrid.Get(), "Solved", isPublic: true);
        var fldColor = GetField<string>(fldStartingCell.Get(), "Color", isPublic: true);

        if (propSolved == null || fldColor == null)
            yield break;

        var color = fldColor.Get();

        while (!propSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_Minesweeper);
        addQuestion(module, Question.MinesweeperStartingColor, correctAnswers: new[] { color });
    }

    private IEnumerable<object> ProcessModernCipher(KMBombModule module)
    {
        var comp = GetComponent(module, "modernCipher");
        var fldWords = GetField<Dictionary<string, string>>(comp, "chosenWords");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        var dictionary = fldWords.Get();
        if (dictionary == null)
            yield break;

        string stage1word, stage2word;
        if (!dictionary.TryGetValue("Stage1", out stage1word) || !dictionary.TryGetValue("Stage2", out stage2word) || stage1word == null || stage2word == null)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Modern Cipher because there is no word for {1}.", _moduleId, stage1word == null ? "stage 1" : "stage 2");
            yield break;
        }

        Debug.LogFormat("<Souvenir #{0}> Modern Cipher words: {1} {2}.", _moduleId, stage1word, stage2word);

        stage1word = stage1word.Substring(0, 1).ToUpperInvariant() + stage1word.Substring(1).ToLowerInvariant();
        stage2word = stage2word.Substring(0, 1).ToUpperInvariant() + stage2word.Substring(1).ToLowerInvariant();

        _modulesSolved.IncSafe(_ModernCipher);
        addQuestions(module,
            makeQuestion(Question.ModernCipherWord, _ModernCipher, new[] { "first" }, new[] { stage1word }, new[] { stage2word }),
            makeQuestion(Question.ModernCipherWord, _ModernCipher, new[] { "second" }, new[] { stage2word }, new[] { stage1word }));
    }

    private IEnumerable<object> ProcessModuleMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "ModuleMazeModule");
        var fldSprites = GetArrayField<Sprite>(comp, "souvenirSprites", true);
        var fldStart = GetField<string>(comp, "souvenirStart", true);

        while (fldSprites.Get().Count() < 6)
            yield return new WaitForSeconds(.1f);

        var sprites = fldSprites.Get();
        var start = fldStart.Get();

        _modulesSolved.IncSafe(_ModuleMaze);

        addQuestions(module,
            makeQuestion(Question.ModuleMazeStartingIcon, _ModuleMaze,
                correctAnswers: new[] { sprites.FirstOrDefault(spr => spr.name == start) }, preferredWrongAnswers: sprites));
    }

    private IEnumerable<object> ProcessMonsplodeFight(KMBombModule module)
    {
        var comp = GetComponent(module, "MonsplodeFightModule");
        var fldCreatureData = GetField<object>(comp, "CD", isPublic: true);
        var fldMovesData = GetField<object>(comp, "MD", isPublic: true);
        var fldCreatureID = GetIntField(comp, "crID");
        var fldMoveIDs = GetArrayField<int>(comp, "moveIDs");
        var fldRevive = GetField<bool>(comp, "revive");
        var fldButtons = GetArrayField<KMSelectable>(comp, "buttons", isPublic: true);
        var fldCorrectCount = GetIntField(comp, "correctCount");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var creatureData = fldCreatureData.Get();
        var movesData = fldMovesData.Get();
        var buttons = fldButtons.Get();
        if (creatureData == null || movesData == null || buttons == null)
            yield break;
        var buttonNullIndex = Array.IndexOf(buttons, null);
        if (buttons.Length != 4 || buttonNullIndex != -1)
        {
            Debug.LogFormat("<Souvenir #{2}> Abandoning Monsplode, Fight! because unexpected buttons array length ({0}, expected 4) or one of them is null ({1}, expected -1).", buttons.Length, buttonNullIndex, _moduleId);
            yield break;
        }

        var fldCreatureNames = GetArrayField<string>(creatureData, "names", isPublic: true);
        var fldMoveNames = GetArrayField<string>(movesData, "names", isPublic: true);
        if (fldCreatureNames == null || fldMoveNames == null)
            yield break;

        var creatureNames = fldCreatureNames.Get();
        var moveNames = fldMoveNames.Get();
        if (creatureNames == null || moveNames == null)
            yield break;

        string displayedCreature = null;
        string[] displayedMoves = null;
        var finished = false;

        var origInteracts = buttons.Select(btn => btn.OnInteract).ToArray();
        for (int i = 0; i < buttons.Length; i++)
        {
            // Need an extra scope to work around bug in Mono 2.0 C# compiler
            new Action<int>(j =>
            {
                buttons[j].OnInteract = delegate
                {
                    // Before processing the button push, get the creature and moves
                    string curCreatureName = null;
                    string[] curMoveNames = null;

                    var creatureID = fldCreatureID.Get();
                    if (creatureID < 0 || creatureID >= creatureNames.Length || string.IsNullOrEmpty(creatureNames[creatureID]))
                        Debug.LogFormat("<Souvenir #{2}> Monsplode, Fight!: Unexpected creature ID: {0}; creature names are: [{1}]", creatureID, creatureNames.Select(cn => cn == null ? "null" : '"' + cn + '"').JoinString(", "), _moduleId);
                    else
                    {
                        var moveIDs = fldMoveIDs.Get();
                        if (moveIDs == null || moveIDs.Length != 4 || moveIDs.Any(mid => mid >= moveNames.Length || string.IsNullOrEmpty(moveNames[mid])))
                            Debug.LogFormat("<Souvenir #{2}> Monsplode, Fight!: Unexpected move IDs: {0}; moves names are: [{1}]",
                                moveIDs == null ? null : "[" + moveIDs.JoinString(", ") + "]",
                                moveNames.Select(mn => mn == null ? "null" : '"' + mn + '"').JoinString(", "),
                                _moduleId);
                        else
                        {
                            curCreatureName = creatureNames[creatureID];
                            curMoveNames = moveIDs.Select(mid => moveNames[mid].Replace("\r", "").Replace("\n", " ")).ToArray();
                        }
                    }

                    var ret = origInteracts[j]();

                    if (curCreatureName == null || curMoveNames == null)
                    {
                        Debug.LogFormat("<Souvenir #{0}> Monsplode, Fight!: Abandoning due to error above.", _moduleId);
                        // Set these to null to signal that something went wrong and we need to abort
                        displayedCreature = null;
                        displayedMoves = null;
                        finished = true;
                    }
                    else
                    {
                        // If ��revive’ is ‘false’, there is not going to be another stage.
                        if (!fldRevive.Get())
                            finished = true;

                        if (curCreatureName != null && curMoveNames != null)
                        {
                            displayedCreature = curCreatureName;
                            displayedMoves = curMoveNames;
                        }
                    }
                    return ret;
                };
            })(i);
        }

        while (!finished)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MonsplodeFight);

        for (int i = 0; i < buttons.Length; i++)
            buttons[i].OnInteract = origInteracts[i];

        if (displayedCreature == null || displayedMoves == null)
            yield break;

        addQuestions(module,
            makeQuestion(Question.MonsplodeFightCreature, _MonsplodeFight, correctAnswers: new[] { displayedCreature }),
            makeQuestion(Question.MonsplodeFightMove, _MonsplodeFight, new[] { "was" }, displayedMoves),
            makeQuestion(Question.MonsplodeFightMove, _MonsplodeFight, new[] { "was not" }, _attributes.Get(Question.MonsplodeFightMove).AllAnswers.Except(displayedMoves).ToArray()));
    }

    private IEnumerable<object> ProcessMonsplodeTradingCards(KMBombModule module)
    {
        var comp = GetComponent(module, "MonsplodeCardModule");
        var fldStage = GetIntField(comp, "correctOffer", isPublic: true);
        var fldStageCount = GetIntField(comp, "offerCount", isPublic: true);
        var fldDeck = GetField<Array>(comp, "deck", isPublic: true);
        var fldOffer = GetField<object>(comp, "offer", isPublic: true);
        var fldData = GetField<object>(comp, "CD", isPublic: true);

        yield return null;

        var stageCount = fldStageCount.Get();
        if (stageCount != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Monsplode Trading Cards because ‘offerCount’ has unexpected value {1} instead of 3.", _moduleId, stageCount);
            yield break;
        }
        var data = fldData.Get();
        if (data == null)
            yield break;
        var fldNames = GetArrayField<string>(data, "names", isPublic: true);
        if (fldNames == null)
            yield break;
        var monsplodeNames = fldNames.Get();
        if (monsplodeNames == null)
            yield break;

        while (fldStage.Get() < stageCount)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MonsplodeTradingCards);

        if (fldStage.Get() != stageCount)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Monsplode Trading Cards because ‘correctOffer’ has unexpected value {1} instead of {2}.", _moduleId, fldStage.Get(), stageCount);
            yield break;
        }

        var deckRaw = fldDeck.Get();
        var offer = fldOffer.Get();
        if (deckRaw == null || offer == null)
            yield break;
        var deck = deckRaw.Cast<object>().ToArray();
        if (deck.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Monsplode Trading Cards because ‘deck’ has unexpected length {1} instead of 3.", _moduleId, deck.Length);
            yield break;
        }

        var fldMonsplode = GetIntField(offer, "monsplode", isPublic: true);
        var fldRarity = GetIntField(offer, "rarity", isPublic: true);
        var fldPrintDigit = GetIntField(offer, "printDigit", isPublic: true);
        var fldPrintChar = GetField<char>(offer, "printChar", isPublic: true);
        if (fldMonsplode == null || fldRarity == null || fldPrintDigit == null || fldPrintChar == null)
            yield break;

        var monsplodeIds = new[] { fldMonsplode.Get() }.Concat(deck.Select(card => fldMonsplode.GetFrom(card))).ToArray();
        if (monsplodeIds.Any(monsplode => monsplode < 0 || monsplode >= monsplodeNames.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Monsplode Trading Cards because of an unexpected Monsplode ({1}). Names are [{2}].", _moduleId, monsplodeIds.JoinString(", "), monsplodeNames.JoinString(", "));
            yield break;
        }
        var monsplodes = monsplodeIds.Select(mn => monsplodeNames[mn].Replace("\r", "").Replace("\n", " ")).ToArray();
        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "card on offer" }, new[] { monsplodes[0] }, monsplodeNames));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "first card in your hand" }, new[] { monsplodes[1] }, monsplodeNames));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "second card in your hand" }, new[] { monsplodes[2] }, monsplodeNames));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsCards, _MonsplodeTradingCards, new[] { "third card in your hand" }, new[] { monsplodes[3] }, monsplodeNames));

        var rarityNames = new[] { "common", "uncommon", "rare", "ultra rare" };
        var rarityIds = new[] { fldRarity.Get() }.Concat(deck.Select(card => fldRarity.GetFrom(card))).ToArray();
        if (rarityIds.Any(rarity => rarity < 0 || rarity >= rarityNames.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Monsplode Trading Cards because of an unexpected rarity ({1}). Names are [{2}].", _moduleId, rarityIds.JoinString(", "), rarityNames.JoinString(", "));
            yield break;
        }
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "card on offer" }, new[] { rarityNames[rarityIds[0]] }));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "first card in your hand" }, new[] { rarityNames[rarityIds[1]] }));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "second card in your hand" }, new[] { rarityNames[rarityIds[2]] }));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsRarities, _MonsplodeTradingCards, new[] { "third card in your hand" }, new[] { rarityNames[rarityIds[3]] }));

        var printVersions = new[] { fldPrintChar.Get() + "" + fldPrintDigit.Get() }.Concat(deck.Select(card => fldPrintChar.GetFrom(card) + "" + fldPrintDigit.GetFrom(card))).ToArray();
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "card on offer" }, new[] { printVersions[0] }, printVersions));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "first card in your hand" }, new[] { printVersions[1] }, printVersions));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "second card in your hand" }, new[] { printVersions[2] }, printVersions));
        qs.Add(makeQuestion(Question.MonsplodeTradingCardsPrintVersions, _MonsplodeTradingCards, new[] { "third card in your hand" }, new[] { printVersions[3] }, printVersions));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessMoon(KMBombModule module)
    {
        var comp = GetComponent(module, "theMoonScript");
        var fldStage = GetIntField(comp, "stage");
        var fldLightIndex = GetIntField(comp, "lightIndex");

        // The Moon sets ‘stage’ to 9 when the module is solved.
        while (fldStage.Get() != 9)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Moon);

        var lightIndex = fldLightIndex.Get();
        if (lightIndex < 0 || lightIndex >= 8)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Moon because ‘lightIndex’ has unexpected value {1}.", _moduleId, lightIndex);
            yield break;
        }

        var qNames = new[] { "first initially lit", "second initially lit", "third initially lit", "fourth initially lit", "first initially unlit", "second initially unlit", "third initially unlit", "fourth initially unlit" };
        var aNames = new[] { "south", "south-west", "west", "north-west", "north", "north-east", "east", "south-east" };
        addQuestions(module, Enumerable.Range(0, 8).Select(i => makeQuestion(Question.MoonLitUnlit, _Moon, new[] { qNames[i] }, new[] { aNames[(i + lightIndex) % 8] })));
    }

    private IEnumerable<object> ProcessMorseAMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "MorseAMaze");
        var fldSolved = GetField<bool>(comp, "_solved");
        var fldStart = GetField<string>(comp, "_souvenirQuestionStartingLocation");
        var fldEnd = GetField<string>(comp, "_souvenirQuestionEndingLocation");
        var fldWord = GetField<string>(comp, "_souvenirQuestionWordPlaying");
        var fldWords = GetArrayField<string>(comp, "_souvenirQuestionWordList");

        while (!_isActivated)
            yield return new WaitForSeconds(0.1f);

        var start = fldStart.Get();
        var end = fldEnd.Get();
        var word = fldWord.Get();
        var words = fldWords.Get();
        if (start == null || start.Length != 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Morse-A-Maze starting coordinate is null or has unexpected value: {1}", _moduleId, start ?? "<null>");
            yield break;
        }
        if (end == null || end.Length != 2)
        {
            Debug.LogFormat("<Souvenir #{0}> Morse-A-Maze ending coordinate is null or has unexpected value: {1}", _moduleId, end ?? "<null>");
            yield break;
        }
        if (word == null || word.Length < 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Morse-A-Maze morse code word is null or has unexpected value: {1}", _moduleId, word ?? "<null>");
            yield break;
        }
        if (words == null || words.Length != 36)
        {
            Debug.LogFormat("<Souvenir #{0}> Morse-A-Maze word list is null or its length is not 36: {1}", _moduleId, words == null ? "<null>" : words.Length.ToString());
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_MorseAMaze);
        addQuestions(module,
            makeQuestion(Question.MorseAMazeStartingCoordinate, _MorseAMaze, correctAnswers: new[] { start }),
            makeQuestion(Question.MorseAMazeEndingCoordinate, _MorseAMaze, correctAnswers: new[] { end }),
            makeQuestion(Question.MorseAMazeMorseCodeWord, _MorseAMaze, correctAnswers: new[] { word }, preferredWrongAnswers: words));
    }

    private IEnumerable<object> ProcessMorseButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "morseButtonsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldLetters = GetArrayField<int>(comp, "letters");
        var fldColors = GetArrayField<int>(comp, "colors");
        var fldAlphabet = GetField<string>(comp, "alphabet");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        int[] letters = fldLetters.Get();
        int[] colors = fldColors.Get();
        string alphabet = fldAlphabet.Get();
        var colorNames = new[] { "Red", "Blue", "Green", "Yellow", "Orange", "Purple" };

        if (letters == null || colors == null || alphabet == null)
            yield break;
        if (letters.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Morse Buttons because 'letters' has length {1} (expected 6).", _moduleId, letters.Length);
            yield break;
        }
        if (colors.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Morse Buttons because 'colors' has length {1} (expected 6).", _moduleId, colors.Length);
            yield break;
        }
        if (letters.Any(x => x < 0 || x >= alphabet.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Morse Buttons because at least one element of 'letters' has an illegal value.", _moduleId);
            yield break;
        }
        if (colors.Any(x => x < 0 || x >= colorNames.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Morse Buttons because at least one element of 'colors' has an illegal value.", _moduleId);
            yield break;
        }

        _modulesSolved.IncSafe(_MorseButtons);
        addQuestions(module,
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "first" }, new[] { alphabet[letters[0]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "second" }, new[] { alphabet[letters[1]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "third" }, new[] { alphabet[letters[2]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "fourth" }, new[] { alphabet[letters[3]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "fifth" }, new[] { alphabet[letters[4]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "character", "sixth" }, new[] { alphabet[letters[5]].ToString() }, alphabet.Select(x => x.ToString()).ToArray()),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "first" }, new[] { colorNames[colors[0]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "second" }, new[] { colorNames[colors[1]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "third" }, new[] { colorNames[colors[2]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "fourth" }, new[] { colorNames[colors[3]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "fifth" }, new[] { colorNames[colors[4]].ToString() }, colorNames),
            makeQuestion(Question.MorseButtonsButton, _MorseButtons, new[] { "color", "sixth" }, new[] { colorNames[colors[5]].ToString() }, colorNames));
    }

    private IEnumerable<object> ProcessMorsematics(KMBombModule module)
    {
        var comp = GetComponent(module, "AdvancedMorse");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldChars = GetArrayField<string>(comp, "DisplayCharsRaw");

        yield return null;

        var chars = fldChars.Get();
        if (chars == null)
            yield break;
        if (chars.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Morsematics: Unexpected length of DisplayCharsRaw array ({1} instead of 3).", _moduleId, chars.Length);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Morsematics);
        addQuestions(module, Enumerable.Range(0, 3).Select(i => makeQuestion(Question.MorsematicsReceivedLetters, _Morsematics, new[] { ordinal(i + 1) }, new[] { chars[i] }, chars)));
    }

    private IEnumerable<object> ProcessMorseWar(KMBombModule module)
    {
        var comp = GetComponent(module, "MorseWar");
        var fldWordNum = GetIntField(comp, "wordNum");
        var fldLights = GetField<string>(comp, "lights");
        var fldWordTable = comp == null ? null : GetStaticField<string[]>(comp.GetType(), "WordTable");
        var fldRowTable = comp == null ? null : GetStaticField<string[]>(comp.GetType(), "RowTable");
        var fldIsSolved = GetField<bool>(comp, "isSolved");

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MorseWar);

        var wordNum = fldWordNum.Get();
        var lights = fldLights.Get();
        var wordTable = fldWordTable.Get();
        var rowTable = fldRowTable.Get();
        if (lights == null || wordTable == null || rowTable == null)
            yield break;
        if (wordNum < 0 || wordNum >= wordTable.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Morse War because ‘wordNum’ is out of range ({1}; expected 0–{2}).", _moduleId, wordNum, wordTable.Length - 1);
            yield break;
        }
        if (lights.Length != 3 || lights.Any(ch => ch < '1' || ch > '6'))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Morse War because ‘lights’ has unexpected value: “{1}” (expected 3 characters 1–6).", _moduleId, lights);
            yield break;
        }
        if (rowTable.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Morse War because ‘RowTable’ has unexpected length ({1}; expected 6).", _moduleId, rowTable.Length);
            yield break;
        }

        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.MorseWarCode, _MorseWar, correctAnswers: new[] { wordTable[wordNum].ToUpperInvariant() }));
        var rowNames = new[] { "bottom", "middle", "top" };
        for (int i = 0; i < 3; i++)
            qs.Add(makeQuestion(Question.MorseWarLeds, _MorseWar, formatArgs: new[] { rowNames[i] }, correctAnswers: new[] { rowTable[lights[i] - '1'] }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessMouseInTheMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "Maze_3d");
        var fldSphereColors = GetArrayField<int>(comp, "sphereColors");
        var fldTorusColor = GetIntField(comp, "torusColor");
        var fldGoalPosition = GetIntField(comp, "goalPosition");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");

        var sphereColors = fldSphereColors.Get();
        if (sphereColors == null)
            yield break;
        if (sphereColors.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{1}> Mouse in the Maze: sphereColors has unexpected length ({0}; expected 4).", sphereColors.Length, _moduleId);
            yield break;
        }

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var goalPos = fldGoalPosition.Get();
        if (goalPos < 0 || goalPos >= 4)
        {
            Debug.LogFormat("<Souvenir #{1}> Mouse in the Maze: Unexpected goalPos ({0}; expected 0 to 3).", goalPos, _moduleId);
            yield break;
        }

        var torusColor = fldTorusColor.Get();
        var goalColor = sphereColors[goalPos];
        if (torusColor < 0 || torusColor >= 4 || goalColor < 0 || goalColor >= 4)
        {
            Debug.LogFormat("<Souvenir #{2}> Mouse in the Maze: Unexpected color (torus={0}; goal={1}).", torusColor, goalColor, _moduleId);
            yield break;
        }

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_MouseInTheMaze);
        addQuestions(module,
            makeQuestion(Question.MouseInTheMazeSphere, _MouseInTheMaze, correctAnswers: new[] { new[] { "white", "green", "blue", "yellow" }[goalColor] }),
            makeQuestion(Question.MouseInTheMazeTorus, _MouseInTheMaze, correctAnswers: new[] { new[] { "white", "green", "blue", "yellow" }[torusColor] }));
    }

    private IEnumerable<object> ProcessMurder(KMBombModule module)
    {
        var comp = GetComponent(module, "MurderModule");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldSolution = GetArrayField<int>(comp, "solution");
        var fldNames = GetField<string[,]>(comp, "names");
        var fldSkipDisplay = GetField<int[,]>(comp, "skipDisplay");
        var fldSuspects = GetIntField(comp, "suspects");
        var fldWeapons = GetIntField(comp, "weapons");
        var fldBodyFound = GetIntField(comp, "bodyFound");

        yield return null;

        if (fldSuspects.Get() != 4 || fldWeapons.Get() != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Murder: Unexpected number of suspects ({1} instead of 4) or weapons ({2} instead of 4).", _moduleId, fldSuspects.Get(), fldWeapons.Get());
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Murder);

        var solution = fldSolution.Get();
        var skipDisplay = fldSkipDisplay.Get();
        var names = fldNames.Get();
        if (solution == null || skipDisplay == null || names == null)
            yield break;
        if (solution.Length != 3 || skipDisplay.GetLength(0) != 2 || skipDisplay.GetLength(1) != 6 || names.GetLength(0) != 3 || names.GetLength(1) != 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Murder: Unexpected length of solution array ({1} instead of 3) or solution array ({2}/{3} instead of 2/6) or names array ({4}/{5} instead of 3/9).", _moduleId, solution.Length, skipDisplay.GetLength(0), skipDisplay.GetLength(1), names.GetLength(0), names.GetLength(1));
            yield break;
        }

        var actualSuspect = solution[0];
        var actualWeapon = solution[1];
        var actualRoom = solution[2];
        var bodyFound = fldBodyFound.Get();
        if (actualSuspect < 0 || actualSuspect >= 6 || actualWeapon < 0 || actualWeapon >= 6 || actualRoom < 0 || actualRoom >= 9 || bodyFound < 0 || bodyFound >= 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Murder: Unexpected suspect, weapon, room or bodyFound (expected 0–5/0–5/0–8/0–8, got {1}/{2}/{3}/{4}).", _moduleId, actualSuspect, actualWeapon, actualRoom, bodyFound);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.MurderSuspect, _Murder,
                new[] { "a suspect but not the murderer" },
                Enumerable.Range(0, 6).Where(suspectIx => skipDisplay[0, suspectIx] == 0 && suspectIx != actualSuspect).Select(suspectIx => names[0, suspectIx]).ToArray()),
            makeQuestion(Question.MurderSuspect, _Murder,
                new[] { "not a suspect" },
                Enumerable.Range(0, 6).Where(suspectIx => skipDisplay[0, suspectIx] == 1).Select(suspectIx => names[0, suspectIx]).ToArray()),

            makeQuestion(Question.MurderWeapon, _Murder,
                new[] { "a potential weapon but not the murder weapon" },
                Enumerable.Range(0, 6).Where(weaponIx => skipDisplay[1, weaponIx] == 0 && weaponIx != actualWeapon).Select(weaponIx => names[1, weaponIx]).ToArray()),
            makeQuestion(Question.MurderWeapon, _Murder,
                new[] { "not a potential weapon" },
                Enumerable.Range(0, 6).Where(weaponIx => skipDisplay[1, weaponIx] == 1).Select(weaponIx => names[1, weaponIx]).ToArray()),

            bodyFound == actualRoom ? null : makeQuestion(Question.MurderBodyFound, _Murder, correctAnswers: new[] { names[2, bodyFound] }));
    }

    private IEnumerable<object> ProcessMysticSquare(KMBombModule module)
    {
        var comp = GetComponent(module, "MysticSquareModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldSkull = GetField<Transform>(comp, "Skull", true);
        var fldKnight = GetField<Transform>(comp, "Knight", true);

        var fldIsInDanger = GetField<bool>(comp, "_isInDanger");
        var fldSkullPos = GetIntField(comp, "_skullPos");
        var fldKnightPos = GetIntField(comp, "_knightPos");
        var fldField = GetArrayField<int>(comp, "_field");

        var skull = fldSkull.Get();
        var knight = fldKnight.Get();

        if (skull == null || knight == null)
            yield break;

        while (!skull.gameObject.activeSelf)
            yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_MysticSquare);

        var knightpos = fldKnightPos.Get();
        var skullpos = fldSkullPos.Get();
        var spacepos = Array.IndexOf(fldField.Get(), 0);
        if (knightpos < 0 || knightpos > 8)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mystic Square because knight is in unexpected position {1} (expected 0-8).", _moduleId, knightpos);
            yield break;
        }
        if (skullpos < 0 || skullpos > 8)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mystic Square because skull is in unexpected position {1} (expected 0-8).", _moduleId, skullpos);
            yield break;
        }

        var answers = new[] { "top left", "top middle", "top right", "middle left", "center", "middle right", "bottom left", "bottom middle", "bottom right" };
        var knightQuestion = makeQuestion(Question.MysticSquareKnightSkull, _MysticSquare, new[] { "knight" }, new[] { answers[knightpos] }, answers);
        var skullQuestion = makeQuestion(Question.MysticSquareKnightSkull, _MysticSquare, new[] { "skull" }, new[] { answers[skullpos] }, answers);

        if (_questions.Count == 0 && _coroutinesActive == 1)
        {
            // If this Mystic Square module is the only supported module, add a question immediately so that it will be asked immediately instead of disarming.
            // Always ask about the skull if the knight is in the empty space, and vice versa.
            if (fldIsInDanger.Get())
            {
                if (skullpos == spacepos)
                {
                    Debug.LogFormat("<Souvenir #{0}> No question for Mystic Square because the skull ended in the empty space, the knight was never uncovered and there are no other supported modules.",
                        _moduleId, skullpos);
                    _legitimatelyNoQuestions.Add(module);
                }
                else
                    addQuestions(module, skullQuestion);
            }
            else
            {
                if (skullpos == spacepos)
                    addQuestions(module, knightQuestion);
                else if (knightpos == spacepos)
                    addQuestions(module, skullQuestion);
                else
                    addQuestions(module, knightQuestion, skullQuestion);
            }
        }
        else
        {
            // If other questions will be asked, the skull and knight will be hidden beforehand.
            addQuestions(module, knightQuestion, skullQuestion);
        }

        // If the skull or knight is in the empty space, shrink and then disappear them.
        if (skullpos == spacepos || knightpos == spacepos)
        {
            // Make sure that the last sliding animation finishes
            yield return new WaitForSeconds(0.5f);

            const float duration = 1.5f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                skull.localScale = Vector3.Lerp(new Vector3(0.004f, 0.004f, 0.004f), Vector3.zero, elapsed / duration);
                knight.localScale = Vector3.Lerp(new Vector3(0.004f, 0.004f, 0.004f), Vector3.zero, elapsed / duration);
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        skull.gameObject.SetActive(false);
        knight.gameObject.SetActive(false);
    }

    private IEnumerable<object> ProcessMysteryModule(KMBombModule module)
    {
        var comp = GetComponent(module, "MysteryModuleScript");
        var fldKeyModules = GetField<List<KMBombModule>>(comp, "keyModules");
        var fldMystifiedModule = GetField<KMBombModule>(comp, "mystifiedModule");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldAnimating = GetField<bool>(comp, "animating");

        yield return null;
        while (fldKeyModules.Get(nullAllowed: true) == null)
            yield return null;
        while (fldMystifiedModule.Get(nullAllowed: true) == null)
            yield return null;

        var keyModules = fldKeyModules.Get();
        if (keyModules == null)
            yield break;
        if (keyModules.Count == 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Mystery Module because ‘keyModules’ is empty.", _moduleId);
            yield break;
        }

        var keyModule = keyModules[0];
        var mystifiedModule = fldMystifiedModule.Get();

        // Do not ask questions while Souvenir is hidden by Mystery Module.
        if (mystifiedModule == Module)
            _avoidQuestions++;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_MysteryModule);

        // Do not ask questions during the solve animation, since Mystery Module modifies the scaling of this module.
        while (fldAnimating.Get())
            yield return new WaitForSeconds(.1f);

        addQuestions(module,
            makeQuestion(Question.MysteryModuleFirstKey, _MysteryModule, correctAnswers: new[] { keyModule.ModuleDisplayName }, preferredWrongAnswers: Bomb.GetSolvableModuleNames().ToArray()),
            makeQuestion(Question.MysteryModuleHiddenModule, _MysteryModule, correctAnswers: new[] { mystifiedModule.ModuleDisplayName }, preferredWrongAnswers: Bomb.GetSolvableModuleNames().ToArray()));

        if (mystifiedModule == Module)
            _avoidQuestions--;
    }

    private IEnumerable<object> ProcessNecronomicon(KMBombModule module)
    {
        var comp = GetComponent(module, "necronomiconScript");
        var fldChapters = GetArrayField<int>(comp, "selectedChapters");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Necronomicon);

        int[] chapters = fldChapters.Get();

        if (chapters == null)
            yield break;
        if (chapters.Length != 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Necronomicon because 'selectedChapters' has unexpected length ({1}; expected 7).", _moduleId, chapters.Length);
            yield break;
        }

        string[] chaptersString = chapters.Select(x => x.ToString()).ToArray();

        addQuestions(module,
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "first" }, new[] { chaptersString[0] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "second" }, new[] { chaptersString[1] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "third" }, new[] { chaptersString[2] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "fourth" }, new[] { chaptersString[3] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "fifth" }, new[] { chaptersString[4] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "sixth" }, new[] { chaptersString[5] }, chaptersString),
            makeQuestion(Question.NecronomiconChapters, _Necronomicon, new[] { "seventh" }, new[] { chaptersString[6] }, chaptersString));
    }

    private IEnumerable<object> ProcessNeutralization(KMBombModule module)
    {
        var comp = GetComponent(module, "neutralization");
        var fldAcidType = GetIntField(comp, "acidType");
        var fldAcidVol = GetIntField(comp, "acidVol");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldColorText = GetField<GameObject>(comp, "colorText", isPublic: true);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var acidType = fldAcidType.Get();
        if (acidType < 0 || acidType > 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Neutralization: Unexpected acid type: {1}", _moduleId, acidType);
            yield break;
        }
        var acidVol = fldAcidVol.Get();
        if (acidVol < 5 || acidVol > 20 || acidVol % 5 != 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Neutralization: Unexpected acid volume: {1}", _moduleId, acidVol);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Neutralization);

        var colorText = fldColorText.Get();
        if (colorText != null)
            colorText.SetActive(false);
        addQuestions(module,
            makeQuestion(Question.NeutralizationColor, _Neutralization, correctAnswers: new[] { new[] { "Yellow", "Green", "Red", "Blue" }[acidType] }),
            makeQuestion(Question.NeutralizationVolume, _Neutralization, correctAnswers: new[] { acidVol.ToString() }));
    }

    private IEnumerable<object> ProcessNandMs(KMBombModule module)
    {
        var comp = GetComponent(module, "NandMs");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldIndex = GetIntField(comp, "otherwordindex");
        var fldWords = GetArrayField<string>(comp, "otherWords");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NandMs);

        var index = fldIndex.Get();
        var words = fldWords.Get();

        if (words == null)
            yield break;
        if (index < 0 || index >= words.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning N&Ms because index = {1} (expected 0–{2}).", _moduleId, index, words.Length - 1);
            yield break;
        }

        addQuestion(module, Question.NandMsAnswer, correctAnswers: new[] { words[index] });
    }

    private IEnumerable<object> ProcessNavinums(KMBombModule module)
    {
        var comp = GetComponent(module, "navinumsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldCenterDigit = GetIntField(comp, "center");
        var fldStage = GetIntField(comp, "stage");
        var fldLookUp = GetArrayField<int[]>(comp, "lookUp");
        var fldDirections = GetField<List<int>>(comp, "directions");
        var fldDirectionsSorted = GetField<List<int>>(comp, "directionsSorted");

        yield return null;

        var lookUp = fldLookUp.Get();
        if (lookUp == null)
            yield break;
        if (lookUp.Length != 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Navinums because ‘lookUp’ has unexpected length {1} (expected 9).", _moduleId, lookUp.Length);
            yield break;
        }
        for (var i = 0; i < 9; i++)
            if (lookUp[i].Length != 8)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Navinums because ‘lookUp’ contains an array of unexpected length {1} (expected 8).", _moduleId, lookUp[i].Length);
                yield break;
            }

        var directionsSorted = fldDirectionsSorted.Get();
        if (directionsSorted == null)
            yield break;
        if (directionsSorted.Count != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Navinums because ‘directionsSorted’ has unexpected length {1} (expected 4).", _moduleId, directionsSorted.Count);
            yield break;
        }

        var centerDigit = fldCenterDigit.Get();
        if (centerDigit < 1 || centerDigit > 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Navinums because ‘center’ has unexpected value {1} (expected 1–9).", _moduleId, centerDigit);
            yield break;
        }

        var curStage = -1;
        var answers = new int[8];
        Debug.LogFormat("<Souvenir #{0}> Waiting for next stage", _moduleId);
        while (true)
        {
            yield return null;
            var newStage = fldStage.Get();
            if (newStage != curStage)
            {
                Debug.LogFormat("<Souvenir #{0}> Stage is now {1}", _moduleId, newStage);
                if (newStage == 8)
                    break;
                var newDirections = fldDirections.Get();
                if (newDirections == null)
                    yield break;
                if (newDirections.Count != 4)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Navinums because ‘directions’ has unexpected length {1} (expected 4).", _moduleId, newDirections.Count);
                    yield break;
                }

                var digit = directionsSorted[lookUp[centerDigit - 1][newStage] - 1];
                answers[newStage] = newDirections.IndexOf(digit);
                if (answers[newStage] == -1)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Navinums because ‘directions’ ({1}) did not contain the value from ‘directionsSorted’ ({2}).", _moduleId, newDirections.JoinString(", "), digit);
                    yield break;
                }
                curStage = newStage;
            }
        }
        Debug.LogFormat("<Souvenir #{0}> Stage 8 reached", _moduleId);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Navinums);

        Debug.LogFormat("<Souvenir #{0}> Navinums solved", _moduleId);

        var directionNames = new[] { "up", "left", "right", "down" };

        var qs = new List<QandA>();
        for (var stage = 0; stage < 8; stage++)
            qs.Add(makeQuestion(Question.NavinumsDirectionalButtons, _Navinums, formatArgs: new[] { ordinal(stage + 1) }, correctAnswers: new[] { directionNames[answers[stage]] }));
        qs.Add(makeQuestion(Question.NavinumsMiddleDigit, _Navinums, correctAnswers: new[] { centerDigit.ToString() }));

        Debug.LogFormat("<Souvenir #{0}> Adding questions", _moduleId);
        addQuestions(module, qs);
        Debug.LogFormat("<Souvenir #{0}> Questions generated", _moduleId);
    }

    private IEnumerable<object> ProcessNotButton(KMBombModule module)
    {
        var comp = GetComponent(module, "NotButton");
        var propSolved = GetProperty<bool>(comp, "Solved", true);
        var propLightColour = GetProperty<object>(comp, "LightColour", true);
        var propMashCount = GetIntField(comp, "MashCount", true);
        var lightColor = 0; var mashCount = 0;
        while (!propSolved.Get())
        {
            mashCount = propMashCount.Get();
            lightColor = (int) propLightColour.Get();
            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_NotButton);
        if (lightColor != 0)
        {
            var strings = _attributes[Question.NotButtonLightColor].AllAnswers;
            if (lightColor <= 0 || lightColor > strings.Length)
                Debug.LogFormat("<Souvenir #{0}> Abandoning Not the Button because LightColour is out of range ({1}).", _moduleId, lightColor);
            else
                addQuestion(module, Question.NotButtonLightColor, correctAnswers: new[] { strings[lightColor - 1] });
        }
        else if (mashCount > 1)
        {
            var wrongAnswerStrings = Enumerable.Range(0, 20).Select(_ => Rnd.Range(10, 100)).Where(i => i != mashCount).Distinct().Take(5).Select(i => i.ToString()).ToArray();
            addQuestion(module, Question.NotButtonMashCount, correctAnswers: new[] { mashCount.ToString() }, preferredWrongAnswers: wrongAnswerStrings);
        }
        else
        {
            Debug.LogFormat("<Souvenir #{0}> No question for Not the Button because the button was tapped (or I missed the light color).", _moduleId);
            _legitimatelyNoQuestions.Add(module);
        }
    }

    private IEnumerable<object> ProcessNotKeypad(KMBombModule module)
    {
        var component = GetComponent(module, "NotKeypad");
        var connectorComponent = GetComponent(module, "NotVanillaModulesLib.NotKeypadConnector");
        var propSolved = GetProperty<bool>(component, "Solved", true);
        var fldColours = GetField<Array>(component, "sequenceColours");
        var fldButtons = GetArrayField<int>(component, "sequenceButtons");
        var fldSymbols = GetField<Array>(connectorComponent, "symbols");
        if (propSolved == null || fldColours == null || fldButtons == null || fldSymbols == null)
            yield break;

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotKeypad);

        var colours = fldColours.Get();
        var buttons = fldButtons.Get();
        var symbols = fldSymbols.Get();
        var stage = Rnd.Range(0, colours.Length);

        var questions = new QandA[2];
        var colour = (int) colours.GetValue(stage);
        var strings = _attributes[Question.NotKeypadColor].AllAnswers;
        if (colour <= 0 || colour > strings.Length)
            Debug.LogFormat("<Souvenir #{0}> Abandoning a question for Not Keypad because colour index is out of range ({1}).", _moduleId, colour);
        else
            questions[0] = makeQuestion(Question.NotKeypadColor, _NotKeypad, new[] { ordinal(stage + 1) }, new[] { strings[colour - 1] });

        var symbol = (int) symbols.GetValue(buttons[stage]);
        if (symbol < 0 || symbol > 30)
            Debug.LogFormat("<Souvenir #{0}> Abandoning a question for Not Keypad because symbol index is out of range ({1}).", _moduleId, colour);
        else
            questions[1] = makeQuestion(Question.NotKeypadSymbol, _NotKeypad, new[] { ordinal(stage + 1) }, new[] { KeypadSprites[symbol] },
                symbols.Cast<int>().Select(i => KeypadSprites[i]).ToArray());

        addQuestions(module, questions);
    }

    private IEnumerable<object> ProcessNotMaze(KMBombModule module)
    {
        var component = GetComponent(module, "NotMaze");
        var propSolved = GetProperty<bool>(component, "Solved", true);
        var fldDistance = GetIntField(component, "distance");
        if (propSolved == null || fldDistance == null)
            yield break;

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotMaze);

        addQuestion(module, Question.NotMazeStartingDistance, correctAnswers: new[] { fldDistance.Get().ToString() });
    }

    private IEnumerable<object> ProcessNotMorseCode(KMBombModule module)
    {
        var component = GetComponent(module, "NotMorseCode");
        var propSolved = GetProperty<bool>(component, "Solved", true);
        var fldCorrectChannels = GetArrayField<int>(component, "correctChannels");
        var fldWords = GetArrayField<string>(component, "words");
        var fldColumns = GetStaticField<string[][]>(component.GetType(), "defaultColumns");
        if (propSolved == null || fldCorrectChannels == null || fldWords == null || fldColumns == null)
            yield break;

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotMorseCode);

        var stage = Rnd.Range(0, 5);
        var words = fldWords.Get();
        var channels = fldCorrectChannels.Get();
        var columns = fldColumns.Get();
        var correctAnswers = new[] { words[channels[stage]] };
        // Pick the other four words shown on the module, and four other random words from the table.
        var wrongAnswers = words.Concat(Enumerable.Range(0, 50).Select(_ => columns.PickRandom().PickRandom())).Except(correctAnswers).Distinct().Take(8).ToArray();

        addQuestion(module, Question.NotMorseCodeWord, new[] { ordinal(stage + 1) }, correctAnswers, wrongAnswers);
    }

    private IEnumerable<object> ProcessNotSimaze(KMBombModule module)
    {
        var component = GetComponent(module, "NotSimaze");
        var propSolved = GetProperty<bool>(component, "Solved", true);
        var fldMazeIndex = GetIntField(component, "mazeIndex");
        var fldX = GetIntField(component, "x");
        var fldY = GetIntField(component, "y");
        var fldGoalX = GetIntField(component, "goalX");
        var fldGoalY = GetIntField(component, "goalY");
        if (propSolved == null || fldMazeIndex == null || fldX == null || fldY == null || fldGoalX == null || fldGoalY == null)
            yield break;

        yield return null;  // Make sure the module has initialised.
        var colours = _attributes[Question.NotSimazeMaze].AllAnswers;
        var startPositionArray = new[] { string.Format("({0}, {1})", colours[fldX.Get()], colours[fldY.Get()]) };

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotSimaze);

        var goalPositionArray = new[] { string.Format("({0}, {1})", colours[fldGoalX.Get()], colours[fldGoalY.Get()]) };

        addQuestions(module,
            makeQuestion(Question.NotSimazeMaze, _NotSimaze, correctAnswers: new[] { colours[fldMazeIndex.Get()] }),
            makeQuestion(Question.NotSimazeStart, _NotSimaze, correctAnswers: startPositionArray, preferredWrongAnswers: goalPositionArray),
            makeQuestion(Question.NotSimazeGoal, _NotSimaze, correctAnswers: goalPositionArray, preferredWrongAnswers: startPositionArray)
        );
    }

    private IEnumerable<object> ProcessNotWhosOnFirst(KMBombModule module)
    {
        var component = GetComponent(module, "NotWhosOnFirst");
        var propSolved = GetProperty<bool>(component, "Solved", true);
        var fldPositions = GetArrayField<int>(component, "rememberedPositions");
        var fldLabels = GetArrayField<string>(component, "rememberedLabels");
        var fldSum = GetIntField(component, "stage2Sum");
        if (propSolved == null || fldPositions == null || fldLabels == null || fldSum == null)
            yield break;

        while (!propSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_NotWhosOnFirst);

        var i = Rnd.Range(0, 6);
        var positions = _attributes[Question.NotWhosOnFirstPressedPosition].AllAnswers;
        var sumCorrectAnswers = new[] { fldSum.Get().ToString() };

        addQuestions(module,
            i >= 4 ? makeQuestion(Question.NotWhosOnFirstReferencePosition, _NotWhosOnFirst, new[] { (i - 1).ToString() }, new[] { positions[fldPositions.Get()[i]] }) :
                makeQuestion(Question.NotWhosOnFirstPressedPosition, _NotWhosOnFirst, new[] { (i + 1).ToString() }, new[] { positions[fldPositions.Get()[i]] }),
            i >= 4 ? makeQuestion(Question.NotWhosOnFirstReferenceLabel, _NotWhosOnFirst, new[] { (i - 1).ToString() }, new[] { fldLabels.Get()[i] }) :
                makeQuestion(Question.NotWhosOnFirstPressedLabel, _NotWhosOnFirst, new[] { (i + 1).ToString() }, new[] { fldLabels.Get()[i] }),
            makeQuestion(Question.NotWhosOnFirstSum, _NotWhosOnFirst, correctAnswers: sumCorrectAnswers)
        );
    }

    private IEnumerable<object> ProcessNumberedButtons(KMBombModule module)
    {
        var comp = GetComponent(module, "NumberedButtonsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldNumbers = GetArrayField<int>(comp, "buttonNums");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_NumberedButtons);

        var numbers = fldNumbers.Get();

        if (numbers.Count() != 16)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Numbered Buttons because 'numbers' has unexpected length ({1}; expected 16).", _moduleId, numbers.Length);
            yield break;
        }

        for (int i = 0; i < 16; i++)
        {
            if (numbers[i] < 1 || numbers[i] > 100)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Numbered Buttons because 'numbers[{1}]' has unexpected value ({2}; expected 1-100).", _moduleId, i.ToString(), numbers[i].ToString());
                yield break;
            }
        }

        int randomIndexChooser = Rnd.Range(0, 16);

        addQuestions(module, (makeQuestion(Question.NumberedButtonsLabels, _NumberedButtons, new[] { (randomIndexChooser + 1).ToString() }, correctAnswers: new[] { numbers[randomIndexChooser].ToString() }, preferredWrongAnswers: new[] { Rnd.Range(1, 101).ToString() })));
    }

    private IEnumerable<object> ProcessObjectShows(KMBombModule module)
    {
        var comp = GetComponent(module, "objectShows");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldSolution = GetField<Array>(comp, "solution");
        var fldContestantNames = GetArrayField<string>(comp, "charnames", isPublic: true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ObjectShows);

        var solution = fldSolution.Get();
        if (solution == null)
            yield break;
        var solutionObjs = solution.Cast<object>().ToArray();
        if (solutionObjs.Length != 5 || solutionObjs.Contains(null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Object Shows because ‘solution’ contains a null value or has unexpected length {1} (expected 5).", _moduleId, solutionObjs.Length);
            yield break;
        }

        var tmp = solutionObjs[0];
        var fldId = GetIntField(tmp, "id", isPublic: true);
        var contestantNames = fldContestantNames.Get();
        if (contestantNames == null)
            yield break;
        if (solutionObjs.Any(c => fldId.GetFrom(c) < 0 || fldId.GetFrom(c) >= contestantNames.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Object Shows because one of the ‘id’s in ‘solution’ is unexpected: [{1}]", _moduleId, solutionObjs.Select(c => fldId.GetFrom(c)).JoinString(", "));
            yield break;
        }
        var solutionNames = solutionObjs.Select(c => contestantNames[fldId.GetFrom(c)]).ToArray();

        addQuestion(module, Question.ObjectShowsContestants, correctAnswers: solutionNames, preferredWrongAnswers: contestantNames);
    }

    private IEnumerable<object> ProcessOddOneOut(KMBombModule module)
    {
        var comp = GetComponent(module, "OddOneOutModule");
        var fldStages = GetField<Array>(comp, "_stages");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);

        var stages = fldStages.Get();

        if (stages == null)
            yield break;
        if (stages.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Odd One Out because '_stages' has unexpected length ({1}; expected 6).", _moduleId, stages.Length);
            yield break;
        }
        if (stages.Cast<object>().Any(x => x == null))
            yield break;

        string[] btnNames = { "top-left", "top-middle", "top-right", "bottom-left", "bottom-middle", "bottom-right" };
        var stageBtnFld = stages.Cast<object>().Select(x => GetIntField(x, "CorrectIndex", isPublic: true));

        if (stageBtnFld.Any(x => x == null))
            yield break;

        var stageBtn = stageBtnFld.Select(x => x.Get()).ToArray();

        if (stageBtn.Any(x => x < 0 || x >= btnNames.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Odd One Out because '_stages' has at least one 'CorrectIndex' that points to an illegal value.", _moduleId);
            yield break;
        }

        _modulesSolved.IncSafe(_OddOneOut);
        addQuestions(module,
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "first" }, new[] { btnNames[stageBtn[0]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "second" }, new[] { btnNames[stageBtn[1]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "third" }, new[] { btnNames[stageBtn[2]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "fourth" }, new[] { btnNames[stageBtn[3]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "fifth" }, new[] { btnNames[stageBtn[4]] }),
            makeQuestion(Question.OddOneOutButton, _OddOneOut, new[] { "sixth" }, new[] { btnNames[stageBtn[5]] }));
    }

    private IEnumerable<object> ProcessOnlyConnect(KMBombModule module)
    {
        var comp = GetComponent(module, "OnlyConnectModule");
        var fldHieroglyphsDisplayed = GetArrayField<int>(comp, "_hieroglyphsDisplayed");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");
        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var hieroglyphsDisplayed = fldHieroglyphsDisplayed.Get();
        if (hieroglyphsDisplayed == null || hieroglyphsDisplayed.Length != 6 || hieroglyphsDisplayed.Any(h => h < 0 || h >= 6))
        {
            Debug.LogFormat("<Souvenir #{0}> Only Connect: hieroglyphsDisplayed has unexpected value: {1}", _moduleId,
                hieroglyphsDisplayed == null ? "null" : string.Format("[{0}]", hieroglyphsDisplayed.JoinString(", ")));
            yield break;
        }

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_OnlyConnect);

        var hieroglyphs = new[] { "Two Reeds", "Lion", "Twisted Flax", "Horned Viper", "Water", "Eye of Horus" };
        var positions = new[] { "top left", "top middle", "top right", "bottom left", "bottom middle", "bottom right" };
        addQuestions(module, positions.Select((p, i) => makeQuestion(Question.OnlyConnectHieroglyphs, _OnlyConnect, new[] { p }, new[] { hieroglyphs[hieroglyphsDisplayed[i]] })));
    }

    private IEnumerable<object> ProcessOrangeArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "OrangeArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldMoves = GetArrayField<string>(comp, "moves");
        var fldButtons = GetArrayField<KMSelectable>(comp, "buttons", isPublic: true);
        var fldStage = GetIntField(comp, "stage");

        yield return null;

        // The module does not modify the arrays; it instantiates a new one for each stage.
        var correctMoves = new string[3][];

        KMSelectable[] buttons = fldButtons.Get();
        if (buttons == null)
            yield break;

        var prevButtonInteracts = buttons.Select(b => b.OnInteract).ToArray();
        for (int i = 0; i < buttons.Length; i++)
        {
            var prevInteract = prevButtonInteracts[i];
            buttons[i].OnInteract = delegate
            {
                var ret = prevInteract();
                var st = fldStage.Get();
                if (st < 1 || st > 3)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Orange Arrows because ‘stage’ was out of range: {1}.", _moduleId, st);
                    correctMoves = null;
                    for (int j = 0; j < buttons.Length; j++)
                        buttons[j].OnInteract = prevButtonInteracts[j];
                }
                else
                {
                    // We need to capture the array at each button press because the user might get a strike and the array might change
                    correctMoves[st - 1] = fldMoves.Get();
                }
                return ret;
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_OrangeArrows);

        if (correctMoves == null)   // an error message has already been output
            yield break;

        for (int i = 0; i < buttons.Length; i++)
            buttons[i].OnInteract = prevButtonInteracts[i];

        var directions = new[] { "UP", "RIGHT", "DOWN", "LEFT" };
        if (correctMoves.Any(arr => arr == null || arr.Any(dir => !directions.Contains(dir))))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Orange Arrows because one of the move arrays has an unexpected value: [{1}].",
                _moduleId, correctMoves.Select(arr => arr == null ? "null" : string.Format("[{0}]", arr.JoinString(", "))).JoinString(", "));
            yield break;
        }

        var qs = new List<QandA>();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                qs.Add(makeQuestion(Question.OrangeArrowsSequences, _OrangeArrows, new[] { ordinal(j + 1), ordinal(i + 1) }, new[] { correctMoves[i][j].Substring(0, 1) + correctMoves[i][j].Substring(1).ToLowerInvariant() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessOrderedKeys(KMBombModule module)
    {
        var comp = GetComponent(module, "OrderedKeysScript");
        var fldInfo = GetArrayField<int[]>(comp, "info");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldStage = GetIntField(comp, "stage");

        yield return null;

        var stage = 0;
        var moduleData = new int[3][][];

        while (!fldSolved.Get())
        {
            var info = fldInfo.Get();
            if (info == null || info.Length != 6 || info.Any(arr => arr == null || arr.Length != 4))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Ordered Keys because ‘info’ has unexpected length data: [{1}] (expected 6 arrays of length 4).", _moduleId, info == null ? "<null>" : info.Select(arr => arr == null ? "<null>" : string.Format("[{0}]", arr.JoinString(", "))).JoinString(", "));
                yield break;
            }
            if (fldStage.Get() < 1 || fldStage.Get() > 3)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Ordered Keys because ’stage’ has unexpected value: {1} (expected 1, 2, or 3).", _moduleId, fldStage.Get());
                yield break;
            }
            if (stage != fldStage.Get() || moduleData[fldStage.Get() - 1] == null || Enumerable.Range(0, 6).All(ix => info[ix].SequenceEqual(moduleData[fldStage.Get() - 1][ix])))
            {
                stage = fldStage.Get();
                moduleData[stage - 1] = info.Select(arr => arr.ToArray()).ToArray(); // Take a copy of the array.
            }
            yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_OrderedKeys);

        var colors = new string[6] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" };

        var qs = new List<QandA>();
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 6; j++)
            {
                qs.Add(makeQuestion(Question.OrderedKeysColors, _OrderedKeys, new[] { (i + 1).ToString(), ordinal(j + 1) }, new[] { colors[moduleData[i][j][0]] }));
                qs.Add(makeQuestion(Question.OrderedKeysLabels, _OrderedKeys, new[] { (i + 1).ToString(), ordinal(j + 1) }, new[] { moduleData[i][j][3].ToString() }));
                qs.Add(makeQuestion(Question.OrderedKeysLabelColors, _OrderedKeys, new[] { (i + 1).ToString(), ordinal(j + 1) }, new[] { colors[moduleData[i][j][1]] }));
            }
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessOrientationCube(KMBombModule module)
    {
        var comp = GetComponent(module, "OrientationModule");
        var fldInitialVirtualViewAngle = GetField<float>(comp, "initialVirtualViewAngle");
        var solved = false;

        module.OnPass += delegate { solved = true; return false; };

        while (!solved)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_OrientationCube);

        var initialVirtualViewAngle = fldInitialVirtualViewAngle.Get();
        var initialAnglePos = Array.IndexOf(new[] { 0f, 90f, 180f, 270f }, initialVirtualViewAngle);
        if (initialAnglePos == -1)
        {
            Debug.LogFormat("<Souvenir #{1}> Orientation Cube: initialVirtualViewAngle has unexpected value: {0}", initialVirtualViewAngle, _moduleId);
            yield break;
        }

        addQuestion(module, Question.OrientationCubeInitialObserverPosition, correctAnswers: new[] { new[] { "front", "left", "back", "right" }[initialAnglePos] });
    }

    private IEnumerable<object> ProcessPalindromes(KMBombModule module)
    {
        var comp = GetComponent(module, "Palindromes");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldX = GetField<string>(comp, "x");
        var fldY = GetField<string>(comp, "y");
        var fldZ = GetField<string>(comp, "z");
        var fldN = GetField<string>(comp, "n");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Palindromes);

        var vars = new[] { fldN, fldX, fldY, fldZ };
        byte randomVar = (byte) Rnd.Range(0, vars.Length);
        byte randomInd = (byte) Rnd.Range(0, randomVar < 2 ? 5 : 4);  // 5 if x or n, else 4
        string numString = vars[randomVar].Get();
        char digit = numString[numString.Length - 1 - randomInd];
        if (digit < '0' || digit > '9')
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Palindromes because the chosen character was unexpected ('{1}').", digit);
            yield break;
        }

        string[] labels = new string[] { "the screen", "X", "Y", "Z" };
        addQuestion(module, Question.PalindromesNumbers, new[] { labels[randomVar], ordinal(randomInd + 1) }, correctAnswers: new[] { digit.ToString() });
    }

    private IEnumerable<object> ProcessPartialDerivatives(KMBombModule module)
    {
        var comp = GetComponent(module, "PartialDerivativesScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldLeds = GetArrayField<int>(comp, "ledIndex");
        var fldDisplay = GetField<TextMesh>(comp, "display", isPublic: true);

        var display = fldDisplay.Get();
        if (display == null)
            yield break;
        var terms = display.text.Split('\n').Select(term => Regex.Replace(Regex.Replace(term.Trim(), @"^(f.*?=|\+) ", ""), @"^- ", "−")).ToArray();
        if (terms.Length != 3)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Partial Derivatives because the display does not appear to contain three terms: ""{1}""", _moduleId, display.text.Replace("\r", "").Replace("\n", "\\n"));
            yield break;
        }

        var vars = new[] { "x", "y", "z" };
        var exponentStrs = new[] { "²", "³", "⁴", "⁵" };
        var writeTerm = new Func<int, bool, int[], string>((int coeff, bool negative, int[] exps) =>
        {
            if (coeff == 0)
                return "0";

            var function = negative ? "−" : "";
            if (coeff > 1)
                function += coeff.ToString();
            for (int j = 0; j < 3; j++)
            {
                if (exps[j] != 0)
                {
                    function += vars[j];
                    if (exps[j] > 1)
                        function += exponentStrs[exps[j] - 2];
                }
            }
            return function;
        });

        var wrongAnswers = new HashSet<string>();
        while (wrongAnswers.Count < 3)
        {
            var exps = new int[3];
            for (int j = 0; j < 3; j++)
                exps[j] = Rnd.Range(0, 6);
            if (exps.All(e => e == 0))
                exps[Rnd.Range(0, 3)] = Rnd.Range(1, 6);
            var wrongTerm = writeTerm(Rnd.Range(1, 10), Rnd.Range(0, 2) != 0, exps);
            if (!terms.Contains(wrongTerm))
                wrongAnswers.Add(wrongTerm);
        }

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };
        while (!isSolved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PartialDerivatives);

        var leds = fldLeds.Get();
        if (leds == null)
            yield break;
        if (leds.Length != 3 || leds.Any(l => l < 0 || l >= 6))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Partial Derivatives because ‘ledIndex’ has unexpected length (expected 3) or contains unexpected value (expected 0–5): [{1}].", _moduleId, leds.JoinString(", "));
            yield break;
        }

        var colorNames = new[] { "blue", "green", "orange", "purple", "red", "yellow" };
        var qs = new List<QandA>();
        for (var stage = 0; stage < 3; stage++)
            qs.Add(makeQuestion(Question.PartialDerivativesLedColors, _PartialDerivatives, formatArgs: new[] { (stage + 1).ToString() }, correctAnswers: new[] { colorNames[leds[stage]] }));
        for (var term = 0; term < 3; term++)
            qs.Add(makeQuestion(Question.PartialDerivativesTerms, _PartialDerivatives, formatArgs: new[] { ordinal(term + 1) }, correctAnswers: new[] { terms[term] }, preferredWrongAnswers: wrongAnswers.ToArray()));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessPassportControl(KMBombModule module)
    {
        var comp = GetComponent(module, "passportControlScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldPassages = GetIntField(comp, "passages");
        var fldExpiration = GetArrayField<int>(comp, "expiration");
        var fldStamps = GetArrayField<KMSelectable>(comp, "stamps", isPublic: true);
        var fldTextToHide1 = GetArrayField<GameObject>(comp, "passport", isPublic: true);
        var fldTextToHide2 = GetField<GameObject>(comp, "ticket", isPublic: true);

        var stamps = fldStamps.Get();
        var textToHide1 = fldTextToHide1.Get();
        var textToHide2 = fldTextToHide2.Get();

        if (stamps == null || textToHide1 == null || textToHide2 == null)
            yield break;

        var textToHide = new List<TextMesh>();

        for (int i = 0; i < textToHide1.Length; i++)
        {
            if (textToHide1[i] == null || textToHide1[i].GetComponent<TextMesh>() == null)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Passport Control because at least one TextMesh that needs to be hidden was null.", _moduleId);
                yield break;
            }
            textToHide.Add(textToHide1[i].GetComponent<TextMesh>());
        }
        if (textToHide2 == null || textToHide2.GetComponent<TextMesh>() == null)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Passport Control because at least one TextMesh that needs to be hidden was null.", _moduleId);
            yield break;
        }
        textToHide.Add(textToHide2.GetComponent<TextMesh>());

        var expirationDates = new List<int>();

        for (int i = 0; i < stamps.Length; i++)
        {
            if (stamps[i] == null)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Passport Control because at least one Selectable null.", _moduleId);
                yield break;
            }
            var oldHandler = stamps[i].OnInteract;
            stamps[i].OnInteract = delegate
            {
                // if an error occurs, function returns earlier and no info is added to lists. The error is caught later when list length is checked.
                var date = fldExpiration.Get();

                if (date == null || date.Length != 3)
                    return oldHandler();

                var year = date[2];

                var passages = fldPassages.Get();

                var ret = oldHandler();

                if (fldPassages.Get() == passages) // player got strike, ignoring retrieved info
                    return ret;

                expirationDates.Add(year);
                return ret;
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        if (expirationDates.Count != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Passport Control because the number of retrieved sets of information wasn't 3 (was {1}).", _moduleId, expirationDates.Count);
            yield break;
        }

        for (int i = 0; i < textToHide.Count; i++)
            textToHide[i].text = "";

        var altDates = new List<string[]>();

        for (int i = 0; i < expirationDates.Count; i++)
        {
            altDates.Add(new string[6]);
            int startVal = expirationDates[i] - Rnd.Range(0, 6);
            for (int j = 0; j < altDates[i].Length; j++)
                altDates[i][j] = (startVal + j).ToString();
        }

        _modulesSolved.IncSafe(_PassportControl);
        addQuestions(module,
            makeQuestion(Question.PassportControlPassenger, _PassportControl, new[] { "first" }, new[] { expirationDates[0].ToString() }, altDates[0]),
            makeQuestion(Question.PassportControlPassenger, _PassportControl, new[] { "second" }, new[] { expirationDates[1].ToString() }, altDates[1]),
            makeQuestion(Question.PassportControlPassenger, _PassportControl, new[] { "third" }, new[] { expirationDates[2].ToString() }, altDates[2]));
    }

    private IEnumerable<object> ProcessPatternCube(KMBombModule module)
    {
        var comp = GetComponent(module, "PatternCubeModule");
        var fldSelectableSymbols = GetField<Array>(comp, "_selectableSymbols");
        var fldSelectableSymbolObjects = GetArrayField<MeshRenderer>(comp, "_selectableSymbolObjs");
        var fldPlaceableSymbolObjects = GetArrayField<MeshRenderer>(comp, "_placeableSymbolObjs");
        var fldHighlightedPosition = GetIntField(comp, "_highlightedPosition");

        yield return null;
        var selectableSymbols = fldSelectableSymbols.Get();
        if (selectableSymbols == null || selectableSymbols.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Pattern Cube because _selectableSymbols {1} (expected length 5).", _moduleId, selectableSymbols == null ? "was null" : "had length " + selectableSymbols.Length);
            yield break;
        }
        var selectableSymbolObjects = fldSelectableSymbolObjects.Get();
        if (selectableSymbolObjects == null || selectableSymbolObjects.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Pattern Cube because _selectableSymbolObjs {1} (expected length 5).", _moduleId, selectableSymbolObjects == null ? "was null" : "had length " + selectableSymbolObjects.Length);
            yield break;
        }
        var placeableSymbolObjects = fldPlaceableSymbolObjects.Get();
        if (placeableSymbolObjects == null || placeableSymbolObjects.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Pattern Cube because _placeableSymbolObjs {1} (expected length 5).", _moduleId, selectableSymbolObjects == null ? "was null" : "had length " + selectableSymbolObjects.Length);
            yield break;
        }
        var highlightPos = fldHighlightedPosition.Get();
        if (highlightPos < 0 || highlightPos > 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Pattern Cube because _highlightedPosition was {1} (expected 0–4).", _moduleId, highlightPos);
            yield break;
        }

        // Wait for it to be solved.
        while (selectableSymbols.Cast<object>().Any(obj => obj != null))
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PatternCube);

        var symbols = selectableSymbolObjects.Concat(placeableSymbolObjects.Where(r => r.gameObject.activeSelf))
            .Select(r => PatternCubeSprites[int.Parse(r.sharedMaterial.mainTexture.name.Substring(6))]).ToArray();

        addQuestion(module, Question.PatternCubeHighlightedSymbol, null, new[] { symbols[highlightPos] }, symbols);
    }

    private IEnumerable<object> ProcessPerspectivePegs(KMBombModule module)
    {
        var comp = GetComponent(module, "PerspectivePegsModule");
        var fldIsComplete = GetField<bool>(comp, "isComplete");
        var fldColourMeshes = GetField<MeshRenderer[,]>(comp, "ColourMeshes");
        while (!fldIsComplete.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PerspectivePegs);

        int keyNumber = 0; string keyColour;
        char prevChar = '\0';
        foreach (var letter in Bomb.GetSerialNumberLetters())
        {
            if (prevChar == 0)
                prevChar = letter;
            else
            {
                keyNumber += Math.Abs(letter - prevChar);
                prevChar = '\0';
            }
        }
        switch (keyNumber % 10)
        {
            case 0: case 3: keyColour = "ColourRed"; break;
            case 4: case 9: keyColour = "ColourYellow"; break;
            case 1: case 7: keyColour = "ColourGreen"; break;
            case 5: case 8: keyColour = "ColourBlue"; break;
            case 2: case 6: keyColour = "ColourPurple"; break;
            default: yield break;
        }

        var colourMeshes = fldColourMeshes.Get();
        int pegIndex;
        for (pegIndex = 0; pegIndex < 5; ++pegIndex)
        {
            if (Enumerable.Range(0, 5).Count(i => colourMeshes[pegIndex, i].sharedMaterial.name.StartsWith(keyColour)) >= 3)
                break;
        }
        if (pegIndex >= 5)
        {
            Debug.LogFormat("<Souvenir #{1}> Abandoning Perspective Pegs because the key peg couldn't be found (the key colour was {0}).", keyColour, _moduleId);
            yield break;
        }

        addQuestions(module, Enumerable.Range(0, 3).Select(i => makeQuestion(
             Question.PerspectivePegsSolution,
            _PerspectivePegs,
            correctAnswers: new[] { PerspectivePegsSprites[pegIndex] },
            preferredWrongAnswers: PerspectivePegsSprites)));
    }

    private IEnumerable<object> ProcessPie(KMBombModule module)
    {
        var comp = GetComponent(module, "PieScript");
        var fldDigits = GetArrayField<string>(comp, "codes");
        var fldSolved = GetField<bool>(comp, "solveCoroutineStarted");

        // get displayed digits
        var digits = fldDigits.Get();

        if (digits == null)
            yield break;

        if (digits.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Pie because 'codes' has unexpected length {1} (expected 5).", _moduleId, digits.Count());
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Pie);
        addQuestions(module, digits.Select((digit, ix) => makeQuestion(Question.PieDigits, _Pie, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { digit }, preferredWrongAnswers: digits)));
    }

    private IEnumerable<object> ProcessPigpenCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "PigpenCycleScript", Question.PigpenCycleWord, _PigpenCycle);
    }

    private IEnumerable<object> ProcessPlaceholderTalk(KMBombModule module)
    {
        var comp = GetComponent(module, "placeholderTalk");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldFirstString = GetField<string>(comp, "firstString");
        var fldFirstPhrase = GetArrayField<string>(comp, "firstPhrase");
        var fldCurrentOrdinal = GetField<string>(comp, "currentOrdinal");
        var fldOrdinals = GetArrayField<string>(comp, "ordinals");
        var fldAnswer = GetField<byte>(comp, "answerId");
        var fldPreviousModules = GetField<sbyte>(comp, "previousModules");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_PlaceholderTalk);

        var answer = fldAnswer.Get() + 1;
        if (answer < 1 || answer > 17)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning PlaceholderTalk because ‘answer’ has unexpected value (expected 1-17): {1}", _moduleId, answer);
            yield break;
        }

        var firstPhrase = fldFirstPhrase.Get();
        var ordinals = fldOrdinals.Get();

        if (!firstPhrase.Contains(fldFirstString.Get()))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning PlaceholderTalk because ‘fldFirstString’ has unexpected value (expected to be one of ‘firstPhrase’): {1}", _moduleId, fldFirstString.Get());
            yield break;
        }

        if (!ordinals.Contains(fldCurrentOrdinal.Get()))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning PlaceholderTalk because ‘fldCurrentOrdinal’ has unexpected value (expected to be one of ‘ordinals’): {1}", _moduleId, fldFirstString.Get());
            yield break;
        }

        var previousModules = fldPreviousModules.Get();

        var qs = new List<QandA>();

        //because the number of solved modules could be any number, the second phrase question should be deactivated if previousModule is either 1 or -1, meaning that they apply to the numbers
        if (previousModules == 0)
            qs.Add(makeQuestion(Question.PlaceholderTalkSecondPhrase, _PlaceholderTalk, correctAnswers: new[] { answer.ToString() }));

        qs.Add(makeQuestion(Question.PlaceholderTalkFirstPhrase, _PlaceholderTalk, correctAnswers: new[] { fldFirstString.Get().ToString() }, preferredWrongAnswers: firstPhrase));
        qs.Add(makeQuestion(Question.PlaceholderTalkOrdinal, _PlaceholderTalk, correctAnswers: new[] { fldCurrentOrdinal.Get().ToString() }, preferredWrongAnswers: ordinals));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessPlanets(KMBombModule module)
    {
        var comp = GetComponent(module, "planetsModScript");
        var fldPlanet = GetIntField(comp, "planetShown");
        var fldStrips = GetArrayField<int>(comp, "stripColours");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        yield return null;

        var planetShown = fldPlanet.Get();
        if (planetShown < 0 || planetShown > 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Planets because ‘planetShown’ has unexpected value (expected 0-9): {1}", _moduleId, planetShown);
            yield break;
        }
        var stripColors = fldStrips.Get();
        if (stripColors.Length != 5 || stripColors.Any(x => x < 0 || x > 8))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Planets because ‘stripColors’ has unexpected length or one of its elements has unexpected value (expected length 5 and values 0-8): {1}", _moduleId, string.Format("[{0}]", stripColors.JoinString(", ")));
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Planets);

        var stripNames = new[] { "Aqua", "Blue", "Green", "Lime", "Orange", "Red", "Yellow", "White", "Off" };
        addQuestions(module,
            stripColors.Select((strip, count) => makeQuestion(Question.PlanetsStrips, _Planets, new[] { ordinal(count + 1) }, new[] { stripNames[strip] }))
                .Concat(new[] { makeQuestion(Question.PlanetsPlanet, _Planets, correctAnswers: new[] { PlanetsSprites[planetShown] }, preferredWrongAnswers: (DateTime.Now.Month == 4 && DateTime.Now.Day == 1) ? PlanetsSprites : PlanetsSprites.Take(PlanetsSprites.Length - 2).ToArray()) }));
    }

    private IEnumerable<object> ProcessPlayfairCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle1(module, "PlayfairCycleScript", Question.PlayfairCycleWord, _PlayfairCycle);
    }

    private IEnumerable<object> ProcessPoetry(KMBombModule module)
    {
        var comp = GetComponent(module, "PoetryModule");
        var fldWordSelectables = GetArrayField<KMSelectable>(comp, "wordSelectables", isPublic: true);
        var fldStage = GetIntField(comp, "currentStage");
        var fldStageCount = GetIntField(comp, "stageCount", isPublic: true);
        var fldWordTextMeshes = GetArrayField<TextMesh>(comp, "words", isPublic: true);

        yield return null;

        var answers = new List<string>();
        var selectables = fldWordSelectables.Get();
        if (selectables == null)
            yield break;
        if (selectables.Length != 6 || selectables.Any(s => s == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Poetry because ‘wordSelectables’ has unexpected length or contains null (expected length 6, got values: [{1}])",
                _moduleId, selectables.Select(s => s == null ? "<null>" : "NOT NULL").JoinString(", "));
            yield break;
        }
        var wordTextMeshes = fldWordTextMeshes.Get();
        if (wordTextMeshes == null)
            yield break;
        if (wordTextMeshes.Length != 6 || wordTextMeshes.Any(s => s == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Poetry because ‘words’ has unexpected length or contains null (expected length 6, got values: [{1}])",
                _moduleId, wordTextMeshes.Select(s => s == null ? "<null>" : "NOT NULL").JoinString(", "));
            yield break;
        }

        for (int i = 0; i < 6; i++)
        {
            var j = i;
            var oldHandler = selectables[i].OnInteract;
            selectables[i].OnInteract = delegate
            {
                var prevStage = fldStage.Get();
                var word = wordTextMeshes[j].text;
                var ret = oldHandler();

                if (fldStage.Get() > prevStage)
                    answers.Add(word);

                return ret;
            };
        }

        while (fldStage.Get() < fldStageCount.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Poetry);

        if (answers.Count != fldStageCount.Get())
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Poetry because the number of answers captured is not equal to the number of stages played ({1}). Answers were: [{2}]",
                _moduleId, fldStageCount.Get(), answers.JoinString(", "));
            yield break;
        }

        addQuestions(module, answers.Select((ans, st) => makeQuestion(Question.PoetryAnswers, _Poetry, formatArgs: new[] { ordinal(st + 1) }, correctAnswers: new[] { ans }, preferredWrongAnswers: answers.ToArray())));
    }

    private IEnumerable<object> ProcessPolyhedralMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "PolyhedralMazeModule");
        var fldStartFace = GetIntField(comp, "_startFace");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_PolyhedralMaze);
        addQuestion(module, Question.PolyhedralMazeStartPosition, null, new[] { fldStartFace.Get().ToString() });
    }

    private IEnumerable<object> ProcessProbing(KMBombModule module)
    {
        var comp = GetComponent(module, "ProbingModule");
        var fldWires = GetField<Array>(comp, "mWires");
        var fldDisplay = GetField<TextMesh>(comp, "display", isPublic: true);
        var fldSelectables = GetArrayField<KMSelectable>(comp, "selectables", isPublic: true);
        var fldSolved = GetField<bool>(comp, "bSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Probing);

        var display = fldDisplay.Get();
        if (display == null)
            yield break;

        // Blank out the display so that the user cannot see the readout for the solution wires
        display.text = "";

        // Prevent the user from interacting with the wires after solving
        var selectables = fldSelectables.Get();
        if (selectables == null || selectables.Length != 6 || selectables.Any(s => s == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Probing because ‘selectables’ is null or has unexpected length (expected 6): {1}",
                _moduleId, selectables == null ? "<null>" : string.Format("[{0}]", selectables.Select(s => s == null ? "<null>" : "SELECTABLE").JoinString(", ")));
            yield break;
        }
        for (int i = 0; i < selectables.Length; i++)
            selectables[i].OnInteract = delegate { return false; };

        var wireNames = new[] { "red-white", "yellow-black", "green", "gray", "yellow-red", "red-blue" };
        var wireFrequenciesRaw = fldWires.Get();
        if (wireFrequenciesRaw == null || wireFrequenciesRaw.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Probing because ‘mWires’ is null or has unexpected length (expected 6): {1}",
                _moduleId, wireFrequenciesRaw == null ? "<nully>" : string.Format("[{0}]", wireFrequenciesRaw.Cast<object>().Select(s => s.ToString()).JoinString("; ")));
            yield break;
        }

        // Retrieve the missing wire frequencies
        var wireFrequencies = wireFrequenciesRaw.Cast<int>().Select((val, ix) =>
        {
            if (val == 7) return "60Hz";
            if (val == 11) return "50Hz";
            if (val == 13) return "22Hz";
            if (val == 14) return "10Hz";
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Probing because wire #{1} has unexpected value {2} (expected 7, 11, 13, 14).", _moduleId, ix, val);
            return null;
        }).ToArray();
        if (wireFrequencies.Any(wf => wf == null))
            yield break;

        addQuestions(module, wireFrequencies.Select((wf, ix) => makeQuestion(Question.ProbingFrequencies, _Probing, new[] { wireNames[ix] }, new[] { wf })));
    }

    private IEnumerable<object> ProcessPurpleArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "PurpleArrowsScript");
        var fldFinish = GetField<string>(comp, "finish");
        var fldWordScreen = GetField<GameObject>(comp, "wordDisplay", isPublic: true);
        var fldWordList = GetArrayField<string>(comp, "words");

        // The module sets moduleSolved to true at the start of its solve animation but before it is actually marked as solved.
        // Therefore, we use OnPass to wait for the end of that animation and then set the text to “SOLVED” afterwards.
        var solved = false;
        module.OnPass += delegate { solved = true; return false; };

        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_PurpleArrows);

        string finishWord = fldFinish.Get();

        if (finishWord == null)
            yield break;

        if (finishWord.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Purple Arrows because ‘finishWord’ has unexpected length (expected 6): “{1}”", finishWord);
            yield break;
        }

        string[] wordList = fldWordList.Get();
        if (wordList == null || wordList.Any(word => word == null))
            yield break;

        if (wordList.Length != (9 * 13) || !wordList.Contains(finishWord))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Purple Arrows because ‘wordList’ has an unexpected length (expected 9 * 13) or does not contain ‘finishWord’ : [Length: {1}, finishWord: {2}]", wordList.Length, finishWord);
            yield break;
        }

        var wordScreen = fldWordScreen.Get();
        var wordScreenTextMesh = wordScreen == null ? null : wordScreen.GetComponent<TextMesh>();
        if (wordScreen == null || wordScreenTextMesh == null)
            yield break;
        wordScreenTextMesh.text = "SOLVED";

        addQuestion(module, Question.PurpleArrowsFinish, correctAnswers: new[] { Regex.Replace(finishWord, @"(?<!^).", m => m.Value.ToLowerInvariant()) }, preferredWrongAnswers: wordList.Select(w => w[0] + w.Substring(1).ToLowerInvariant()).ToArray());
    }

    private IEnumerable<object> ProcessQuintuples(KMBombModule module)
    {
        var comp = GetComponent(module, "quintuplesScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldNumbers = GetArrayField<int>(comp, "cyclingNumbers", isPublic: true);
        var fldColors = GetArrayField<string>(comp, "chosenColorsName", isPublic: true);
        var fldColorCounts = GetArrayField<int>(comp, "numberOfEachColour", isPublic: true);
        var fldColorNames = GetArrayField<string>(comp, "potentialColorsName", isPublic: true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Quintuples);

        var numbers = fldNumbers.Get();
        var colors = fldColors.Get();
        var colorCounts = fldColorCounts.Get();
        var colorNames = fldColorNames.Get();
        if (numbers == null || numbers.Length != 25 || numbers.Any(n => n < 1 || n > 10) ||
            colors == null || colors.Length != 25 ||
            colorCounts == null || colorCounts.Length != 5 || colorCounts.Any(cc => cc < 0 || cc > 25) ||
            colorNames == null || colorNames.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Quintuples because an array has unexpected length or values: (numbers={1} / colors={2} / numberOfEachColour={3} / potentialColorsName={4})", _moduleId,
                numbers == null ? "<null>" : string.Format("[{0}]", numbers.JoinString(", ")),
                colors == null ? "<null>" : string.Format("[{0}]", colors.Select(c => string.Format(@"""{0}""", c)).JoinString(", ")),
                colorCounts == null ? "<null>" : string.Format("[{0}]", colorCounts.JoinString(", ")),
                colorNames == null ? "<null>" : string.Format("[{0}]", colorNames.JoinString(", ")));
            yield break;
        }

        addQuestions(module,
            numbers.Select((n, ix) => makeQuestion(Question.QuintuplesNumbers, _Quintuples, new[] { ordinal(ix % 5 + 1), ordinal(ix / 5 + 1) }, new[] { (n % 10).ToString() })).Concat(
            colors.Select((color, ix) => makeQuestion(Question.QuintuplesColors, _Quintuples, new[] { ordinal(ix % 5 + 1), ordinal(ix / 5 + 1) }, new[] { color }))).Concat(
            colorCounts.Select((cc, ix) => makeQuestion(Question.QuintuplesColorCounts, _Quintuples, new[] { colorNames[ix] }, new[] { cc.ToString() }))));
    }

    private IEnumerable<object> ProcessRecoloredSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "Recolored_Switches");
        var fldLedColors = GetField<StringBuilder>(comp, "LEDsColorsString");

        var isSolved = false;
        module.OnPass += delegate { isSolved = true; return false; };

        while (!isSolved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_RecoloredSwitches);

        var colorNames = new Dictionary<char, string>
        {
            { 'R', "red" },
            { 'G', "green" },
            { 'B', "blue" },
            { 'T', "turquoise" },
            { 'O', "orange" },
            { 'P', "purple" },
            { 'W', "white" }
        };
        var ledColors = fldLedColors.Get();
        if (ledColors == null)
            yield break;
        if (ledColors.Length != 10 || Enumerable.Range(0, 10).Any(ix => !colorNames.ContainsKey(ledColors[ix])))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Recolored Switches because 'LEDsColorsString' has unexpected length {1} (expected 10) or unexpected character ({2}) (expected {3}).",
                _moduleId, ledColors.Length, ledColors.ToString(), colorNames.Keys.JoinString());
            yield break;
        }

        addQuestions(module, Enumerable.Range(0, 10).Select(ix => makeQuestion(Question.RecoloredSwitchesLedColors, _RecoloredSwitches, formatArgs: new[] { ordinal(ix + 1) }, correctAnswers: new[] { colorNames[ledColors[ix]] })));
    }

    private IEnumerable<object> ProcessRedArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "RedArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldStart = GetIntField(comp, "start");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_RedArrows);

        int startNumber = fldStart.Get();
        if (startNumber < 0 || startNumber > 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Red Arrows because 'start' is {1} (expected 0–9).", _moduleId, startNumber);
            yield break;
        }

        addQuestion(module, Question.RedArrowsStartNumber, correctAnswers: new[] { startNumber.ToString() });
    }

    private IEnumerable<object> ProcessRetirement(KMBombModule module)
    {
        var comp = GetComponent(module, "retirementScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldHomes = GetArrayField<string>(comp, "retirementHomeOptions", isPublic: true);
        var fldAvailable = GetArrayField<string>(comp, "selectedHomes");
        var fldCorrect = GetField<string>(comp, "correctHome");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Retirement);

        string[] homes = fldHomes.Get();
        string[] available = fldAvailable.Get();
        string correct = fldCorrect.Get();

        if (homes == null || available == null || correct == null)
            yield break;
        if (correct == "")
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Retirement because 'correctHome' was empty.", _moduleId);
            yield break;
        }

        addQuestion(module, Question.RetirementHouses, correctAnswers: available.Where(x => x != correct).ToArray(), preferredWrongAnswers: homes);
    }

    private IEnumerable<object> ProcessReverseMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "reverseMorseScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldMessage1 = GetField<List<string>>(comp, "selectedLetters1", isPublic: true);
        var fldMessage2 = GetField<List<string>>(comp, "selectedLetters2", isPublic: true);

        var message1 = fldMessage1.Get();
        var message2 = fldMessage2.Get();
        if (message1 == null || message2 == null)
            yield break;
        if (message1.Count != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Reverse Morse because ‘selectedLetters1’ has an unexpected length: {1} (expected length: 6).", _moduleId, message1.Count);
            yield break;
        }
        if (message2.Count != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Reverse Morse because ‘selectedLetters2’ has an unexpected length: {1} (expected length: 6).", _moduleId, message2.Count);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ReverseMorse);

        var qs = new List<QandA>();
        for (int i = 0; i < 6; i++)
        {
            qs.Add(makeQuestion(Question.ReverseMorseCharacters, _ReverseMorse, new[] { ordinal(i + 1), "first" }, new[] { message1[i] }, message1.ToArray()));
            qs.Add(makeQuestion(Question.ReverseMorseCharacters, _ReverseMorse, new[] { ordinal(i + 1), "second" }, new[] { message2[i] }, message2.ToArray()));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessRGBMaze(KMBombModule module)
    {
        var comp = GetComponent(module, "RGBMazeScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldKeyPos = GetArrayField<int[]>(comp, "keylocations");
        var fldMazeNum = GetArrayField<int[]>(comp, "mazenumber");
        var fldExitPos = GetArrayField<int>(comp, "exitlocation");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_RGBMaze);

        var keyPos = fldKeyPos.Get();
        var mazeNum = fldMazeNum.Get();
        var exitPos = fldExitPos.Get();

        if (keyPos == null || mazeNum == null || exitPos == null)
            yield break;

        if (keyPos.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RGB Maze because 'KeyPos' has an unexpected length: Length = {1}", _moduleId, keyPos.Length);
            yield break;
        }

        if (keyPos.Any(key => key.Length != 2 || key.Any(number => number < 0 || number > 7)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RGB Maze because 'KeyPos' contains keys with invalid positions: [{1}]", _moduleId, keyPos.Select(key => string.Format("Length = {0}, ({1},{2})", key.Length, key[1], key[0])).JoinString("; "));
            yield break;
        }

        if (mazeNum.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RGB Maze because 'MazeNum' or has an unexpected length: Length = {1}", _moduleId, mazeNum.Length);
            yield break;
        }

        if (mazeNum.Any(maze => maze.Length != 2 || maze[0] < 0 || maze[0] > 9))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RGB Maze because 'MazeNum' contains mazes with invalid number: [{1}]", _moduleId, mazeNum.Select(maze => string.Format("Length = {0}, Maze {1}", maze.Length, maze[0])).JoinString("; "));
            yield break;
        }

        if (exitPos.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RGB Maze because 'exitPos' has an unexpected length: Length = {1}", _moduleId, exitPos.Length);
            yield break;
        }

        if (exitPos[1] < 0 || exitPos[1] > 7 || exitPos[2] < 0 || exitPos[2] > 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RGB Maze because 'exitPos' contains invalid coordinate: ({1},{2})", _moduleId, exitPos[2], exitPos[1]);
            yield break;
        }

        string[] colors = { "red", "green", "blue" };

        var qs = new List<QandA>();

        for (int index = 0; index < 3; index++)
        {
            qs.Add(makeQuestion(Question.RGBMazeKeys, _RGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { "ABCDEFGH"[keyPos[index][1]] + (keyPos[index][0] + 1).ToString() }));
            qs.Add(makeQuestion(Question.RGBMazeNumber, _RGBMaze,
                formatArgs: new[] { colors[index] },
                correctAnswers: new[] { mazeNum[index][0].ToString() }));
        }

        qs.Add(makeQuestion(Question.RGBMazeExit, _RGBMaze,
            correctAnswers: new[] { "ABCDEFGH"[exitPos[2]] + (exitPos[1] + 1).ToString() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessRhythms(KMBombModule module)
    {
        var comp = GetComponent(module, "Rhythms");
        var fldSolved = GetField<bool>(comp, "isSolved", isPublic: true);
        var fldColor = GetIntField(comp, "lightColor");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Rhythms);

        var color = fldColor.Get();
        if (color < 0 || color >= 4)
            Debug.LogFormat("<Souvenir #{0}> Abandoning Rhythms because lightColor has unexpected value ({1}).", _moduleId, color);
        else
            addQuestion(module, Question.RhythmsColor, correctAnswers: new[] { new[] { "Blue", "Red", "Green", "Yellow" }[color] });
    }

    private IEnumerable<object> ProcessRoleReversal(KMBombModule module)
    {
        var comp = GetComponent(module, "roleReversal");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldAnswerIndex = GetField<byte>(comp, "souvenir");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_RoleReversal);

        var fldRedWires = GetField<List<byte>>(comp, "redWires");
        var fldOrangeWires = GetField<List<byte>>(comp, "orangeWires");
        var fldYellowWires = GetField<List<byte>>(comp, "yellowWires");
        var fldGreenWires = GetField<List<byte>>(comp, "greenWires");
        var fldBlueWires = GetField<List<byte>>(comp, "blueWires");
        var fldPurpleWires = GetField<List<byte>>(comp, "purpleWires");

        var redWires = fldRedWires.Get();
        var orangeWires = fldOrangeWires.Get();
        var yellowWires = fldYellowWires.Get();
        var greenWires = fldGreenWires.Get();
        var blueWires = fldBlueWires.Get();
        var purpleWires = fldPurpleWires.Get();

        if (redWires.Count > 7 || orangeWires.Count > 7 || yellowWires.Count > 7 || greenWires.Count > 7 || blueWires.Count > 7 || purpleWires.Count > 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RoleReversal because a wire color has unexpected amount of bytes in list (expected 0-7): {1}, {2}, {3}, {4}, {5}, {6}", _moduleId, redWires, orangeWires, yellowWires, greenWires, blueWires, purpleWires);
            yield break;
        }

        if (redWires.Count + orangeWires.Count + yellowWires.Count + greenWires.Count + blueWires.Count + purpleWires.Count < 2 ||
            redWires.Count + orangeWires.Count + yellowWires.Count + greenWires.Count + blueWires.Count + purpleWires.Count > 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RoleReversal because all wires combined has unexpected value (expected 2-7): {1}", _moduleId, redWires.Count + orangeWires.Count + yellowWires.Count + greenWires.Count + blueWires.Count + purpleWires.Count);
            yield break;
        }

        var answerIndex = fldAnswerIndex.Get();
        if (answerIndex < 2 || answerIndex > 8)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning RoleReversal because ‘answerIndex’ has unexpected value (expected 2-8): {1}", _moduleId, answerIndex);
            yield break;
        }

        string[] color = new string[4] { "warm-colored", "cold-colored", "primary-colored", "secondary-colored" };
        byte randomIndex = (byte) Rnd.Range(0, color.Length);

        byte correct;
        switch (randomIndex)
        {
            case 0:
                correct = (byte) (redWires.Count + orangeWires.Count + yellowWires.Count);
                break;

            case 1:
                correct = (byte) (greenWires.Count + blueWires.Count + purpleWires.Count);
                break;

            case 2:
                correct = (byte) (redWires.Count + yellowWires.Count + blueWires.Count);
                break;

            case 3:
                correct = (byte) (orangeWires.Count + greenWires.Count + purpleWires.Count);
                break;

            default:
                Debug.LogFormat("<Souvenir #{0}> Abandoning RoleReversal because ‘index’ has unexpected value (expected 0-3): {1}", _moduleId, randomIndex);
                yield break;
        }

        addQuestions(module,
            makeQuestion(Question.RoleReversalWires, _RoleReversal, new[] { color[randomIndex] }, correctAnswers: new[] { correct.ToString() }, preferredWrongAnswers: new[] { "0", "1", "2", "3", "4", "5", "6", "7" }),
            makeQuestion(Question.RoleReversalNumber, _RoleReversal, correctAnswers: new[] { fldAnswerIndex.Get().ToString() }, preferredWrongAnswers: new[] { "2", "3", "4", "5", "6", "7", "8" }));
    }

    private IEnumerable<object> ProcessRule(KMBombModule module)
    {
        var comp = GetComponent(module, "TheRuleScript");
        var fldRuleNum = GetIntField(comp, "ruleNumber");

        yield return null;

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Rule);

        var number = fldRuleNum.Get();
        addQuestion(module, Question.RuleNumber, correctAnswers: new[] { number.ToString() });
    }

    private IEnumerable<object> ProcessScavengerHunt(KMBombModule module)
    {
        var comp = GetComponent(module, "scavengerHunt");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldKeySquare = GetIntField(comp, "keySquare");
        var fldRelTiles = GetArrayField<int>(comp, "relTiles");    // Coordinates of the color that the user needed
        var fldDecoyTiles = GetArrayField<int>(comp, "decoyTiles");    // Coordinates of the other colors
        var fldColorIndex = GetIntField(comp, "colorIndex");  // Which color is the ‘relTiles’ color

        var keySquare = fldKeySquare.Get();
        if (keySquare < 0 || keySquare >= 16)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Scavenger Hunt because ‘keySquare’ has value {1} (expected 0–15).", _moduleId, keySquare);
            yield break;
        }

        var relTiles = fldRelTiles.Get();
        if (relTiles == null)
            yield break;
        if (relTiles.Length != 2 && relTiles.Any(v => v < 0 || v >= 16))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Scavenger Hunt because ‘relTiles’ has unexpected value: [{1}] (expected 2 values in the range 0–15).", _moduleId, relTiles.JoinString(", "));
            yield break;
        }

        var decoyTiles = fldDecoyTiles.Get();
        if (decoyTiles == null)
            yield break;
        if (decoyTiles.Length != 4 && decoyTiles.Any(v => v < 0 || v >= 16))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Scavenger Hunt because ‘decoyTiles’ has unexpected value: [{1}] (expected 4 values in the range 0–15).", _moduleId, decoyTiles.JoinString(", "));
            yield break;
        }

        var colorIndex = fldColorIndex.Get();
        if (colorIndex < 0 || colorIndex >= 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Scavenger Hunt because ‘colorIndex’ has unexpected value: {1} (expected 0–2).", _moduleId, colorIndex);
            yield break;
        }

        // 0 = red, 1 = green, 2 = blue
        var redTiles = colorIndex == 0 ? relTiles : decoyTiles.Take(2).ToArray();
        var greenTiles = colorIndex == 1 ? relTiles : colorIndex == 0 ? decoyTiles.Take(2).ToArray() : decoyTiles.Skip(2).ToArray();
        var blueTiles = colorIndex == 2 ? relTiles : decoyTiles.Skip(2).ToArray();

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ScavengerHunt);

        var tileNames = new[] { "A1", "B1", "C1", "D1", "A2", "B2", "C2", "D2", "A3", "B3", "C3", "D3", "A4", "B4", "C4", "D4" };
        var qs = new List<QandA>();
        qs.Add(makeQuestion(Question.ScavengerHuntKeySquare, _ScavengerHunt, correctAnswers: new[] { Tiles4x4Sprites.First(k => k.name == tileNames[keySquare]) }, preferredWrongAnswers: Tiles4x4Sprites));
        qs.Add(makeQuestion(Question.ScavengerHuntColoredTiles, _ScavengerHunt, formatArgs: new[] { "red" }, correctAnswers: redTiles.Select(c => Tiles4x4Sprites.First(k => k.name == tileNames[c])).ToArray(), preferredWrongAnswers: Tiles4x4Sprites));
        qs.Add(makeQuestion(Question.ScavengerHuntColoredTiles, _ScavengerHunt, formatArgs: new[] { "green" }, correctAnswers: greenTiles.Select(c => Tiles4x4Sprites.First(k => k.name == tileNames[c])).ToArray(), preferredWrongAnswers: Tiles4x4Sprites));
        qs.Add(makeQuestion(Question.ScavengerHuntColoredTiles, _ScavengerHunt, formatArgs: new[] { "blue" }, correctAnswers: blueTiles.Select(c => Tiles4x4Sprites.First(k => k.name == tileNames[c])).ToArray(), preferredWrongAnswers: Tiles4x4Sprites));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSchlagDenBomb(KMBombModule module)
    {
        var comp = GetComponent(module, "qSchlagDenBomb");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldContestant = GetField<string>(comp, "contestantName");
        var fldContScore = GetIntField(comp, "scoreC");
        var fldBombScore = GetIntField(comp, "scoreB");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SchlagDenBomb);

        var contestant = fldContestant.Get();
        if (contestant == null)
            yield break;

        var cScore = fldContScore.Get();
        var bScore = fldBombScore.Get();
        if (cScore > 75 || cScore < 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Schlag den Bomb because the contestant’s score was {1} when it should have been from 0 to 75.", _moduleId, cScore);
            yield break;
        }
        if (bScore > 75 || bScore < 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Schlag den Bomb because the bomb’s score was {1} when it should have been from 0 to 75.", _moduleId, bScore);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.SchlagDenBombContestantName, _SchlagDenBomb, correctAnswers: new[] { contestant }),
            makeQuestion(Question.SchlagDenBombContestantScore, _SchlagDenBomb, correctAnswers: new[] { cScore.ToString() }, preferredWrongAnswers:
               Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(0, 75).ToString()).Distinct().Take(6).ToArray()),
            makeQuestion(Question.SchlagDenBombBombScore, _SchlagDenBomb, correctAnswers: new[] { bScore.ToString() }, preferredWrongAnswers:
               Enumerable.Range(0, int.MaxValue).Select(i => Rnd.Range(0, 75).ToString()).Distinct().Take(6).ToArray()));
    }

    private IEnumerable<object> ProcessSeaShells(KMBombModule module)
    {
        var comp = GetComponent(module, "SeaShellsModule");
        var fldRow = GetIntField(comp, "row");
        var fldCol = GetIntField(comp, "col");
        var fldKeynum = GetIntField(comp, "keynum");
        var fldStage = GetIntField(comp, "stage");
        var fldSolved = GetField<bool>(comp, "isPassed");
        var fldDisplay = GetField<TextMesh>(comp, "Display", isPublic: true);

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var rows = new int[3];
        var cols = new int[3];
        var keynums = new int[3];
        while (true)
        {
            while (fldDisplay.Get().text == " ")
            {
                yield return new WaitForSeconds(.1f);
                if (fldSolved.Get())
                    goto solved;
            }

            var stage = fldStage.Get();
            if (stage < 0 || stage > 2)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Sea Shells because ‘stage’ has unexpected value (expected 0-2): {1}", _moduleId, stage);
                yield break;
            }
            rows[stage] = fldRow.Get();
            cols[stage] = fldCol.Get();
            keynums[stage] = fldKeynum.Get();

            while (fldDisplay.Get().text != " ")
            {
                yield return new WaitForSeconds(.1f);
                if (fldSolved.Get())
                    goto solved;
            }
        }

        solved:
        _modulesSolved.IncSafe(_SeaShells);

        var qs = new List<QandA>();
        for (int i = 0; i < 3; i++)
        {
            qs.Add(makeQuestion(Question.SeaShells1, _SeaShells, new[] { ordinal(i + 1) }, new[] { new[] { "she sells", "she shells", "sea shells", "sea sells" }[rows[i]] }));
            qs.Add(makeQuestion(Question.SeaShells2, _SeaShells, new[] { ordinal(i + 1) }, new[] { new[] { "sea shells", "she shells", "sea sells", "she sells" }[cols[i]] }));
            qs.Add(makeQuestion(Question.SeaShells3, _SeaShells, new[] { ordinal(i + 1) }, new[] { new[] { "sea shore", "she sore", "she sure", "seesaw" }[keynums[i]] }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSemamorse(KMBombModule module)
    {
        var comp = GetComponent(module, "semamorse");
        var fldDisplayedLetters = GetArrayField<int[]>(comp, "displayedLetters");
        var fldDisplayedColors = GetArrayField<int>(comp, "displayedColors");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Semamorse);

        var letters = fldDisplayedLetters.Get();
        if (letters == null)
            yield break;
        if (letters.Length != 2 || letters.Any(arr => arr == null || arr.Length != 5 || arr.Any(v => v < 0 || v >= 26)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Semamorse because ‘displayedLetters’ has unexpected values: [{1}]", _moduleId, letters.Select(arr => string.Format(@"[{0}]", arr.JoinString(", "))).JoinString("; "));
            yield break;
        }
        var colorNames = new string[] { "red", "green", "cyan", "indigo", "pink" };
        var colors = fldDisplayedColors.Get();
        if (colors == null)
            yield break;
        if (colors.Length != 5 || colors.Any(c => c < 0 || c >= colorNames.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Semamorse because ‘displayedColors’ has unexpected values: [{1}]", _moduleId, colors.JoinString(", "));
            yield break;
        }

        var qs = new List<QandA>();
        for (var dispIx = 0; dispIx < 5; dispIx++)
        {
            qs.Add(makeQuestion(Question.SemamorseColors, _Semamorse, formatArgs: new[] { ordinal(dispIx + 1) }, correctAnswers: new[] { colorNames[colors[dispIx]] }));
            qs.Add(makeQuestion(Question.SemamorseLetters, _Semamorse, formatArgs: new[] { ordinal(dispIx + 1), "Semaphore" }, correctAnswers: new[] { ((char) ('A' + letters[0][dispIx])).ToString() }));
            qs.Add(makeQuestion(Question.SemamorseLetters, _Semamorse, formatArgs: new[] { ordinal(dispIx + 1), "Morse" }, correctAnswers: new[] { ((char) ('A' + letters[1][dispIx])).ToString() }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessShapesAndBombs(KMBombModule module)
    {
        var comp = GetComponent(module, "ShapesBombs");
        var fldLetter = GetIntField(comp, "selectLetter");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        yield return null;

        var initialLetter = fldLetter.Get();
        if (initialLetter < 0 || initialLetter > 14)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Shapes And Bombs because ‘initialLetter’ has unexpected value (expected 0-14): {1}", _moduleId, initialLetter);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_ShapesBombs);
        var letterChars = new[] { "A", "B", "D", "E", "G", "I", "K", "L", "N", "O", "P", "S", "T", "X", "Y" };
        addQuestion(module, Question.ShapesAndBombsInitialLetter, correctAnswers: new[] { letterChars[initialLetter] });
    }

    private IEnumerable<object> ProcessShapeShift(KMBombModule module)
    {
        var comp = GetComponent(module, "ShapeShiftModule");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldStartL = GetIntField(comp, "startL");
        var fldStartR = GetIntField(comp, "startR");
        var fldSolutionL = GetIntField(comp, "solutionL");
        var fldSolutionR = GetIntField(comp, "solutionR");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ShapeShift);

        var stL = fldStartL.Get();
        var stR = fldStartR.Get();
        var solL = fldSolutionL.Get();
        var solR = fldSolutionR.Get();
        var answers = new HashSet<string>();
        for (int l = 0; l < 4; l++)
            if (stL != solL || l == stL)
                for (int r = 0; r < 4; r++)
                    if (stR != solR || r == stR)
                        answers.Add(((char) ('A' + r + (4 * l))).ToString());
        if (answers.Count < 4)
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Shape Shift because the answer was the same as the initial state.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
        }
        else
            addQuestion(module, Question.ShapeShiftInitialShape, correctAnswers: new[] { ((char) ('A' + stR + (4 * stL))).ToString() }, preferredWrongAnswers: answers.ToArray());
    }

    private IEnumerable<object> ProcessShellGame(KMBombModule module)
    {
        var comp = GetComponent(module, "shellGame");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldInitialCup = GetIntField(comp, "startingCup");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_ShellGame);

        int initialCup = fldInitialCup.Get();

        if (initialCup < 0 || initialCup > 2)
        {
            Debug.LogFormat("<Souvenir {0}> Abandoning Shell Game because 'initialCup' has an unexpected value (expected 0 - 2): {1}", _moduleId, initialCup);
            yield break;
        }

        string[] position = new string[3] { "Left", "Middle", "Right" };

        addQuestions(module, makeQuestion(Question.ShellGameStartingCup, _ShellGame, correctAnswers: new[] { position[initialCup] }));
    }

    private IEnumerable<object> ProcessSillySlots(KMBombModule module)
    {
        var comp = GetComponent(module, "SillySlots");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldPrevSlots = GetField<IList>(comp, "mPreviousSlots");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_SillySlots);

        var prevSlots = fldPrevSlots.Get();
        if (prevSlots == null)
            yield break;
        if (prevSlots.Count < 2)
        {
            // Legitimate: first stage was a keep already
            Debug.LogFormat("[Souvenir #{0}] No question for Silly Slots because there was only one stage.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        if (prevSlots.Cast<object>().Any(obj => !(obj is Array) || ((Array) obj).Length != 3))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Silly Slots because prevSlots {1}.",
                _moduleId,
                prevSlots == null ? "is null" :
                prevSlots.Count == 0 ? "has length 0" :
                string.Format("has an unexpected item (expected arrays of length 3): [{0}]", prevSlots.Cast<object>().Select(obj => obj == null ? "<null>" : !(obj is Array) ? string.Format("<{0}>", obj.GetType().FullName) : string.Format("<Array, length={0}>", ((Array) obj).Length)).JoinString(", ")));
            yield break;
        }

        var testSlot = ((Array) prevSlots[0]).GetValue(0);
        var fldShape = GetField<object>(testSlot, "shape", isPublic: true);
        var fldColor = GetField<object>(testSlot, "color", isPublic: true);
        if (fldShape == null || fldColor == null)
            yield break;

        var qs = new List<QandA>();
        // Skip the last stage because if the last action was Keep, it is still visible on the module
        for (int stage = 0; stage < prevSlots.Count - 1; stage++)
        {
            var slotStrings = ((Array) prevSlots[stage]).Cast<object>().Select(obj => (fldColor.GetFrom(obj).ToString() + " " + fldShape.GetFrom(obj).ToString()).ToLowerInvariant()).ToArray();
            for (int slot = 0; slot < slotStrings.Length; slot++)
                qs.Add(makeQuestion(Question.SillySlots, _SillySlots, new[] { ordinal(slot + 1), ordinal(stage + 1) }, new[] { slotStrings[slot] }, slotStrings));
        }
        addQuestions(module, qs);
    }

    private static readonly string[] _simonSamplesFAs = new[] { "played in the first stage", "added in the second stage", "added in the third stage" };
    private IEnumerable<object> ProcessSimonSamples(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSamples");
        var fldCalls = GetField<List<string>>(comp, "_calls");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSamples);

        var calls = fldCalls.Get();
        if (calls == null || calls.Count != 3 || Enumerable.Range(1, 2).Any(i => calls[i].Length <= calls[i - 1].Length || !calls[i].StartsWith(calls[i - 1])))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Samples because _calls={1} (expected length 3 and expected each element to start with the previous).", _moduleId, calls == null ? "<null>" : string.Format("[{0}]", calls.Select(c => string.Format(@"""{0}""", c)).JoinString(", ")));
            yield break;
        }

        addQuestions(module, calls.Select((c, ix) => makeQuestion(Question.SimonSamplesSamples, _SimonSamples, new[] { _simonSamplesFAs[ix] }, new[] { (ix == 0 ? c : c.Substring(calls[ix - 1].Length)).Replace("0", "K").Replace("1", "S").Replace("2", "H").Replace("3", "O") })));
    }

    private IEnumerable<object> ProcessSimonSays(KMBombModule module)
    {
        var component = GetComponent(module, "SimonComponent");
        var fldSolved = GetField<bool>(component, "IsSolved", true);
        var fldSequence = GetArrayField<int>(component, "currentSequence");
        if (fldSolved == null || fldSequence == null)
            yield break;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSays);

        var sequence = fldSequence.Get();
        var i = Rnd.Range(0, sequence.Length);
        string answer;
        switch (sequence[i])
        {
            case 0: answer = "red"; break;
            case 1: answer = "blue"; break;
            case 2: answer = "green"; break;
            case 3: answer = "yellow"; break;
            default: Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Says because currentSequence item is out of range ({1}).", _moduleId, sequence[i]); yield break;
        }
        addQuestion(module, Question.SimonSaysFlash, new[] { ordinal(i + 1) }, new[] { answer });
    }

    private IEnumerable<object> ProcessSimonScrambles(KMBombModule module)
    {
        var comp = GetComponent(module, "simonScramblesScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldSequence = GetArrayField<int>(comp, "sequence");
        var fldColors = GetArrayField<string>(comp, "colorNames");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonScrambles);

        int[] sequence = fldSequence.Get();
        string[] colors = fldColors.Get();

        if (sequence == null || colors == null)
            yield break;
        if (sequence.Length != 10)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Scrambles because 'sequence' length is {1} (expected 10).", _moduleId, sequence.Length);
            yield break;
        }
        if (colors.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Scrambles because 'colors' length is {1} (expected 4).", _moduleId, colors.Length);
            yield break;
        }
        if (sequence[9] < 0 || sequence[9] >= colors.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Scrambles because 'sequence[9]' points to illegal color: {1} (expected 0-3).", _moduleId, sequence[9]);
            yield break;
        }

        addQuestion(module, Question.SimonScramblesLastColor, correctAnswers: new[] { colors[sequence[9]] });
    }

    private IEnumerable<object> ProcessSimonScreams(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonScreamsModule");
        var fldSequences = GetArrayField<int[]>(comp, "_sequences");
        var fldColors = GetField<Array>(comp, "_colors");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldRowCriteria = GetField<Array>(comp, "_rowCriteria");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_SimonScreams);

        var seqs = fldSequences.Get();
        var colorsRaw = fldColors.Get();
        var rules = fldRowCriteria.Get();
        if (seqs == null || colorsRaw == null || fldRowCriteria == null)
            yield break;
        // colorsRaw contains enum values; stringify them.
        var colors = colorsRaw.Cast<object>().Select(obj => obj.ToString()).ToArray();

        if (seqs.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Screams because _sequences length is {1} (expected 3).", _moduleId, seqs.Length);
            yield break;
        }
        if (colors.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Screams because _colors has length {1} (expected 6).", _moduleId, colors.Length);
            yield break;
        }
        if (rules.Length != 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Screams because _rowCriteria has length {1} (expected 6).", _moduleId, rules.Length);
            yield break;
        }

        var qs = new List<QandA>();
        var lastSeq = seqs.Last();
        for (int i = 0; i < lastSeq.Length; i++)
            qs.Add(makeQuestion(Question.SimonScreamsFlashing, _SimonScreams, new[] { ordinal(i + 1) }, new[] { colors[lastSeq[i]] }));

        // First determine which rule applied in which stage
        var fldCheck = GetField<Func<int[], bool>>(rules.GetValue(0), "Check", isPublic: true);
        var fldRuleName = GetField<string>(rules.GetValue(0), "Name", isPublic: true);
        if (fldCheck == null || fldRuleName == null)
            yield break;
        var stageRules = new int[seqs.Length];
        for (int i = 0; i < seqs.Length; i++)
        {
            stageRules[i] = rules.Cast<object>().IndexOf(rule => fldCheck.GetFrom(rule)(seqs[i]));
            if (stageRules[i] == -1)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Screams because apparently none of the criteria applies to Stage {1} ({2}).", _moduleId, i + 1, seqs[i].Select(ix => colors[ix]).JoinString(", "));
                yield break;
            }
        }

        // Now set the questions
        // Skip the last rule because it’s the “otherwise” row
        for (int rule = 0; rule < rules.Length - 1; rule++)
        {
            var applicableStages = new List<string>();
            for (int stage = 0; stage < stageRules.Length; stage++)
                if (stageRules[stage] == rule)
                    applicableStages.Add(ordinal(stage + 1));
            if (applicableStages.Count > 0)
                qs.Add(makeQuestion(Question.SimonScreamsRule, _SimonScreams,
                    new[] { fldRuleName.GetFrom(rules.GetValue(rule)) },
                    new[] { applicableStages.Count == stageRules.Length ? "all of them" : applicableStages.JoinString(", ", lastSeparator: " and ") },
                    applicableStages.Count == 1
                        ? Enumerable.Range(1, seqs.Length).Select(i => ordinal(i)).ToArray()
                        : Enumerable.Range(1, seqs.Length).SelectMany(a => Enumerable.Range(a + 1, seqs.Length - a).Select(b => ordinal(a) + " and " + ordinal(b))).Concat(new[] { "all of them" }).ToArray()));
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonSelects(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSelectsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldStg1order = GetArrayField<int>(comp, "stg1order");
        var fldStg2order = GetArrayField<int>(comp, "stg2order");
        var fldStg3order = GetArrayField<int>(comp, "stg3order");
        var fldButtonrend = GetArrayField<Renderer>(comp, "buttonrend", isPublic: true);

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSelects);

        var order = new int[3][];

        order[0] = fldStg1order.Get();
        order[1] = fldStg2order.Get();
        order[2] = fldStg3order.Get();

        if (order == null)
            yield break;

        if (order.Any(flashes => flashes == null || flashes.Length < 3 || flashes.Length > 5))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Selects because one of ‘stg1order’/‘stg2order’/‘stg3order’ is null or has an unexpected length: [{1}], expected length 3-5.",
                _moduleId, order.Select(flashes => flashes == null ? "<null>" : "length " + flashes.Length).JoinString(", "));
            yield break;
        }

        var btnRenderers = fldButtonrend.Get();

        if (btnRenderers == null)
            yield break;

        if (btnRenderers.Length != 8)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Selects because ‘buttonrend’ has an unexpected length: {1}, expected length 8.", _moduleId, btnRenderers.Length);
            yield break;
        }

        // Sequences of colors that flashes in each stage
        var seqs = new string[3][];

        // Parsing the received string
        for (int stage = 0; stage < 3; stage++)
        {
            var parsedString = new string[order[stage].Length];
            for (int flash = 0; flash < order[stage].Length; flash++)
                parsedString[flash] = btnRenderers[order[stage][flash]].material.name.Replace(" (Instance)", "");
            seqs[stage] = parsedString;
        }

        // Used to validate colors
        string[] colorNames = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Magenta", "Cyan" };

        if (seqs.Any(seq => seq.Any(color => !colorNames.Contains(color))))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Selects because ‘colors’ contains an invalid color: [{1}]", _moduleId, seqs.Select(seq => seq.JoinString(", ")).JoinString("; "));
            yield break;
        }

        addQuestions(module, seqs.SelectMany((seq, stage) => seq.Select((col, ix) => makeQuestion(Question.SimonSelectsOrder, _SimonSelects,
            formatArgs: new[] { ordinal(ix + 1), ordinal(stage + 1) },
            correctAnswers: new[] { col }))));
    }

    private static readonly string[] _SimonSends_Morse = { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--.." };

    private IEnumerable<object> ProcessSimonSends(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSendsModule");
        var fldAnswerSoFar = GetField<List<int>>(comp, "_answerSoFar");
        var fldMorseR = GetField<string>(comp, "_morseR");
        var fldMorseG = GetField<string>(comp, "_morseG");
        var fldMorseB = GetField<string>(comp, "_morseB");

        yield return null;

        var morseR = fldMorseR.Get();
        var morseG = fldMorseG.Get();
        var morseB = fldMorseB.Get();

        if (morseR == null || morseG == null || morseB == null)
            yield break;

        var charR = ((char) ('A' + Array.IndexOf(_SimonSends_Morse, morseR.Replace("###", "-").Replace("#", ".").Replace("_", "")))).ToString();
        var charG = ((char) ('A' + Array.IndexOf(_SimonSends_Morse, morseG.Replace("###", "-").Replace("#", ".").Replace("_", "")))).ToString();
        var charB = ((char) ('A' + Array.IndexOf(_SimonSends_Morse, morseB.Replace("###", "-").Replace("#", ".").Replace("_", "")))).ToString();

        // Simon Sends sets “_answerSoFar” to null when it’s done
        while (fldAnswerSoFar.Get(nullAllowed: true) != null)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_SimonSends);
        addQuestions(module,
            makeQuestion(Question.SimonSendsReceivedLetters, _SimonSends, new[] { "red" }, new[] { charR }, new[] { charG, charB }),
            makeQuestion(Question.SimonSendsReceivedLetters, _SimonSends, new[] { "green" }, new[] { charG }, new[] { charR, charB }),
            makeQuestion(Question.SimonSendsReceivedLetters, _SimonSends, new[] { "blue" }, new[] { charB }, new[] { charR, charG }));
    }

    private IEnumerable<object> ProcessSimonShrieks(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonShrieksModule");
        var fldArrow = GetIntField(comp, "_arrow");
        var fldButtonColors = GetArrayField<int>(comp, "_buttonColors");
        var fldFlashingButtons = GetArrayField<int>(comp, "_flashingButtons");
        var fldStage = GetIntField(comp, "_stage");

        while (fldStage.Get() < 3)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonShrieks);

        var arrow = fldArrow.Get();
        var flashingButtons = fldFlashingButtons.Get();

        if (arrow < 0 || arrow > 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Shrieks because ‘_arrow’ has an unexpected value ({1}, expected 0–6).", _moduleId, arrow);
            yield break;
        }
        if (flashingButtons == null)
            yield break;
        if (flashingButtons.Length != 8 || flashingButtons.Any(b => b < 0 || b > 6))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Shrieks because ‘_flashingButtons’ has an unexpected length or value: [{1}], expected length 8 and values 0–6.",
                _moduleId, flashingButtons.JoinString(", "));
            yield break;
        }

        var qs = new List<QandA>();
        for (int i = 0; i < flashingButtons.Length; i++)
            qs.Add(makeQuestion(Question.SimonShrieksFlashingButton, _SimonShrieks, formatArgs: new[] { ordinal(i + 1) }, correctAnswers: new[] { ((flashingButtons[i] + 7 - arrow) % 7).ToString() }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonSimons(KMBombModule module)
    {
        var comp = GetComponent(module, "simonsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldSelButtons = GetArrayField<KMSelectable>(comp, "selButtons");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSimons);

        var buttonFlashes = fldSelButtons.Get();
        if (buttonFlashes == null)
            yield break;
        if (buttonFlashes.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Simons because ‘selButtons’ has an unexpected length (expected 5): {1}", _moduleId, buttonFlashes.Length);
            yield break;
        }

        var flashes = new[] { "TR", "TY", "TG", "TB", "LR", "LY", "LG", "LB", "RR", "RY", "RG", "RB", "BR", "BY", "BG", "BB" };

        for (int i = 0; i < 5; i++)
        {
            if (!flashes.Contains(buttonFlashes[i].name.ToUpperInvariant()))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Simons because one of the button flash #{1} is not valid: {2}", _moduleId, i, buttonFlashes[i].name.ToUpperInvariant());
                yield break;
            }
        }
        var qs = new List<QandA>();
        for (int i = 0; i < 5; i++)
            qs.Add(makeQuestion(Question.SimonSimonsFlashingColors, _SimonSimons, formatArgs: new[] { ordinal(i + 1) }, correctAnswers: new[] { buttonFlashes[i].name.ToUpperInvariant() }));
        addQuestions(module, qs);
    }

    private static readonly string[] _SimonSings_Notes = { "C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B" };

    private IEnumerable<object> ProcessSimonSings(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSingsModule");
        var fldCurStage = GetIntField(comp, "_curStage");
        var fldFlashingColors = GetArrayField<int[]>(comp, "_flashingColors");

        while (fldCurStage.Get() < 3)
            yield return new WaitForSeconds(.1f);

        var flashingColorSequences = fldFlashingColors.Get();
        if (flashingColorSequences == null || flashingColorSequences.Length != 3 || flashingColorSequences.Any(seq => seq.Any(col => col < 0 || col >= _SimonSings_Notes.Length)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Sings because one of the flashing “colors” is out of range (values from 0–11 expected): [{1}].", _moduleId, flashingColorSequences.Select(seq => string.Format("[{0}]", seq.JoinString(", "))).JoinString("; "));
            yield break;
        }

        _modulesSolved.IncSafe(_SimonSings);
        addQuestions(module, flashingColorSequences.SelectMany((seq, stage) => seq.Select((col, ix) => makeQuestion(Question.SimonSingsFlashing, _SimonSings, new[] { ordinal(ix + 1), ordinal(stage + 1) }, new[] { _SimonSings_Notes[col] }))));
    }

    private IEnumerable<object> ProcessSimonSounds(KMBombModule module)
    {
        var comp = GetComponent(module, "simonSoundsScript");
        var fldFlashedColors = GetField<List<int>[]>(comp, "stage");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSounds);

        var colorNames = new[] { "red", "blue", "yellow", "green" };
        var flashed = fldFlashedColors.Get();
        if (flashed == null)
            yield break;
        if (flashed.Contains(null) || flashed.Any(list => list.Last() < 0 || list.Last() >= colorNames.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Sounds because ‘stage’ contains a null value or an invalid color: [{1}].", _moduleId,
                flashed.Select(v => v == null ? "NULL" : string.Format("[{0}]", v.JoinString(", "))).JoinString("; "));
            yield break;
        }

        var qs = new List<QandA>();
        for (var stage = 0; stage < flashed.Length; stage++)
            qs.Add(makeQuestion(Question.SimonSoundsFlashingColors, _SimonSounds, formatArgs: new[] { ordinal(stage + 1) }, correctAnswers: new[] { colorNames[flashed[stage].Last()] }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonSpeaks(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonSpeaksModule");
        var fldSequence = GetArrayField<int>(comp, "_sequence");
        var fldColors = GetArrayField<int>(comp, "_colors");
        var fldWords = GetArrayField<int>(comp, "_words");
        var fldLanguages = GetArrayField<int>(comp, "_languages");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldWordsTable = GetStaticField<string[][]>(comp.GetType(), "_wordsTable");
        var fldPositionNames = GetStaticField<string[]>(comp.GetType(), "_positionNames");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonSpeaks);

        var sequence = fldSequence.Get();
        var colors = fldColors.Get();
        var words = fldWords.Get();
        var languages = fldLanguages.Get();
        var wordsTable = fldWordsTable.Get();
        var positionNames = fldPositionNames.Get();
        if (sequence == null || colors == null || words == null || languages == null || wordsTable == null || positionNames == null)
            yield break;
        if (colors.Length != 9 || words.Length != 9 || languages.Length != 9 || wordsTable.Length != 9 || positionNames.Length != 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Speaks because one of “_colors” ({1})/“_words” ({2})/“_languages” ({3})/“_wordsTable” ({4})/“_positionNames” ({5}) is not of length 9.", _moduleId, colors.Length, words.Length, languages.Length, wordsTable.Length, positionNames.Length);
            yield break;
        }
        if (sequence.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Speaks because “_sequence” is of length {1} instead of 5.", _moduleId, sequence.Length);
            yield break;
        }

        addQuestions(module,
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.SimonSpeaksPositions, _SimonSpeaks, new[] { ordinal(ix + 1) }, new[] { positionNames[sequence[ix]] })).Concat(
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.SimonSpeaksColors, _SimonSpeaks, new[] { ordinal(ix + 1) }, new[] { wordsTable[colors[sequence[ix]]][0] })).Concat(
            Enumerable.Range(0, 5).Select(ix => makeQuestion(Question.SimonSpeaksWords, _SimonSpeaks, new[] { ordinal(ix + 1) }, new[] { wordsTable[words[sequence[ix]]][languages[sequence[ix]]] })))));
    }

    private IEnumerable<object> ProcessSimonsStar(KMBombModule module)
    {
        var comp = GetComponent(module, "simonsStarScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldFlashes = "first,second,third,fourth,fifth".Split(',').Select(n => GetField<string>(comp, n + "FlashColour", isPublic: true)).ToArray();

        yield return null;

        var flashes = fldFlashes.Select(f => f.Get()).ToArray();
        var validColors = new[] { "red", "yellow", "green", "blue", "purple" };

        if (flashes.Any(f => !validColors.Contains(f)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon’s Star because one of the flashes has an unexpected value: [{1}] (expected red, green, yellow, blue, or purple).", _moduleId, flashes.JoinString(", ", @"""", @""""));
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonsStar);

        addQuestions(module, flashes.Select((f, ix) => makeQuestion(Question.SimonsStarColors, _SimonsStar, new[] { ordinal(ix + 1) }, new[] { f })));
    }

    private IEnumerable<object> ProcessSimonStates(KMBombModule module)
    {
        var comp = GetComponent(module, "AdvancedSimon");
        var fldPuzzleDisplay = GetArrayField<bool[]>(comp, "PuzzleDisplay");
        var fldAnswer = GetArrayField<int>(comp, "Answer");
        var fldProgress = GetIntField(comp, "Progress");

        bool[][] puzzleDisplay;
        while ((puzzleDisplay = fldPuzzleDisplay.Get(nullAllowed: true)) == null)
            yield return new WaitForSeconds(.1f);

        if (puzzleDisplay.Length != 4 || puzzleDisplay.Any(arr => arr.Length != 4))
        {
            Debug.LogFormat("<Souvenir #{1}> Abandoning Simon States because PuzzleDisplay has an unexpected length or value: [{0}]",
                puzzleDisplay.Select(arr => arr == null ? "null" : "[" + arr.JoinString(", ") + "]").JoinString("; "), _moduleId);
            yield break;
        }

        var colorNames = new[] { "Red", "Yellow", "Green", "Blue" };

        while (fldProgress.Get() < 4)
            yield return new WaitForSeconds(.1f);
        // Consistency check
        if (fldPuzzleDisplay.Get(nullAllowed: true) != null)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon States because PuzzleDisplay was expected to be null when Progress reached 4, but wasn’t.", _moduleId);
            yield break;
        }

        _modulesSolved.IncSafe(_SimonStates);

        var qs = new List<QandA>();
        for (int i = 0; i < 4; i++)
        {
            var c = puzzleDisplay[i].Count(b => b);
            if (c != 3)
                qs.Add(makeQuestion(Question.SimonStatesDisplay, _SimonStates,
                    new[] { "color(s) flashed", ordinal(i + 1) },
                    new[] { c == 4 ? "all 4" : puzzleDisplay[i].Select((v, j) => v ? colorNames[j] : null).Where(x => x != null).JoinString(", ") }));
            if (c != 1)
                qs.Add(makeQuestion(Question.SimonStatesDisplay, _SimonStates,
                    new[] { "color(s) didn’t flash", ordinal(i + 1) },
                    new[] { c == 4 ? "none" : puzzleDisplay[i].Select((v, j) => v ? null : colorNames[j]).Where(x => x != null).JoinString(", ") }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSimonStops(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonStops");
        var fldColors = GetArrayField<string>(comp, "outputSequence");
        var fldSolved = GetField<bool>(comp, "isSolved");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SimonStops);

        var colors = fldColors.Get();
        if (colors == null || colors.Length != 5)
        {
            Debug.LogFormat(@"<Souvenir #{0}> Abandoning Simon Stops because the sequence is [{1}], which is {2}, when we expected five colors.", _moduleId,
                colors == null ? "null" : string.Format("[{0}]", colors.JoinString(", ")), colors == null ? "null" : colors.Length + " colors");
            yield break;
        }

        addQuestions(module, Enumerable.Range(0, 5).Select(ix =>
             makeQuestion(Question.SimonStopsColors, _SimonStops, new[] { ordinal(ix + 1) }, new[] { colors[ix] }, colors)));
    }

    private IEnumerable<object> ProcessSimonStores(KMBombModule module)
    {
        var comp = GetComponent(module, "SimonStoresScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldFlashingColours = GetField<List<string>>(comp, "flashingColours");
        var fldAnswer = GetArrayField<int[]>(comp, "step");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_SimonStores);

        var flashSequences = fldFlashingColours.Get();

        if (flashSequences == null || flashSequences.Any(flash => flash == null))
            yield break;

        var colors = "RGBCMY";

        foreach (var flash in flashSequences)
        {
            var set = new HashSet<char>();
            if (flash.Any(color => !set.Add(color) || !colors.Contains(color)) || flash.Length < 1 || flash.Length > 3)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Stores because 'flashingColours' contains value with duplicated colors, invalid color, or unexpected length (expected: 1-3): [flash: {1}, length: {2}]",
                    _moduleId, flash, flash.Length);
                yield break;
            }
        }

        var correctAnswers = fldAnswer.Get();
        if (correctAnswers == null || correctAnswers.Any(answer => answer == null))
            yield break;

        if (correctAnswers.Length != 3 || correctAnswers.Any(answer => answer.Length != 6))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Stores because 'step' or its elements has an unexpected length (expected: 1-3 and 1-6): {1}, [{2}]", _moduleId, correctAnswers.Length, correctAnswers.Select(seq => seq.Length).JoinString(", "));
            yield break;
        }

        int[] integerAnswers = new int[3];
        integerAnswers[0] = correctAnswers[0][3];
        integerAnswers[1] = correctAnswers[1][4];
        integerAnswers[2] = correctAnswers[2][5];

        if (integerAnswers.Any(answer => answer <= -365 || answer >= 365))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Simon Stores because 'step' contains an invalid answer to a stage (expected -364 to 364): [{1}]", _moduleId, integerAnswers.JoinString(", "));
            yield break;
        }

        var colorNames = new Dictionary<char, string> {
            { 'R', "Red" },
            { 'G', "Green" },
            { 'B', "Blue" },
            { 'C', "Cyan" },
            { 'M', "Magenta" },
            { 'Y', "Yellow" }
        };

        var qs = new List<QandA>();
        for (var i = 0; i < 5; i++)
            qs.Add(makeQuestion(Question.SimonStoresColors, _SimonStores,
                formatArgs: new[] { flashSequences[i].Length == 1 ? "flashed" : "was one of the colors flashed", ordinal(i + 1) },
                correctAnswers: flashSequences[i].Select(ch => colorNames[ch]).ToArray()));

        for (var i = 0; i < 3; i++)
            qs.Add(makeQuestion(Question.SimonStoresAnswers, _SimonStores,
                formatArgs: new[] { (i + 1).ToString() },
                correctAnswers: new[] { integerAnswers[i].ToString() }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSkewedSlots(KMBombModule module)
    {
        var comp = GetComponent(module, "SkewedModule");
        var fldNumbers = GetArrayField<int>(comp, "Numbers");
        var fldModuleActivated = GetField<bool>(comp, "moduleActivated");
        var fldSolved = GetField<bool>(comp, "solved");

        var originalNumbers = new List<string>();

        while (true)
        {
            // Skewed Slots sets moduleActivated to false while the slots are spinning.
            // If there was a correct answer, it will set solved to true, otherwise it will set moduleActivated to true.
            while (!fldModuleActivated.Get() && !fldSolved.Get())
                yield return new WaitForSeconds(.1f);

            if (fldSolved.Get())
                break;

            // Get the current original digits.
            var numbers = fldNumbers.Get();
            if (numbers == null)
                yield break;
            if (numbers.Length != 3 || numbers.Any(n => n < 0 || n > 9))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Skewed Slots because numbers has unexpected length (3) or a number outside expected range (0–9): [{1}].", _moduleId, numbers.JoinString(", "));
                yield break;
            }
            originalNumbers.Add(numbers.JoinString());

            // When the user presses anything, Skewed Slots sets moduleActivated to false while the slots are spinning.
            while (fldModuleActivated.Get())
                yield return new WaitForSeconds(.1f);
        }

        _modulesSolved.IncSafe(_SkewedSlots);
        addQuestion(module, Question.SkewedSlotsOriginalNumbers, correctAnswers: new[] { originalNumbers.Last() },
            preferredWrongAnswers: originalNumbers.Take(originalNumbers.Count - 1).ToArray());
    }

    private static readonly string[] _skyrimFieldNames = new[] { "race", "weapon", "enemy", "city" };
    private static readonly string[] _skyrimFieldNames2 = new[] { "correctRace", "correctWeapon", "correctEnemy", "correctCity" };
    private static readonly string[] _skyrimButtonNames = new[] { "cycleUp", "cycleDown", "accept", "submit", "race", "weapon", "enemy", "city", "shout" };
    private KMSelectable.OnInteractHandler getSkyrimButtonHandler(KMSelectable btn)
    {
        return delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn.transform);
            btn.AddInteractionPunch(.5f);
            return false;
        };
    }
    private IEnumerable<object> ProcessSkyrim(KMBombModule module)
    {
        var comp = GetComponent(module, "skyrimScript");
        var questions = new[] { Question.SkyrimRace, Question.SkyrimWeapon, Question.SkyrimEnemy, Question.SkyrimCity };
        var flds = _skyrimFieldNames.Select(name => GetField<List<Texture>>(comp, name + "Images", isPublic: true)).ToArray();
        var fldsCorrect = _skyrimFieldNames2.Select(name => GetField<Texture>(comp, name)).ToArray();
        var fldShoutNames = GetField<List<string>>(comp, "shoutNameOptions");
        var fldCorrectShoutName = GetField<string>(comp, "shoutName");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldsButtons = _skyrimButtonNames.Select(fieldName => GetField<KMSelectable>(comp, fieldName, isPublic: true)).ToArray();
        yield return null;
        while (!fldSolved.Get())
            // Usually we’d wait 0.1 seconds at a time, but in this case we need to know immediately so that we can hook the buttons
            yield return null;
        _modulesSolved.IncSafe(_Skyrim);

        var btns = fldsButtons.Select(b => b.Get()).ToArray();
        if (btns.Any(b => b == null))
            yield break;
        foreach (var btn in btns)
            btn.OnInteract = getSkyrimButtonHandler(btn);

        var qs = new List<QandA>();
        for (int i = 0; i < _skyrimFieldNames.Length; i++)
        {
            var list = flds[i].Get();
            if (list.Count != 3)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Skyrim because “{1}” array has unexpected length {2} (expected 3).", _moduleId, _skyrimFieldNames[i], list.Count);
                yield break;
            }
            var correct = fldsCorrect[i].Get();
            if (correct == null)
                yield break;
            qs.Add(makeQuestion(questions[i], _Skyrim, correctAnswers: list.Except(new[] { correct }).Select(t => t.name.Replace("'", "’")).ToArray()));
        }
        var shoutNames = fldShoutNames.Get();
        if (shoutNames.Count != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Skyrim because “shoutNameOptions” array has unexpected length {1} (expected 3).", _moduleId, shoutNames.Count);
            yield break;
        }
        var correctShoutName = fldCorrectShoutName.Get();
        if (correctShoutName == null)
            yield break;
        qs.Add(makeQuestion(Question.SkyrimDragonShout, _Skyrim, correctAnswers: shoutNames.Except(new[] { correctShoutName }).Select(n => n.Replace("'", "’")).ToArray()));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessSnooker(KMBombModule module)
    {
        var comp = GetComponent(module, "snookerScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldActiveReds = GetIntField(comp, "activeReds");

        yield return null;

        var activeReds = fldActiveReds.Get();
        if (activeReds < 8 || activeReds > 10)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Snooker because activeReds has an unexpected value: {1} (expected 8-10).", _moduleId, activeReds);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Snooker);

        addQuestion(module, Question.SnookerReds, correctAnswers: new[] { activeReds.ToString() });
    }

    private sealed class SonicPictureInfo { public string Name; public int Stage; }
    private IEnumerable<object> ProcessSonicTheHedgehog(KMBombModule module)
    {
        var comp = GetComponent(module, "sonicScript");
        var fldsButtonSounds = new[] { "boots", "invincible", "life", "rings" }.Select(name => GetField<string>(comp, name + "Press"));
        var fldsPics = Enumerable.Range(0, 3).Select(i => GetField<Texture>(comp, "pic" + (i + 1))).ToArray();
        var fldStage = GetIntField(comp, "stage");

        while (fldStage.Get() < 5)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_SonicTheHedgehog);

        var soundNameMapping =
            @"boss=Boss Theme;breathe=Breathe;continueSFX=Continue;drown=Drown;emerald=Emerald;extraLife=Extra Life;finalZone=Final Zone;invincibleSFX=Invincibility;jump=Jump;lamppost=Lamppost;marbleZone=Marble Zone;bumper=Bumper;skid=Skid;spikes=Spikes;spin=Spin;spring=Spring"
                .Split(';').Select(str => str.Split('=')).ToDictionary(ar => ar[0], ar => ar[1]);
        var pictureNameMapping =
            @"annoyedSonic=Annoyed Sonic=2;ballhog=Ballhog=1;blueLamppost=Blue Lamppost=3;burrobot=Burrobot=1;buzzBomber=Buzz Bomber=1;crabMeat=Crab Meat=1;deadSonic=Dead Sonic=2;drownedSonic=Drowned Sonic=2;fallingSonic=Falling Sonic=2;motoBug=Moto Bug=1;redLamppost=Red Lamppost=3;redSpring=Red Spring=3;standingSonic=Standing Sonic=2;switch=Switch=3;yellowSpring=Yellow Spring=3"
                .Split(';').Select(str => str.Split('=')).ToDictionary(ar => ar[0], ar => new SonicPictureInfo { Name = ar[1], Stage = int.Parse(ar[2]) - 1 });

        var pics = fldsPics.Select(f => f.Get()).ToArray();
        if (pics.Any(p => p == null || p.name == null || !pictureNameMapping.ContainsKey(p.name)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Sonic The Hedgehog because a pic was null or not recognized: [{1}]", _moduleId, pics.Select(p => p == null ? "<null>" : "\"" + p.name + "\"").JoinString(", "));
            yield break;
        }
        var sounds = fldsButtonSounds.Select(f => f.Get()).ToArray();
        if (sounds.Any(s => s == null || !soundNameMapping.ContainsKey(s)))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Sonic The Hedgehog because a sound was null: [{1}]", _moduleId, sounds.Select(s => s == null ? "<null>" : "\"" + s + "\"").JoinString(", "));
            yield break;
        }

        addQuestions(module,
            Enumerable.Range(0, 3).Select(i =>
                makeQuestion(
                    Question.SonicTheHedgehogPictures,
                    _SonicTheHedgehog,
                    new[] { ordinal(i + 1) },
                    new[] { pictureNameMapping[pics[i].name].Name },
                    pictureNameMapping.Values.Where(inf => inf.Stage == i).Select(inf => inf.Name).ToArray()))
            .Concat(new[] { "Running Boots", "Invincibility", "Extra Life", "Rings" }.Select((screenName, i) =>
                makeQuestion(
                    Question.SonicTheHedgehogSounds,
                    _SonicTheHedgehog,
                    new[] { screenName },
                    new[] { soundNameMapping[sounds[i]] },
                    sounds.Select(s => soundNameMapping[s]).ToArray()))));
    }

    private IEnumerable<object> ProcessSorting(KMBombModule module)
    {
        var comp = GetComponent(module, "Sorting");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldLastSwap = GetField<byte>(comp, "swapButtons");
        var fldSwapCount = GetField<byte>(comp, "swapIndex");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Sorting);

        var lastSwap = fldLastSwap.Get();
        if ((lastSwap % 10 == 0 || lastSwap % 10 > 5) || (lastSwap / 10 == 0 || lastSwap / 10 > 5) || lastSwap / 10 == lastSwap % 10)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Sorting because ‘swap’ has unexpected value (expected two digit number, each with a unique digit from 1-5): {1}", _moduleId, lastSwap);
            yield break;
        }

        string[] answers = new string[10];
        byte index = 0;

        for (int i = 1; i <= 4; i++)
        {
            for (int j = i + 1; j <= 5; j++)
            {
                answers[index] = string.Format("{0} & {1}", i, j);
                index++;
            }
        }

        string[] randomNumbers = new string[16];
        for (int i = 1; i <= randomNumbers.Length; i++)
        {
            randomNumbers[i - 1] = i.ToString();
        }

        addQuestions(module, makeQuestion(Question.SortingLastSwap, _Sorting, correctAnswers: new[] { fldLastSwap.Get().ToString().Insert(1, " & ") }, preferredWrongAnswers: answers));
    }

    private IEnumerable<object> ProcessSouvenir(KMBombModule module)
    {
        var comp = module.GetComponent<SouvenirModule>();
        if (comp == null || comp == this)
        {
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        yield return null;

        int souvenirCount;
        if (!_moduleCounts.TryGetValue(_Souvenir, out souvenirCount) || souvenirCount != 2)
        {
            if (souvenirCount > 2)
                Debug.LogFormat("[Souvenir #{0}] There are more than two Souvenir modules on this bomb. Not asking any questions about them.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            yield break;
        }

        // Prefer names of supported modules on the bomb other than Souvenir.
        IEnumerable<string> modules = supportedModuleNames.Except(new[] { "Souvenir" });
        if (supportedModuleNames.Count < 5)
        {
            // If there are less than 4 eligible modules, fill the remaining spaces with random other modules.
            var allModules = _attributes.Where(x => x.Value != null).Select(x => x.Value.ModuleNameWithThe).Distinct().ToList();
            modules = modules.Concat(Enumerable.Range(0, 1000).Select(i => allModules[Rnd.Range(0, allModules.Count)]).Except(supportedModuleNames).Take(5 - supportedModuleNames.Count));
        }
        while (comp._currentQuestion == null)
            yield return new WaitForSeconds(0.1f);

        var firstQuestion = comp._currentQuestion;
        var firstModule = firstQuestion.ModuleNameWithThe;

        // Wait for the user to solve that question before asking about it
        while (comp._currentQuestion == firstQuestion)
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_Souvenir);
        addQuestion(module, Question.SouvenirFirstQuestion, null, new[] { firstModule }, modules.ToArray());
    }

    private IEnumerable<object> ProcessSphere(KMBombModule module)
    {
        var comp = GetComponent(module, "theSphereScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldColorNames = GetArrayField<string>(comp, "colourNames", isPublic: true);
        var fldColors = GetArrayField<int>(comp, "selectedColourIndices", isPublic: true);

        string[] colorNames = fldColorNames.Get();
        int[] colors = fldColors.Get();

        if (colorNames == null || colors == null)
            yield break;

        if (colors.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Sphere because 'selectedColourIndices' has length {1}, but expected 5.", _moduleId, colors.Length);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Sphere);
        addQuestions(module,
            makeQuestion(Question.SphereColors, _Sphere, new[] { "first" }, new[] { colorNames[colors[0]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "second" }, new[] { colorNames[colors[1]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "third" }, new[] { colorNames[colors[2]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "fourth" }, new[] { colorNames[colors[3]] }),
            makeQuestion(Question.SphereColors, _Sphere, new[] { "fifth" }, new[] { colorNames[colors[4]] }));
    }

    private IEnumerable<object> ProcessSplittingTheLoot(KMBombModule module)
    {
        var comp = GetComponent(module, "SplittingTheLootScript");
        var fldSolved = GetField<bool>(comp, "isSolved");
        var fldBags = GetField<object>(comp, "bags");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        var bagsRaw = fldBags.Get();
        if (bagsRaw == null || !(bagsRaw is IList))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Splitting the Loot because 'bags' is {1} (expected something that implements IList).", _moduleId, bagsRaw == null ? "null" : bagsRaw.GetType().FullName);
            yield break;
        }

        var bags = (IList) bagsRaw;
        if (bags.Count != 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Splitting the Loot because 'bags' had unexpected length: {1} (expected 7).", _moduleId, bags.Count);
            yield break;
        }

        var fldBagColor = GetField<object>(bags[0], "Color");
        var fldBagLabel = GetField<string>(bags[0], "Label");

        if (fldBagColor == null || fldBagLabel == null)
            yield break;

        var bagColors = bags.Cast<object>().Select(obj => fldBagColor.GetFrom(obj)).ToArray();
        var bagNames = bags.Cast<object>().Select(obj => fldBagLabel.GetFrom(obj)).ToArray();
        var paintedBag = bagColors.IndexOf(bc => bc.ToString() != "Normal");
        if (paintedBag == -1)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Splitting the Loot because no colored bag was found: [{1}]", _moduleId, bagColors.JoinString(", "));
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_SplittingTheLoot);
        addQuestion(module, Question.SplittingTheLootColoredBag, correctAnswers: new[] { bagNames[paintedBag] }, preferredWrongAnswers: bagNames);
    }

    private IEnumerable<object> ProcessSwitch(KMBombModule module)
    {
        var comp = GetComponent(module, "Switch");
        var fldSolved = GetField<bool>(comp, "SOLVED");
        var fldBottomColor = GetIntField(comp, "BottomColor");
        var fldTopColor = GetIntField(comp, "TopColor");
        var fldSwitch = GetField<KMSelectable>(comp, "FlipperSelectable", isPublic: true);
        var fldFirstSuccess = GetField<bool>(comp, "FirstSuccess");

        yield return null;

        var colorNames = new[] { "red", "orange", "yellow", "green", "blue", "purple" };

        var topColor1 = fldTopColor.Get();
        var bottomColor1 = fldBottomColor.Get();
        var topColor2 = -1;
        var bottomColor2 = -1;

        Debug.LogFormat("<Souvenir #{0}> The Switch: initial colors: {1}/{2}", _moduleId, topColor1, bottomColor1);

        var switchSelectable = fldSwitch.Get();
        if (switchSelectable == null)
            yield break;

        var prevInteract = switchSelectable.OnInteract;
        switchSelectable.OnInteract = delegate
        {
            var ret = prevInteract();
            var firstSuccess = fldFirstSuccess.Get();
            if (!firstSuccess)  // This means the user got a strike. Need to retrieve the new colors
            {
                topColor1 = fldTopColor.Get();
                bottomColor1 = fldBottomColor.Get();
                Debug.LogFormat("<Souvenir #{0}> The Switch: Strike! Initial colors now: {1}/{2}", _moduleId, topColor1, bottomColor1);
            }
            else if (!fldSolved.Get())
            {
                topColor2 = fldTopColor.Get();
                bottomColor2 = fldBottomColor.Get();
                Debug.LogFormat("<Souvenir #{0}> The Switch: Success! Second set of colors now: {1}/{2}", _moduleId, topColor2, bottomColor2);
            }
            return ret;
        };

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Switch);

        if (topColor1 < 1 || topColor1 > 6 || bottomColor1 < 1 || bottomColor1 > 6 || topColor2 < 1 || topColor2 > 6 || bottomColor2 < 1 || bottomColor2 > 6)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Switch because topColor1/bottomColor1/topColor2/bottomColor2 has an unexpected value: {1}, {2}, {3}, {4} (expected 1–6).", _moduleId, topColor1, bottomColor1, topColor2, bottomColor2);
            yield break;
        }

        Debug.LogFormat("<Souvenir #{0}> The Switch: Asking questions. Color values: {1}/{2}/{3}/{4}", _moduleId, topColor1, bottomColor1, topColor2, bottomColor2);

        addQuestions(module,
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "top", "first" }, new[] { colorNames[topColor1 - 1] }),
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "bottom", "first" }, new[] { colorNames[bottomColor1 - 1] }),
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "top", "second" }, new[] { colorNames[topColor2 - 1] }),
            makeQuestion(Question.SwitchInitialColor, _Switch, new[] { "bottom", "second" }, new[] { colorNames[bottomColor2 - 1] }));
    }

    private IEnumerable<object> ProcessSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "SwitchModule");
        var fldSwitches = GetArrayField<MonoBehaviour>(comp, "Switches", isPublic: true);
        var fldGoal = GetField<object>(comp, "_goalConfiguration");
        var mthCurConfig = GetMethod<object>(comp, "GetCurrentConfiguration", 0);
        yield return null;
        var switches = fldSwitches.Get();
        if (switches == null || switches.Length != 5 || switches.Any(s => s == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Switches because Switches is {1} (expected length 5 and no nulls).", _moduleId,
                switches == null ? "<null>" : string.Format("[{0}]", switches.Select(sw => sw == null ? "null" : "not null").JoinString(", ")));
            yield break;
        }
        var initialState = switches.Select(sw => sw.GetComponent<Animator>().GetBool("Up") ? "Q" : "R").JoinString();

        while (!fldGoal.Get().Equals(mthCurConfig.Invoke()))
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Switches);
        addQuestion(module, Question.SwitchesInitialPosition, correctAnswers: new[] { initialState });
    }

    private IEnumerable<object> ProcessSymbolCycle(KMBombModule module)
    {
        var comp = GetComponent(module, "SymbolCycleModule");
        var fldCycles = GetArrayField<int[]>(comp, "_cycles");
        var fldState = GetField<object>(comp, "_state");

        yield return null;

        int[][] cycles = null;
        while (fldState.Get().ToString() != "Solved")
        {
            cycles = fldCycles.Get();
            if (cycles == null)
                yield break;
            if (cycles.Length != 2)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Symbol Cycle because the number of screens is unexpected (expected 2, got {1}).", _moduleId, cycles.Length);
                yield break;
            }

            if (cycles.Any(x => x == null || x.Length < 2 || x.Length > 5))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Symbol Cycle because the number of cycles per screen is unexpected (expected 2-5, got {1}).", _moduleId, cycles.Select(x => x == null ? "<null>" : x.Length.ToString()).JoinString(", "));
                yield break;
            }

            while (fldState.Get().ToString() == "Cycling")
                yield return new WaitForSeconds(0.1f);

            while (fldState.Get().ToString() == "Retrotransphasic" || fldState.Get().ToString() == "Anterodiametric")
                yield return new WaitForSeconds(0.1f);
        }

        if (cycles == null)
            yield break;

        _modulesSolved.IncSafe(_SymbolCycle);
        addQuestions(module, new[] { "left", "right" }.Select((screen, ix) => makeQuestion(Question.SymbolCycleSymbolCounts, _SymbolCycle, new[] { screen }, new[] { cycles[ix].Length.ToString() })));
    }

    private IEnumerable<object> ProcessSymbolicCoordinates(KMBombModule module)
    {
        var comp = GetComponent(module, "symbolicCoordinatesScript");
        var fldLetter1 = GetField<string>(comp, "letter1");
        var fldLetter2 = GetField<string>(comp, "letter2");
        var fldLetter3 = GetField<string>(comp, "letter3");
        var fldStage = GetIntField(comp, "stage");

        yield return null;

        var letter1 = fldLetter1.Get();
        var letter2 = fldLetter2.Get();
        var letter3 = fldLetter3.Get();

        if (letter1 == null || letter2 == null || letter3 == null)
            yield break;

        var stageLetters = new[] { letter1.Split(' '), letter2.Split(' '), letter3.Split(' ') };

        if (stageLetters.Any(x => x.Length != 3) || stageLetters.SelectMany(x => x).Any(y => !"PLACE".Contains(y)))
        {
            Debug.LogFormat("<Souvenir #{1}> Abandoning Symbolic Coordinates because one of the stages has fewer than 3 symbols or symbols are of unexpected value (expected symbols “ACELP”, got “{0}”).", stageLetters.Select(x => string.Format("“{0}”", x.JoinString())).JoinString(", "), _moduleId);
            yield break;
        }

        while (fldStage.Get() < 4)
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_SymbolicCoordinates);

        var position = new[] { "left", "middle", "right" };
        addQuestions(module, stageLetters.SelectMany((letters, stage) => letters.Select((letter, pos) => makeQuestion(
            Question.SymbolicCoordinateSymbols,
            _SymbolicCoordinates,
            formatArgs: new[] { position[pos], ordinal(stage + 1) },
            correctAnswers: new[] { SymbolicCoordinatesSprites["ACELP".IndexOf(letter, StringComparison.Ordinal)] },
            preferredWrongAnswers: SymbolicCoordinatesSprites))));
    }

    private IEnumerable<object> ProcessSynonyms(KMBombModule module)
    {
        var comp = GetComponent(module, "Synonyms");
        var fldNumberText = GetField<TextMesh>(comp, "NumberText", isPublic: true);
        var fldGoodLabel = GetField<TextMesh>(comp, "GoodLabel", isPublic: true);
        var fldBadLabel = GetField<TextMesh>(comp, "BadLabel", isPublic: true);
        var fldSolved = GetField<bool>(comp, "_isSolved");

        yield return null;
        var numberText = fldNumberText.Get();
        var goodLabel = fldGoodLabel.Get();
        var badLabel = fldBadLabel.Get();
        if (numberText == null || goodLabel == null || badLabel == null)
            yield break;
        int number;
        if (numberText.text == null || !int.TryParse(numberText.text, out number) || number < 0 || number > 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Synonyms because the display text (“{1}”) is not an integer 0–9.", _moduleId, numberText.text ?? "<null>");
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Synonyms);
        numberText.gameObject.SetActive(false);
        badLabel.text = "INPUT";
        goodLabel.text = "ACCEPTED";

        addQuestion(module, Question.SynonymsNumber, correctAnswers: new[] { number.ToString() });
    }

    private IEnumerable<object> ProcessBrushStrokes(KMBombModule module)
    {
        var comp = GetComponent(module, "BrushStrokesScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldColorNames = GetStaticField<string[]>(comp.GetType(), "colorNames");
        var fldColors = GetArrayField<int>(comp, "colors");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_BrushStrokes);

        string[] colorNames = fldColorNames.Get();
        int[] colors = fldColors.Get();

        if (colorNames == null || colors == null)
            yield break;
        if (colors.Length != 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Brush Strokes because 'colors' had unexpected length {1} (expected 9).", _moduleId, colors.Length);
            yield break;
        }
        if (colors[4] < 0 || colors[4] >= colorNames.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Brush Strokes because 'colors[4]' pointed to illegal color: {1}.", _moduleId, colors[4]);
            yield break;
        }

        addQuestion(module, Question.BrushStrokesMiddleColor, correctAnswers: new[] { char.ToUpperInvariant(colorNames[colors[4]][0]) + colorNames[colors[4]].Substring(1) });
    }

    private IEnumerable<object> ProcessBulb(KMBombModule module)
    {
        var comp = GetComponent(module, "TheBulbModule");
        var fldButtonPresses = GetField<string>(comp, "_correctButtonPresses");
        var fldStage = GetIntField(comp, "_stage");

        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        while (fldStage.Get() != 0)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Bulb);

        var buttonPresses = fldButtonPresses.Get();
        if (buttonPresses == null || buttonPresses.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> The Bulb: _correctButtonPresses has unexpected value ({1})", _moduleId, buttonPresses == null ? "<null>" : string.Format(@"""{0}""", buttonPresses));
            yield break;
        }

        addQuestion(module, Question.BulbButtonPresses, correctAnswers: new[] { buttonPresses });
    }

    private IEnumerable<object> ProcessGamepad(KMBombModule module)
    {
        var comp = GetComponent(module, "GamepadModule");
        var fldX = GetIntField(comp, "x");
        var fldY = GetIntField(comp, "y");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldDisplay = GetField<GameObject>(comp, "Input", isPublic: true);
        var fldDigits1 = GetField<GameObject>(comp, "Digits1", isPublic: true);
        var fldDigits2 = GetField<GameObject>(comp, "Digits2", isPublic: true);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.05f);

        var x = fldX.Get();
        var y = fldY.Get();
        if (x < 1 || x > 99 || y < 1 || y > 99)
        {
            Debug.LogFormat("<Souvenir #{0}> The Gamepad: x or y has unexpected value (x={1}, y={2})", _moduleId, x, y);
            yield break;
        }

        var display = fldDisplay.Get();
        var digits1 = fldDigits1.Get();
        var digits2 = fldDigits2.Get();
        if (display == null || display.GetComponent<TextMesh>() == null)
        {
            Debug.LogFormat("<Souvenir #{0}> The Gamepad: display is null or not a TextMesh.", _moduleId);
            yield break;
        }
        if (digits1 == null || digits1.GetComponent<TextMesh>() == null)
        {
            Debug.LogFormat("<Souvenir #{0}> The Gamepad: digits1 is null or not a TextMesh.", _moduleId);
            yield break;
        }
        if (digits2 == null || digits2.GetComponent<TextMesh>() == null)
        {
            Debug.LogFormat("<Souvenir #{0}> The Gamepad: digits2 is null or not a TextMesh.", _moduleId);
            yield break;
        }

        _modulesSolved.IncSafe(_Gamepad);
        addQuestions(module, makeQuestion(Question.GamepadNumbers, _Gamepad, correctAnswers: new[] { string.Format("{0:00}:{1:00}", x, y) },
            preferredWrongAnswers: Enumerable.Range(0, int.MaxValue).Select(i => string.Format("{0:00}:{1:00}", Rnd.Range(1, 99), Rnd.Range(1, 99))).Distinct().Take(6).ToArray()));
        digits1.GetComponent<TextMesh>().text = "--";
        digits2.GetComponent<TextMesh>().text = "--";
    }

    private IEnumerable<object> ProcessTapCode(KMBombModule module)
    {
        var comp = GetComponent(module, "TapCodeScript");
        var fldSolved = GetField<bool>(comp, "modulepass");
        var fldChosenWord = GetField<string>(comp, "chosenWord");
        var fldWords = GetArrayField<string>(comp, "words");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_TapCode);

        var words = fldWords.Get();
        if (words == null)
            yield break;

        var chosenWord = fldChosenWord.Get();
        if (chosenWord == null || !words.Contains(chosenWord))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Tap Code because the initial word ({1}) is not in the word bank.", _moduleId, chosenWord ?? "<null>");
            yield break;
        }

        addQuestion(module, Question.TapCodeReceivedWord, correctAnswers: new[] { chosenWord }, preferredWrongAnswers: words);
    }

    private IEnumerable<object> ProcessTashaSqueals(KMBombModule module)
    {
        var comp = GetComponent(module, "tashaSquealsScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldColors = GetStaticField<string[]>(comp.GetType(), "colorNames");
        var fldSequence = GetArrayField<int>(comp, "flashing");

        string[] colors = fldColors.Get();
        int[] sequence = fldSequence.Get();

        if (colors == null || sequence == null)
            yield break;
        if (colors.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Tasha Squeals because 'colorNames' had length {1} instead of 4.", _moduleId, colors.Length);
            yield break;
        }
        if (sequence.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Tasha Squeals because 'flashing' had length {1} instead of 5.", _moduleId, sequence.Length);
            yield break;
        }
        for (int i = 0; i < sequence.Length; i++)
        {
            if (sequence[i] < 0 || sequence[i] >= colors.Length)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Tasha Squeals because 'sequence[{1}]' pointed to illegal color: {2}.", _moduleId, i, sequence[i]);
                yield break;
            }
        }

        for (int i = 0; i < colors.Length; i++)
            colors[i] = char.ToUpperInvariant(colors[i][0]) + colors[i].Substring(1);

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_TashaSqueals);
        addQuestions(module,
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "first" }, new[] { colors[sequence[0]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "second" }, new[] { colors[sequence[1]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "third" }, new[] { colors[sequence[2]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "fourth" }, new[] { colors[sequence[3]] }),
            makeQuestion(Question.TashaSquealsColors, _TashaSqueals, new[] { "fifth" }, new[] { colors[sequence[4]] }));
    }

    private IEnumerable<object> ProcessTenButtonColorCode(KMBombModule module)
    {
        var comp = GetComponent(module, "scr_colorCode");
        var fldSolvedFirstStage = GetField<bool>(comp, "solvedFirst");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldColors = GetArrayField<int>(comp, "prevColors");

        var firstStageColors = fldColors.Get();
        if (firstStageColors == null || firstStageColors.Length != 10)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Ten-Button Color Code because “prevColors” has unexpected value {1}.", _moduleId, firstStageColors == null ? "<null>" : string.Format("[{0}]", firstStageColors.JoinString(", ")));
            yield break;
        }
        // Take a copy because the module modifies the same array in the second stage
        firstStageColors = firstStageColors.ToArray();

        while (!fldSolvedFirstStage.Get())
            yield return new WaitForSeconds(.1f);

        var secondStageColors = fldColors.Get();
        if (secondStageColors == null || secondStageColors.Length != 10)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Ten-Button Color Code because “prevColors” has unexpected value {1}.", _moduleId, secondStageColors == null ? "<null>" : string.Format("[{0}]", secondStageColors.JoinString(", ")));
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_TenButtonColorCode);

        var colorNames = new[] { "red", "green", "blue" };
        addQuestions(module, new[] { firstStageColors, secondStageColors }.SelectMany((colors, stage) => Enumerable.Range(0, 10)
            .Select(slot => makeQuestion(Question.TenButtonColorCodeInitialColors, _TenButtonColorCode, new[] { ordinal(slot + 1), ordinal(stage + 1) }, new[] { colorNames[colors[slot]] }))));
    }

    private IEnumerable<object> ProcessTextField(KMBombModule module)
    {
        var comp = GetComponent(module, "TextField");
        var fldDisplay = GetArrayField<TextMesh>(comp, "ButtonLabels", true);
        var fldActivated = GetField<bool>(comp, "_lightson");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        var displayMeshes = fldDisplay.Get();
        if (displayMeshes == null)
            yield break;

        if (displayMeshes.Any(x => x == null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Text Field because one of the text meshes in ‘ButtonLabels’ is null.", _moduleId);
            yield break;
        }

        if (displayMeshes.Length != 12)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Text Field because ‘ButtonLabels’ has unexpected length {1} (expected 12).", _moduleId, displayMeshes.Length);
            yield break;
        }

        while (!fldActivated.Get())
            yield return new WaitForSeconds(0.1f);

        var answer = displayMeshes.Select(x => x.text).FirstOrDefault(x => x != "✓" && x != "✗");
        var possibleAnswers = new[] { "A", "B", "C", "D", "E", "F" };

        if (!possibleAnswers.Contains(answer))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Text Field because answer ‘{1}’ is not of expected value ({2}).", _moduleId, answer ?? "<null>", possibleAnswers.JoinString(", "));
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        for (var i = 0; i < 12; i++)
            if (displayMeshes[i].text == answer)
                displayMeshes[i].text = "✓";

        _modulesSolved.IncSafe(_TextField);
        addQuestion(module, Question.TextFieldDisplay, correctAnswers: new[] { answer });
    }

    private IEnumerable<object> ProcessThinkingWires(KMBombModule module)
    {
        var comp = GetComponent(module, "thinkingWiresScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldFirstWireToCut = GetIntField(comp, "firstWireToCut");
        var fldSecondWireToCut = GetField<string>(comp, "secondWireToCut");
        var fldScreenNumber = GetField<string>(comp, "screenNumber");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);
        _modulesSolved.IncSafe(_ThinkingWires);

        var firstCorrectWire = fldFirstWireToCut.Get();
        var secondCorrectWire = fldSecondWireToCut.Get();
        var displayNumber = fldScreenNumber.Get();

        if (secondCorrectWire == null || displayNumber == null)
            yield break;

        if (firstCorrectWire < 1 || firstCorrectWire > 7)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Thinking Wires because ‘firstCorrectWire’ has an unexpected value (expected 1–7): {1}", _moduleId, firstCorrectWire);
            yield break;
        }

        if (!new[] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White", "Black", "Any" }.Contains(secondCorrectWire))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Thinking Wires because ‘secondCorrectWire’ is an invalid color: {1}", _moduleId, secondCorrectWire);
            yield break;
        }

        // List of valid display numbers for validation. 69 happens in the case of "Any" while 11 is expected to be the longest.
        // Basic calculations by hand and algorithm seem to confirm this, but may want to recalculate to ensure it is right.
        if (!new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "69" }.Contains(displayNumber))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Thinking Wires because ‘displayNumber’ has an unexpected value: {1}", _moduleId, displayNumber);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.ThinkingWiresFirstWire, _ThinkingWires, null, new[] { firstCorrectWire.ToString() }),
            makeQuestion(Question.ThinkingWiresSecondWire, _ThinkingWires, null, new[] { secondCorrectWire }),
            makeQuestion(Question.ThinkingWiresDisplayNumber, _ThinkingWires, null, new[] { displayNumber }));
    }

    private IEnumerable<object> ProcessThirdBase(KMBombModule module)
    {
        var comp = GetComponent(module, "ThirdBaseModule");
        var fldDisplay = GetField<TextMesh>(comp, "Display", isPublic: true);
        var fldStage = GetIntField(comp, "stage");
        var fldActivated = GetField<bool>(comp, "isActivated");
        var fldSolved = GetField<bool>(comp, "isPassed");

        yield return null;

        var displayTextMesh = fldDisplay.Get();
        if (displayTextMesh == null)
            yield break;

        while (!fldActivated.Get())
            yield return new WaitForSeconds(0.1f);

        var displayWords = new string[2];

        for (var i = 0; i < 2; i++)
            while (fldStage.Get() == i)
            {
                while (!fldActivated.Get())
                    yield return new WaitForSeconds(0.1f);

                displayWords[i] = displayTextMesh.text;

                while (fldActivated.Get())
                    yield return new WaitForSeconds(0.1f);
            }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_ThirdBase);
        addQuestions(module, displayWords.Select((word, stage) => makeQuestion(Question.ThirdBaseDisplay, _ThirdBase, new[] { ordinal(stage + 1) }, new[] { word })));
    }

    private IEnumerable<object> ProcessTicTacToe(KMBombModule module)
    {
        var comp = GetComponent(module, "TicTacToeModule");
        var fldKeypadButtons = GetArrayField<KMSelectable>(comp, "KeypadButtons", isPublic: true);
        var fldKeypadPhysical = GetArrayField<KMSelectable>(comp, "_keypadButtonsPhysical");
        var fldPlacedX = GetField<bool?[]>(comp, "_placedX");
        var fldIsInitialized = GetField<bool>(comp, "_isInitialized");
        var fldIsSolved = GetField<bool>(comp, "_isSolved");

        while (!fldIsInitialized.Get())
            yield return new WaitForSeconds(.1f);

        var keypadButtons = fldKeypadButtons.Get();
        var keypadPhysical = fldKeypadPhysical.Get();
        var placedX = fldPlacedX.Get();
        if (keypadButtons == null || keypadPhysical == null || placedX == null)
            yield break;
        if (keypadButtons.Length != 9 || keypadPhysical.Length != 9 || placedX.Length != 9)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Tic Tac Toe because one of the arrays has an unexpected length (expected 9): {1}, {2}, {3}.", _moduleId, keypadButtons.Length, keypadPhysical.Length, placedX.Length);
            yield break;
        }

        // Take a copy of the placedX array because it changes
        placedX = placedX.ToArray();

        while (!fldIsSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_TicTacToe);

        var buttonNames = new[] { "top-left", "top-middle", "top-right", "middle-left", "middle-center", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };
        addQuestions(module, Enumerable.Range(0, 9).Select(ix => makeQuestion(Question.TicTacToeInitialState, _TicTacToe,
            formatArgs: new[] { buttonNames[Array.IndexOf(keypadPhysical, keypadButtons[ix])] },
            correctAnswers: new[] { placedX[ix] == null ? (ix + 1).ToString() : placedX[ix].Value ? "X" : "O" })));
    }

    private IEnumerable<object> ProcessTimezone(KMBombModule module)
    {
        var comp = GetComponent(module, "TimezoneScript");
        var fldFromCity = GetField<string>(comp, "from");
        var fldToCity = GetField<string>(comp, "to");
        var fldTextFromCity = GetField<TextMesh>(comp, "TextFromCity", isPublic: true);
        var fldTextToCity = GetField<TextMesh>(comp, "TextToCity", isPublic: true);
        var fldInputButton = GetField<KMSelectable>(comp, "InputButton", isPublic: true);

        yield return null;

        var inputButton = fldInputButton.Get();
        var textFromCity = fldTextFromCity.Get();
        var textToCity = fldTextToCity.Get();
        if (inputButton == null || textFromCity == null || textToCity == null)
            yield break;

        if (fldFromCity.Get() != textFromCity.text || fldToCity.Get() != textToCity.text)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Timezone because the city names don’t match up: “{1}” vs. “{2}” and “{3}” vs. “{4}”.", _moduleId, fldFromCity.Get(), textFromCity.text, fldToCity.Get(), textToCity.text);
            yield break;
        }

        var prevHandler = inputButton.OnInteract;
        var solved = false;
        inputButton.OnInteract = delegate
        {
            var prevSolved = Bomb.GetSolvedModuleNames().Count();
            var result = prevHandler();
            if (Bomb.GetSolvedModuleNames().Count() > prevSolved)
            {
                textFromCity.text = "WELL";
                textToCity.text = "DONE!";
                solved = true;
            }
            return result;
        };

        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Timezone);
        inputButton.OnInteract = prevHandler;
        addQuestions(module,
            makeQuestion(Question.TimezoneCities, _Timezone, new[] { "departure" }, new[] { fldFromCity.Get() }),
            makeQuestion(Question.TimezoneCities, _Timezone, new[] { "destination" }, new[] { fldToCity.Get() }));
    }

    private IEnumerable<object> ProcessTransmittedMorse(KMBombModule module)
    {
        var comp = GetComponent(module, "TransmittedMorseScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldMessage = GetField<string>(comp, "messagetrans");
        var fldStage = GetIntField(comp, "stage");

        yield return null;

        string[] messages = new string[2];
        string message;
        int stage = 0;

        while (!fldSolved.Get())
        {
            stage = fldStage.Get();
            if (stage < 1 || stage > 2)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Transmitted Morse because 'stage' has an unexpected value (expected 1 - 2): {1}", _moduleId, stage);
                yield break;
            }
            message = fldMessage.Get();
            if (message == null)
                yield break;
            messages[stage - 1] = message;
            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_TransmittedMorse);

        addQuestions(module, messages.Select((msg, index) => makeQuestion(Question.TransmittedMorseMessage, _TransmittedMorse,
            formatArgs: new[] { ordinal(index + 1) },
            correctAnswers: new[] { msg },
            preferredWrongAnswers: messages)));
    }

    private IEnumerable<object> ProcessTurtleRobot(KMBombModule module)
    {
        var comp = GetComponent(module, "TurtleRobot");
        var fldCursor = GetIntField(comp, "_cursor");
        var fldCommands = GetField<IList>(comp, "_commands");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldButtonDelete = GetField<KMSelectable>(comp, "ButtonDelete", isPublic: true);
        var mthFormatCommand = GetMethod<string>(comp, "FormatCommand", 2);

        yield return null;

        var commands = fldCommands.Get();
        var deleteButton = fldButtonDelete.Get();
        if (commands == null || deleteButton == null)
            yield break;

        var codeLines = commands.Cast<object>().Select(cmd => mthFormatCommand.Invoke(cmd, false)).ToArray();
        Debug.LogFormat("<Souvenir #{0}> Turtle Robot lines:\n{1}", _moduleId, codeLines.Select((cl, ix) => string.Format("{0}. {1}", ix, cl)).JoinString("\n"));
        var bugs = new List<string>();
        var bugsMarked = new HashSet<int>();

        var buttonHandler = deleteButton.OnInteract;
        deleteButton.OnInteract = delegate
        {
            var ret = buttonHandler();
            var cursor = fldCursor.Get();
            var command = mthFormatCommand.Invoke(commands[cursor], true);
            Debug.LogFormat("<Souvenir #{0}> Turtle Robot: Delete button pressed on {1} at cursor position {2}", _moduleId, command, cursor);
            if (command.StartsWith("#") && bugsMarked.Add(cursor))
            {
                bugs.Add(codeLines[cursor]);
                Debug.LogFormat("<Souvenir #{0}> Turtle Robot: — Added", _moduleId);
            }
            else
                Debug.LogFormat("<Souvenir #{0}> Turtle Robot: — NOT added", _moduleId);
            return ret;
        };

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        Debug.LogFormat("<Souvenir #{0}> Turtle Robot solved. Bugs:\n{1}", _moduleId, bugs.JoinString("\n"));
        _modulesSolved.IncSafe(_TurtleRobot);
        addQuestions(module, bugs.Take(2).Select((bug, ix) => makeQuestion(Question.TurtleRobotCodeLines, _TurtleRobot, new[] { ordinal(ix + 1) }, new[] { bug }, codeLines)));
    }

    private IEnumerable<object> ProcessTwoBits(KMBombModule module)
    {
        var comp = GetComponent(module, "TwoBitsModule");
        var fldFirstQueryCode = GetIntField(comp, "firstQueryCode");
        var fldQueryLookups = GetField<Dictionary<int, string>>(comp, "queryLookups");
        var fldQueryResponses = GetField<Dictionary<string, int>>(comp, "queryResponses");
        var fldCurrentState = GetField<object>(comp, "currentState");

        while (fldCurrentState.Get().ToString() != "Complete")
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_TwoBits);

        var queryLookups = fldQueryLookups.Get();
        var queryResponses = fldQueryResponses.Get();
        if (queryLookups == null || queryResponses == null)
            yield break;

        var qs = new List<QandA>();
        try
        {
            var zerothNumCode = fldFirstQueryCode.Get();
            var zerothLetterCode = queryLookups[zerothNumCode];
            var firstResponse = queryResponses[zerothLetterCode];
            var firstLookup = queryLookups[firstResponse];
            var secondResponse = queryResponses[firstLookup];
            var secondLookup = queryLookups[secondResponse];
            var thirdResponse = queryResponses[secondLookup];
            var preferredWrongAnswers = new[] { zerothNumCode.ToString("00"), firstResponse.ToString("00"), secondResponse.ToString("00"), thirdResponse.ToString("00") };
            qs.Add(makeQuestion(Question.TwoBitsResponse, _TwoBits, new[] { "first" }, new[] { firstResponse.ToString("00") }, preferredWrongAnswers));
            qs.Add(makeQuestion(Question.TwoBitsResponse, _TwoBits, new[] { "second" }, new[] { secondResponse.ToString("00") }, preferredWrongAnswers));
            qs.Add(makeQuestion(Question.TwoBitsResponse, _TwoBits, new[] { "third" }, new[] { thirdResponse.ToString("00") }, preferredWrongAnswers));
        }
        catch (Exception e)
        {
            Debug.LogFormat("<Souvenir #{0}> Two Bits: Exception: {1} ({2})", _moduleId, e.Message, e.GetType().FullName);
        }

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessUltimateCycle(KMBombModule module)
    {
        return processSpeakingEvilCycle2(module, "UltimateCycleScript", "Ultimate Cycle", Question.UltimateCycleWord, _UltimateCycle);
    }

    private IEnumerable<object> ProcessUltracube(KMBombModule module)
    {
        var comp = GetComponent(module, "TheUltracubeModule");
        var fldSequence = GetArrayField<int>(comp, "_rotations");
        var fldRotations = GetStaticField<string[]>(comp.GetType(), "_rotationNames");

        int[] sequence = fldSequence.Get();
        string[] rotations = fldRotations.Get();

        if (sequence == null || rotations == null)
            yield break;
        if (sequence.Length != 5)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Ultracube because '_rotations' had length {1} instead of 5.", _moduleId, sequence.Length);
            yield break;
        }
        for (int i = 0; i < sequence.Length; i++)
        {
            if (sequence[i] < 0 || sequence[i] >= rotations.Length)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning The Ultracube because the '_rotations[{1}]' pointed to illegal rotation: {2}.", _moduleId, i, sequence[i]);
                yield break;
            }
        }

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Ultracube);
        addQuestions(module,
            makeQuestion(Question.UltracubeRotations, _Ultracube, new[] { "first" }, new[] { rotations[sequence[0]] }),
            makeQuestion(Question.UltracubeRotations, _Ultracube, new[] { "second" }, new[] { rotations[sequence[1]] }),
            makeQuestion(Question.UltracubeRotations, _Ultracube, new[] { "third" }, new[] { rotations[sequence[2]] }),
            makeQuestion(Question.UltracubeRotations, _Ultracube, new[] { "fourth" }, new[] { rotations[sequence[3]] }),
            makeQuestion(Question.UltracubeRotations, _Ultracube, new[] { "fifth" }, new[] { rotations[sequence[4]] }));
    }

    private IEnumerable<object> ProcessUncoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "UncoloredSquaresModule");
        var fldSolved = GetField<bool>(comp, "_isSolved");
        var fldFirstStageColor1 = GetField<object>(comp, "_firstStageColor1");
        var fldFirstStageColor2 = GetField<object>(comp, "_firstStageColor2");

        yield return null;

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_UncoloredSquares);
        addQuestions(module,
            makeQuestion(Question.UncoloredSquaresFirstStage, _UncoloredSquares, new[] { "first" }, new[] { fldFirstStageColor1.Get().ToString() }),
            makeQuestion(Question.UncoloredSquaresFirstStage, _UncoloredSquares, new[] { "second" }, new[] { fldFirstStageColor2.Get().ToString() }));
    }

    private IEnumerable<object> ProcessUncoloredSwitches(KMBombModule module)
    {
        var comp = GetComponent(module, "UncoloredSwitches");
        var fldInitialState = GetField<StringBuilder>(comp, "Switches_Current_State");
        var fldLedColors = GetField<StringBuilder>(comp, "LEDsColorsString");
        var fldStage = GetIntField(comp, "stage");

        yield return null;

        var curStage = -1;
        var ledColors = new int[3][];
        var switchStates = new bool[3][];
        var colorNames = new[] { "red", "green", "blue", "turquoise", "orange", "purple", "white", "black" };
        while (fldStage.Get() < 4)
        {
            var newStage = fldStage.Get();
            if (newStage == 3 || newStage < 0 || newStage > 4)
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Uncolored Switches because ‘stage’ had unexpected value {1} (expected 0–2 or 4).", _moduleId, newStage);
                yield break;
            }
            if (newStage != curStage)
            {
                var curLedColors = fldLedColors.Get();
                if (curLedColors == null)
                    yield break;
                if (curLedColors.Length != 10)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Uncolored Switches because ‘LEDsColorsString’ had unexpected length {1} (expected 10).", _moduleId, curLedColors.Length);
                    yield break;
                }

                var switchState = fldInitialState.Get();
                if (switchState == null)
                    yield break;
                if (switchState.Length != 5)
                {
                    Debug.LogFormat("<Souvenir #{0}> Abandoning Uncolored Switches because ‘Switches_State’ had unexpected length {1} (expected 5).", _moduleId, switchState.Length);
                    yield break;
                }

                ledColors[newStage] = Enumerable.Range(0, 10).Select(ledIx => "RGBTOPWK".IndexOf(curLedColors[ledIx])).ToArray();
                switchStates[newStage] = Enumerable.Range(0, 5).Select(swIx => switchState[swIx] == '1').ToArray();
            }
            yield return null;
        }
        _modulesSolved.IncSafe(_UncoloredSwitches);

        if (ledColors.Contains(null) || switchStates.Contains(null))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Uncolored Switches because a stage was skipped: [{1}], [{2}]", _moduleId,
                ledColors.Select(arr => arr == null ? "null" : string.Format(@"[{0}]", arr.JoinString(", "))),
                switchStates.Select(arr => arr == null ? "null" : string.Format(@"[{0}]", arr.JoinString(", "))));
            yield break;
        }

        var qs = new List<QandA>();
        for (var stage = 0; stage < 3; stage++)
        {
            qs.Add(makeQuestion(Question.UncoloredSwitchesInitialState, _UncoloredSwitches, formatArgs: new[] { ordinal(stage + 1) }, correctAnswers: new[] { switchStates[stage].Select(b => b ? 'Q' : 'R').JoinString() }));
            for (var ledIx = 0; ledIx < 10; ledIx++)
                qs.Add(makeQuestion(Question.UncoloredSwitchesLedColors, _UncoloredSwitches, formatArgs: new[] { ordinal(ledIx + 1), ordinal(stage + 1) }, correctAnswers: new[] { colorNames[ledColors[stage][ledIx]] }));
        }
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessUnfairCipher(KMBombModule module)
    {
        var comp = GetComponent(module, "unfairCipherScript");
        var fldSolved = GetField<bool>(comp, "solved");
        var fldInstructions = GetArrayField<string>(comp, "Message");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_UnfairCipher);

        string[] instructions = fldInstructions.Get();

        if (instructions == null)
            yield break;
        if (instructions.Length != 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Unfair Cipher because 'Message' had an unexpected length {1} (expected 4).", _moduleId, instructions.Length);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "first" }, new[] { instructions[0] }),
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "second" }, new[] { instructions[1] }),
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "third" }, new[] { instructions[2] }),
            makeQuestion(Question.UnfairCipherInstructions, _UnfairCipher, new[] { "fourth" }, new[] { instructions[3] }));
    }

    private IEnumerable<object> ProcessUnownCipher(KMBombModule module)
    {
        var comp = GetComponent(module, "UnownCipher");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldUnown = GetArrayField<int>(comp, "letterIndexes");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_UnownCipher);

        int[] unownAnswer = fldUnown.Get();
        if (unownAnswer == null)
            yield break;
        if (unownAnswer.Length != 5 || unownAnswer.Any(v => v < 0 || v > 25))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Unown Cipher because ‘letterIndexes’ had an unexpected length or value: [{1}] (expected 5 values of 0–25).", _moduleId, unownAnswer.JoinString(", "));
            yield break;
        }

        addQuestions(module, unownAnswer.Select((ans, i) => makeQuestion(Question.UnownCipherAnswers, _UnownCipher, new[] { ordinal(i + 1) }, new[] { ((char) ('A' + ans)).ToString() })));
    }

    private IEnumerable<object> ProcessUSAMaze(KMBombModule module)
    {
        return ProcessWorldMaze(module, "USAMaze", _USAMaze, Question.USAMazeOrigin);
    }

    private IEnumerable<object> ProcessVaricoloredSquares(KMBombModule module)
    {
        var comp = GetComponent(module, "VaricoloredSquaresModule");
        var fldFirstColor = GetField<object>(comp, "_firstStageColor");

        var solved = false;
        module.OnPass += delegate { solved = true; return false; };
        while (!solved)
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_VaricoloredSquares);

        var firstColor = fldFirstColor.Get();
        if (firstColor == null)
            yield break;

        addQuestion(module, Question.VaricoloredSquaresInitialColor, correctAnswers: new[] { firstColor.ToString() });
    }

    private IEnumerable<object> ProcessVcrcs(KMBombModule module)
    {
        var comp = GetComponent(module, "VcrcsScript");
        var fldSolved = GetField<bool>(comp, "ModuleSolved");
        var fldWord = GetField<TextMesh>(comp, "Words", isPublic: true);

        yield return null;

        var wordTextMesh = fldWord.Get();
        if (wordTextMesh == null)
            yield break;
        var word = wordTextMesh.text;
        if (word == null)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Vcrcs because ‘Words.text’ is null.", _moduleId);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Vcrcs);

        addQuestion(module, Question.VcrcsWord, correctAnswers: new[] { word });
    }

    private IEnumerable<object> ProcessVectors(KMBombModule module)
    {
        var comp = GetComponent(module, "VectorsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldColors = GetArrayField<string>(comp, "colors");
        var fldVectorsPicked = GetArrayField<int>(comp, "vectorsPicked");
        var fldVectorCount = GetIntField(comp, "vectorct");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Vectors);

        int vectorCount = fldVectorCount.Get();
        if (vectorCount < 1 || vectorCount > 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Vectors because ‘vectorct’ has an unexpected value (expected 1–3): {1}.", _moduleId, vectorCount);
            yield break;
        }

        var colors = fldColors.Get();
        var pickedVectors = fldVectorsPicked.Get();

        if (colors == null || pickedVectors == null)
            yield break;
        var colorsName = new[] { "Red", "Orange", "Yellow", "Green", "Blue", "Purple" };
        if (pickedVectors.Length != 3 || pickedVectors.Any(v => v < 0 || v >= colors.Length))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Vectors because ‘vectorsPicked’ has unexpected length or value: [{1}] (expected length 3, values 0–{2}).", _moduleId, pickedVectors.JoinString(", "), colors.Length - 1);
            yield break;
        }

        for (int i = 0; i < vectorCount; i++)
        {
            if (!colorsName.Contains(colors[pickedVectors[i]]))
            {
                Debug.LogFormat("<Souvenir #{0}> Abandoning Vectors because ‘colors[{1}]’ pointed to illegal color “{2}” (colors=[{3}], pickedVectors=[{4}], index {5}).",
                    _moduleId, pickedVectors[i], colors[pickedVectors[i]], colors.JoinString(", "), pickedVectors.JoinString(", "), i);
                yield break;
            }
        }

        var qs = new List<QandA>();
        for (int i = 0; i < vectorCount; i++)
            qs.Add(makeQuestion(Question.VectorsColors, _Vectors, new[] { vectorCount == 1 ? "only" : ordinal(i + 1) }, new[] { colors[pickedVectors[i]] }));
        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessVexillology(KMBombModule module)
    {
        var comp = GetComponent(module, "vexillologyScript");
        var fldSolved = GetField<bool>(comp, "_issolved");
        var fldColors = GetArrayField<string>(comp, "coloursStrings");
        var fldColor1 = GetIntField(comp, "ActiveFlagTop1");
        var fldColor2 = GetIntField(comp, "ActiveFlagTop2");
        var fldColor3 = GetIntField(comp, "ActiveFlagTop3");

        string[] colors = fldColors.Get();
        int color1 = fldColor1.Get();
        int color2 = fldColor2.Get();
        int color3 = fldColor3.Get();

        if (colors == null)
            yield break;
        if (color1 < 0 || color1 >= colors.Length || color2 < 0 || color2 >= colors.Length || color3 < 0 || color3 >= colors.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Vexillology because one or more of the flagpole colors points to an illegal color: {1}, {2}, {3}.", _moduleId, color1, color2, color3);
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        _modulesSolved.IncSafe(_Vexillology);
        addQuestions(module,
            makeQuestion(Question.VexillologyColors, _Vexillology, new[] { "first" }, new[] { colors[color1] }, new[] { colors[color2], colors[color3] }),
            makeQuestion(Question.VexillologyColors, _Vexillology, new[] { "second" }, new[] { colors[color2] }, new[] { colors[color1], colors[color3] }),
            makeQuestion(Question.VexillologyColors, _Vexillology, new[] { "third" }, new[] { colors[color3] }, new[] { colors[color2], colors[color1] }));
    }

    private IEnumerable<object> ProcessVisualImpairment(KMBombModule module)
    {
        var comp = GetComponent(module, "VisualImpairment");
        var fldRoundsFinished = GetIntField(comp, "roundsFinished");
        var fldStageCount = GetIntField(comp, "stageCount");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldColor = GetIntField(comp, "color");
        var fldAnyPressed = GetField<bool>(comp, "anyPressed");
        var fldPicture = GetArrayField<string>(comp, "picture");

        // Wait for the first picture to be assigned
        while (fldPicture.Get(nullAllowed: true) == null)
            yield return new WaitForSeconds(.1f);

        var stageCount = fldStageCount.Get();
        if (stageCount < 2 || stageCount >= 4)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Visual Impairment because stageCount is not 2 or 3 as expected (it’s {1}).", _moduleId, stageCount);
            yield break;
        }
        var colorsPerStage = new int[stageCount];
        var colorNames = new[] { "Blue", "Green", "Red", "White" };

        while (!fldSolved.Get())
        {
            var newStage = fldRoundsFinished.Get();
            if (newStage >= stageCount)
                break;

            var newColor = fldColor.Get();
            if (newColor != colorsPerStage[newStage])
                Debug.LogFormat("<Souvenir #{0}> Visual Impairment: stage {1} color changed to {2} ({3}).", _moduleId, newStage, newColor, newColor >= 0 && newColor < 4 ? colorNames[newColor] : "<out of range>");
            colorsPerStage[newStage] = newColor;
            yield return new WaitForSeconds(.1f);
        }
        _modulesSolved.IncSafe(_VisualImpairment);

        if (colorsPerStage.Any(c => c < 0 || c > 3))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Visual Impairment because one of the colors is invalid (expected 0–3): [{1}].", _moduleId, colorsPerStage.JoinString(", "));
            yield break;
        }

        addQuestions(module, colorsPerStage.Select((col, ix) => makeQuestion(Question.VisualImpairmentColors, _VisualImpairment, new[] { ordinal(ix + 1) }, new[] { colorNames[col] })));
    }

    private IEnumerable<object> ProcessWavetapping(KMBombModule module)
    {
        var comp = GetComponent(module, "scr_wavetapping");
        var fldStageColors = GetArrayField<int>(comp, "stageColors");
        var fldIntPatterns = GetArrayField<int>(comp, "intPatterns");
        var fldSolved = GetField<bool>(comp, "moduleSolved");

        yield return null;

        var stageColors = fldStageColors.Get();
        var intPatterns = fldIntPatterns.Get();
        if (stageColors.Length != 3 || intPatterns.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Wavetapping because ‘intPatterns/stageColors’ has unexpected length (expected 3): {1}).", _moduleId, string.Format("[{0}] | [{1}]", intPatterns.JoinString(", "), stageColors.JoinString(", ")));
            yield break;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);

        var patternSprites = new Dictionary<int, Sprite[]>();
        var spriteTake = new[] { 4, 4, 3, 2, 2, 2, 2, 2, 9, 4, 40, 13, 4, 8, 21, 38 };
        var spriteSkip = 0;
        for (int i = 0; i < spriteTake.Length; i++)
        {
            patternSprites.Add(i, WavetappingSprites.Skip(spriteSkip).Take(spriteTake[i]).ToArray());
            spriteSkip += spriteTake[i];
        }

        var colorNames = new[] { "Red", "Orange", "Orange-Yellow", "Chartreuse", "Lime", "Green", "Seafoam Green", "Cyan-Green", "Turquoise", "Dark Blue", "Indigo", "Purple", "Purple-Magenta", "Magenta", "Pink", "Gray" };
        _modulesSolved.IncSafe(_Wavetapping);

        var qs = new List<QandA>();

        for (int stage = 0; stage < intPatterns.Length; stage++)
            qs.Add(makeQuestion(Question.WavetappingPatterns, _Wavetapping,
                formatArgs: new[] { ordinal(stage + 1) },
                correctAnswers: new[] { patternSprites[stageColors[stage]][intPatterns[stage]] },
                preferredWrongAnswers: stageColors.SelectMany(stages => patternSprites[stages]).ToArray()));
        for (int stage = 0; stage < 2; stage++)
            qs.Add(makeQuestion(Question.WavetappingColors, _Wavetapping,
                formatArgs: new[] { ordinal(stage + 1) },
                correctAnswers: new[] { colorNames[stageColors[stage]] }));

        addQuestions(module, qs);
    }

    private IEnumerable<object> ProcessWhosOnFirst(KMBombModule module)
    {
        var component = GetComponent(module, "WhosOnFirstComponent");
        var fldSolved = GetField<bool>(component, "IsSolved", true);
        var fldDisplay = GetField<MonoBehaviour>(component, "DisplayText", true);  // TextMeshPro
        var propStage = GetProperty<int>(component, "CurrentStage", true);
        var propButtonsEmerged = GetProperty<bool>(component, "ButtonsEmerged", true);
        if (fldSolved == null || fldDisplay == null || propStage == null || propButtonsEmerged == null)
            yield break;

        yield return null;

        var displayTextMesh = fldDisplay.Get();
        var propText = GetProperty<string>(displayTextMesh, "text", true);
        if (displayTextMesh == null || propText == null)
            yield break;

        while (!propButtonsEmerged.Get())
            yield return new WaitForSeconds(0.1f);

        var displayWords = new string[2];
        for (var i = 0; i < 2; i++)
            while (propStage.Get() == i)
            {
                while (!propButtonsEmerged.Get())
                    yield return new WaitForSeconds(0.1f);

                displayWords[i] = propText.Get();

                while (propButtonsEmerged.Get())
                    yield return new WaitForSeconds(0.1f);
            }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(0.1f);

        _modulesSolved.IncSafe(_WhosOnFirst);
        addQuestions(module, displayWords.Select((word, stage) => makeQuestion(Question.WhosOnFirstDisplay, _WhosOnFirst, new[] { ordinal(stage + 1) }, new[] { word }, displayWords)));
    }

    private IEnumerable<object> ProcessWire(KMBombModule module)
    {
        var comp = GetComponent(module, "wireScript");
        var fldSolved = GetField<bool>(comp, "moduleDone");
        var fldDials = GetArrayField<Renderer>(comp, "renderers", isPublic: true);
        var fldDisplayedNumber = GetIntField(comp, "displayedNumber");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Wire);

        var dials = fldDials.Get();
        if (dials == null || dials.Length != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning The Wire because ‘renderers’ has unexpected length ({1}, expected 3).", _moduleId, dials == null ? "<null>" : dials.Length.ToString());
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.WireDialColors, _Wire, new[] { "top" }, new[] { dials[0].material.mainTexture.name.Replace("Mat", "") }),
            makeQuestion(Question.WireDialColors, _Wire, new[] { "bottom-left" }, new[] { dials[1].material.mainTexture.name.Replace("Mat", "") }),
            makeQuestion(Question.WireDialColors, _Wire, new[] { "bottom-right" }, new[] { dials[2].material.mainTexture.name.Replace("Mat", "") }),
            makeQuestion(Question.WireDisplayedNumber, _Wire, correctAnswers: new[] { fldDisplayedNumber.Get().ToString() }));
    }

    private IEnumerable<object> ProcessWireOrdering(KMBombModule module)
    {
        var comp = GetComponent(module, "WireOrderingScript");
        var fldSolved = GetField<bool>(comp, "_modSolved");
        var fldChosenColorsDisplay = GetArrayField<int>(comp, "_chosenColorsDis");
        var fldChosenColorsWire = GetArrayField<int>(comp, "_chosenColorsWire");
        var fldChosenDisplayNumbers = GetArrayField<int>(comp, "_chosenDisNum");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_WireOrdering);

        var colors = _attributes[Question.WireOrderingDisplayColor].AllAnswers;
        var index = Rnd.Range(0, 4);
        var ordinal = new[] { this.ordinal(index + 1) };
        addQuestions(module,
            makeQuestion(Question.WireOrderingDisplayColor, _WireOrdering, ordinal, new[] { colors[fldChosenColorsDisplay.Get()[index]] }),
            makeQuestion(Question.WireOrderingDisplayNumber, _WireOrdering, ordinal, new[] { fldChosenDisplayNumbers.Get()[index].ToString() }),
            makeQuestion(Question.WireOrderingWireColor, _WireOrdering, ordinal, new[] { colors[fldChosenColorsWire.Get()[index]] }));
    }

    private IEnumerable<object> ProcessWireSequence(KMBombModule module)
    {
        var component = GetComponent(module, "WireSequenceComponent");
        var fldSolved = GetField<bool>(component, "IsSolved", true);
        var fldWireSequence = GetField<IEnumerable>(component, "wireSequence");
        if (fldSolved == null || fldWireSequence == null)
            yield break;

        while (!fldSolved.Get()) yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_WireSequence);

        var wireSequence = fldWireSequence.Get();
        if (wireSequence == null) yield break;

        var color = Rnd.Range(0, 3);
        var colorWord = color == 0 ? "black" : (color == 1 ? "blue" : "red");
        var counts = new int[3];
        var typeWireConfiguration = wireSequence.GetType().GetGenericArguments()[0];
        var fldNoWire = GetField<bool>(typeWireConfiguration, "NoWire", true);
        var fldColor = GetField<object>(typeWireConfiguration, "Color", true);
        if (fldNoWire == null || fldColor == null)
            yield break;

        foreach (var item in wireSequence.Cast<object>().Take(12))
        {
            if (!fldNoWire.GetFrom(item))
                ++counts[(int) fldColor.GetFrom(item)];
        }

        var preferredWrongAnswers = new string[4];
        for (int i = 0; i < 3; ++i)
            preferredWrongAnswers[i] = counts[i].ToString();
        preferredWrongAnswers[3] = (counts[color] == 0 ? 1 : counts[color] - 1).ToString();
        addQuestion(module, Question.WireSequenceColorCount, new[] { colorWord }, new[] { counts[color].ToString() }, preferredWrongAnswers);
    }

    // Function used by modules in the World Mazes mod (currently: USA Maze, DACH Maze)
    private IEnumerable<object> ProcessWorldMaze(KMBombModule module, string script, string moduleCode, Question question)
    {
        var comp = GetComponent(module, script);
        var fldOrigin = GetField<string>(comp, "_originState");
        var fldActive = GetField<bool>(comp, "_isActive");
        var mthGetStates = GetMethod<List<string>>(comp, "GetAllStates", 0);
        var mthGetName = GetMethod<string>(comp, "GetStateFullName", 1);

        // wait for activation
        while (!_isActivated)
            yield return new WaitForSeconds(.1f);

        // then wait for the module to get solved
        while (fldActive.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(moduleCode);

        var stateCodes = mthGetStates.Invoke();
        if (stateCodes == null)
            yield break;

        var states = stateCodes.Select(code => mthGetName.Invoke(code)).ToArray();
        var origin = mthGetName.Invoke(fldOrigin.Get());
        if (!states.Contains(origin))
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning {1} because '_originState' was not contained in the list of all states ({2} not in: {3}).", _moduleId, module.ModuleDisplayName, origin, states.JoinString(", "));
            yield break;
        }

        addQuestions(module, makeQuestion(question, moduleCode, correctAnswers: new[] { origin }, preferredWrongAnswers: states));
    }

    private IEnumerable<object> ProcessYahtzee(KMBombModule module)
    {
        var comp = GetComponent(module, "YahtzeeModule");
        var fldDiceValues = GetArrayField<int>(comp, "_diceValues");
        var fldSolved = GetField<bool>(comp, "_isSolved");

        // This array only changes its contents, it’s never reassigned, so we only need to get it once
        var diceValues = fldDiceValues.Get();

        while (diceValues.Any(v => v == 0))
            yield return new WaitForSeconds(.1f);

        string result;

        // Capture the first roll
        if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 5))
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Yahtzee because the first roll was a Yahtzee.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            result = null;
        }
        else if (diceValues.Contains(2) && diceValues.Contains(3) && diceValues.Contains(4) && diceValues.Contains(5) && (diceValues.Contains(1) || diceValues.Contains(6)))
            result = "large straight";
        else if (diceValues.Contains(3) && diceValues.Contains(4) && (
            (diceValues.Contains(1) && diceValues.Contains(2)) ||
            (diceValues.Contains(2) && diceValues.Contains(5)) ||
            (diceValues.Contains(5) && diceValues.Contains(6))))
            result = "small straight";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 4))
            result = "four of a kind";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 3) && Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 2))
            result = "full house";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 3))
            result = "three of a kind";
        else if (Enumerable.Range(1, 6).Count(i => diceValues.Count(val => val == i) == 2) == 2)
            result = "two pairs";
        else if (Enumerable.Range(1, 6).Any(i => diceValues.Count(val => val == i) == 2))
            result = "pair";
        else
        {
            Debug.LogFormat("[Souvenir #{0}] No question for Yahtzee because the first roll was nothing.", _moduleId);
            _legitimatelyNoQuestions.Add(module);
            result = null;
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Yahtzee);

        if (result != null)
            addQuestion(module, Question.YahtzeeInitialRoll, correctAnswers: new[] { result });
    }

    private IEnumerable<object> ProcessYellowArrows(KMBombModule module)
    {
        var comp = GetComponent(module, "YellowArrowsScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldLetIndex = GetIntField(comp, "letindex");

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_YellowArrows);

        int letterIndex = fldLetIndex.Get();
        if (letterIndex < 0 || letterIndex > 25)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Yellow Arrows because ‘letindex’ points to illegal letter: {1} (expected 0–25).", _moduleId, letterIndex);
            yield break;
        }
        addQuestion(module, Question.YellowArrowsStartingRow, correctAnswers: new[] { ((char) ('A' + letterIndex)).ToString() });
    }

    private IEnumerable<object> ProcessZoni(KMBombModule module)
    {
        var comp = GetComponent(module, "ZoniModuleScript");
        var fldSolved = GetField<bool>(comp, "moduleSolved");
        var fldButtons = GetArrayField<KMSelectable>(comp, "buttons", isPublic: true);
        var fldWords = GetArrayField<string>(comp, "wordlist", isPublic: true);
        var fldIndex = GetIntField(comp, "wordIndex");
        var fldStage = GetIntField(comp, "solvedStages");

        List<int> wordsAnswered = new List<int>();

        KMSelectable[] buttons = fldButtons.Get();
        string[] words = fldWords.Get();
        int index = fldIndex.Get();
        int stage = fldStage.Get();

        if (buttons == null || words == null)
            yield break;
        if (index < 0 || index >= words.Length)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Zoni because 'wordIndex' points to illegal word: {1}.", _moduleId, index);
            yield break;
        }
        if (stage != 0)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Zoni because 'solvedStages' did not start at 0: was {1}.", _moduleId, stage);
            yield break;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            var prevInteract = buttons[i].OnInteract;
            buttons[i].OnInteract = delegate
            {
                var ret = prevInteract();
                var st = fldStage.Get();
                if (stage != st)  // If they are equal, the user got a strike
                {
                    wordsAnswered.Add(index);
                    stage = st;
                }
                index = fldIndex.Get();
                return ret;
            };
        }

        while (!fldSolved.Get())
            yield return new WaitForSeconds(.1f);
        _modulesSolved.IncSafe(_Zoni);

        if (wordsAnswered.Count != 3)
        {
            Debug.LogFormat("<Souvenir #{0}> Abandoning Zoni because the received number of valid words was not 3: was {1}.", _moduleId, wordsAnswered.Count);
            yield break;
        }

        addQuestions(module,
            makeQuestion(Question.ZoniWords, _Zoni, new[] { "first" }, new[] { words[wordsAnswered[0]] }, words),
            makeQuestion(Question.ZoniWords, _Zoni, new[] { "second" }, new[] { words[wordsAnswered[1]] }, words),
            makeQuestion(Question.ZoniWords, _Zoni, new[] { "third" }, new[] { words[wordsAnswered[2]] }, words));
    }
    #endregion

    #region Twitch Plays
    [NonSerialized]
    public bool TwitchPlaysActive = false;
    [NonSerialized]
    public List<KMBombModule> TwitchAbandonModule = new List<KMBombModule>();
    public static readonly string TwitchHelpMessage = @"!{0} answer 3 [order is from top to bottom, then left to right]";

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (Application.isEditor && !TwitchPlaysActive && command == "tp")
        {
            ActivateTwitchPlaysNumbers();
            TwitchPlaysActive = true;
            yield break;
        }

        if (Application.isEditor)
        {
            var questions = Ut.GetEnumValues<Question>();
            var i = 0;
            do
            {
                Answers[1].OnInteract();
                i++;
            }
            while ((_currentQuestion == null || !_currentQuestion.QuestionText.ContainsIgnoreCase(command)) && i < questions.Length);
            yield break;
        }

        var m = Regex.Match(command.ToLowerInvariant(), @"\A\s*answer\s+(\d)\s*\z");
        if (!m.Success || _isSolved)
            yield break;

        int number;
        if (_animating || _currentQuestion == null)
        {
            yield return "sendtochaterror {0}, there is no question active right now on module {1} (Souvenir).";
            yield break;
        }
        if (!int.TryParse(m.Groups[1].Value, out number) || number <= 0 || number > Answers.Length || Answers[number - 1] == null || !Answers[number - 1].gameObject.activeSelf)
        {
            yield return string.Format("sendtochaterror {{0}}, that’s not a valid answer; give me a number from 1 to {0}.", Answers.Count(a => a != null && a.gameObject.activeSelf));
            yield break;
        }

        yield return null;
        if (_currentQuestion.CorrectIndex == number - 1)
            yield return "awardpoints 1";
        yield return new[] { Answers[number - 1] };
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (true)
        {
            while (_currentQuestion == null)
            {
                if (_isSolved)
                    yield break;
                yield return true;
            }

            Answers[_currentQuestion.CorrectIndex].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    private void ActivateTwitchPlaysNumbers()
    {
        AnswersParent.transform.localPosition = new Vector3(.005f, 0, 0);
        foreach (var gobj in TpNumbers)
            gobj.SetActive(true);
    }
    #endregion
}
