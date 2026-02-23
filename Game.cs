using SkiaSharp;

namespace Gdr2333.Plugin.Minesweeper;

internal class Game : IDisposable
{
    private static readonly SKFont font = SKFontManager.Default.MatchFamily("Noto Sans").ToFont();
    private readonly SKBitmap bitmap;
    private readonly SKCanvas canvas;
    public bool IsEnd { get; private set; } = false;
    public bool IsWin { get; private set; } = false;
    private readonly byte[][] map;
    private readonly bool[][] mask, maybe, marks;
    private readonly int height, width;
    public Game(int width, int height, int mines)
    {
        this.height = height;
        this.width = width;
        #region SkiaSharp INIT
        font.Size = 14;
        var bmWidth = width * 20 + 100;
        var bmHeight = height * 20 + 100;
        bitmap = new(bmWidth, bmHeight, SKColorType.Rgb888x, SKAlphaType.Opaque);
        canvas = new(bitmap);
        canvas.Clear(SKColors.LightGray);
        using var paint = new SKPaint();
        paint.IsStroke = true;
        paint.Color = SKColors.Black;
        paint.StrokeWidth = 2;
        for (int i = 0; i < height; i++)
        {
            var text = i.ToString();
            canvas.DrawText(text, 50, 60 + i * 20, SKTextAlign.Right, font, paint);
        }
        for (int i = 0; i < width; i++)
        {
            var text = GetColString(i);
            canvas.DrawText(text, 60 + i * 20, 40, SKTextAlign.Center, font, paint);
        }
        float xStart = 50, xEnd = width * 20 + 50, yStart = 50, yEnd = height * 20 + 50;
        using var bgPaint = new SKPaint();
        bgPaint.IsStroke = false;
        bgPaint.Color = SKColors.Gray;
        canvas.DrawRect(xStart, yStart, width * 20, height * 20, bgPaint);
        for (int i = 0; i <= height; i++)
        {
            float yAddr = i * 20 + 50;
            canvas.DrawLine(xStart, yAddr, xEnd, yAddr, paint);
        }
        for (int i = 0; i <= width; i++)
        {
            float xAddr = i * 20 + 50;
            canvas.DrawLine(xAddr, yStart, xAddr, yEnd, paint);
        }
        #endregion
        #region Map INIT
        map = new byte[height][];
        mask = new bool[height][];
        maybe = new bool[height][];
        marks = new bool[height][];
        for (int i = 0; i < height; i++)
        {
            map[i] = new byte[width];
            mask[i] = new bool[width];
            maybe[i] = new bool[width];
            marks[i] = new bool[width];
            Array.Fill(mask[i], true);
        }
        for (int i = 0; i < mines; i++)
        {
            int x = Random.Shared.Next(0, width);
            int y = Random.Shared.Next(0, height);
            while (map[y][x] != 0)
            {
                x = Random.Shared.Next(0, width);
                y = Random.Shared.Next(0, height);
            }
            map[y][x] = 100;
        }
        if (map[0][0] != 100)
            map[0][0] = (byte)(
                (map[0][1] == 100 ? 1 : 0) +
                (map[1][1] == 100 ? 1 : 0) +
                (map[1][0] == 100 ? 1 : 0));
        if (map[0][^1] != 100)
            map[0][^1] = (byte)(
                (map[0][^2] == 100 ? 1 : 0) +
                (map[1][^2] == 100 ? 1 : 0) +
                (map[1][^1] == 100 ? 1 : 0));
        if (map[^1][0] != 100)
            map[^1][0] = (byte)(
                (map[^2][0] == 100 ? 1 : 0) +
                (map[^2][1] == 100 ? 1 : 0) +
                (map[^1][1] == 100 ? 1 : 0));
        if (map[^1][^1] != 100)
            map[^1][^1] = (byte)(
                (map[^1][^2] == 100 ? 1 : 0) +
                (map[^2][^2] == 100 ? 1 : 0) +
                (map[^2][^1] == 100 ? 1 : 0));
        for (int i = 1; i < height - 1; i++)
        {
            if (map[i][0] != 100)
                map[i][0] = (byte)(
                    (map[i - 1][0] == 100 ? 1 : 0) +
                    (map[i - 1][1] == 100 ? 1 : 0) +
                    (map[i][1] == 100 ? 1 : 0) +
                    (map[i + 1][1] == 100 ? 1 : 0) +
                    (map[i + 1][0] == 100 ? 1 : 0));
            if (map[i][^1] != 100)
                map[i][^1] = (byte)(
                    (map[i - 1][^1] == 100 ? 1 : 0) +
                    (map[i - 1][^2] == 100 ? 1 : 0) +
                    (map[i][^2] == 100 ? 1 : 0) +
                    (map[i + 1][^2] == 100 ? 1 : 0) +
                    (map[i + 1][^1] == 100 ? 1 : 0));
        }
        for (int i = 1; i < width - 1; i++)
        {
            if (map[0][i] != 100)
                map[0][i] = (byte)(
                    (map[0][i - 1] == 100 ? 1 : 0) +
                    (map[1][i - 1] == 100 ? 1 : 0) +
                    (map[1][i] == 100 ? 1 : 0) +
                    (map[1][i + 1] == 100 ? 1 : 0) +
                    (map[0][i + 1] == 100 ? 1 : 0));
            if (map[^1][i] != 100)
                map[^1][i] = (byte)(
                    (map[^1][i - 1] == 100 ? 1 : 0) +
                    (map[^2][i - 1] == 100 ? 1 : 0) +
                    (map[^2][i] == 100 ? 1 : 0) +
                    (map[^2][i + 1] == 100 ? 1 : 0) +
                    (map[^1][i + 1] == 100 ? 1 : 0));
        }
        for (int i = 1; i < height - 1; i++)
            for (int j = 1; j < width - 1; j++)
                if (map[i][j] != 100)
                    map[i][j] = (byte)(
                        (map[i - 1][j - 1] == 100 ? 1 : 0) +
                        (map[i - 1][j] == 100 ? 1 : 0) +
                        (map[i - 1][j + 1] == 100 ? 1 : 0) +
                        (map[i][j + 1] == 100 ? 1 : 0) +
                        (map[i + 1][j + 1] == 100 ? 1 : 0) +
                        (map[i + 1][j] == 100 ? 1 : 0) +
                        (map[i + 1][j - 1] == 100 ? 1 : 0) +
                        (map[i][j - 1] == 100 ? 1 : 0));
        #endregion
    }
    public bool TryOpen(string row, string col) =>
        TryOpen(int.Parse(row), GetColNumber(col));
    public bool Maybe(string row, string col)
    {
        var y = int.Parse(row);
        var x = GetColNumber(col);
        if (!mask[y][x] || maybe[y][x] || marks[y][x])
            return false;
        maybe[y][x] = true;
        using var textPaint = new SKPaint();
        textPaint.IsStroke = true;
        textPaint.Color = SKColors.Red;
        canvas.DrawText("?", x * 20 + 60, y * 20 + 60, SKTextAlign.Center, font, textPaint);
        return true;
    }
    public bool Mark(string row, string col)
    {
        var y = int.Parse(row);
        var x = GetColNumber(col);
        if (!mask[y][x] || maybe[y][x] || marks[y][x])
            return false;
        marks[y][x] = true;
        using var textPaint = new SKPaint();
        textPaint.IsStroke = true;
        textPaint.Color = SKColors.Red;
        canvas.DrawText("X", x * 20 + 60, y * 20 + 60, SKTextAlign.Center, font, textPaint);
        return true;
    }
    public bool Restore(string row, string col)
    {
        var y = int.Parse(row);
        var x = GetColNumber(col);
        if (!(maybe[y][x] || marks[y][x]))
            return false;
        maybe[y][x] = marks[y][x] = false;
        using var bgPaint = new SKPaint();
        bgPaint.Color = SKColors.Gray;
        canvas.DrawRect(x * 20 + 51, y * 20 + 51, 18, 18, bgPaint);
        return true;
    }
    private bool TryOpen(int y, int x)
    {
        if (IsEnd)
            return false;
        else if (!mask[y][x])
            return false;
        else if (map[y][x] == 100)
        {
            IsEnd = true;
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    if (mask[i][j])
                    {
                        mask[i][j] = false;
                        Render(i, j);
                    }
            return false;
        }
        else
        {
            Open(y, x);
            bool isEnd = true;
            for(int i=0;i<height;i++)
                for(int j=0;j<width;j++)
                    if (mask[i][j] && map[i][j]!=100)
                    {
                        isEnd = false;
                        break;
                    }
            if (isEnd)
            {
                IsEnd = true;
                IsWin = true;
            }
            return true;
        }
    }
    private void Open(int y, int x)
    {
        if (!mask[y][x])
            return;
        mask[y][x] = false;
        Render(y, x);
        if (map[y][x] == 0)
        {
            bool f_ys1 = y - 1 >= 0, f_ya1 = y + 1 < height, f_xs1 = x - 1 >= 0, f_xa1 = x + 1 < width;
            if (f_ys1)
            {
                if (f_xa1)
                    Open(y - 1, x + 1);
                Open(y - 1, x);
                if (f_xs1)
                    Open(y - 1, x - 1);
            }
            if (f_xa1)
                Open(y, x + 1);
            if (f_xs1)
                Open(y, x - 1);
            if (f_ya1)
            {
                if (f_xa1)
                    Open(y + 1, x + 1);
                Open(y + 1, x);
                if (f_xs1)
                    Open(y + 1, x - 1);
            }
        }
        return;
    }
    private void Render(int y, int x)
    {
        using var bgPaint = new SKPaint();
        bgPaint.IsStroke = false;
        bgPaint.Color = map[y][x] == 100 ? SKColors.Black : SKColors.White;
        canvas.DrawRect(x * 20 + 51, y * 20 + 51, 18, 18, bgPaint);
        if (map[y][x] != 0 && map[y][x] != 100)
        {
            using var textPaint = new SKPaint();
            textPaint.IsStroke = true;
            textPaint.Color = map[y][x] switch
            {
                1 => SKColors.Blue,
                2 => SKColors.Green,
                3 => SKColors.Red,
                4 => SKColors.DarkBlue,
                5 => SKColors.Brown,
                6 => SKColors.Cyan,
                7 => SKColors.Black,
                8 => SKColors.Gray
            };
            canvas.DrawText(map[y][x].ToString(), x * 20 + 60, y * 20 + 60, SKTextAlign.Center, font, textPaint);
        }
        return;
    }
    public byte[] Render()
    {
        canvas.Flush();
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
    public void Dispose()
    {
        canvas.Dispose();
        bitmap.Dispose();
    }
    private static string GetColString(int row)
    {
        if (row < 26)
            return $"{(char)('A' + row)}";
        else
            return $"{(char)('A' + (row / 26 - 1))}{(char)('A' + row % 26)}";
    }
    private static int GetColNumber(string str)
    {
        str = str.ToUpper();
        if (str.Length == 1)
            return str[0] - 'A';
        else
            return (str[0] - 'A' + 1) * 26 + (str[1] - 'A');
    }
}
