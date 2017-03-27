﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using com.game.flatgen;
using FlatBuffers;
using System;


public class IAPUnityHelper : MonoBehaviour
{

    private static IAPUnityHelper instance = null;
   

    private void Awake()
    {
       #if UNITY_IOS
        instance = this;
        InitIAPHelperIOS(gameObject.name, "");
       #endif
    }



    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public static IAPUnityHelper getInstance()
    {
        return instance;
    }


    /// <summary>
    /// 开始购买商品
    /// </summary>
    /// <param name="productId"></param>
    public void StartBuyProduct(string productId)
    {
        StartBuyProductIOS(productId);
    }


    /// <summary>
    /// receiptData 验证成功之后 ----接收服务器端数据 完成订单处理
    /// </summary>
    /// <param name="identifier">transactionIdentifier是每个订单的唯一表示ID</param>
    public void CompletedSKPaymentTransaction(string identifier)
    {
        CompletedSKPaymentTransactionIOS(identifier);
    }

    /// <summary>
    ///  购买失败
    /// </summary>
    /// <param name="productInfo"></param>
    void PreBuyProductFailed(string productInfo)
    {
        Debug.Log("请求商品列表失败:" + productInfo + " ");
    }


    /// <summary>
    /// 购买成功  进行服务器回调---
    /// </summary>
    /// <param name="productInfo"></param>
    void BuyProductSuccess(string productInfo)
    {

    }


    /// <summary>
    /// 支付失败
    /// </summary>
    /// <param name="transactionIdentifier"></param>
    void BuyProudctFailed(string transactionIdentifier)
    {
        Debug.Log("支付失败 CancelProductOrder ：" + transactionIdentifier);
        //NetWorkDispatcher.Instance.CancelProductOrder(transactionIdentifier);
    }



    /// <summary>
    /// 验证购买数据---发送给服务器
    ///沙盒测试环境验证
    /// https://sandbox.itunes.apple.com/verifyReceipts
    ///正式环境验证
    /// https://buy.itunes.apple.com/verifyReceipt
    /// </summary>
    /// <param name="receiptData">验证数据---发送给游戏服务器，服务器请求验证appstore服务器</param>
    void VerifyProductReceiptData(string receiptData)
    {
        Debug.Log("VerifyProductReceiptData: " + receiptData);
        WWWForm form = new WWWForm();
        form.AddField("receiptData", receiptData);
        HttpRequest.SendPost(GameConfig.AppPurchaseUrl + "/iap", form,(response)=> {

            var transactionIdentifier = response["transactionIdentifier"].Value;
            var IapStatus = (IapStatus)response["IapStatus"].AsInt;
            Debug.Log("transactionIdentifier:" + transactionIdentifier + " IapStatus:" + IapStatus);


        },(error)=> {

            if (String.IsNullOrEmpty(error))
            {
                Debug.Log(error);
            }
        });
        //  NetWorkDispatcher.Instance.ReceiptDataVerify(receiptData);
        //FlatBuffers.FlatBufferBuilder bufferBuilder = new FlatBufferBuilder(256);
        //bufferBuilder.Finish(VerifyProductReceiptDataRequest.CreateVerifyProductReceiptDataRequest(
        //        bufferBuilder, 0, bufferBuilder.CreateString(receiptData))
        //    .Value);

        //AsyncNetClient.getInstance.SendMessage(Kit.BuildBytes(VerifyProductReceiptDataRequest.MsgID,bufferBuilder));
    }


    void CancelRestoreProducts()
    {
        Debug.Log("CancelRestoreProducts------>>>");
    }


    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void BuyProductIOS(string productId);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern bool canMakePay();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void InitIAPHelperIOS(string callBackObjectName, string gameServerAddress);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void StartBuyProductIOS(string productId);

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void CompletedSKPaymentTransactionIOS(string identifier);


}

public enum IapStatus {
    SUCCESS,
    FAIL

}
