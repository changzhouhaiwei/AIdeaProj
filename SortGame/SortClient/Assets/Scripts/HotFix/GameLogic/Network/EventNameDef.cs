public enum EventNameDef
{
    BaseNumber = 1000000,

    // 通信
    ConnectSuccess,
    ReConnectSuccess,
    ConnectFail,

    LoginServer,

    UIReceiveMessage,
    UISvrLogicError,

    // UI
    UIStaminaRecover,

    UIMailListRefresh,
    UIGrowthFundRefresh,
    UIDiscountsRefresh,
    UICommonGiftRefresh,
    UINumberOfPurchasesRefresh,

    MakeFiveClockRefreshUI,

    UIPlayerDataRefresh,
    UIFristTopUpRefresh,

    // Fight
    PassFightFleeMonster,
    PassFightRemainHp,
    MathchSpecificPiech,

    //FishTilePlay
    SubmitOver,
    EncouragePlayEffectsHide,

    //商品信息获取 
    ReqQueryShopProductSuc,

    //购买免广告成功
    BuyNoAdPackSuccess,

    //购买商店商品成功不带广告
    BuyProductSuccess,
    //胜利界面的额外奖励
    UICompleteExtraAward,
    //底部技能按钮看广告获得道具
    PUUIAdGetItem,
    //商店不够卖的免费广告飞金币+50
    ShopFreeCoinPanelAnimPlay,
    //看广告抽奖
    ADCircleDailyGetAward,

    //SDK敏感词检测
    SensitiveCheckSuc,
    SensitiveCheckException,
    SensitiveCheckError,

    //用户头像变化
    UserHeadChange,
    //用户头像框变化
    UserFrameChange,
    //火花连胜记录更新（关卡胜利等）
    DailyVictoryWinRecorded,

    //进场动画结束
    LoadLevelAnimFinish,
    LoadSaveLevelAnimFinish,
    //黄金树收集到的通知.
    GoldTreeCollected,
    GoldTreeRewardedFinish,//奖励领取完成.
    GoldTreeActOpened,//活动开启.

    //增加第七格子
    ADAddSlot7Suc,

    AwardColorEggCoverPickAll,

}