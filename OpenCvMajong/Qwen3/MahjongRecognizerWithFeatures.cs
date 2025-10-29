using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;

public class MahjongRecognizerWithFeatures
{
    private readonly Dictionary<string, Mat> _templates; // 模板字典
    private readonly Feature2D _detector; // 特征检测器

    public MahjongRecognizerWithFeatures(Dictionary<string, Mat> templates)
    {
        _templates = templates;
        // 修正：直接赋值 Feature2D 实例，不需要 Ptr<T>
        _detector = OpenCvSharp.ORB.Create();
        // 如果你想使用 SIFT (注意：SIFT 在 OpenCV 中可能需要非自由模块)
        // _detector = OpenCvSharp.SIFT.Create();
    }

    /// <summary>
    /// 从游戏截图中识别所有麻将牌
    /// </summary>
    /// <param name="screenshot">游戏截图</param>
    /// <returns>二维数组，表示每个位置的牌面类型</returns>
    public string[,] RecognizeMahjongBoard(Mat screenshot)
    {
        // 1. 预处理：转换为灰度图
        Mat grayScreenshot = new Mat();
        Cv2.CvtColor(screenshot, grayScreenshot, ColorConversionCodes.BGR2GRAY);

        // 2. 定位棋盘区域 (这里需要你根据实际截图来确定棋盘的ROI)
        Rect boardRect = new Rect(50, 100, 900, 700); // 示例坐标，请根据你的截图调整
        Mat boardRegion = grayScreenshot[boardRect];

        // 3. 分割棋盘为一个个单元格
        int rows = 10;
        int cols = 9;
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

                // 5. 提取当前单元格的特征点
                KeyPoint[] keypointsCell;
                Mat descriptorsCell = new Mat();
                _detector.DetectAndCompute(cell, null, out keypointsCell, descriptorsCell);

                if (keypointsCell.Length == 0 || descriptorsCell.Empty())
                {
                    result[row, col] = "空";
                    continue;
                }

                // 6. 对当前单元格与所有模板进行特征匹配
                string bestMatch = "未知";
                int bestInlierCount = 0;

                foreach (var kvp in _templates)
                {
                    string 牌名 = kvp.Key;
                    Mat template = kvp.Value;

                    // 提取模板的特征点
                    KeyPoint[] keypointsTemplate;
                    Mat descriptorsTemplate = new Mat();
                    _detector.DetectAndCompute(template, null, out keypointsTemplate,  descriptorsTemplate);

                    if (keypointsTemplate.Length == 0 || descriptorsTemplate.Empty())
                        continue;

                    // 7. 使用 BFMatcher 进行特征匹配
                    var matcher = new BFMatcher(NormTypes.Hamming, crossCheck: false);
                    DMatch[] matches = matcher.Match(descriptorsCell, descriptorsTemplate);

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
                        Mat homography = new Mat();
                        OutputArray mask =  OutputArray.Create(homography);
                        // 调用 FindHomography
                        // Cv2.FindHomography(
                        //     srcList, // 源点
                        //     dstList, // 目标点
                        //     HomographyMethods.Ransac, // 方法
                        //     3.0, // 距离阈值
                        //     mask // 输出掩码 (用于存储内点/外点标记)
                        //     // 最后两个参数 maxIters 和 confidence 通常可以省略，使用默认值
                        // );

                        // 计算内点数量
                        int inlierCount = 0;
                        // for (int i = 0; i < mask.Rows; i++)
                        // {
                        //     if (mask.At<byte>(i, 0) > 0)
                        //         inlierCount++;
                        // }

                        // 更新最佳匹配
                        if (inlierCount > bestInlierCount)
                        {
                            bestInlierCount = inlierCount;
                            bestMatch = 牌名;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"RANSAC 匹配 {牌名} 时出错: {ex.Message}");
                        continue;
                    }
                }

                // 10. 设置阈值，如果内点数太少，则认为没有匹配
                result[row, col] = bestInlierCount > 5 ? bestMatch : "空"; // 阈值可以根据实际情况调整

                // 可选：在原图上画框标记识别结果
                // Cv2.Rectangle(screenshot, new Point(col * cellWidth + boardRect.X, row * cellHeight + boardRect.Y),
                //               new Point((col + 1) * cellWidth + boardRect.X, (row + 1) * cellHeight + boardRect.Y),
                //               Scalar.Red, 2);
            }
        }

        return result;
    }
}