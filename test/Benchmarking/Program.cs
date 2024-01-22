using System.Text;












//using MediaOrganizer.Helpers;
//using BenchmarkDotNet.Attributes;
//using BenchmarkDotNet.Running;
//using MediaOrganizer;
//using BenchmarkDotNet.Jobs;

//#region Manual
////#region Commands
////const string ExifDefaultCreationTag = "CreateDate";
////const char ExifTagsWriteSeparator = ' ';
////const string ExifDateFormat = "yyyy:MM:dd HH:mm:sszzz";

////var ExifWriteAllDatesValuePlaceHolder = Guid.NewGuid().ToString();
////var ExifTargetedDateTimeTags = new[] { ExifDefaultCreationTag, "FileCreateDate", "FileModifyDate" };
////var ExifWriteAllDatesArgs = new string(string.Join(ExifTagsWriteSeparator,
////                ExifTargetedDateTimeTags.Select(i => $"\"-{i}={ExifWriteAllDatesValuePlaceHolder}\""))
////                .ToArray());

////var Commands = new string[]
////{
////    "-ver",
////    $"-s -AllDates -FileCreateDate -FileModifyDate -FileAccessDate -{ExifDefaultCreationTag}",
////    $"{ExifWriteAllDatesArgs.Replace(ExifWriteAllDatesValuePlaceHolder, DateTime.Now.ToString(ExifDateFormat))}"
////};

////var path = @"C:\Data\SynologyMediaHelperTEST\TEST\IMAGE.jpg";
////Commands[1] += $" {path}";
////Commands[2] += $" {path}";
////#endregion

////var helper = new ExifHelper();
////var o11 = helper.ExecuteTEST(Commands[0]);
////var o12 = helper.ExecuteTEST(Commands[1]);
////var o13 = helper.ExecuteTEST(Commands[2]);
////helper.Dispose();

////var wrapper = new ExifWrapper();
////var o21 = wrapper.Execute(Commands[0]);
////var o22 = wrapper.Execute(Commands[1]);
////var o23 = wrapper.Execute(Commands[2]);
////wrapper.Dispose();

////LogHelper.Warning("\nPress any key to exit.");
////LogHelper.ReadKey();
//#endregion

////| Method | Mean | Error | StdDev | Allocated |
////| -------- | -----------:| ----------:| ----------:| ----------:|
////| Helper | 5,623.2 ms | 105.57 ms | 112.95 ms | 1186 KB |
////| Wrapper | 781.7 ms | 11.17 ms | 10.45 ms | 596.89 KB |


////new Benchmarker().Helper();
////new Benchmarker().Wrapper();

//var summary = BenchmarkRunner.Run<Benchmarker>();

//[MemoryDiagnoser]
//[SimpleJob(RuntimeMoniker.NativeAot80)]
//public class Benchmarker
//{
//    #region Fields
//    private int TasksCount = 2;
//    private int Iterations = 10;
//    private FileInfo Image = new FileInfo(@"C:\Data\SynologyMediaHelperTEST\TEST\IMAGE.jpg");

//    private static string[] Commands;
//    private static string[] Images;
//    #endregion

//    #region Defaults
//    private bool Initialized;
//    private object tool;

//    public Benchmarker() => Setup();

//    [GlobalSetup]
//    public void Setup()
//    {
//        if (Initialized) return;
//        Initialized = true;

//        Initialize();
//    }
//    #endregion

//    private void Initialize()
//    {
//        #region Commands
//        const string ExifDefaultCreationTag = "CreateDate";
//        const char ExifTagsWriteSeparator = ' ';
//        const string ExifDateFormat = "yyyy:MM:dd HH:mm:sszzz";

//        var ExifWriteAllDatesValuePlaceHolder = Guid.NewGuid().ToString();
//        var ExifTargetedDateTimeTags = new[] { ExifDefaultCreationTag, "FileCreateDate", "FileModifyDate" };
//        var ExifWriteAllDatesArgs = new string(string.Join(ExifTagsWriteSeparator,
//                        ExifTargetedDateTimeTags.Select(i => $"\"-{i}={ExifWriteAllDatesValuePlaceHolder}\""))
//                        .ToArray());

//        Commands =
//        [
//            "-ver",
//            $"-s -AllDates -FileCreateDate -FileModifyDate -FileAccessDate -{ExifDefaultCreationTag}",
//            $"{ExifWriteAllDatesArgs.Replace(ExifWriteAllDatesValuePlaceHolder, DateTime.Now.ToString(ExifDateFormat))}"
//        ];
//        #endregion

//        #region Data
//        var dir = Image.Directory;
//        var tmp = new DirectoryInfo(Path.Combine(dir.FullName, "TEMP"));

//        if (!tmp.Exists) tmp.Create();

//        Images = new string[Iterations];
//        for (int i = 0; i < Iterations; i++)
//        {
//            Images[i] = Path.Combine(tmp.FullName, $"{i}{Image.Extension}");
//            Image.CopyTo(Images[i], true);
//        }
//        #endregion
//    }

//    [Benchmark]
//    public void Helper()
//    {
//        RunExcution(
//            (ExifHelper exif, string input) => exif.ExecuteTEST(input),
//            () => new ExifHelper(true, true), TasksCount, Iterations);

//        //ExifHelper.CleanUp();
//    }

//    [Benchmark]
//    public void Wrapper()
//    {
//        RunExcution(
//            (ExifWrapper exif, string input) => exif.Execute(input),
//            () => new ExifWrapper(), TasksCount, Iterations);

//        //ExifWrapper.CleanUp();
//    }

//    private static int GetIteratedIndex(long number, int count) => (int)(number % count);
//    private static void RunExcution<TArg>(Func<TArg, string, string> execute, Func<TArg> getPerTaskArgument, int tasksCount, int iterations)
//    {
//        new int[iterations]
//            .ParallelForEachTask(tasksCount, (int num, long index, TArg arg) =>
//            {
//                execute(arg, Commands[0]);
//                execute(arg, $"{Commands[1]} \"{Images[index]}\"");
//                execute(arg, $"{Commands[2]} \"{Images[index]}\"");
//            },
//            () => getPerTaskArgument()).Wait();
//    }
//}