using UnityEngine;
using System.Collections;

public class IAPUnityHelper : SingletonComponent<IAPUnityHelper> {

	private static IAPUnityHelper instance;

	protected override void Awake ()
	{
		Init();

		base.Awake ();
	}

	private void Init()
	{
#if UNITY_IOS && !UNITY_EDITOR
			InitIAPHelperIOS (gameObject.name, "");
#endif
	}

	public void StartBuyProduct(string productId)
	{
#if UNITY_IOS
        StartBuyProductIOS(productId);
#endif

	}

	void PreBuyProductFailed(string productInfo)
	{
		ClientMsg.Instance.Send(MsgCmd.GET_APPLE_PRODUCT_FAILED, this);
	}

	void BuyProductSuccess(string productInfo)
	{
		
	}

	void BuyProudctFailed(string transactionIdentifier)
	{
		NetWorkDispatcher.Instance.CancelProductOrder(transactionIdentifier);
	}

	void VerifyProductReceiptData(string receiptData)
	{
		Debug.Log("VerifyProductReceiptData: " + receiptData);

		NetWorkDispatcher.Instance.ReceiptDataVerify(receiptData);
	}

	public void CompletedSKPaymentTransaction(string identifier)
	{
#if UNITY_IOS
	    CompletedSKPaymentTransactionIOS(identifier);
#endif
	}

#if UNITY_IOS
    [System.Runtime.InteropServices.DllImport("__Internal")]
	private static extern void BuyProductIOS(string productId);

	[System.Runtime.InteropServices.DllImport("__Internal")]
	private static extern void InitIAPHelperIOS (string callBackObjectName, string gameServerAddress);

	[System.Runtime.InteropServices.DllImport("__Internal")]
	private static extern void StartBuyProductIOS(string productId);

	[System.Runtime.InteropServices.DllImport("__Internal")]
	private static extern void CompletedSKPaymentTransactionIOS(string identifier);
#endif
}
