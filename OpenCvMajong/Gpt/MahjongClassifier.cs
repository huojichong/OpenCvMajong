using System.Text.Json;
using OpenCvSharp;

namespace OpenCvMajong;

public class MahjongClassifier
{
    protected string filePath { get; set; }
    public MahjongClassifier(string imagePath)
    {
        filePath = imagePath;
    }

    public void Execute()
    {
        string imagePath = filePath;
        Mat src = Cv2.ImRead(imagePath, ImreadModes.Color);
        if (src.Empty())
        {
            Console.WriteLine("无法读取图片");
            return;
        }

        // ---------- 1. 找棋盘的四边形 ---------- 
        if (!TryFindBoardQuad(src, out Point2f[] quad))
        {
            Console.WriteLine("自动检测棋盘失败：无法找到四边形轮廓。请手动提供 boardRect 或调整阈值。");
            // 失败回退：直接用手动矩形（需要你根据图像调整）
            Rect fallback = new Rect(20, 180, 730, 700);
            Mat boardFallback = new Mat(src, fallback);
            ProcessBoard(boardFallback);
            return;
        }

        // 4 点排序（tl,tr,br,bl）
        quad = OrderQuadPoints(quad);

        // ---------- 2. 透视变换到矩形 ----------
        int warpW = 1010; // 目标宽（可调）
        int warpH = 1210; // 目标高（可调），使用正方便于处理
        Point2f[] dst = new Point2f[]
        {
            new Point2f(0,0),
            new Point2f(warpW-1,0),
            new Point2f(warpW-1,warpH-1),
            new Point2f(0,warpH-1)
        };
        Mat M = Cv2.GetPerspectiveTransform(quad, dst);
        Cv2.ImShow("quad", M);
        Mat board = new Mat();
        Cv2.WarpPerspective(src, board, M, new Size(warpW, warpH));
        Cv2.ImWrite("board_warped.png", board);
        // return;
        // ---------- 3. 自动检测行列（投影法） ----------
        if (!TryDetectGrid(board, out int rows, out int cols, out int[,] cellRects))
        {
            Console.WriteLine("网格自动检测失败，回退到默认 10x12。");
            rows = 12; cols = 10;
            cellRects = BuildUniformGrid(warpW, warpH, rows, cols);
        }
        Console.WriteLine($"检测到网格：rows={rows}, cols={cols}");
        // return;
        // ---------- 4. 切格子并聚类 -> 枚举ID ----------
        int[,] boardEnum = new int[rows, cols];
        List<Mat> prototypes = new List<Mat>();
        int nextId = 0;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int x = cellRects[r, c * 4 + 0];
                int y = cellRects[r, c * 4 + 1];
                int w = cellRects[r, c * 4 + 2];
                int h = cellRects[r, c * 4 + 3];
                // 安全裁切
                Rect rc = new Rect(
                    Math.Max(0, x),
                    Math.Max(0, y),
                    Math.Min(board.Width - x, Math.Max(1, w)),
                    Math.Min(board.Height - y, Math.Max(1, h))
                );
                Mat cell = new Mat(board, rc);
                Cv2.ImWrite("cell_r" + r + "_c" + c + ".png", cell);
                Cv2.Resize(cell, cell, new Size(64, 64));
                // 计算相似度，匹配已有 prototype
                int matched = -1;
                double best = -1.0;
                for (int i = 0; i < prototypes.Count; i++)
                {
                    double sim = CompareHistSimilarity(cell, prototypes[i]);
                    if (sim > best) { best = sim; matched = i; }
                }
                double threshold = 0.90; // 相似度阈值：可调（0.85~0.95）
                if (best < threshold || matched == -1)
                {
                    prototypes.Add(cell.Clone());
                    matched = nextId++;
                }
                boardEnum[r, c] = matched;
            }
        }

        Console.WriteLine($"聚类得到 {nextId} 种牌型。");

        // ---------- 输出 JSON（System.Text.Json） ----------
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new Int2DArrayConverter() }
        };
        string json = JsonSerializer.Serialize(boardEnum, options);
        File.WriteAllText("mahjong_board.json", json);
        Console.WriteLine("结果写入 mahjong_board.json");
    }
    
    // 如果自动检测失败，直接处理给定board小图
    void ProcessBoard(Mat board)
    {
        // 简单分割为 8x12 并输出
        int rows = 8, cols = 12;
        int[,] rects = BuildUniformGrid(board.Width, board.Height, rows, cols);
        int[,] ids = new int[rows, cols];
        List<Mat> prototypes = new List<Mat>();
        int nextId = 0;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            int x = rects[r, c * 4 + 0], y = rects[r, c * 4 + 1];
            int w = rects[r, c * 4 + 2], h = rects[r, c * 4 + 3];
            Mat cell = new Mat(board, new Rect(x, y, w, h));
            Cv2.Resize(cell, cell, new Size(64, 64));
            int matched = -1; double best = -1;
            for (int i = 0; i < prototypes.Count; i++)
            {
                double sim = CompareHistSimilarity(cell, prototypes[i]);
                if (sim > best) { best = sim; matched = i; }
            }
            double threshold = 0.90;
            if (best < threshold || matched == -1) { prototypes.Add(cell.Clone()); matched = nextId++; }
            ids[r, c] = matched;
        }
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new Int2DArrayConverter() }
        };
        File.WriteAllText("mahjong_board_fallback.json", JsonSerializer.Serialize(ids, options));
        Console.WriteLine("回退处理完成，输出 mahjong_board_fallback.json");
    }
    
    
    // ---------------- 辅助方法 ----------------

    // 尝试找到图片中最大的四边形轮廓，返回四个顶点（浮点）
    bool TryFindBoardQuad(Mat src, out Point2f[] quad)
    {
        quad = null;
        Mat gray = new Mat();
        Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
        Cv2.GaussianBlur(gray, gray, new Size(5, 5), 0);
        Mat edges = new Mat();
        Cv2.Canny(gray, edges, 50, 150);

        // 膨胀闭运算，填充缝隙
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(5, 5));
        Cv2.MorphologyEx(edges, edges, MorphTypes.Dilate, kernel);

        Point[][] contours;
        HierarchyIndex[] hi;
        Cv2.FindContours(edges, out contours, out hi, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        if (contours.Length == 0) return false;

        // 按面积降序
        var sorted = contours.Select(c => new { Contour = c, Area = Cv2.ContourArea(c) })
                             .OrderByDescending(z => z.Area)
                             .ToList();

        foreach (var item in sorted)
        {
            // 忽略太小
            if (item.Area < 10000) break;

            double peri = Cv2.ArcLength(item.Contour, true);
            Point[] approx = Cv2.ApproxPolyDP(item.Contour, 0.02 * peri, true);
            if (approx.Length == 4)
            {
                // 转换为 Point2f
                quad = approx.Select(p => new Point2f(p.X, p.Y)).ToArray();
                return true;
            }
        }
        return false;
    }

    // 按 tl,tr,br,bl 的顺序排序四点
    Point2f[] OrderQuadPoints(Point2f[] pts)
    {
        if (pts.Length != 4) return pts;
        var list = pts.ToList();
        // 按 x+y 排序：左上最小，右下最大
        list.Sort((a, b) => (a.X + a.Y).CompareTo(b.X + b.Y));
        Point2f tl = list[0];
        Point2f br = list[3];
        // 剩余两个是 tr 与 bl，按 x 进行区分
        Point2f p1 = list[1], p2 = list[2];
        Point2f tr = (p1.X > p2.X) ? p1 : p2;
        Point2f bl = (p1.X > p2.X) ? p2 : p1;
        return new Point2f[] { tl, tr, br, bl };
    }

    // 使用投影法尝试检测 rows 和 cols 并计算每个格子的 Rects
    // 返回 cellRects: an int[rows, cols*4] where for each (r,c) we store x,y,w,h
    bool TryDetectGrid(Mat board, out int rows, out int cols, out int[,] cellRects)
    {
        
        rows = 0; cols = 0; cellRects = null;
        return false;
        Mat gray = new Mat();
        Cv2.CvtColor(board, gray, ColorConversionCodes.BGR2GRAY);
        Mat bw = new Mat();
        Cv2.AdaptiveThreshold(gray, bw, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 15, 8);
        // small morphology to separate tiles
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
        Cv2.MorphologyEx(bw, bw, MorphTypes.Open, kernel);

        // 垂直方向投影（对每一列求和）
        double[] vertProj = new double[board.Width];
        for (int x = 0; x < board.Width; x++)
        {
            double sum = 0;
            for (int y = 0; y < board.Height; y++)
            {
                sum += bw.At<byte>(y, x) > 0 ? 1 : 0;
            }
            vertProj[x] = sum;
        }
        // 水平方向投影
        double[] horzProj = new double[board.Height];
        for (int y = 0; y < board.Height; y++)
        {
            double sum = 0;
            for (int x = 0; x < board.Width; x++)
            {
                sum += bw.At<byte>(y, x) > 0 ? 1 : 0;
            }
            horzProj[y] = sum;
        }

        // 平滑投影（移动平均）
        vertProj = Smooth(vertProj, Math.Max(3, board.Width/200));
        horzProj = Smooth(horzProj, Math.Max(3, board.Height/200));

        // 阈值决定哪些列/行视为“有牌区域”
        double vThr = vertProj.Max() * 0.40;
        double hThr = horzProj.Max() * 0.40;

        List<(int s, int e)> vRegions = ExtractRegions(vertProj, vThr);
        List<(int s, int e)> hRegions = ExtractRegions(horzProj, hThr);

        // 验证检测到的列数与行数（合理范围）
        if (vRegions.Count < 4 || vRegions.Count > 30 || hRegions.Count < 4 || hRegions.Count > 30)
        {
            // 失败
            return false;
        }

        cols = vRegions.Count;
        rows = hRegions.Count;

        // 计算每格的矩形：用 region 的中点做中心，或者用 region 边界做边界
        // 先求每列的中心 x, 每行的中心 y
        double[] cx = vRegions.Select(r => (r.s + r.e) / 2.0).ToArray();
        double[] cy = hRegions.Select(r => (r.s + r.e) / 2.0).ToArray();

        // 为每个格子求边界：使用相邻中心的中点作为分割线
        int[,] rects = new int[rows, cols * 4];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                double left, right, top, bottom;
                if (c == 0) left = Math.Max(0, vRegions[c].s - 2);
                else left = (cx[c - 1] + cx[c]) / 2.0;
                if (c == cols - 1) right = Math.Min(board.Width - 1, vRegions[c].e + 2);
                else right = (cx[c] + cx[c + 1]) / 2.0;

                if (r == 0) top = Math.Max(0, hRegions[r].s - 2);
                else top = (cy[r - 1] + cy[r]) / 2.0;
                if (r == rows - 1) bottom = Math.Min(board.Height - 1, hRegions[r].e + 2);
                else bottom = (cy[r] + cy[r + 1]) / 2.0;

                int x = (int)Math.Round(left);
                int y = (int)Math.Round(top);
                int w = (int)Math.Round(right - left);
                int h = (int)Math.Round(bottom - top);
                rects[r, c * 4 + 0] = x;
                rects[r, c * 4 + 1] = y;
                rects[r, c * 4 + 2] = Math.Max(1, w);
                rects[r, c * 4 + 3] = Math.Max(1, h);
            }
        }

        cellRects = rects;
        return true;
    }

    // 平滑（简单移动平均）
    double[] Smooth(double[] data, int k)
    {
        int n = data.Length;
        double[] outArr = new double[n];
        int half = k / 2;
        for (int i = 0; i < n; i++)
        {
            int a = Math.Max(0, i - half), b = Math.Min(n - 1, i + half);
            double sum = 0; int cnt = 0;
            for (int j = a; j <= b; j++) { sum += data[j]; cnt++; }
            outArr[i] = sum / Math.Max(1, cnt);
        }
        return outArr;
    }

    // 从投影数组中提取连续“有值”区间（value > thr）
    List<(int s, int e)> ExtractRegions(double[] proj, double thr)
    {
        List<(int s, int e)> regs = new List<(int s, int e)>();
        bool inReg = false;
        int s = 0;
        for (int i = 0; i < proj.Length; i++)
        {
            bool on = proj[i] > thr;
            if (on && !inReg) { inReg = true; s = i; }
            if (!on && inReg) { inReg = false; regs.Add((s, i - 1)); }
        }
        if (inReg) regs.Add((s, proj.Length - 1));
        // 合并过窄的区间（噪声）
        regs = regs.Where(r => (r.e - r.s) > 4).ToList();
        return regs;
    }

    // 构建均匀网格（回退时使用）
    int[,] BuildUniformGrid(int width, int height, int rows, int cols)
    {
        int[,] rects = new int[rows, cols * 4];
        int cellW = width / cols;
        int cellH = height / rows;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int x = c * cellW;
                int y = r * cellH;
                rects[r, c * 4 + 0] = x;
                rects[r, c * 4 + 1] = y;
                rects[r, c * 4 + 2] = (c == cols - 1) ? width - x : cellW;
                rects[r, c * 4 + 3] = (r == rows - 1) ? height - y : cellH;
            }
        }
        return rects;
    }

    // 使用 HSV 颜色直方图比较相似度（返回 -1..1）
    double CompareHistSimilarity(Mat a, Mat b)
    {
        Mat A = new Mat(), B = new Mat();
        Cv2.CvtColor(a, A, ColorConversionCodes.BGR2HSV);
        Cv2.CvtColor(b, B, ColorConversionCodes.BGR2HSV);
        int[] histSize = { 50, 60 };
        Rangef[] ranges = { new Rangef(0, 180), new Rangef(0, 256) };
        int[] channels = { 0, 1 };

        Mat histA = new Mat(), histB = new Mat();
        Cv2.CalcHist(new Mat[] { A }, channels, null, histA, 2, histSize, ranges);
        Cv2.CalcHist(new Mat[] { B }, channels, null, histB, 2, histSize, ranges);
        Cv2.Normalize(histA, histA, 0, 1, NormTypes.MinMax);
        Cv2.Normalize(histB, histB, 0, 1, NormTypes.MinMax);
        double score = Cv2.CompareHist(histA, histB, HistCompMethods.Correl);
        return score;
    }

}