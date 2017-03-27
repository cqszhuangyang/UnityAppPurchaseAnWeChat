# Unity 内购

标签（空格分隔）： 未分类

---

苹果IAP接入

相比微信支付和支付宝支付要麻烦一些,麻烦的地方主要体现在对测试支付环境的要求以及苹果审核方面的要求上面。本文是自己在接入iOS的IAP模块的经验及总结，发出来分享下。建议需要接入的话还是浏览一遍苹果官方文档

注意事项

测试支付的ipa必须使用[App-Store]证书
越狱机器无法测试IAP
用SandBox账号测试支付的时候,必须把在系统[设置]里面把[Itunes Store 与 App Store]登录的非SandBox账号注销掉,否则向苹果服务器请求不到订单信息
Sandbox账号不要在正式支付环境登陆支付，登陆过的正式支付环境的SandBox账号会失效
所有在itunes上配置的商品都必须可购买,不能有某些商品根据商户自己的服务器的数据在某个时期出现免费的情况
商品列表不能按照某些特定条件进行排序(比如说下载量)
非消耗型商品必须的有恢复商品功能
非消耗类型的商品不要和商户自己的服务器关联
关于证书

证书名称	证书类型	适用场景
Ad-hoc	内测版本	需要把设备Identifier添加到证书才可安装
In-house	企业版本	任何iOS设备都可以安装
App-Store	App-Store版本	上传到App-Store审核过了，在App-Store中安装
说明: 如果需要接入IAP则需要用App-Store版本证书，在开发调试的时候我们使用App-Store Developer证书和SandBox账号即可调试。平时我们提交到fir.im的包可以用In-house证书，也可用Ad-hoc证书。关于证书的更多介绍可以参考这里。

商品配置

首先在 [iTunes Connect] 构建一个App版本，然后我们在[Xcode]里面或者[Application Loader]上传一个App到App-Store。上传好之后我们在[iTunes Connect]中的[我的App]中看到我们刚上传的App。(配置IAP商品之前必须先上传版本，苹果要求的)

在 [iTunes Connect] 打开我的App中的[功能]可以看到配置App内购项目的选项[App内购项目]。然后添加对应的商品就可以了。这里有个地方需要注意的是商品的类型，具体的商品类型说明可以看这里。切记要选对商品类型，在此说下如果是游戏中的虚拟金币之类的商品的话就选择 Consumable 类型即可。

在 [iTunes Connect] 添加Sandbox测试账号。

购买流程



订单收据验证

两种验证方式

客户端本地验证

发送receipt数据给游戏服务器，让游戏服务器向AppStore服务器验证(我现在用的这种，安全)

如果在购买的时候没有进行receipt验证退出app了，那么在下次购买的时候会storekit会首先查询receipt数据把未验证订单先验证了才会进行接下来的购买。

收据(receipt)

对于消费类型的商品(Consumable)收据会在验证之后自动删除掉,对于非消费类型的商品(Non-Consumable)每次购买的订单都保存在了收据里面，并且收据文件不会删除。如果用户手动把receipt文件删除了的话，那么在下次app启动的时候会自动生成一个和之前一样的receipt数据文件并且在下次购买的时候会自动验证。

丢单的考虑

整个支付过程从表面上看有可能丢单的地方是在客户端支付完成的时候准备向游戏服务器发送receipt数据进行验证的时候，如果这个时候客户端退出游戏了(比如用户按Home键退出游戏杀掉进程)，那么此时用户已经付费了但是收不到商品。经过验证在这个过程中如果我们在购买完成的时候没有调用storekit的完成订单API的话也会导致每次app重新启动的时候storekit会回调订单完成的接口直到你调用finishTransaction才会停止。注意storekit回调成功的前提是你的购买监听API已经实例化，在这里提供的对应的是IAPHelper类，必须在app启动的时候就实例化。所以我们的做法是先向游戏服务器进行订单验证，验证成功之后在调用storekit的finishTrasaction，那么整个过程基本上就不会有丢单的情况了。

支付过程

//IAPHelper.mm

- (void)getProductInfoById:(NSString*)productID
{
    NSLog(@"--productId: %@", productID);
    
    NSArray *product = nil;
    product = [[NSArray alloc] initWithObjects:productID, nil];
    
    NSSet *set = [NSSet setWithArray:product];
    SKProductsRequest *request = [[SKProductsRequest alloc] initWithProductIdentifiers:set];
    
    //设置并启动监听
    request.delegate = self;
    [request start];
}

//这个函数是getProductInfoById查询订单的回调函数
//一般查询不到商品信息有三种情况: 
//1.没有在iTunes中没有配置对应商品 
//2.没有使用Appstore Developer证书
//3.使用了越狱的机器

- (void)productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response
{
    NSArray *myProduct = response.products;
    if (myProduct.count == 0)
    {
        UnitySendMessage(self.mCallBackObjectName.UTF8String, "PreBuyProductFailed", "ProductNotExist");
        
        return;
    }
    
    //如果iOS已经登录了[iTunes Store与App Store]账号，则这一步会失败
    SKPayment * payment = [SKPayment paymentWithProduct:myProduct[0]];
    [[SKPaymentQueue defaultQueue] addPayment:payment];
}

...

- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions
{
    for (SKPaymentTransaction *transaction in transactions) {
        switch (transaction.transactionState) {
                // Call the appropriate custom method for the transaction state.
            case SKPaymentTransactionStatePurchasing:
                [self showTransactionAsInProgress:transaction deferred:NO];
                break;
            case SKPaymentTransactionStateDeferred:
                [self showTransactionAsInProgress:transaction deferred:YES];
                break;
            case SKPaymentTransactionStateFailed:
                [self failedTransaction:transaction];
                break;
            case SKPaymentTransactionStatePurchased:
                 //这个函数是支付成功之后App Store服务器会回调的函数,里面包含了Receipt数据
                 //我这边的处理是先把这个数据发送给Mono层，通过Mono层的网路接口把数据发给自己的商户服务器
                 //商户服务器会把Receipt数据发往App Store服务器进行验证，再把验证结果返回给客户端，如果验证成功
                 //这次支付才算完成
                [self verifyPurchaseWithPaymentTransaction:transaction];
                break;
            case SKPaymentTransactionStateRestored:
                [self restoreTransaction:transaction];
                break;
            default:
                // For debugging
                NSLog(@"Unexpected transaction state %@", @(transaction.transactionState));
                break;
        }
    }
}


//这个函数是服务器验证成功之后必须调用的函数，
//作用就是将购买完成的订单从peymentQueue中移除，否则这个订单会在你的机器上一直
//保留，算作一个未完成的订单。就算你的App删除重装也没有用。
//transactionIdentifier是每个订单的唯一表示ID
- (void) completeTransactionByIdentifier:(NSString*)transactionIdentifier
{
     NSArray<SKPaymentTransaction *> * transactions = [[SKPaymentQueue defaultQueue] transactions];
    
    for (SKPaymentTransaction *transaction in transactions)
    {
        NSLog(@"completeTransactionByIdentifier %@ ---  %@", transaction.transactionIdentifier, transactionIdentifier);
        
        BOOL result = [transaction.transactionIdentifier compare:transactionIdentifier];
        
        if (NULL != transaction && !result)
        {
            [self completeTransaction:transaction];
            return;
        }
    }
}

- (void) completeTransaction:(SKPaymentTransaction*)transaction
{
    NSLog(@"completeTransaction: %@", transaction.transactionIdentifier);
    //NSLog(@"completeTransaction: %@", transaction.transactionIdentifier);

    // Remove the transaction from the payment queue.
    [[SKPaymentQueue defaultQueue] finishTransaction: transaction];
    
    // Your application should implement these two methods.
    NSString * productIdentifier = transaction.payment.productIdentifier;
    
    if([productIdentifier length] > 0)
    {
        NSLog(@"productIdentifier : %@", productIdentifier);
    }
    
    
    UnitySendMessage(self.mCallBackObjectName.UTF8String, "BuyProductSuccess", productIdentifier.UTF8String);
}
微信支付

微信支付的流程及操作步骤官方文档已经描述的非常清楚了，这里就不介绍了。这里主要讲下遇到的问题及在Unity中接入的问题

遇到的问题

支付完成之后不回调onResp函数

由于没有在AndroidManifest中注册回调监听
   <receiver
   android:name=".AppRegister">
   <intent-filter>
   <action android:name="com.tencent.mm.plugin.openapi.Intent.ACTION_REFRESH_WXAPP" />
   </intent-filter>
   </receiver>
Unity生成APK的时候Bundle Identifier填写的和微信开放平台注册的Bundle Identifier不一致

应用包签名错误,确保在Unity生成APK的时候选择一个固定的Keystore文件，这个文件的签名要和微信开放平台注册的应用签名保持一致

支付第一次成功之后，后面就再也支付一直返回错误码-1

解决：网上有答案说把微信的缓存清除掉就可以再支付，但是每支付一次就清楚一次。这其实没有解决根本问题。其实返回错误码-1官方明确说了一种可能是APPID或者签名错了。我这边还遇到了另外一种情况就是服务器传递给客户端的sign值错了，这个得服务器确认好。

在游戏中启动不了微信支付回调界面

解决: 把WXPayEntryActivity这个Activity添加到Unity的Plugins下面的AndroidManifest文件中




