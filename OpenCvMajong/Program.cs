using MahjongRecognizer;
using OpenCvMajong;
using OpenCvSharp;

class Program
{
    static void Main(string[] args)
    {
        MahjongBoard.Execute(args:args);
        
        // var files = Directory.GetFiles("Cards", "*.png");
        // foreach (var file in files)
        // {
        //     // Cards cards;
        //     var fileName = Path.GetFileNameWithoutExtension(file);
        //     var newName = (Cards)Convert.ToInt32(fileName);
        //     var mat = Cv2.ImRead(file);
        //     mat.ImWrite("Out/" + newName + ".png");
        //     // File.Move(file,);
        // }
        
        
        
        // var mat = Cv2.ImRead("Pics/Daily_Mahjong_Match.jpg");
        // for (int i = 1005; i <= 1010; i+=10)
        // {
        //     for (int j = 1200; j <= 1200; j+=10)
        //     {
        //         Rect boardRect = new Rect(38, 570, 1005, 1210); // 根据你的截图调整
        //         var boardMat = new Mat(mat, boardRect);
        //         Cv2.ImShow(i + "_" + j, boardMat);
        //     }
        // }

        
        // Cv2.WaitKey();
        // MahjongClassifier classifier = new MahjongClassifier("Pics/Daily_Mahjong_Match.jpg");
        // classifier.Execute();
        // // 读取大图和小图（模板）
        // Mat sourceImage = new Mat("Pics/Daily_Mahjong_Match.jpg", ImreadModes.Color);
        // Mat templateImage = new Mat("Cards/43.png", ImreadModes.Color);
        //
        // // 查找所有匹配位置
        // List<Rect> matches = FindTemplateMatchesMultiScale(sourceImage, templateImage, 0.8);
        //
        // // 在原图上标记所有找到的位置
        // Mat resultImage = sourceImage.Clone();
        // foreach (Rect match in matches)
        // {
        //     Cv2.Rectangle(resultImage, match, Scalar.Red, 2);
        // }
        //
        // // 显示结果
        // Cv2.ImShow("Result", resultImage);
        // Cv2.WaitKey(0);
        // Cv2.DestroyAllWindows();
    }

    public static List<Rect> FindTemplateMatchesMultiScale(Mat source, Mat template,
        double scaleMin = 0.5, double scaleMax = 1.0, double scaleStep = 0.05, double threshold = 0.8)
    {
        var allMatches = new List<Rect>();

        Mat sourceGray = new Mat();
        Mat templateGray = new Mat();

        if (source.Channels() == 3)
            Cv2.CvtColor(source, sourceGray, ColorConversionCodes.BGR2GRAY);
        else
            sourceGray = source;

        if (template.Channels() == 3)
            Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);
        else
            templateGray = template;

        // 多尺度搜索
        for (double scale = scaleMin; scale <= scaleMax; scale += scaleStep)
        {
            Console.WriteLine("current scale:" + scale);
            // 缩放模板
            Mat resizedTemplate = new Mat();
            Cv2.Resize(templateGray, resizedTemplate,
                new Size(templateGray.Width * scale, templateGray.Height * scale));

            // 如果缩放后的模板比原图大，跳过
            if (resizedTemplate.Width > sourceGray.Width || resizedTemplate.Height > sourceGray.Height)
                continue;

            Mat result = new Mat();
            Cv2.MatchTemplate(sourceGray, resizedTemplate, result, TemplateMatchModes.CCoeffNormed);

            // 找到匹配位置
            result.MinMaxLoc(out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

            if (maxVal >= threshold)
            {
                Rect matchRect = new Rect(maxLoc, resizedTemplate.Size());
                allMatches.Add(matchRect);
                Console.WriteLine($"找到匹配位置: X={matchRect.X}, Y={matchRect.Y}, Width={matchRect.Width}, Height={matchRect.Height}");
            }
        }

        return allMatches;
    }

    public static List<Rect> FindTemplateMatchesWithNMS(Mat source, Mat template,
        double threshold = 0.8, double nmsThreshold = 0.3)
    {
        var matches = new List<Rect>();
        var scores = new List<double>();

        Mat sourceGray = new Mat();
        Mat templateGray = new Mat();

        if (source.Channels() == 3)
            Cv2.CvtColor(source, sourceGray, ColorConversionCodes.BGR2GRAY);
        else
            sourceGray = source;

        if (template.Channels() == 3)
            Cv2.CvtColor(template, templateGray, ColorConversionCodes.BGR2GRAY);
        else
            templateGray = template;

        Mat result = new Mat();
        Cv2.MatchTemplate(sourceGray, templateGray, result, TemplateMatchModes.CCoeffNormed);

        // 收集所有超过阈值的匹配
        for (int y = 0; y < result.Height; y++)
        {
            for (int x = 0; x < result.Width; x++)
            {
                double score = result.At<float>(y, x);
                if (score >= threshold)
                {
                    matches.Add(new Rect(x, y, template.Width, template.Height));
                    scores.Add(score);
                }
            }
        }

        // 应用非极大值抑制
        return ApplyNMS(matches, scores, nmsThreshold);
    }

    private static List<Rect> ApplyNMS(List<Rect> boxes, List<double> scores, double threshold)
    {
        if (boxes.Count == 0) return new List<Rect>();

        // 根据分数排序
        var indices = new List<int>();
        for (int i = 0; i < boxes.Count; i++) indices.Add(i);

        indices.Sort((a, b) => scores[b].CompareTo(scores[a]));

        var picked = new List<int>();

        while (indices.Count > 0)
        {
            int current = indices[0];
            picked.Add(current);

            for (int i = indices.Count - 1; i > 0; i--)
            {
                int idx = indices[i];
                double iou = CalculateIOU(boxes[current], boxes[idx]);

                if (iou > threshold)
                {
                    indices.RemoveAt(i);
                }
            }

            indices.RemoveAt(0);
        }

        var result = new List<Rect>();
        foreach (int index in picked)
        {
            result.Add(boxes[index]);
        }

        return result;
    }

    private static double CalculateIOU(Rect rect1, Rect rect2)
    {
        Rect intersection = rect1 & rect2;
        double intersectionArea = intersection.Width * intersection.Height;
        double unionArea = (rect1.Width * rect1.Height) + (rect2.Width * rect2.Height) - intersectionArea;

        return intersectionArea / unionArea;
    }
}