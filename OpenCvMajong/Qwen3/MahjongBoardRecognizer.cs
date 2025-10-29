using MahjongRecognizer;
using OpenCvSharp;

namespace MahjongRecognizer
{
    public class MahjongBoardRecognizer
    {
        // 假设你有一个包含所有可能牌面模板的字典
        private Dictionary<string, Mat> _templates; // Key: 牌面名称 (如 "西", "一筒"), Value: 模板图像

        public MahjongBoardRecognizer(Dictionary<string, Mat> templates)
        {
            _templates = templates;
        }

        /// <summary>
        /// 从截图中识别麻将牌并返回二维数组
        /// </summary>
        /// <param name="screenshotPath">游戏截图路径</param>
        /// <returns>二维字符串数组，表示棋盘上的牌面</returns>
        public string[,] RecognizeBoard(string screenshotPath)
        {
            // 1. 加载图像
            Mat image = Cv2.ImRead(screenshotPath);
            if (image.Empty())
                throw new ArgumentException("无法加载图像");

            // 2. 预处理：转为灰度图
            Mat grayImage = new Mat();
            Cv2.CvtColor(image, grayImage, ColorConversionCodes.BGR2GRAY);

            // 3. 定位棋盘区域 (这里简化处理，假设棋盘位置已知)
            // 在实际应用中，你需要使用边缘检测等方法自动定位棋盘
            Rect boardRect = new Rect(38, 570, 1005, 1210); // 根据你的截图调整
            Mat boardImage = new Mat(grayImage, boardRect);
            
            Cv2.ImShow("board",boardImage);
            // 4. 分割牌面
            int rows = 12; // 根据你的截图，棋盘是10行
            int cols = 10;  // 根据你的截图，棋盘是9列

            int tileHeight = boardImage.Rows / rows;
            int tileWidth = boardImage.Cols / cols;

            string[,] board = new string[rows, cols];

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    // 计算当前牌的位置
                    int x = col * tileWidth;
                    int y = row * tileHeight;
                    Rect tileRect = new Rect(x, y, tileWidth, tileHeight);

                    // 提取单个牌的图像
                    Mat tileImage = new Mat(boardImage, tileRect);

                    // 5. 模板匹配
                    string recognizedTile = MatchTile(tileImage);

                    board[row, col] = recognizedTile;
                }
            }

            return board;
        }

        /// <summary>
        /// 对单个牌面图像进行模板匹配
        /// </summary>
        /// <param name="tileImage">单个牌面的灰度图像</param>
        /// <returns>匹配到的牌面名称</returns>
        private string MatchTile(Mat tileImage)
        {
            double bestMatchValue = -1;
            string bestMatchName = "未知";

            foreach (var template in _templates)
            {
                // 确保模板和目标图像大小一致，如果不一致，需要调整大小
                Mat resizedTemplate = new Mat();
                Cv2.Resize(template.Value, resizedTemplate, tileImage.Size());

                // 使用归一化相关系数匹配
                Mat result = new Mat();
                Cv2.MatchTemplate(tileImage, resizedTemplate, result, TemplateMatchModes.CCoeffNormed);

                // 找到最佳匹配位置和值
                double minVal, maxVal;
                Point minLoc, maxLoc;
                Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);

                if (maxVal > bestMatchValue)
                {
                    bestMatchValue = maxVal;
                    bestMatchName = template.Key;
                }
            }

            // 可以设置一个阈值，如果匹配度太低，则标记为“未知”
            if (bestMatchValue < 0.7) // 阈值可以根据实际情况调整
                bestMatchName = "未知";

            return bestMatchName;
        }
        
        
    }
}
