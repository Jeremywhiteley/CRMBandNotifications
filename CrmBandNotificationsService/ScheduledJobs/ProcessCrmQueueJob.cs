using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus.Notifications;
using Microsoft.WindowsAzure.Mobile.Service;
using Microsoft.Xrm.Sdk;
using System;
using System.Threading.Tasks;

namespace CrmBandNotificationsService
{
    public class ProcessCrmQueueJob : ScheduledJob
    {
        //TODO: Populate based on your Azure instance
        private const string IssuerName = "Azure Service Bus - CRM Namespace - Default Issuer";
        private const string IssuerSecret = "Azure Service Bus - CRM Namespace - Default Key";
        private const string ServiceNamespace = "Azure Service Bus - CRM Namespace Name";
        private const string QueueName = "Azure Service Bus - CRM Namespace - Queue Name";

        private const string NotificationHubConnectionString = "Azure Service Bus - Notification Hub - DefaultFullSharedAccessSignature";
        private const string NotificationHubName = "Azure Service Bus - Notification Hub - Name";
        public override Task ExecuteAsync()
        {
            TokenProvider credentials = TokenProvider.CreateSharedSecretTokenProvider(IssuerName, IssuerSecret);

            MessagingFactory factory = MessagingFactory.Create(ServiceBusEnvironment.CreateServiceUri("sb", ServiceNamespace, string.Empty), credentials);
            QueueClient myQueueClient = factory.CreateQueueClient(QueueName);

            //Get the message from the queue
            BrokeredMessage message;
            while ((message = myQueueClient.Receive(new TimeSpan(0, 0, 5))) != null)
            {
                RemoteExecutionContext ex = message.GetBody<RemoteExecutionContext>();

                Entity pushNotification = (Entity)ex.InputParameters["Target"];

                //Make sure Recipient is populated
                var ownerId = pushNotification.GetAttributeValue<EntityReference>("lat_recipient").Id.ToString();
                var subject = pushNotification.GetAttributeValue<string>("lat_message");

                SendNotificationAsync(subject, ownerId);
                message.Complete();
            }

            return Task.FromResult(true);
        }

        private static async void SendNotificationAsync(string message, string userid)
        {
            NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString(
                NotificationHubConnectionString,
                NotificationHubName);

            //content-available = 1 makes sure ReceivedRemoteNotification in 
            //AppDelegate.cs get executed when the app is closed
            var alert = "{\"aps\":{\"alert\":\"" + message + "\", \"content-available\" : \"1\"}}";

            //Would need to handle Windows & Android separately
            await hub.SendAppleNativeNotificationAsync(alert, userid);
        }
    }
}