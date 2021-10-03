using Futu.OpenApi;
using Futu.OpenApi.Pb;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FutuApiAsync {
    public class FutuQuoteClient : FutuClientBase, FTSPI_Qot {
        /// <summary>
        /// 获取底层 <see cref="FTAPI_Qot"/> 对象。
        /// </summary>
        public FTAPI_Qot Quote => (FTAPI_Qot)_connectionObject;

        public FutuQuoteClient() : this(string.Empty) { }

        public FutuQuoteClient(string rsaKey) : base(rsaKey) {
            Quote.SetQotCallback(this);
        }

        protected override FTAPI_Conn CreateConnectionObject() => new FTAPI_Qot();

        #region Public

        #region 实时行情

        /// <summary>
        /// 订阅注册需要的实时信息。
        /// </summary>
        /// <param name="securities">股票集合。</param>
        /// <param name="subTypes">订阅类型集合。</param>
        /// <param name="isSubOrUnSub">true 表示订阅，false 表示反订阅。</param>
        /// <param name="isSubOrderBookDetail">是否订阅摆盘明细。</param>
        /// <param name="extendedTime">是否允许美股盘前盘后数据。</param>
        public Task Sub(IEnumerable<QotCommon.Security> securities, IEnumerable<QotCommon.SubType> subTypes, bool isSubOrUnSub = true,
                        bool? isRegOrUnRegPush = null, IEnumerable<QotCommon.RehabType> rehabTypes = null, bool? isFirstPush = null,
                        bool? isSubOrderBookDetail = null, bool? extendedTime = null) {
            var builder = QotSub.C2S.CreateBuilder().AddRangeSecurityList(securities)
                                                    .AddRangeSubTypeList(subTypes.Cast<int>())
                                                    .SetIsSubOrUnSub(isSubOrUnSub);
            if (isRegOrUnRegPush.HasValue)
                builder.SetIsRegOrUnRegPush(isRegOrUnRegPush.Value);
            if (rehabTypes != null)
                builder.AddRangeRegPushRehabTypeList(rehabTypes.Cast<int>());
            if (isFirstPush.HasValue)
                builder.SetIsFirstPush(isFirstPush.Value);
            if (isSubOrderBookDetail.HasValue)
                builder.SetIsSubOrderBookDetail(isSubOrderBookDetail.Value);
            if (extendedTime.HasValue)
                builder.SetExtendedTime(extendedTime.Value);

            var request = QotSub.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask(Quote.Sub(request));
        }

        /// <summary>
        /// 取消当前连接的所有订阅
        /// </summary>
        public Task UnSubAll() {
            var request = QotSub.Request.CreateBuilder()
                                        .SetC2S(QotSub.C2S.CreateBuilder()
                                                          .SetIsUnsubAll(true)
                                                          .SetIsSubOrUnSub(false))
                                        .Build();

            return CreateRequestTask(Quote.Sub(request));
        }

        /// <summary>
        /// 获取订阅信息。
        /// </summary>
        /// <param name="isReqAllConn">是否返回所有连接的订阅状态，false 只返回当前连接数据。</param>
        /// <returns></returns>
        public Task<QotGetSubInfo.S2C> GetSubInfo(bool isReqAllConn = false) {
            var request = QotGetSubInfo.Request.CreateBuilder()
                                               .SetC2S(QotGetSubInfo.C2S.CreateBuilder().SetIsReqAllConn(isReqAllConn))
                                               .Build();

            return CreateRequestTask<QotGetSubInfo.S2C>(Quote.GetSubInfo(request));
        }

        /// <summary>
        /// 获取市场快照。
        /// </summary>
        /// <param name="securities">股票集合。</param>
        /// <returns>快照列表。</returns>
        public Task<IList<QotGetSecuritySnapshot.Snapshot>> GetSecuritySnapshot(IEnumerable<QotCommon.Security> securities) {
            var request = QotGetSecuritySnapshot.Request.CreateBuilder()
                                                        .SetC2S(QotGetSecuritySnapshot.C2S.CreateBuilder().AddRangeSecurityList(securities))
                                                        .Build();

            return CreateRequestTask<IList<QotGetSecuritySnapshot.Snapshot>>(Quote.GetSecuritySnapshot(request));
        }

        /// <summary>
        /// 获取订阅股票报价的实时数据。
        /// </summary>
        /// <param name="securities">股票集合。</param>
        /// <returns>报价列表。</returns>
        public Task<IList<QotCommon.BasicQot>> GetBasicQot(IEnumerable<QotCommon.Security> securities) {
            var request = QotGetBasicQot.Request.CreateBuilder()
                                                .SetC2S(QotGetBasicQot.C2S.CreateBuilder().AddRangeSecurityList(securities))
                                                .Build();

            return CreateRequestTask<IList<QotCommon.BasicQot>>(Quote.GetBasicQot(request));
        }

        /// <summary>
        /// 获取已订阅股票的实时摆盘。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <param name="num">请求的摆盘个数。</param>
        /// <returns></returns>
        public Task<QotGetOrderBook.S2C> GetOrderBook(QotCommon.Security security, int num) {
            var request = QotGetOrderBook.Request.CreateBuilder()
                                                 .SetC2S(QotGetOrderBook.C2S.CreateBuilder().SetSecurity(security).SetNum(num))
                                                 .Build();

            return CreateRequestTask<QotGetOrderBook.S2C>(Quote.GetOrderBook(request));
        }

        /// <summary>
        /// 实时获取指定股票最近 num 个 K 线数据。
        /// </summary>
        /// <param name="rehabType">复权类型。</param>
        /// <param name="klType">K 线时间周期。</param>
        /// <param name="security">股票。</param>
        /// <param name="reqNum">请求 K 线根数。</param>
        /// <returns></returns>
        public Task<QotGetKL.S2C> GetKL(QotCommon.RehabType rehabType, QotCommon.KLType klType, QotCommon.Security security, int reqNum) {
            var request = QotGetKL.Request.CreateBuilder().SetC2S(QotGetKL.C2S.CreateBuilder()
                                                                              .SetRehabType((int)rehabType)
                                                                              .SetKlType((int)klType)
                                                                              .SetSecurity(security)
                                                                              .SetReqNum(reqNum))
                                                          .Build();

            return CreateRequestTask<QotGetKL.S2C>(Quote.GetKL(request));
        }

        /// <summary>
        /// 获取指定股票的分时数据
        /// </summary>
        /// <param name="security">股票。</param>
        /// <returns></returns>
        public Task<QotGetRT.S2C> GetRT(QotCommon.Security security) {
            var request = QotGetRT.Request.CreateBuilder()
                                          .SetC2S(QotGetRT.C2S.CreateBuilder().SetSecurity(security))
                                          .Build();

            return CreateRequestTask<QotGetRT.S2C>(Quote.GetRT(request));
        }

        /// <summary>
        /// 获取指定股票的实时逐笔。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <param name="maxRetNum">最多返回的逐笔个数，实际返回数量不一定会返回这么多，最多返回1000个。</param>
        /// <returns></returns>
        public Task<QotGetTicker.S2C> GetTicker(QotCommon.Security security, int maxRetNum) {
            var request = QotGetTicker.Request.CreateBuilder()
                                              .SetC2S(QotGetTicker.C2S.CreateBuilder().SetSecurity(security).SetMaxRetNum(maxRetNum))
                                              .Build();

            return CreateRequestTask<QotGetTicker.S2C>(Quote.GetTicker(request));
        }

        /// <summary>
        /// 获取股票的经纪队列。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <returns></returns>
        public Task<QotGetBroker.S2C> GetBroker(QotCommon.Security security) {
            var request = QotGetBroker.Request.CreateBuilder()
                                              .SetC2S(QotGetBroker.C2S.CreateBuilder().SetSecurity(security))
                                              .Build();

            return CreateRequestTask<QotGetBroker.S2C>(Quote.GetBroker(request));
        }

        /// <summary>
        /// 实时报价事件，异步处理已订阅股票的实时报价推送。
        /// </summary>
        public event EventHandler<IList<QotCommon.BasicQot>> UpdateBasicQot;

        /// <summary>
        /// 实时摆盘事件，异步处理已订阅股票的实时摆盘推送。
        /// </summary>
        public event EventHandler<QotUpdateOrderBook.S2C> UpdateOrderBook;

        /// <summary>
        /// 实时 K 线事件，异步处理已订阅股票的实时 K 线推送。
        /// </summary>
        public event EventHandler<QotUpdateKL.S2C> UpdateKL;

        /// <summary>
        /// 实时逐笔事件，异步处理已订阅股票的实时逐笔推送。
        /// </summary>
        public event EventHandler<QotUpdateTicker.S2C> UpdateTicker;

        /// <summary>
        /// 实时分时事件，异步处理已订阅股票的实时分时推送。
        /// </summary>
        public event EventHandler<QotUpdateRT.S2C> UpdateRT;

        /// <summary>
        /// 实时经纪队列事件，异步处理已订阅股票的实时经纪队列推送。
        /// </summary>
        public event EventHandler<QotUpdateBroker.S2C> UpdateBroker;

        #endregion // 实时行情

        #region 基本数据	

        /// <summary>
        /// 获取股票对应市场的市场状态。
        /// </summary>
        /// <param name="securities">股票集合。</param>
        /// <returns>市场信息列表。</returns>
        public Task<IList<QotGetMarketState.MarketInfo>> GetMarketState(IEnumerable<QotCommon.Security> securities) {
            var request = QotGetMarketState.Request.CreateBuilder()
                                                   .SetC2S(QotGetMarketState.C2S.CreateBuilder().AddRangeSecurityList(securities))
                                                   .Build();

            return CreateRequestTask<IList<QotGetMarketState.MarketInfo>>(Quote.GetMarketState(request));
        }

        /// <summary>
        /// 获取个股资金流向。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <returns></returns>
        public Task<QotGetCapitalFlow.S2C> GetCapitalFlow(QotCommon.Security security) {
            var request = QotGetCapitalFlow.Request.CreateBuilder()
                                                   .SetC2S(QotGetCapitalFlow.C2S.CreateBuilder().SetSecurity(security))
                                                   .Build();

            return CreateRequestTask<QotGetCapitalFlow.S2C>(Quote.GetCapitalFlow(request));
        }

        /// <summary>
        /// 获取个股资金分布。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <returns></returns>
        public Task<QotGetCapitalDistribution.S2C> GetCapitalDistribution(QotCommon.Security security) {
            var request = QotGetCapitalDistribution.Request.CreateBuilder()
                                                           .SetC2S(QotGetCapitalDistribution.C2S.CreateBuilder().SetSecurity(security))
                                                           .Build();

            return CreateRequestTask<QotGetCapitalDistribution.S2C>(Quote.GetCapitalDistribution(request));
        }

        /// <summary>
        /// 获取单支或多支股票的所属板块信息列表。
        /// </summary>
        /// <param name="securities">股票集合。</param>
        /// <returns></returns>
        public Task<QotGetOwnerPlate.S2C> GetOwnerPlate(IEnumerable<QotCommon.Security> securities) {
            var request = QotGetOwnerPlate.Request.CreateBuilder()
                                                  .SetC2S(QotGetOwnerPlate.C2S.CreateBuilder().AddRangeSecurityList(securities))
                                                  .Build();

            return CreateRequestTask<QotGetOwnerPlate.S2C>(Quote.GetOwnerPlate(request));
        }

        /// <summary>
        /// 获取大股东持股变动列表，只提供美股数据，并最多只返回前 100 个。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <param name="category">持有者类别。</param>
        /// <param name="begin">开始时间。</param>
        /// <param name="end">结束时间。</param>
        /// <returns></returns>
        public Task<QotGetHoldingChangeList.S2C> GetHoldingChangeList(QotCommon.Security security, QotCommon.HolderCategory category,
                                                                      DateTime? begin, DateTime? end) {
            var builder = QotGetHoldingChangeList.C2S.CreateBuilder()
                                                     .SetSecurity(security)
                                                     .SetHolderCategory((int)category);
            if (begin.HasValue)
                builder.SetBeginTime(begin.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            if (end.HasValue)
                builder.SetEndTime(end.Value.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var request = QotGetHoldingChangeList.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<QotGetHoldingChangeList.S2C>(Quote.GetHoldingChangeList(request));
        }

        /// <summary>
        /// 获取 K 线，不需要事先下载 K 线数据。
        /// </summary>
        /// <param name="rehabType">复权类型。</param>
        /// <param name="klType">K 线时间周期。</param>
        /// <param name="security">股票。</param>
        /// <param name="beginDate">开始日期。</param>
        /// <param name="endDate">结束日期。</param>
        /// <param name="maxAckKLNum">最多返回多少根 K 线，如果未指定表示不限制。</param>
        /// <param name="needKLFieldsFlag">指定返回 K 线结构体特定某几项数据，<see cref="QotCommon.KLFields"/> 枚举值或组合，如果未指定返回全部字段。</param>
        /// <param name="nextReqKey">分页请求 key。</param>
        /// <param name="extendedTime">是否获取美股盘前盘后数据，当前仅支持1分 k。</param>
        /// <returns></returns>
        public Task<QotRequestHistoryKL.S2C> RequestHistoryKL(QotCommon.RehabType rehabType, QotCommon.KLType klType,
                                                              QotCommon.Security security, DateTime beginDate, DateTime endDate,
                                                              int? maxAckKLNum = null, QotCommon.KLFields? needKLFieldsFlag = null,
                                                              ByteString nextReqKey = null, bool? extendedTime = null) {
            var builder = QotRequestHistoryKL.C2S.CreateBuilder()
                                                 .SetRehabType((int)rehabType)
                                                 .SetKlType((int)klType)
                                                 .SetSecurity(security)
                                                 .SetBeginTime(beginDate.ToString("yyyy-MM-dd"))
                                                 .SetEndTime(endDate.ToString("yyyy-MM-dd"));
            if (maxAckKLNum.HasValue)
                builder.SetMaxAckKLNum(maxAckKLNum.Value);
            if (needKLFieldsFlag.HasValue)
                builder.SetNeedKLFieldsFlag((long)needKLFieldsFlag.Value);
            if (nextReqKey != null)
                builder.SetNextReqKey(nextReqKey);
            if (extendedTime.HasValue)
                builder.SetExtendedTime(extendedTime.Value);

            var request = QotRequestHistoryKL.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<QotRequestHistoryKL.S2C>(Quote.RequestHistoryKL(request));
        }

        /// <summary>
        /// 获取给定股票的复权因子。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <returns></returns>
        public Task<IList<QotCommon.Rehab>> RequestRehab(QotCommon.Security security) {
            var request = QotRequestRehab.Request.CreateBuilder()
                                                 .SetC2S(QotRequestRehab.C2S.CreateBuilder().SetSecurity(security))
                                                 .Build();

            return CreateRequestTask<IList<QotCommon.Rehab>>(Quote.RequestRehab(request));
        }

        #endregion // 基本数据

        #region 相关衍生品

        /// <summary>
        /// 通过标的股票，查询期权链的所有到期日。
        /// </summary>
        /// <param name="owner">期权标的股，目前仅支持传入港美正股以及恒指国指。</param>
        /// <param name="indexOptionType">指数期权的类型，仅用于恒指国指。</param>
        /// <returns></returns>
        public Task<IList<QotGetOptionExpirationDate.OptionExpirationDate>> GetOptionExpirationDate(
            QotCommon.Security owner, QotCommon.IndexOptionType? indexOptionType = null) {
            var builder = QotGetOptionExpirationDate.C2S.CreateBuilder().SetOwner(owner);
            if (indexOptionType.HasValue)
                builder.SetIndexOptionType((int)indexOptionType.Value);

            var request = QotGetOptionExpirationDate.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<QotGetOptionExpirationDate.OptionExpirationDate>>(Quote.GetOptionExpirationDate(request));
        }

        /// <summary>
        /// 通过标的股查询期权。
        /// </summary>
        /// <param name="owner">期权标的股，目前仅支持传入港美正股以及恒指国指。</param>
        /// <param name="beginDate">期权到期日开始日期。</param>
        /// <param name="endDate">期权到期日结束日期，时间跨度最多一个月。</param>
        /// <param name="indexOptionType">指数期权的类型，仅用于恒指国指。</param>
        /// <param name="optionType">期权类型，可选字段，不指定则表示都返回。</param>
        /// <param name="optionCondType">价内价外，可选字段，不指定则表示都返回。</param>
        /// <param name="dataFilter">数据字段筛选。</param>
        /// <returns></returns>
        public Task<IList<QotGetOptionChain.OptionChain>> GetOptionChain(
            QotCommon.Security owner, DateTime beginDate, DateTime endDate,
            QotCommon.IndexOptionType? indexOptionType = null, QotCommon.OptionType? optionType = null,
            QotGetOptionChain.OptionCondType? optionCondType = null, QotGetOptionChain.DataFilter dataFilter = null) {
            var builder = QotGetOptionChain.C2S.CreateBuilder()
                                               .SetOwner(owner)
                                               .SetBeginTime(beginDate.ToString("yyyy-MM-dd"))
                                               .SetEndTime(endDate.ToString("yyyy-MM-dd"));
            if (indexOptionType.HasValue)
                builder.SetIndexOptionType((int)indexOptionType.Value);
            if (optionType.HasValue)
                builder.SetType((int)optionType.Value);
            if (optionCondType.HasValue)
                builder.SetCondition((int)optionCondType.Value);
            if (dataFilter != null)
                builder.SetDataFilter(dataFilter);

            var request = QotGetOptionChain.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<QotGetOptionChain.OptionChain>>(Quote.GetOptionChain(request));
        }

        /// <summary>
        /// 拉取窝轮和相关衍生品数据接口。
        /// </summary>
        /// <param name="c2s"></param>
        /// <returns></returns>
        public Task<QotGetWarrant.S2C> GetWarrant(QotGetWarrant.C2S c2s) {
            var request = QotGetWarrant.Request.CreateBuilder().SetC2S(c2s).Build();

            return CreateRequestTask<QotGetWarrant.S2C>(Quote.GetWarrant(request));
        }

        /// <summary>
        /// 获取证券的关联数据。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <param name="referenceType">相关类型。</param>
        /// <returns></returns>
        public Task<IList<QotCommon.SecurityStaticInfo>> GetReference(QotCommon.Security security, QotGetReference.ReferenceType referenceType) {
            var request = QotGetReference.Request.CreateBuilder()
                                                 .SetC2S(QotGetReference.C2S.CreateBuilder()
                                                                            .SetSecurity(security)
                                                                            .SetReferenceType((int)referenceType))
                                                 .Build();

            return CreateRequestTask<IList<QotCommon.SecurityStaticInfo>>(Quote.GetReference(request));
        }

        /// <summary>
        /// 获取期货合约资料。
        /// </summary>
        /// <param name="securities">股票集合。</param>
        /// <returns></returns>
        public Task<IList<QotGetFutureInfo.FutureInfo>> GetFutureInfo(IEnumerable<QotCommon.Security> securities) {
            var request = QotGetFutureInfo.Request.CreateBuilder()
                                                  .SetC2S(QotGetFutureInfo.C2S.CreateBuilder().AddRangeSecurityList(securities))
                                                  .Build();

            return CreateRequestTask<IList<QotGetFutureInfo.FutureInfo>>(Quote.GetFutureInfo(request));
        }

        #endregion // 相关衍生品

        #region 全市场筛选

        /// <summary>
        /// 获取条件选股。
        /// </summary>
        /// <param name="begin">数据起始点。</param>
        /// <param name="num">请求数据个数，最大200。</param>
        /// <param name="market">股票市场，支持沪股和深股，且沪股和深股不做区分都代表 A 股市场。</param>
        /// <param name="plate">板块。</param>
        /// <param name="baseFilters">简单指标过滤器。</param>
        /// <param name="accumulateFilters">累积指标过滤器 累积属性的同一筛选条件数量上限 10 个。</param>
        /// <param name="financialFilters">财务指标过滤器。</param>
        /// <param name="patternFilters">形态技术指标过滤器。</param>
        /// <param name="customIndicatorFilters">自定义技术指标过滤器。</param>
        /// <returns></returns>
        public Task<QotStockFilter.S2C> StockFilter(int begin, int num, QotCommon.QotMarket market,
                                                    QotCommon.Security plate = null,
                                                    IEnumerable<QotStockFilter.BaseFilter> baseFilters = null,
                                                    IEnumerable<QotStockFilter.AccumulateFilter> accumulateFilters = null,
                                                    IEnumerable<QotStockFilter.FinancialFilter> financialFilters = null,
                                                    IEnumerable<QotStockFilter.PatternFilter> patternFilters = null,
                                                    IEnumerable<QotStockFilter.CustomIndicatorFilter> customIndicatorFilters = null) {
            var builder = QotStockFilter.C2S.CreateBuilder().SetBegin(begin).SetNum(num).SetMarket((int)market);
            if (plate != null)
                builder.SetPlate(plate);
            if (baseFilters != null)
                builder.AddRangeBaseFilterList(baseFilters);
            if (accumulateFilters != null)
                builder.AddRangeAccumulateFilterList(accumulateFilters);
            if (financialFilters != null)
                builder.AddRangeFinancialFilterList(financialFilters);
            if (patternFilters != null)
                builder.AddRangePatternFilterList(patternFilters);
            if (customIndicatorFilters != null)
                builder.AddRangeCustomIndicatorFilterList(customIndicatorFilters);

            var request = QotStockFilter.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<QotStockFilter.S2C>(Quote.StockFilter(request));
        }

        /// <summary>
        /// 获取特定板块下的股票列表，获取股指的成分股。
        /// </summary>
        /// <param name="plate">板块。</param>
        /// <param name="sortField">根据哪个字段排序，不填默认 Code 排序。</param>
        /// <param name="ascend">升序 true，降序 false，不填默认升序。</param>
        /// <returns></returns>
        public Task<IList<QotCommon.SecurityStaticInfo>> GetPlateSecurity(QotCommon.Security plate,
                                                                          QotCommon.SortField? sortField = null, bool? ascend = null) {
            var builder = QotGetPlateSecurity.C2S.CreateBuilder().SetPlate(plate);
            if (sortField.HasValue)
                builder.SetSortField((int)sortField.Value);
            if (ascend.HasValue)
                builder.SetAscend(ascend.Value);

            var request = QotGetPlateSecurity.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<QotCommon.SecurityStaticInfo>>(Quote.GetPlateSecurity(request));
        }

        /// <summary>
        /// 获取板块集合下的子板块列表。
        /// </summary>
        /// <param name="market"></param>
        /// <param name="plateSetType"></param>
        /// <returns></returns>
        public Task<IList<QotCommon.PlateInfo>> GetPlateSet(QotCommon.QotMarket market, QotCommon.PlateSetType plateSetType) {
            var request = QotGetPlateSet.Request.CreateBuilder()
                                                .SetC2S(QotGetPlateSet.C2S.CreateBuilder()
                                                                          .SetMarket((int)market)
                                                                          .SetPlateSetType((int)plateSetType))
                                                .Build();

            return CreateRequestTask<IList<QotCommon.PlateInfo>>(Quote.GetPlateSet(request));
        }

        /// <summary>
        /// 获取指定市场中特定类型或特定股票的基本信息。当 <paramref name="market"/> 和 <paramref name="securities"/> 
        /// 同时存在时，会忽略 <paramref name="market"/>，仅对 <paramref name="securities"/> 进行查询。
        /// </summary>
        /// <param name="market">股票市场。</param>
        /// <param name="securityType">股票类型。</param>
        /// <param name="securities">股票集合，若该字段存在，忽略其他字段，只返回该字段股票的静态信息。</param>
        /// <returns></returns>
        public Task<IList<QotCommon.SecurityStaticInfo>> GetStaticInfo(QotCommon.QotMarket? market = null, 
                                                                       QotCommon.SecurityType? securityType = null,
                                                                       IEnumerable<QotCommon.Security> securities = null) {
            var builder = QotGetStaticInfo.C2S.CreateBuilder();
            if (market.HasValue)
                builder.SetMarket((int)market);
            if (securityType.HasValue)
                builder.SetSecType((int)securityType.Value);
            if (securities != null)
                builder.AddRangeSecurityList(securities);

            var request = QotGetStaticInfo.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<QotCommon.SecurityStaticInfo>>(Quote.GetStaticInfo(request));
        }

        /// <summary>
        /// 获取指定市场的 ipo 列表。
        /// </summary>
        /// <param name="market">股票市场，支持沪股和深股，且沪股和深股不做区分都代表 A 股市场。</param>
        /// <returns></returns>
        public Task<IList<QotGetIpoList.IpoData>> GetIpoList(QotCommon.QotMarket market) {
            var request = QotGetIpoList.Request.CreateBuilder().SetC2S(QotGetIpoList.C2S.CreateBuilder().SetMarket((int)market)).Build();

            return CreateRequestTask<IList<QotGetIpoList.IpoData>>(Quote.GetIpoList(request));
        }

        /// <summary>
        /// 获取全局市场状态。
        /// </summary>
        /// <returns></returns>
        public Task<GetGlobalState.S2C> GetGlobalState() {
            var request = Futu.OpenApi.Pb.GetGlobalState.Request.CreateBuilder()
                                                                .SetC2S(Futu.OpenApi.Pb.GetGlobalState.C2S.CreateBuilder().SetUserID(0))
                                                                .Build();

            return CreateRequestTask<GetGlobalState.S2C>(Quote.GetGlobalState(request));
        }

        /// <summary>
        /// 请求指定市场 / 指定标的的交易日历。当 <paramref name="market"/> 和 <paramref name="security"/> 
        /// 同时存在时，会忽略 <paramref name="market"/>，仅对 <paramref name="security"/> 进行查询。
        /// <para>注意：该交易日是通过自然日剔除周末和节假日得到，未剔除临时休市数据。</para>
        /// </summary>
        /// <param name="market">要查询的市场。</param>
        /// <param name="beginDate">开始日期。</param>
        /// <param name="endDate">结束日期。</param>
        /// <param name="security">指定标的。</param>
        /// <returns></returns>
        public Task<IList<QotRequestTradeDate.TradeDate>> RequestTradeDate(QotCommon.QotMarket market, DateTime beginDate, DateTime endDate,
                                                                           QotCommon.Security security = null) {
            var builder = QotRequestTradeDate.C2S.CreateBuilder()
                                                 .SetMarket((int)market)
                                                 .SetBeginTime(beginDate.ToString("yyyy-MM-dd"))
                                                 .SetEndTime(endDate.ToString("yyyy-MM-dd"));
            if (security != null)
                builder.SetSecurity(security);

            var request = QotRequestTradeDate.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<QotRequestTradeDate.TradeDate>>(Quote.RequestTradeDate(request));
        }

        #endregion // 全市场筛选

        #region 个性化

        /// <summary>
        /// 获取历史 K 线额度使用明细。
        /// </summary>
        /// <param name="getDetail">是否返回详细拉取过的历史纪录。</param>
        /// <returns></returns>
        public Task<QotRequestHistoryKLQuota.S2C> RequestHistoryKLQuota(bool? getDetail = null) {
            var builder = QotRequestHistoryKLQuota.C2S.CreateBuilder();
            if (getDetail.HasValue)
                builder.SetBGetDetail(getDetail.Value);

            var request = QotRequestHistoryKLQuota.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<QotRequestHistoryKLQuota.S2C>(Quote.RequestHistoryKLQuota(request));
        }

        /// <summary>
        /// 设置到价提醒。新增、删除、修改、启用、禁用指定股票的到价提醒。
        /// </summary>
        /// <param name="security">股票。</param>
        /// <param name="op">操作类型。</param>
        /// <param name="key">到价提醒的标识，GetPriceReminder 协议可获得，用于指定要操作的到价提醒项，对于新增的情况不需要填。</param>
        /// <param name="priceReminderType">提醒类型，删除、启用、禁用的情况下会忽略该字段。</param>
        /// <param name="priceReminderFreq">提醒频率类型，删除、启用、禁用的情况下会忽略该字段。</param>
        /// <param name="value">提醒值，删除、启用、禁用的情况下会忽略该字段（精确到小数点后 3 位，超出部分会被舍弃）。</param>
        /// <param name="note">用户设置到价提醒时的标注，仅支持 20 个以内的中文字符，删除、启用、禁用的情况下会忽略该字段。</param>
        /// <returns>设置成功的情况下返回对应的 key。</returns>
        public Task<long> SetPriceReminder(QotCommon.Security security, QotSetPriceReminder.SetPriceReminderOp op,
                                           long? key = null, QotCommon.PriceReminderType? priceReminderType = null,
                                           QotCommon.PriceReminderFreq? priceReminderFreq = null, double? value = null,
                                           string note = null) {
            var builder = QotSetPriceReminder.C2S.CreateBuilder()
                                                 .SetSecurity(security)
                                                 .SetOp((int)op);
            if (key.HasValue)
                builder.SetKey(key.Value);
            if (priceReminderType.HasValue)
                builder.SetType((int)priceReminderType.Value);
            if (priceReminderFreq.HasValue)
                builder.SetFreq((int)priceReminderFreq.Value);
            if (value.HasValue)
                builder.SetValue(value.Value);
            if (note != null)
                builder.SetNote(note);

            var request = QotSetPriceReminder.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<long>(Quote.SetPriceReminder(request));
        }

        /// <summary>
        /// 获取到价提醒列表。获取对指定股票 / 指定市场设置的到价提醒列表。
        /// <paramref name="security"/> 和 <paramref name="market"/> 二选一，都存在的情况下 <paramref name="security"/> 优先。
        /// </summary>
        /// <param name="security"></param>
        /// <param name="market"></param>
        /// <returns></returns>
        public Task<IList<QotGetPriceReminder.PriceReminder>> GetPriceReminder(QotCommon.Security security = null,
                                                                               QotCommon.QotMarket? market = null) {
            var builder = QotGetPriceReminder.C2S.CreateBuilder();
            if (security != null)
                builder.SetSecurity(security);
            if (market.HasValue)
                builder.SetMarket((int)market.Value);

            var request = QotGetPriceReminder.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask<IList<QotGetPriceReminder.PriceReminder>>(Quote.GetPriceReminder(request));
        }

        /// <summary>
        /// 获取自选股分组列表。
        /// </summary>
        /// <param name="groupType">自选股分组类型。</param>
        /// <returns></returns>
        public Task<IList<QotGetUserSecurityGroup.GroupData>> GetUserSecurityGroup(QotGetUserSecurityGroup.GroupType groupType) {
            var request = QotGetUserSecurityGroup.Request.CreateBuilder()
                                                         .SetC2S(QotGetUserSecurityGroup.C2S.CreateBuilder().SetGroupType((int)groupType))
                                                         .Build();

            return CreateRequestTask<IList<QotGetUserSecurityGroup.GroupData>>(Quote.GetUserSecurityGroup(request));
        }

        /// <summary>
        /// 获取指定分组的自选股列表。
        /// </summary>
        /// <param name="groupName">分组名，有同名的返回排序首个。</param>
        /// <returns></returns>
        public Task<IList<QotCommon.SecurityStaticInfo>> GetUserSecurity(string groupName) {
            var request = QotGetUserSecurity.Request.CreateBuilder()
                                                    .SetC2S(QotGetUserSecurity.C2S.CreateBuilder().SetGroupName(groupName))
                                                    .Build();

            return CreateRequestTask<IList<QotCommon.SecurityStaticInfo>>(Quote.GetUserSecurity(request));
        }

        /// <summary>
        /// 修改自选股列表。
        /// </summary>
        /// <param name="groupName">分组名，有同名的返回排序的首个。</param>
        /// <param name="op">操作类型。</param>
        /// <param name="securities">新增、删除或移出该分组下的股票。</param>
        /// <returns></returns>
        public Task ModifyUserSecurity(string groupName, QotModifyUserSecurity.ModifyUserSecurityOp op, 
                                       IEnumerable<QotCommon.Security> securities) {
            var builder = QotModifyUserSecurity.C2S.CreateBuilder()
                                                   .SetGroupName(groupName)
                                                   .SetOp((int)op)
                                                   .AddRangeSecurityList(securities);
            var request = QotModifyUserSecurity.Request.CreateBuilder().SetC2S(builder).Build();

            return CreateRequestTask(Quote.ModifyUserSecurity(request));
        }

        /// <summary>
        /// 到价提醒通知事件，异步处理已设置到价提醒的通知推送。
        /// </summary>
        public event EventHandler<QotUpdatePriceReminder.S2C> UpdatePriceReminder;

        #endregion // 个性化

        #region 基础功能

        /// <summary>
        /// 通知 FutuOpenD 一些重要消息，类似连接断开等。
        /// </summary>
        public event EventHandler<Notify.S2C> Notify;

        #endregion // 基础功能

        #endregion // Public


        #region FTSPI_Qot

        #region 实时行情

        void FTSPI_Qot.OnReply_Sub(FTAPI_Conn client, uint nSerialNo, QotSub.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, null);

        void FTSPI_Qot.OnReply_GetSubInfo(FTAPI_Conn client, uint nSerialNo, QotGetSubInfo.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetSecuritySnapshot(FTAPI_Conn client, uint nSerialNo, QotGetSecuritySnapshot.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.SnapshotListList);

        void FTSPI_Qot.OnReply_GetBasicQot(FTAPI_Conn client, uint nSerialNo, QotGetBasicQot.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.BasicQotListList);

        void FTSPI_Qot.OnReply_GetOrderBook(FTAPI_Conn client, uint nSerialNo, QotGetOrderBook.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetKL(FTAPI_Conn client, uint nSerialNo, QotGetKL.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetRT(FTAPI_Conn client, uint nSerialNo, QotGetRT.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetTicker(FTAPI_Conn client, uint nSerialNo, QotGetTicker.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetBroker(FTAPI_Conn client, uint nSerialNo, QotGetBroker.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_UpdateBasicQot(FTAPI_Conn client, uint nSerialNo, QotUpdateBasicQot.Response rsp) =>
            UpdateBasicQot?.Invoke(this, rsp.S2C.BasicQotListList);

        void FTSPI_Qot.OnReply_UpdateOrderBook(FTAPI_Conn client, uint nSerialNo, QotUpdateOrderBook.Response rsp) =>
            UpdateOrderBook?.Invoke(this, rsp.S2C);

        void FTSPI_Qot.OnReply_UpdateKL(FTAPI_Conn client, uint nSerialNo, QotUpdateKL.Response rsp) =>
            UpdateKL?.Invoke(this, rsp.S2C);

        void FTSPI_Qot.OnReply_UpdateTicker(FTAPI_Conn client, uint nSerialNo, QotUpdateTicker.Response rsp) =>
            UpdateTicker?.Invoke(this, rsp.S2C);

        void FTSPI_Qot.OnReply_UpdateRT(FTAPI_Conn client, uint nSerialNo, QotUpdateRT.Response rsp) =>
            UpdateRT?.Invoke(this, rsp.S2C);

        void FTSPI_Qot.OnReply_UpdateBroker(FTAPI_Conn client, uint nSerialNo, QotUpdateBroker.Response rsp) =>
            UpdateBroker?.Invoke(this, rsp.S2C);

        #endregion // 实时行情

        #region 基本数据

        void FTSPI_Qot.OnReply_GetMarketState(FTAPI_Conn client, uint nSerialNo, QotGetMarketState.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.MarketInfoListList);

        void FTSPI_Qot.OnReply_GetCapitalFlow(FTAPI_Conn client, uint nSerialNo, QotGetCapitalFlow.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetCapitalDistribution(FTAPI_Conn client, uint nSerialNo, QotGetCapitalDistribution.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetOwnerPlate(FTAPI_Conn client, uint nSerialNo, QotGetOwnerPlate.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetHoldingChangeList(FTAPI_Conn client, uint nSerialNo, QotGetHoldingChangeList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_RequestHistoryKL(FTAPI_Conn client, uint nSerialNo, QotRequestHistoryKL.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_RequestRehab(FTAPI_Conn client, uint nSerialNo, QotRequestRehab.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.RehabListList);

        #endregion // 基本数据

        #region 相关衍生品

        void FTSPI_Qot.OnReply_GetOptionExpirationDate(FTAPI_Conn client, uint nSerialNo, QotGetOptionExpirationDate.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.DateListList);

        void FTSPI_Qot.OnReply_GetOptionChain(FTAPI_Conn client, uint nSerialNo, QotGetOptionChain.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.OptionChainList);

        void FTSPI_Qot.OnReply_GetWarrant(FTAPI_Conn client, uint nSerialNo, QotGetWarrant.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetReference(FTAPI_Conn client, uint nSerialNo, QotGetReference.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.StaticInfoListList);

        void FTSPI_Qot.OnReply_GetFutureInfo(FTAPI_Conn client, uint nSerialNo, QotGetFutureInfo.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.FutureInfoListList);

        #endregion // 相关衍生品

        #region 全市场筛选

        void FTSPI_Qot.OnReply_StockFilter(FTAPI_Conn client, uint nSerialNo, QotStockFilter.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_GetPlateSecurity(FTAPI_Conn client, uint nSerialNo, QotGetPlateSecurity.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.StaticInfoListList);

        void FTSPI_Qot.OnReply_GetPlateSet(FTAPI_Conn client, uint nSerialNo, QotGetPlateSet.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.PlateInfoListList);

        void FTSPI_Qot.OnReply_GetStaticInfo(FTAPI_Conn client, uint nSerialNo, QotGetStaticInfo.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.StaticInfoListList);

        void FTSPI_Qot.OnReply_GetIpoList(FTAPI_Conn client, uint nSerialNo, QotGetIpoList.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.IpoListList);

        void FTSPI_Qot.OnReply_GetGlobalState(FTAPI_Conn client, uint nSerialNo, GetGlobalState.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_RequestTradeDate(FTAPI_Conn client, uint nSerialNo, QotRequestTradeDate.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.TradeDateListList);

        #endregion // 全市场筛选

        #region 个性化

        void FTSPI_Qot.OnReply_RequestHistoryKLQuota(FTAPI_Conn client, uint nSerialNo, QotRequestHistoryKLQuota.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_SetPriceReminder(FTAPI_Conn client, uint nSerialNo, QotSetPriceReminder.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.Key);

        void FTSPI_Qot.OnReply_GetPriceReminder(FTAPI_Conn client, uint nSerialNo, QotGetPriceReminder.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.PriceReminderListList);

        void FTSPI_Qot.OnReply_GetUserSecurityGroup(FTAPI_Conn client, uint nSerialNo, QotGetUserSecurityGroup.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.GroupListList);

        void FTSPI_Qot.OnReply_GetUserSecurity(FTAPI_Conn client, uint nSerialNo, QotGetUserSecurity.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C.StaticInfoListList);

        void FTSPI_Qot.OnReply_ModifyUserSecurity(FTAPI_Conn client, uint nSerialNo, QotModifyUserSecurity.Response rsp) =>
            HandleReply(nSerialNo, rsp.RetType, rsp.ErrCode, rsp.RetMsg, rsp.S2C);

        void FTSPI_Qot.OnReply_UpdatePriceReminder(FTAPI_Conn client, uint nSerialNo, QotUpdatePriceReminder.Response rsp) =>
            UpdatePriceReminder?.Invoke(this, rsp.S2C);

        #endregion // 个性化

        #region 基础功能

        void FTSPI_Qot.OnReply_Notify(FTAPI_Conn client, uint nSerialNo, Notify.Response rsp) => Notify?.Invoke(this, rsp.S2C);

        #endregion // 基础功能

        #region 过时的

        [Obsolete]
        void FTSPI_Qot.OnReply_RegQotPush(FTAPI_Conn client, uint nSerialNo, QotRegQotPush.Response rsp) =>
            throw new NotImplementedException();

        [Obsolete]
        void FTSPI_Qot.OnReply_GetCodeChange(FTAPI_Conn client, uint nSerialNo, QotGetCodeChange.Response rsp) =>
            throw new NotImplementedException();

        #endregion // 过时的

        #endregion // FTSPI_Qot
    }
}
