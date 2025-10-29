using OpenCvSharp;

namespace MahjongRecognizer;


// 使用示例
class MahjongBoard
{
    public static void Execute(string[] args)
    {
        // 1. 加载所有模板图像
        var templates = new Dictionary<string, Mat>();
        // 这里需要你提供具体的模板文件路径
        var files = Directory.GetFiles("Cards", "*.png");
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            templates.Add(fileName, Cv2.ImRead(file));
        }
        // 2. 创建识别器实例
        var recognizer = new MahjongRecognizerWithFeatures(templates);

        // 3. 识别棋盘
        try
        {
            string[,] board = recognizer.RecognizeMahjongBoard("Pics/Daily_Mahjong_Match.jpg");
            Console.WriteLine("识别结果：");
            for (int i = 0; i < board.GetLength(0); i++)
            {
                for (int j = 0; j < board.GetLength(1); j++)
                {
                    Console.Write($"{board[i, j],-4} ");
                }
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"识别失败: {ex.Message}");
        }

        Cv2.WaitKey();
    }
}