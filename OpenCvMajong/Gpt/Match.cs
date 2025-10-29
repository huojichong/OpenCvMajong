
using System.Text.Json;
using OpenCvSharp;

namespace OpenCvMajong;

public class Match
{
    public static void Entry()
    {
        string bigPath = "Pics/Daily_Mahjong_Match.jpg";
        string smallPath = "Cards/43.png";

        using var big = Cv2.ImRead(bigPath, ImreadModes.Color);
        using var bigGray = new Mat();
        Cv2.CvtColor(big, bigGray, ColorConversionCodes.BGR2GRAY);

        // foreach (var smallFile in Directory.GetFiles(smallPath,"*.png",SearchOption.AllDirectories))
        {
            using var small = Cv2.ImRead(smallPath, ImreadModes.Color);
            using var smallGray = new Mat();
            Cv2.CvtColor(small, smallGray, ColorConversionCodes.BGR2GRAY);

            double bestScore = 0;
            double bestScale = 1.0;
            Rect bestRect = new Rect();

            // 多尺度匹配
            // for (double scale = 0.5; scale <= 1.5; scale += 0.05)
            var scale = 0.7;
            {
                int newW = (int)(smallGray.Width * scale);
                int newH = (int)(smallGray.Height * scale);
                if (newW < 5 || newH < 5 || newW > bigGray.Width || newH > bigGray.Height)
                    return;

                using var resized = smallGray.Resize(new Size(newW, newH));

                using var result = bigGray.MatchTemplate(resized, TemplateMatchModes.CCoeffNormed);

                
                using var mask = new Mat();
                double threshold = 0.8;
                Cv2.Threshold(result, mask, threshold, 1.0, ThresholdTypes.Tozero);
                var nonZero = mask.FindNonZero(); // ✅ 正确写法

                if (nonZero.Empty()) return;
                
                // var points = new List<Point>();
                List<Rect> rects = new List<Rect>();
                for (int i = 0; i < nonZero.Rows; i++)
                {
                    var xy = nonZero.At<Point>(i);
                    // points.Add(xy);
                    rects.Add( new Rect(xy.X, xy.Y, resized.Width, resized.Height));
                    Console.WriteLine(xy);
                }
                foreach (var rect in rects)
                {
                    Cv2.Rectangle(big, rect, new Scalar(0, 0, 255), 2);
                }
                
                Console.WriteLine("scale:"+scale+" count:"+rects.Count);
                Cv2.ImShow("big",big);
                // Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

                // if (maxVal > bestScore)
                // {
                //     bestScore = maxVal;
                //     bestScale = scale;
                //     bestRect = new Rect(maxLoc.X, maxLoc.Y, resized.Width, resized.Height);
                //     Cv2.ImShow("aled",result);
                // }
            }

        }

        Cv2.WaitKey();

    }

    class MatchResult
    {
        public string Name { get; set; }
        public double Scale { get; set; }
        public double Score { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public static void Entry2()
    {
        string bigPath = "Pics/Daily_Mahjong_Match.jpg";
        string smallDir = "Cards";
        string outputJson = "result.json";
        
        using var bigColor = Cv2.ImRead(bigPath, ImreadModes.Color);
        if (bigColor.Empty())
        {
            Console.WriteLine("大图加载失败");
            return;
        }
        
        // 转灰度可以提升速度 & 稳定性
        using var bigGray = new Mat();
        Cv2.CvtColor(bigColor, bigGray, ColorConversionCodes.BGR2GRAY);
        
        var results = new List<MatchResult>();

        var random = new Random();
        foreach (var file in Directory.GetFiles(smallDir, "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".png") && !file.EndsWith(".jpg")) continue;
            using var smallColor = Cv2.ImRead(file, ImreadModes.Color);
            if (smallColor.Empty()) continue;
        
            using var smallGray = new Mat();
            Cv2.CvtColor(smallColor, smallGray, ColorConversionCodes.BGR2GRAY);
        
            Console.WriteLine($"🔍 搜索 {Path.GetFileName(file)} ...");
        
            for (double scale = 0.5; scale <= 1.5; scale += 0.05)
            {
                // var color = random.Next(0, 255);
                int w = (int)(smallGray.Width * scale);
                int h = (int)(smallGray.Height * scale);
                if (w < 5 || h < 5 || w > bigGray.Width || h > bigGray.Height) continue;
        
                using var resized = smallGray.Resize(new Size(w, h));
                using var result = bigGray.MatchTemplate(resized, TemplateMatchModes.CCoeffNormed);
        
                double threshold = 0.8;
                // 阈值过滤
                using var mask = new Mat();
                Cv2.Threshold(result, mask, threshold, 1.0, ThresholdTypes.Tozero);
        
                // 找出所有非零位置
                using var nonZero = new Mat();
                Cv2.FindNonZero(mask, nonZero); // ✅ 正确写法

                if (nonZero.Empty()) continue;
        
                var points = new List<Point>();
                for (int i = 0; i < nonZero.Rows; i++)
                {
                    var xy = nonZero.At<Point>(i);
                    points.Add(xy);
                }
                
                foreach (var p in points)
                {
                    double score = result.Get<float>(p.Y, p.X);
                    var rect = new Rect(p.X, p.Y, w, h);
        
                    // 过滤太接近的点（避免重叠）
                    bool overlap = false;
                    foreach (var r in results)
                    {
                        if (r.Name == Path.GetFileName(file) &&
                            Math.Abs(r.X - rect.X) < w / 4 &&
                            Math.Abs(r.Y - rect.Y) < h / 4)
                        {
                            overlap = true;
                            break;
                        }
                    }
        
                    if (!overlap)
                    {
                        results.Add(new MatchResult
                        {
                            Name = Path.GetFileName(file),
                            Scale = scale,
                            Score = score,
                            X = rect.X,
                            Y = rect.Y,
                            Width = rect.Width,
                            Height = rect.Height
                        });
        
                        Cv2.Rectangle(bigColor, rect, new Scalar(0, 0, 255), 2);
                    }
                }
            }
        }

        Cv2.ImWrite("matched_multi.png", bigColor);
        File.WriteAllText("result.json",
            JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        
        Console.WriteLine($"✅ 匹配完成，共 {results.Count} 个匹配点。结果已输出到 result.json 和 matched_multi.png。");

    }
}