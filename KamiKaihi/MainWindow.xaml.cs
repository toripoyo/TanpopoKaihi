using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace KamiKaihi
{
    /// <summary>
    public partial class MainWindow : Window
    {
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);
        // DLLをインポートする必要がある
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(Keys vKey);
        Stopwatch sw = new Stopwatch();

        private const int resoX = 1000;
        private const int resoY = 700;

        private bool continueflag = false;
        private int atariHantei = 15;
        private int searchResolution = 20;

        System.Random r = new System.Random(1);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            atariHantei = Convert.ToInt32(txtRadius.Text);
            continueflag = true;
            ProcessAsync();
        }

        Bitmap tmpBmp = new Bitmap(resoX, resoY);
        Point localPoint = new Point(resoX / 2, resoY / 2);
        Point globalPoint = new Point(0, 0);
        double fpsFilt = 0;
        // 処理
        private async void ProcessAsync()
        {
            while (continueflag)
            {
                sw.Restart();
                if (IsKeyPress(Keys.Escape)) { continueflag = false; }
                GetBmpImage(ref tmpBmp);
                localPoint = SearchSafePosition(tmpBmp, ref localPoint);
                globalPoint.X = localPoint.X + Convert.ToInt32(txtLeftUpperPixelX.Text);
                globalPoint.Y = localPoint.Y + Convert.ToInt32(txtLeftUpperPixelY.Text);
                MoveMousePos(globalPoint);
                txtMousePosX.Text = localPoint.X.ToString();
                txtMousePosY.Text = localPoint.Y.ToString();
                await Task.Delay(1);
                sw.Stop();
                fpsFilt = (1 / sw.Elapsed.TotalSeconds) * 0.01 + fpsFilt * 0.99;
                lblfps.Content = fpsFilt.ToString("F1") + " fps";
            }
        }

        private Point SearchSafePosition(Bitmap tmpBmp, ref Point tmpPoint)
        {
            // 局所的に安全エリアが見つからなかった場合、全体探索する
            if (SearchLocalAreas(tmpBmp, ref tmpPoint) == false)
            {
                tmpPoint = SearchAllAreas(tmpBmp);
            }
            return tmpPoint;
        }

        private bool SearchLocalAreas(Bitmap tmpBmp, ref Point tmpPoint)
        {
            // 中心を目指す
            int tmpX = tmpPoint.X - (tmpPoint.X - resoX / 2) / 25;
            int tmpY = tmpPoint.Y - (tmpPoint.Y - resoY / 2 - 200) / 25;

            if (tmpX >= resoX - 1) { tmpX = resoX - 1; }
            if (tmpX <= 0) { tmpX = 0; }
            if (tmpY >= resoY - 1) { tmpY = resoY - 1; }
            if (tmpY <= 0) { tmpY = 0; }

            if (HanteiAreaWide(tmpBmp, tmpX, tmpY, atariHantei))
            {
                tmpPoint.X = tmpX;
                tmpPoint.Y = tmpY;
                return true;
            }

            // 安全でないなら、安全な方に逃げる
            int tmpMove = atariHantei;
            int count = 0;
            bool tmpHantei = false;
            while (tmpHantei == false)
            {
                tmpX = tmpPoint.X + r.Next(-tmpMove, tmpMove);
                tmpY = tmpPoint.Y + r.Next(-tmpMove, tmpMove);

                if (tmpX >= resoX - 1) { tmpX = resoX - 1; }
                if (tmpX <= 0) { tmpX = 0; }
                if (tmpY >= resoY - 1) { tmpY = resoY - 1; }
                if (tmpY <= 0) { tmpY = 0; }

                tmpHantei = HanteiAreaWide(tmpBmp, tmpX, tmpY, atariHantei);
                count++;
                if (count >= 4)
                {
                    tmpMove *= 2;
                    count = 0;
                }
                if (tmpMove >= resoX / 4)
                {
                    // 全探索モードに移行
                    if (chkAllSearch.IsChecked.Value)
                    {
                        return false;
                    }
                }
            }
            tmpPoint.X = tmpX;
            tmpPoint.Y = tmpY;

            return true;
        }

        // スクショする
        private void GetBmpImage(ref Bitmap tmp)
        {
            //Graphicsの作成
            Graphics g = Graphics.FromImage(tmp);
            //画面全体をコピーする
            int x = Convert.ToInt32(txtLeftUpperPixelX.Text);
            int y = Convert.ToInt32(txtLeftUpperPixelY.Text);
            System.Drawing.Size s = new System.Drawing.Size
            {
                Width = resoX,
                Height = resoY
            };
            g.CopyFromScreen(x, y, 0, 0, s);
            //解放
            g.Dispose();
        }

        // 画面全体から安全エリアを探す
        private Point SearchAllAreas(Bitmap tmpBmp)
        {
            Point tmpPoint = new Point(0, 0);

            // 画面全体から探す
            for (int y = resoY - 10; y > 10; y -= searchResolution)
            {
                for (int x = 100; x < resoX - 10; x += searchResolution)
                {
                    bool tmpHantei = true;
                    tmpHantei &= HanteiAreaWide(tmpBmp, x, y, atariHantei);

                    if (tmpHantei)
                    {
                        tmpPoint.X = x;
                        tmpPoint.Y = y;
                        return tmpPoint;
                    }
                }
            }
            return tmpPoint;
        }

        private bool HanteiAreaWide(Bitmap tmpBmp, int x, int y, int radius)
        {
            Task<bool> t1 = HanteiArea(tmpBmp, x, y, radius);
            Task<bool> t2 = HanteiArea(tmpBmp, x + radius * 2, y, radius);
            Task<bool> t3 = HanteiArea(tmpBmp, x - radius * 2, y, radius);
            Task<bool> t4 = HanteiArea(tmpBmp, x, y + radius * 2, radius);
            Task<bool> t5 = HanteiArea(tmpBmp, x, y - radius * 2, radius);
            Task<bool> t6 = HanteiArea(tmpBmp, x + radius * 2, y + radius * 2, radius);
            Task<bool> t7 = HanteiArea(tmpBmp, x - radius * 2, y + radius * 2, radius);
            Task<bool> t8 = HanteiArea(tmpBmp, x - radius * 2, y - radius * 2, radius);
            Task<bool> t9 = HanteiArea(tmpBmp, x + radius * 2, y - radius * 2, radius);
            Task.WaitAll(t1, t2, t3, t4, t5, t6, t7, t8, t9);
            return t1.Result && t2.Result && t3.Result && t4.Result 
                && t5.Result && t6.Result && t7.Result && t8.Result && t9.Result;
        }

        // エリア判定
        private async Task<bool> HanteiArea(Bitmap tmpBmp, int x, int y, int radius)
        {
            // マルチスレッド処理
            Task<bool> t1 = HanteiPixSide(tmpBmp, x, y, radius);
            Task<bool> t2 = HanteiPixCross(tmpBmp, x, y, radius);
            Task.WaitAll(t1, t2);
            return t1.Result && t2.Result;
        }

        private async Task<bool> HanteiPixSide(Bitmap tmpBmp, int x, int y, int radius)
        {
            bool tmpHantei = true;
            tmpHantei &= HanteiPix(tmpBmp, x + radius, y);
            tmpHantei &= HanteiPix(tmpBmp, x - radius, y);
            tmpHantei &= HanteiPix(tmpBmp, x, y + radius);
            tmpHantei &= HanteiPix(tmpBmp, x, y - radius);
            return tmpHantei;
        }
        private async Task<bool> HanteiPixCross(Bitmap tmpBmp, int x, int y, int radius)
        {
            bool tmpHantei = true;
            tmpHantei &= HanteiPix(tmpBmp, x + radius, y + radius);
            tmpHantei &= HanteiPix(tmpBmp, x + radius, y - radius);
            tmpHantei &= HanteiPix(tmpBmp, x - radius, y - radius);
            tmpHantei &= HanteiPix(tmpBmp, x - radius, y + radius);
            return tmpHantei;
        }

        // ピクセル判定
        private bool HanteiPix(Bitmap tmpBmp, int x, int y)
        {
            System.Drawing.Color tmpColor;

            if (x >= resoX - 1) { x = resoX - 1; }
            if (x <= 0) { x = 0; }
            if (y >= resoY - 1) { y = resoY - 1; }
            if (y <= 0) { y = 0; }

            tmpColor = tmpBmp.GetPixel(x, y);
            if (tmpColor.R > 220 && tmpColor.G > 150 && tmpColor.B > 40 && tmpColor.B < 100) // Chromeでの判定
            {
                return false;
            }
            return true;
        }

        // マウスカーソルを動かす
        private void MoveMousePos(Point tmpPoint)
        {
            SetCursorPos(tmpPoint.X, tmpPoint.Y);
        }

        // キーが押されていたら終了
        public bool IsKeyPress(Keys keyCode)
        {
            // GetKeyStateは最上位ビットが1か否かでキー投下の有無を取得できる
            return GetKeyState(keyCode) < 0;
        }

        // マウスカーソルの位置を取得
        private void buttonXY_Click(object sender, RoutedEventArgs e)
        {
            txtLeftUpperPixelX.Text = System.Windows.Forms.Cursor.Position.X.ToString();
            txtLeftUpperPixelY.Text = System.Windows.Forms.Cursor.Position.Y.ToString();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            continueflag = false;
        }
    }
}
