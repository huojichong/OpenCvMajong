using OpenCvSharp;
using OpenCvSharp.XFeatures2D;

namespace OpenCvMajong.Qwen3;

public class MahjongRecognizerWithFeatures
{
    private readonly Dictionary<string, Mat> _templates; // 模板字典
    public Feature2D _detector { get; private set; } // 特征检测器
    public DescriptorMatcher _matcher { get; private set; }

    public MahjongRecognizerWithFeatures(Dictionary<string, Mat> templates)
    {
        _templates = templates;
    }

    public MahjongRecognizerWithFeatures(Dictionary<string, Mat> templates,Feature2D detector, DescriptorMatcher mather)
    {
        this._templates = templates;
        this._matcher = mather;
        // 修正：直接赋值 Feature2D 实例，不需要 Ptr<T>
        this._detector = detector;
        // 如果你想使用 SIFT (注意：SIFT 在 OpenCV 中可能需要非自由模块)
        // _detector = OpenCvSharp.SIFT.Create();
    }
    
    public void SetupDetector(Feature2D detector, DescriptorMatcher mather)
    {
        this._matcher = mather;
        // 修正：直接赋值 Feature2D 实例，不需要 Ptr<T>
        this._detector = detector;
    }

    /// <summary>
    /// 从游戏截图中识别所有麻将牌
    /// </summary>
    /// <param name="screenshot">游戏截图</param>
    /// <returns>二维数组，表示每个位置的牌面类型</returns>
    public string[,] RecognizeMahjongBoard(string  filepath)
    {
        if(_matcher == null || _detector == null)
            throw new InvalidOperationException("请先设置特征检测器和匹配器");
        // 1. 预处理：转换为灰度图
        var screenshot = Cv2.ImRead(filepath);
        Mat grayScreenshot = new Mat();
        Cv2.CvtColor(screenshot, grayScreenshot, ColorConversionCodes.BGR2GRAY);

        // 2. 定位棋盘区域 (这里需要你根据实际截图来确定棋盘的ROI)
        Rect boardRect = new Rect(38, 570, 1005, 1210); // 示例坐标，请根据你的截图调整
        Mat boardRegion = grayScreenshot[boardRect];

        Cv2.ImShow("board",boardRegion);
        // 3. 分割棋盘为一个个单元格
        int rows = 12;
        int cols = 10;
        int cellHeight = boardRegion.Height / rows;
        int cellWidth = boardRegion.Width / cols;

        string[,] result = new string[rows, cols];

        // 4. 遍历每个单元格
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // 提取当前单元格
                Rect cellRect = new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                Mat cell = boardRegion[cellRect];

                Cv2.ImWrite("tempBoard/"+row + "_" + col + ".png", cell);
                MatchTemplate(result, cell, row, col);
            }
        }
        return result;
    }

    private void MatchTemplate(string[,] result, Mat cell, int row, int col)
    {
        string bestMatch = "未知";
        int bestInlierCount = 0;

        foreach (var kvp in _templates)
        {
            string cardName = kvp.Key;
            Mat template = kvp.Value;
            Mat newTemplate = template.Clone();
            if (cell.Width > template.Width || cell.Height > template.Height)
            {
                var newWidth = template.Width * 0.7;
                var newHeight = template.Height * 0.7f;
                Cv2.Resize(template, newTemplate, new Size(newWidth, newHeight));
                template = newTemplate;
            }

            // 7. 使用 Template Matching 进行匹配
            using var matchResult = new Mat();
            Cv2.MatchTemplate(cell, template, matchResult, TemplateMatchModes.CCoeff);

            // 找到最佳匹配位置
            Cv2.MinMaxLoc(matchResult, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

            // 设置阈值，判断是否匹配成功
            // if (maxVal > 0.7) // 阈值可以根据实际情况调整
            {
                // 计算内点数量（这里简单使用匹配值作为内点数量的近似）
                int inlierCount = (int)(maxVal * 100); // 简单放大以便比较

                // 更新最佳匹配
                if (inlierCount > bestInlierCount)
                {
                    bestInlierCount = inlierCount;
                    bestMatch = cardName;
                }
            }
        }
        
        result[row, col] = bestMatch;
    }

    private void DetectAndCompute(string[,] result, Mat cell, int row, int col)
    {
        // 5. 提取当前单元格的特征点
        using var descriptorsCell = new Mat<double>();
        _detector.DetectAndCompute(cell, null, out var keypointsCell, descriptorsCell);

        if (keypointsCell.Length == 0 || descriptorsCell.Rows == 0)
        {
            result[row, col] = "Emp";
            return;
        }

        // 6. 对当前单元格与所有模板进行特征匹配
        string bestMatch = "未知";
        int bestInlierCount = 0;

        foreach (var kvp in _templates)
        {
            string cardName = kvp.Key;
            Mat template = kvp.Value;

            // 提取模板的特征点
            KeyPoint[] keypointsTemplate;
            Mat descriptorsTemplate = new Mat();
            _detector.DetectAndCompute(template, null, out keypointsTemplate, descriptorsTemplate);

            if (keypointsTemplate.Length == 0 || descriptorsTemplate.Empty())
                continue;

            // 7. 使用 BFMatcher 进行特征匹配
            DMatch[] matches = this._matcher.Match(descriptorsCell, descriptorsTemplate);

            // 8. 使用 RANSAC 筛选内点，提高匹配鲁棒性
            if (matches.Length < 4) // 至少需要4个匹配点才能进行透视变换
                continue;

            // 准备匹配点坐标
            Point2f[] srcPoints = keypointsCell.Select(k => k.Pt).ToArray();
            Point2f[] dstPoints = keypointsTemplate.Select(k => k.Pt).ToArray();

            List<Point2f> srcList = new List<Point2f>();
            List<Point2f> dstList = new List<Point2f>();

            foreach (var match in matches)
            {
                srcList.Add(srcPoints[match.QueryIdx]);
                dstList.Add(dstPoints[match.TrainIdx]);
            }

            // 9. 使用 findHomography 和 RANSAC 筛选内点
            try
            {

                Mat mask = new Mat();
                Mat homography = Cv2.FindHomography(srcList.Select(m => new Point2d(m.X, m.Y)),
                    dstList.Select(m => new Point2d(m.X, m.Y)), HomographyMethods.Ransac, 3.0, mask);

                // 计算内点数量
                int inlierCount = 0;
                for (int i = 0; i < mask.Rows; i++)
                {
                    if (mask.At<byte>(i, 0) > 0)
                        inlierCount++;
                }

                // 更新最佳匹配
                if (inlierCount > bestInlierCount)
                {
                    bestInlierCount = inlierCount;
                    bestMatch = cardName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RANSAC 匹配 {cardName} 时出错: {ex.Message}");
                continue;
            }
        }

        // 10. 设置阈值，如果内点数太少，则认为没有匹配
        result[row, col] = bestInlierCount > 5 ? bestMatch : "空"; // 阈值可以根据实际情况调整

        // 可选：在原图上画框标记识别结果
        // Cv2.Rectangle(screenshot, new Point(col * cellWidth + boardRect.X, row * cellHeight + boardRect.Y),
        //     new Point((col + 1) * cellWidth + boardRect.X, (row + 1) * cellHeight + boardRect.Y),
        //     Scalar.Red, 2);
    }
}