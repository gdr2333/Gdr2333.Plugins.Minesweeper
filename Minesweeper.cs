using Gdr2333.BotLib.OnebotV11.Events;
using Gdr2333.BotLib.OnebotV11.Messages;
using Gdr2333.MausBot3.PluginSdk;
using System.Text.RegularExpressions;

namespace Gdr2333.Plugin.Minesweeper;

using Plugin = MausBot3.PluginSdk.Plugin;

public partial class Minesweeper : Plugin
{
    public override CommandBase[] Commands => [
        .. new CommandEx("扫雷",
            ["minesweeper"],
            "简单的扫雷游戏。使用#扫雷(简单/中等/困难/宽*高 雷数)启动，然后输入标记/怀疑/打开/取消 坐标进行游戏",
            "^{0}{1}(?:\\s*)(?<diff>简单|中等|困难|(?<height>\\d+)\\*(?<width>\\d+)(?:\\s+)(?<mines>\\d+))",
            async(mp, e, ct) => {
                var result = MineswapperStartRegex().Match(e.Message.ToString());
                int height, width, mines;
                switch(result.Groups["diff"].Value)
                {
                    case "简单":
                        height = 9;
                        width = 9;
                        mines = 10;
                        break;
                    case "中等":
                        height = 16;
                        width = 16;
                        mines = 40;
                        break;
                    case "困难":
                        height = 16;
                        width = 30;
                        mines = 99;
                        break;
                    default:
                        height = int.Parse(result.Groups["height"].Value);
                        width = int.Parse(result.Groups["width"].Value);
                        mines = int.Parse(result.Groups["mines"].Value);
                        if(height < 9)
                            height = 9;
                        if(height > 26 * 26)
                            height = 26 * 26;
                        if(width < 9)
                            width = 9;
                        if(width > 100)
                            width = 100;
                        if(mines < 10)
                            mines = 10;
                        if(mines > height * width)
                            mines = height * width;
                        break;
                }
                using var game = new Game(width, height, mines);
                await mp.SendMessageAsync(new(new ImagePart(game.Render())));
                while(!game.IsEnd)
                {
                    var m = await mp.ReadMessageAsync(ct);
                    var r = MineswapperContinueRegex().Match(m.ToString());
                    var row = r.Groups["row"].Value;
                    var col = r.Groups["col"].Value;
                    switch(r.Groups["verb"].Value)
                    {
                        case "标记":
                            if(game.Mark(row, col))
                                await mp.SendMessageAsync(new(new ImagePart(game.Render())));
                            else
                                await mp.SendMessageAsync(new("操作失败"));
                            break;
                        case "怀疑":
                            if(game.Maybe(row, col))
                                await mp.SendMessageAsync(new(new ImagePart(game.Render())));
                            else
                                await mp.SendMessageAsync(new("操作失败"));
                            break;
                        case "取消":
                            if(game.Restore(row, col))
                                await mp.SendMessageAsync(new(new ImagePart(game.Render())));
                            else
                                await mp.SendMessageAsync(new("操作失败"));
                            break;
                        case "打开":
                            if(game.TryOpen(row, col))
                                await mp.SendMessageAsync(new(new ImagePart(game.Render())));
                            else if(!game.IsEnd)
                                await mp.SendMessageAsync(new("操作失败"));
                            break;
                    }
                }
                await mp.SendMessageAsync(new($"您{(game.IsWin?'赢':'输')}了"));
                return;
        },
            stillExtraCheck: e => e is MessageReceivedEventArgsBase mre && MineswapperContinueRegex().IsMatch(mre.Message.ToString())).Commands
    ];

    public override string PluginId => "Gdr2333.Plugins.Minesweeper";

    public override string PluginName => "扫雷";

    [GeneratedRegex("(?<verb>标记|取消|怀疑|打开)(?:\\s+)(?<col>\\p{L}+)(?<row>\\d+)")]
    private static partial Regex MineswapperContinueRegex();
    [GeneratedRegex("(?:\\s*)(?<diff>简单|中等|困难|(?<height>\\d+)\\*(?<width>\\d+)(?:\\s+)(?<mines>\\d+))")]
    private static partial Regex MineswapperStartRegex();
}
