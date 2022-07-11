# FutuApiAsync 
Asynchronous wrapper based on Futu broker official API, so that the interface is more in line with the async/await usage habits of modern C#.

在富途官方 API 的基础上进行异步包装，使接口更符合现代 C# 的 async/await 使用习惯。

由于许多接口需要付费开通相关权限，大部分接口未经测试，欢迎贡献单元测试。

# NuGet
https://www.nuget.org/packages/FutuApiAsync/

# 用例

```cs
// 创建行情连接并初始化
using var quote = new FutuQuoteClient();
await quote.InitConnect();

// 订阅事件
quote.Disconnected += ...
quote.UpdateBasicQot += ...
...

// 调用相关行情接口
var result = await quote.GetRT(...);
...

// 创建交易连接并初始化
using var trade = new FutuTradeClient();
await trade.InitConnect();

// 订阅事件
trade.Disconnected += ...
trade.UpdateOrder += ...

// 调用相关交易接口
var result = await trade.PlaceOrder(...);
...
```

# 免责声明
由于许多接口需要付费开通相关权限，大部分接口未经测试，使用前请自行测试确保接口按预期方式运行，由于你的使用不当或本库的 BUG 造成的任何经济损失，本人概不负责。
