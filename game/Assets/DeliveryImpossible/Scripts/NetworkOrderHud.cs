using DeliveryImpossible.Orders;
using Mirror;
using TMPro;
using UnityEngine;

namespace DeliveryImpossible.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkOrderHud : NetworkBehaviour
    {
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private GameObject orderPanel;
        [SerializeField] private TMP_Text orderText;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            SetVisible(true);
        }

        private void Update()
        {
            if (!isLocalPlayer || orderText == null)
            {
                return;
            }

            NetworkDeliveryOrder order = NetworkDeliveryOrder.Active;
            orderText.text = order != null
                ? order.GetHudText()
                : "Ожидание заказа...";
        }

        public override void OnStopLocalPlayer()
        {
            SetVisible(false);
            base.OnStopLocalPlayer();
        }

        private void SetVisible(bool visible)
        {
            if (hudCanvas != null)
            {
                hudCanvas.enabled = visible;
            }

            if (orderPanel != null)
            {
                orderPanel.SetActive(visible);
            }
        }
    }
}
