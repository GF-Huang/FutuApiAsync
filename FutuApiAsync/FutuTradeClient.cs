using Futu.OpenApi;
using Futu.OpenApi.Pb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FutuApiAsync {
    public class FutuTradeClient : FutuClientBase, FTSPI_Trd {
        private static readonly string NoTradeHeaderMessage = $"trdHeader 为空，同时也未设置 {nameof(DefaultTradeHeader)}。";

        /// <summary>
        /// 获取底层 <see cref="FTAPI_Trd"/> 对象。
        /// </summary>
        public FTAPI_Trd Trade => (FTAPI_Trd)_connectionObject;

        /// <summary>
        /// 默认的交易公共参数头。
        /// 设置后可以避免在每个需要 <see cref="TrdCommon.TrdHeader"/> 的方法都传入交易公共参数头而带来的不便。 
        /// </summary>
        public TrdCommon.TrdHeader DefaultTradeHeader { get; set; }

        public FutuTradeClient() : this(string.Empty) { }

        public FutuTradeClient(string rsaKey) : base(rsaKey) {
            Trade.SetTrdCallback(this);
        }

        protected override FTAPI_Conn CreateConnectionObject() => new FTAPI_Trd();

        #region Public

        #region 账户

        /// <summary>
        /// 获取交易业务账户列表。
        /// </summary>
        /// <returns></returns>
        public Task<IList<TrdCommon.TrdAcc>> GetAccList() {
            var request = TrdGetAccList.Request.CreateBuilder()
                                               .SetC2S(TrdGetAccList.C2S.CreateBuilder().SetUserID(0))
                                               .Build();

            return CreateRequestTask<IList<TrdCommon.TrdAcc>>(Trade.GetAccList(request));
        }

        /// <summary>
        /// 解锁或锁定交易。
        /// </summary>
        /// <param name="unlock">true 解锁交易，false 锁定交易。</param>
        /// <param name="pwdMD5">交易密码的 MD5 转16进制(全小写)，解锁交易必须要填密码，锁定交易不需要验证密码，可不填。</param>
        /// <param name="securityFirm">券商标识。</param>
        /// <returns></returns>
        public Task UnlockTrade(bool unlock, string pwdMD5, TrdCommon.SecurityFirm? securityFirm) {
            var builder = TrdUnlockTrade.C2S.CreateBuilder().SetUnlock(unlock);
            if (!string.IsNullOrWhiteSpace(pwdMD5))
                builder.SetPwdMD5(pwdMD5);
            if (securityFirm.HasValue)
                builder.SetSecurityFirm((int)securityFirm.Value);

            var request = TrdUnlockTrade.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask(Trade.UnlockTrade(request));
        }

        #endregion // 账户

        #region 资产持仓

        /// <summary>
        /// 查询交易业务账户的资产净值、证券市值、现金、购买力等资金数据。
        /// </summary>
        /// <param name="refreshCache">立即刷新 OpenD 缓存的此数据，默认不填。true 向服务器获取最新数据更新缓存并返回；
        /// flase 或没填则返回 OpenD 缓存的数据，不会向服务器请求。
        /// <para>正常情况下，服务器有更新就会立即推送到 OpenD，OpenD 缓存着数据，API 请求过来，返回同步的缓存数据，
        /// 一般不需要指定刷新缓存，保证快速返回且减少对服务器的压力。</para>
        /// <para>如果遇到丢包等情况，可能出现缓存数据与服务器不一致，用户如果发现数据更新有异样，可指定刷新缓存，解决数据同步的问题。</para>
        /// </param>
        /// <param name="currency">货币种类。期货账户必填，其它账户忽略。</param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<TrdCommon.Funds> GetFunds(bool? refreshCache, TrdCommon.Currency? currency, TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetFunds.C2S.CreateBuilder().SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                                    throw new InvalidOperationException(NoTradeHeaderMessage));
            if (refreshCache.HasValue)
                builder.SetRefreshCache(refreshCache.Value);
            if (currency.HasValue)
                builder.SetCurrency((int)currency.Value);

            var request = TrdGetFunds.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<TrdCommon.Funds>(Trade.GetFunds(request));
        }

        /// <summary>
        /// 查询指定交易业务账户下的最大可买卖数量，亦可查询指定交易业务账户下指定订单的最大可改成的数量。
        /// </summary>
        /// <param name="orderType">订单类型。</param>
        /// <param name="code">代码，港股必须是5位数字，A 股必须是6位数字，美股没限制。</param>
        /// <param name="price">价格，（证券账户精确到小数点后 3 位，期货账户精确到小数点后 9 位，超出部分会被舍弃）。如果是竞价、市价单，请也填入一个当前价格，服务器才好计算。</param>
        /// <param name="orderID">订单号，新下订单不需要，如果是修改订单就需要把原订单号带上才行，因为改单的最大买卖数量会包含原订单数量。</param>
        /// <param name="adjustPrice">是否调整价格，如果价格不合法，是否调整到合法价位，true 调整，false 不调整。
        /// <para>为保证与下单的价格同步，此参数为调整价格使用，对港、A 股有意义，因为港股有价位，A 股2位精度，美股可不传。</para>
        /// </param>
        /// <param name="adjustSideAndLimit">调整方向和调整幅度百分比限制，正数代表向上调整，负数代表向下调整，具体值代表调整幅度限制，如：0.015代表向上调整且幅度不超过1.5%；-0.01代表向下调整且幅度不超过1%。
        /// <para>为保证与下单的价格同步，此参数为调整价格使用，对港、A 股有意义，因为港股有价位，A 股2位精度，美股可不传。</para>
        /// </param>
        /// <param name="secMarket">证券所属市场。</param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<TrdCommon.MaxTrdQtys> GetMaxTrdQtys(TrdCommon.OrderType orderType, string code, double price,
                                                        ulong? orderID, bool? adjustPrice, double? adjustSideAndLimit,
                                                        TrdCommon.TrdSecMarket? secMarket, TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetMaxTrdQtys.C2S.CreateBuilder()
                                              .SetOrderType((int)orderType)
                                              .SetCode(code)
                                              .SetPrice(price)
                                              .SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                         throw new InvalidOperationException(NoTradeHeaderMessage));
            if (orderID.HasValue)
                builder.SetOrderID(orderID.Value);
            if (adjustPrice.HasValue)
                builder.SetAdjustPrice(adjustPrice.Value);
            if (adjustSideAndLimit.HasValue)
                builder.SetAdjustSideAndLimit(adjustSideAndLimit.Value);
            if (secMarket.HasValue)
                builder.SetSecMarket((int)secMarket.Value);

            var request = TrdGetMaxTrdQtys.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<TrdCommon.MaxTrdQtys>(Trade.GetMaxTrdQtys(request));
        }

        /// <summary>
        /// 查询交易业务账户的持仓列表。
        /// </summary>
        /// <param name="filterConditions">过滤条件。</param>
        /// <param name="filterPLRatioMin">过滤盈亏百分比下限，高于此比例的会返回，比如传10.0，返回盈亏比例大于10%的持仓。</param>
        /// <param name="filterPLRatioMax">过滤盈亏百分比上限，低于此比例的会返回，比如传20.0，返回盈亏比例小于20%的持仓。</param>
        /// <param name="refreshCache">立即刷新 OpenD 缓存的此数据，默认不填。true 向服务器获取最新数据更新缓存并返回；
        /// flase 或没填则返回 OpenD 缓存的数据，不会向服务器请求。
        /// <para>正常情况下，服务器有更新就会立即推送到 OpenD，OpenD 缓存着数据，API 请求过来，返回同步的缓存数据，
        /// 一般不需要指定刷新缓存，保证快速返回且减少对服务器的压力。
        /// </para>
        /// <para>如果遇到丢包等情况，可能出现缓存数据与服务器不一致，用户如果发现数据更新有异样，可指定刷新缓存，解决数据同步的问题。</para>
        /// </param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<IList<TrdCommon.Position>> GetPositionList(TrdCommon.TrdFilterConditions filterConditions = null,
                                                               double? filterPLRatioMin = null, double? filterPLRatioMax = null,
                                                               bool? refreshCache = null, TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetPositionList.C2S.CreateBuilder()
                                                .SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                           throw new InvalidOperationException(NoTradeHeaderMessage));
            if (filterConditions != null)
                builder.SetFilterConditions(filterConditions);
            if (filterPLRatioMin.HasValue)
                builder.SetFilterPLRatioMin(filterPLRatioMin.Value);
            if (filterPLRatioMax.HasValue)
                builder.SetFilterPLRatioMax(filterPLRatioMax.Value);
            if (refreshCache.HasValue)
                builder.SetRefreshCache(refreshCache.Value);

            var request = TrdGetPositionList.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<TrdCommon.Position>>(Trade.GetPositionList(request));
        }

        /// <summary>
        /// 查询股票的融资融券数据。
        /// </summary>
        /// <param name="securities">股票集合。</param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<IList<TrdGetMarginRatio.MarginRatioInfo>> GetMarginRatio(IEnumerable<QotCommon.Security> securities,
                                                                             TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetMarginRatio.C2S.CreateBuilder()
                                               .AddRangeSecurityList(securities)
                                               .SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                          throw new InvalidOperationException(NoTradeHeaderMessage));

            var request = TrdGetMarginRatio.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<TrdGetMarginRatio.MarginRatioInfo>>(Trade.GetMarginRatio(request));
        }

        #endregion // 资产持仓

        #region 订单

        /// <summary>
        /// 下单。
        /// </summary>
        /// <param name="trdSide">交易方向。</param>
        /// <param name="orderType">订单类型。</param>
        /// <param name="code">代码，港股必须是5位数字，A 股必须是6位数字，美股没限制。</param>
        /// <param name="qty">数量，期权单位是"张"（精确到小数点后 0 位，超出部分会被舍弃。期权期货单位是"张"）。</param>
        /// <param name="price">价格，（证券账户精确到小数点后 3 位，期货账户精确到小数点后 9 位，超出部分会被舍弃）。</param>
        /// <param name="adjustPrice">是否调整价格，如果价格不合法，是否调整到合法价位，true 调整，false 不调整。如果价格不合法又不允许调整，则会返回错误。
        /// </param>
        /// <param name="adjustSideAndLimit">调整方向和调整幅度百分比限制，正数代表向上调整，负数代表向下调整，具体值代表调整幅度限制，
        /// 如：0.015代表向上调整且幅度不超过1.5%；-0.01代表向下调整且幅度不超过1%。</param>
        /// <param name="secMarket">证券所属市场。</param>
        /// <param name="remark">用户备注字符串，最多只能传64字节。可用于标识订单唯一信息等，下单填上，订单结构就会带上。</param>
        /// <param name="timeInForce">订单有效期限。</param>
        /// <param name="fillOutsideRTH">是否允许盘前盘后成交。仅适用于美股限价单。默认 false。</param>
        /// <param name="auxPrice">触发价格。</param>
        /// <param name="trailType">跟踪类型。</param>
        /// <param name="trailValue">跟踪金额/百分比。</param>
        /// <param name="trailSpread">指定价差。</param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<ulong> PlaceOrder(TrdCommon.TrdSide trdSide, TrdCommon.OrderType orderType, string code, double qty, double price,
                                      bool? adjustPrice = null, double? adjustSideAndLimit = null, TrdCommon.TrdSecMarket? secMarket = null,
                                      string remark = null, TrdCommon.TimeInForce? timeInForce = null, bool? fillOutsideRTH = null,
                                      double? auxPrice = null, TrdCommon.TrailType? trailType = null, double? trailValue = null,
                                      double? trailSpread = null, TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdPlaceOrder.C2S.CreateBuilder()
                                           .SetPacketID(Trade.NextPacketID())
                                           .SetTrdSide((int)trdSide)
                                           .SetOrderType((int)orderType)
                                           .SetCode(code)
                                           .SetQty(qty)
                                           .SetPrice(price)
                                           .SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                      throw new InvalidOperationException(NoTradeHeaderMessage));
            if (adjustPrice.HasValue)
                builder.SetAdjustPrice(adjustPrice.Value);
            if (adjustSideAndLimit.HasValue)
                builder.SetAdjustSideAndLimit(adjustSideAndLimit.Value);
            if (secMarket.HasValue)
                builder.SetSecMarket((int)secMarket.Value);
            if (remark != null)
                builder.SetRemark(remark);
            if (timeInForce.HasValue)
                builder.SetTimeInForce((int)timeInForce.Value);
            if (fillOutsideRTH.HasValue)
                builder.SetFillOutsideRTH(fillOutsideRTH.Value);
            if (auxPrice.HasValue)
                builder.SetAuxPrice(auxPrice.Value);
            if (trailType.HasValue)
                builder.SetTrailType((int)trailType.Value);
            if (trailValue.HasValue)
                builder.SetTrailValue(trailValue.Value);
            if (trailSpread.HasValue)
                builder.SetTrailSpread(trailSpread.Value);

            var request = TrdPlaceOrder.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<ulong>(Trade.PlaceOrder(request));
        }

        /// <summary>
        /// 修改订单的价格和数量、撤单、操作订单的失效和生效、删除订单等。
        /// </summary>
        /// <param name="orderID">订单号，<paramref name="forAll"/> 为 true 时，传 0。</param>
        /// <param name="modifyOrderOp">修改操作类型。</param>
        /// <param name="forAll">是否对此业务账户的全部订单操作，true 是，false 否(对单个订单)，不传此参数代表 false，仅对单个订单。</param>
        /// <param name="qty">数量，期权单位是"张"（精确到小数点后 0 位，超出部分会被舍弃）。</param>
        /// <param name="price">价格，（证券账户精确到小数点后 3 位，期货账户精确到小数点后 9 位，超出部分会被舍弃）。</param>
        /// <param name="adjustPrice">是否调整价格，如果价格不合法，是否调整到合法价位，true 调整，false 不调整。
        /// 如果价格不合法又不允许调整，则会返回错误。</param>
        /// <param name="adjustSideAndLimit">调整方向和调整幅度百分比限制，正数代表向上调整，负数代表向下调整，具体值代表调整幅度限制。
        /// 如：0.015代表向上调整且幅度不超过1.5%；-0.01代表向下调整且幅度不超过1%。</param>
        /// <param name="auxPrice">触发价格。</param>
        /// <param name="trailType">跟踪类型。</param>
        /// <param name="trailValue">跟踪金额/百分比。</param>
        /// <param name="trailSpread">指定价差。</param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<ulong> ModifyOrder(ulong orderID, TrdCommon.ModifyOrderOp modifyOrderOp,
                                       bool? forAll = null, double? qty = null, double? price = null,
                                       bool? adjustPrice = null, double? adjustSideAndLimit = null,
                                       double? auxPrice = null, TrdCommon.TrailType? trailType = null, double? trailValue = null,
                                       double? trailSpread = null, TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdModifyOrder.C2S.CreateBuilder()
                                            .SetPacketID(Trade.NextPacketID())
                                            .SetOrderID(orderID)
                                            .SetModifyOrderOp((int)modifyOrderOp)
                                            .SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                      throw new InvalidOperationException(NoTradeHeaderMessage));
            if (forAll.HasValue)
                builder.SetForAll(forAll.Value);
            if (qty.HasValue)
                builder.SetQty(qty.Value);
            if (price.HasValue)
                builder.SetPrice(price.Value);
            if (adjustPrice.HasValue)
                builder.SetAdjustPrice(adjustPrice.Value);
            if (adjustSideAndLimit.HasValue)
                builder.SetAdjustSideAndLimit(adjustSideAndLimit.Value);
            if (auxPrice.HasValue)
                builder.SetAuxPrice(auxPrice.Value);
            if (trailType.HasValue)
                builder.SetTrailType((int)trailType.Value);
            if (trailValue.HasValue)
                builder.SetTrailValue(trailValue.Value);
            if (trailSpread.HasValue)
                builder.SetTrailSpread(trailSpread.Value);

            var request = TrdModifyOrder.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<ulong>(Trade.ModifyOrder(request));
        }

        /// <summary>
        /// 查询指定交易业务账户的今日订单列表。
        /// </summary>
        /// <param name="filterConditions">过滤条件。</param>
        /// <param name="filterStatuses">需要过滤的订单状态列表。</param>
        /// <param name="refreshCache">立即刷新 OpenD 缓存的此数据，默认不填。
        /// true 向服务器获取最新数据更新缓存并返回；flase 或没填则返回 OpenD 缓存的数据，不会向服务器请求。
        /// <para>正常情况下，服务器有更新就会立即推送到 OpenD，OpenD 缓存着数据，API 请求过来，返回同步的缓存数据，
        /// 一般不需要指定刷新缓存，保证快速返回且减少对服务器的压力。</para>
        /// <para>如果遇到丢包等情况，可能出现缓存数据与服务器不一致，用户如果发现数据更新有异样，可指定刷新缓存，解决数据同步的问题。</para>
        /// </param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<IList<TrdCommon.Order>> GetOrderList(TrdCommon.TrdFilterConditions filterConditions = null,
                                                         IEnumerable<TrdCommon.OrderStatus> filterStatuses = null,
                                                         bool? refreshCache = null, TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetOrderList.C2S.CreateBuilder()
                                             .SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                        throw new InvalidOperationException(NoTradeHeaderMessage));
            if (filterConditions != null)
                builder.SetFilterConditions(filterConditions);
            if (filterStatuses != null)
                builder.AddRangeFilterStatusList(filterStatuses.Cast<int>());
            if (refreshCache.HasValue)
                builder.SetRefreshCache(refreshCache.Value);

            var request = TrdGetOrderList.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<TrdCommon.Order>>(Trade.GetOrderList(request));
        }

        /// <summary>
        /// 查询指定交易业务账户的历史订单列表。
        /// </summary>
        /// <param name="filterConditions">过滤条件。</param>
        /// <param name="filterStatuses">需要过滤的订单状态列表。</param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<IList<TrdCommon.Order>> GetHistoryOrderList(TrdCommon.TrdFilterConditions filterConditions,
                                                                IEnumerable<TrdCommon.OrderStatus> filterStatuses = null,
                                                                TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetHistoryOrderList.C2S.CreateBuilder()
                                                    .SetFilterConditions(filterConditions)
                                                    .SetHeader(trdHeader ?? DefaultTradeHeader ??
                                                               throw new InvalidOperationException(NoTradeHeaderMessage));
            if (filterStatuses != null)
                builder.AddRangeFilterStatusList(filterStatuses.Cast<int>());

            var request = TrdGetHistoryOrderList.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<TrdCommon.Order>>(Trade.GetHistoryOrderList(request));
        }

        /// <summary>
        /// 订阅接收交易账户的推送数据。指定发送该协议的连接接收交易数据（订单状态，成交状态等）推送。
        /// </summary>
        /// <param name="accIDs">要接收推送数据的业务账号列表，全量非增量，即使用者请每次传需要接收推送数据的所有业务账号。</param>
        /// <returns></returns>
        public Task SubAccPush(IEnumerable<ulong> accIDs) {
            var request = TrdSubAccPush.Request.CreateBuilder()
                                               .SetC2S(TrdSubAccPush.C2S.CreateBuilder().AddRangeAccIDList(accIDs))
                                               .Build();

            return CreateRequestTask(Trade.SubAccPush(request));
        }

        /// <summary>
        /// 订单推送事件，异步处理 FutuOpenD 推送过来的订单状态信息。
        /// </summary>
        public event EventHandler<TrdCommon.Order> UpdateOrder;

        #endregion // 订单

        #region 成交

        /// <summary>
        /// 获查询指定交易业务账户的当日成交列表。
        /// </summary>
        /// <param name="filterConditions">过滤条件。</param>
        /// <param name="refreshCache">立即刷新 OpenD 缓存的此数据，默认不填。
        /// true 向服务器获取最新数据更新缓存并返回；flase 或没填则返回 OpenD 缓存的数据，不会向服务器请求。
        /// <para>正常情况下，服务器有更新就会立即推送到 OpenD，OpenD 缓存着数据，API 请求过来，返回同步的缓存数据，
        /// 一般不需要指定刷新缓存，保证快速返回且减少对服务器的压力。</para>
        /// <para>如果遇到丢包等情况，可能出现缓存数据与服务器不一致，用户如果发现数据更新有异样，可指定刷新缓存，解决数据同步的问题。</para>
        /// </param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<IList<TrdCommon.OrderFill>> GetOrderFillList(TrdCommon.TrdFilterConditions filterConditions = null,
                                                                 bool? refreshCache = null,
                                                                 TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetOrderFillList.C2S.CreateBuilder().SetHeader(trdHeader ?? DefaultTradeHeader ?? 
                                                                            throw new InvalidOperationException(NoTradeHeaderMessage));
            if (filterConditions != null)
                builder.SetFilterConditions(filterConditions);
            if (refreshCache.HasValue)
                builder.SetRefreshCache(refreshCache.Value);

            var request = TrdGetOrderFillList.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<TrdCommon.OrderFill>>(Trade.GetOrderFillList(request));
        }

        /// <summary>
        /// 查询指定交易业务账户的历史成交列表。
        /// </summary>
        /// <param name="filterConditions">过滤条件。</param>
        /// <param name="trdHeader">交易公共参数头。
        /// <para>可以设置 <see cref="DefaultTradeHeader"/> 作为一个默认的 <paramref name="trdHeader"/> 避免每次都传 header 带来的麻烦。
        /// 当传入 <paramref name="trdHeader"/> 时，将使用传入的 <paramref name="trdHeader"/>；
        /// 不传 <paramref name="trdHeader"/> 时将使用 <see cref="DefaultTradeHeader"/>。
        /// 当没有设置 <see cref="DefaultTradeHeader"/> 也没有传入 <paramref name="trdHeader"/> 时将抛出 <see cref="InvalidOperationException"/> 异常。
        /// </para>
        /// </param>
        /// <returns></returns>
        public Task<IList<TrdCommon.OrderFill>> GetHistoryOrderFillList(TrdCommon.TrdFilterConditions filterConditions,
                                                                        TrdCommon.TrdHeader trdHeader = null) {
            var builder = TrdGetHistoryOrderFillList.C2S.CreateBuilder()
                                                        .SetFilterConditions(filterConditions)
                                                        .SetHeader(trdHeader ?? DefaultTradeHeader ?? 
                                                                   throw new InvalidOperationException(NoTradeHeaderMessage));

            var request = TrdGetHistoryOrderFillList.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<TrdCommon.OrderFill>>(Trade.GetHistoryOrderFillList(request));
        }

        /// <summary>
        /// 成交推送事件，异步处理 FutuOpenD 推送过来的成交状态信息。
        /// </summary>
        public event EventHandler<TrdCommon.OrderFill> UpdateOrderFill;

        #endregion // 成交

        #endregion // Public

        #region FTSPI_Trd

        #region 账户

        void FTSPI_Trd.OnReply_GetAccList(FTAPI_Conn client, uint nSerialNo, TrdGetAccList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.AccListList);

        void FTSPI_Trd.OnReply_UnlockTrade(FTAPI_Conn client, uint nSerialNo, TrdUnlockTrade.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        #endregion // 账户

        #region 资产持仓

        void FTSPI_Trd.OnReply_GetFunds(FTAPI_Conn client, uint nSerialNo, TrdGetFunds.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.Funds);

        void FTSPI_Trd.OnReply_GetMaxTrdQtys(FTAPI_Conn client, uint nSerialNo, TrdGetMaxTrdQtys.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.MaxTrdQtys);

        void FTSPI_Trd.OnReply_GetPositionList(FTAPI_Conn client, uint nSerialNo, TrdGetPositionList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.PositionListList);

        void FTSPI_Trd.OnReply_GetMarginRatio(FTAPI_Conn client, uint nSerialNo, TrdGetMarginRatio.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.MarginRatioInfoListList);

        #endregion // 资产持仓

        #region 订单

        void FTSPI_Trd.OnReply_PlaceOrder(FTAPI_Conn client, uint nSerialNo, TrdPlaceOrder.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.OrderID);

        void FTSPI_Trd.OnReply_ModifyOrder(FTAPI_Conn client, uint nSerialNo, TrdModifyOrder.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.OrderID);

        void FTSPI_Trd.OnReply_GetOrderList(FTAPI_Conn client, uint nSerialNo, TrdGetOrderList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.OrderListList);

        void FTSPI_Trd.OnReply_GetHistoryOrderList(FTAPI_Conn client, uint nSerialNo, TrdGetHistoryOrderList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.OrderListList);

        void FTSPI_Trd.OnReply_SubAccPush(FTAPI_Conn client, uint nSerialNo, TrdSubAccPush.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Trd.OnReply_UpdateOrder(FTAPI_Conn client, uint nSerialNo, TrdUpdateOrder.Response rsp) =>
            UpdateOrder?.Invoke(this, rsp.S2C.Order);

        #endregion // 订单

        #region 成交

        void FTSPI_Trd.OnReply_GetOrderFillList(FTAPI_Conn client, uint nSerialNo, TrdGetOrderFillList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.OrderFillListList);

        void FTSPI_Trd.OnReply_GetHistoryOrderFillList(FTAPI_Conn client, uint nSerialNo, TrdGetHistoryOrderFillList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.OrderFillListList);

        void FTSPI_Trd.OnReply_UpdateOrderFill(FTAPI_Conn client, uint nSerialNo, TrdUpdateOrderFill.Response rsp) =>
            UpdateOrderFill?.Invoke(this, rsp.S2C.OrderFill);

        #endregion // 成交

        #endregion // FTSPI_Trd
    }
}
