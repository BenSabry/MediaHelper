using System.Globalization;

//var s = "2020:06:13 10:18:43+03:00";
//var format = "yyyy:MM:dd HH:mm:ss";
//DateTime result;

////TODO: not parsing even with correct format
//DateTime.TryParseExact(s, format, default, DateTimeStyles.None, out result);

//Console.WriteLine(result);

//var length = 100;
//for (int i = 0; i <= length; i++)
//    Console.WriteLine(Engine.GenerateConsoleProgressBar(i, length));

string dateString = "2020:06:13 10:18:43+03:00";
string format = "yyyy:MM:dd HH:mm:sszzz";
DateTime result;

if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
{
    Console.WriteLine("Date parsed successfully.");
}
else
{
    Console.WriteLine("Date parsing failed.");
}












//using BenchmarkDotNet.Attributes;
//using BenchmarkDotNet.Jobs;
//using BenchmarkDotNet.Running;
//using MediaOrganizer;
//using MediaOrganizer.Core;
//using MediaOrganizer.Helpers;

//var summary = BenchmarkRunner.Run<Benchmarker>();

//[MemoryDiagnoser]
////[SimpleJob(RuntimeMoniker.NativeAot80)]
//public class Benchmarker
//{
//    private FileInfo[] Files;
//    private int TasksCount = 8;
//    private int FilesCount = 100;

//    private bool Initialized;
//    public Benchmarker() => Setup();

//    [GlobalSetup]
//    public void Setup()
//    {
//        if (Initialized) return;
//        Initialized = true;

//        Initialize();
//    }

//    private void Initialize() => Files = Engine.InitAndGetFilesForTest().Take(FilesCount).ToArray();

//    [Benchmark]
//    public void ForEachUsingTasks() => Files.ForEachUsingTasks((file, index, arg) => { ProcessFile(file, index, arg.exif, arg.logger); }, CreateTaskArg, TasksCount);

//    [Benchmark]
//    public void ParallelForEach() => Files.ParallelForEach((file, index, arg) => { ProcessFile(file, index, arg.exif, arg.logger); }, CreateTaskArg, TasksCount);

//    private static void ProcessFile(FileInfo file, long index, ExifHelper exif, LogHelper logger)
//        => exif.TryReadMediaDefaultCreationDate(file.FullName, out var date);
//    private static (ExifHelper exif, LogHelper logger) CreateTaskArg() => (new ExifHelper(), new LogHelper());
//}