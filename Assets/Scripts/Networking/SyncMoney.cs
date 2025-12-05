using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;

public class SyncMoney : NetworkBehaviour
{
    private int accumMoney = 0;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!base.IsOwner)
            this.enabled = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddMoney(int moneyAmount)
    {
        UpdateMoneyCounter(accumMoney, moneyAmount);
        accumMoney += moneyAmount;
    }

    [ObserversRpc]
    public void UpdateMoneyCounter(int prevTotalMoney, int increaseAmount)
    {
        StartCoroutine(InteractController.instance.EaseValueAdd(prevTotalMoney, increaseAmount, 0.2f));
    }
}
