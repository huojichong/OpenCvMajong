using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.XFeatures2D;

namespace OpenCvMajong.Qwen3;


// 使用示例
class MahjongExecute
{
    public static void Execute()
    {
        // 1. 加载所有模板图像
        var templates = new Dictionary<string, Mat>();
        // 这里需要你提供具体的模板文件路径
        var files = Directory.GetFiles("Cards", "*.png");
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            templates.Add(fileName, Cv2.ImRead(file,ImreadModes.Grayscale));
        }

        // 2. 创建识别器实例
        var recognizer = new MahjongRecognizerWithFeatures(templates);
        using var sift = SIFT.Create();
        // using var surf = SURF.Create(200);
        using var bfMatcher = new BFMatcher();
        // using var flannMatcher = new FlannBasedMatcher();
        
        Recognize(recognizer, sift, bfMatcher);
        // Recognize(recognizer, sift, flannMatcher);

        // Recognize(recognizer, surf, bfMatcher);
        // Recognize(recognizer, ORB.Create(), bfMatcher);
        // Recognize(recognizer, surf, flannMatcher);
        
        Cv2.WaitKey();
    }

    private static void Recognize(MahjongRecognizerWithFeatures recognizer,Feature2D detector, DescriptorMatcher matcher)
    {
        recognizer.SetupDetector(detector, matcher);
        // 3. 识别棋盘
        try
        {
            string[,] board = recognizer.RecognizeMahjongBoard("Pics/Daily_Mahjong_Match.jpg");
            Print(recognizer,board);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"识别失败: {ex.Message}");
        }

    }

    private static void Print(MahjongRecognizerWithFeatures recognize,string[,] board)
    {
        Console.WriteLine($"{recognize._detector} == {recognize._matcher} => 识别结果：");
        for (int i = 0; i < board.GetLength(0); i++)
        {
            for (int j = 0; j < board.GetLength(1); j++)
            {
                if (board[i, j] != SampleBoards.EaseBoard3[i, j].ToString())
                {
                    Console.WriteLine($"{i}:{j} => recognize is {board[i, j]} , record is {SampleBoards.EaseBoard3[i, j]}");
                }
            }
            Console.WriteLine();
        }
    }
}