# Gold Overdraft / 金币透支

在《杀戮尖塔 2》的商店遇到心仪的遗物或卡牌，却差一点金币？**Gold Overdraft 让你提前消费，而不必打开控制台给自己加钱。**你现在买下想要的东西，再用本幕之后赚到的金币偿还。

它不会凭空送你金币：单人和多人模式下，每位玩家的商店购买最多只能让自己的余额降到 **-100**；之后获得金币会直接提高自己的余额；**离开本幕时只要任意玩家仍为负数，全队都会死亡**。最后一幕在胜利结算前也会检查。透支上限和还款期限让购物时机更灵活，同时保留赚钱、取舍和失败的风险。

例如，你有 30 金币，商店里一件遗物售价 100 金币。购买后余额变为 **-70**；接下来获得 40 金币，余额变为 **-30**。你需要在本幕结束前赚回至少 30 金币。

金币余额本身就是债务，无贷款按钮或单独的债务计数器。鼠标悬停在金币上可查看规则。多人模式按玩家分别计算金币；幕末由房主统一执行全队清算。本 Mod 为纯代码 Mod，当前适配并验证游戏 `v0.111.0`。

## 本机安装

需要 Steam 默认位置安装的游戏和 .NET 9 SDK。在此文件夹运行 `sh install.sh`，然后重启游戏。不需要 Godot 编辑器。

## GitHub Actions

仓库中的 `Validate Gold Overdraft` 工作流会在 `main` push、针对 `main` 的 PR，以及手动运行时检查项目。它会使用 .NET 9 还原真实的 `OverdraftMod.csproj`，验证 `OverdraftMod.json`、安装脚本和游戏程序集引用是否完整。

GitHub 托管 runner 没有你的 Steam 游戏，因此工作流不会把 `sts2.dll` 等游戏程序集上传到公开仓库，也不会在云端伪造游戏编译。完整构建仍使用本机命令 `sh install.sh`；Actions 的绿色结果表示项目输入和自动化检查通过，不代表已经在游戏中完成联机测试。

## 验证

1. 单人或多人商店购买一件原本买不起、但购买后余额不低于 -100 的商品，确认对应玩家金币显示负数。
2. 确认购买后会低于 -100 的商品仍无法购买。
3. 获得金币，确认负余额向 0 增加。
4. 保存退出并继续，确认负余额仍在。
5. 多人时让一名玩家在本幕结束仍为负数，确认房主触发全队死亡；所有玩家非负时可正常进入下一幕。

卸载时先退出游戏，再删除游戏包中的 `Contents/MacOS/mods/OverdraftMod` 文件夹。

## English

Spot a relic or card you want but don't have enough gold? **Gold Overdraft lets you buy it now without giving yourself free gold through the console.** Spend against the gold you expect to earn later in the act.

In single-player and multiplayer shops, each player's purchases may lower that player's balance to **-100 gold**, but no further. Future gold raises that player's balance immediately. If any player is still negative when the party leaves the act, the whole party dies; the same check applies before the final victory. The credit limit and deadline preserve the tradeoffs and risk of each purchase. No loan button or separate debt counter is needed: the gold balance shows the debt.

This code-only mod currently targets Slay the Spire 2 `v0.111.0`. Build against the installed game's DLLs with the .NET 9 SDK, then run `sh install.sh` on macOS and restart the game. No Godot editor is needed.

The `Validate Gold Overdraft` GitHub Actions workflow checks the real project restore, manifest, installer, and game-assembly reference contract. Because GitHub-hosted runners do not contain the Steam game assemblies, the final game build remains a local `sh install.sh` step.
